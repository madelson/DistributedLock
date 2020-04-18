using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Specialized;
using Medallion.Threading.Internal;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Medallion.Threading.Azure
{
    public sealed partial class AzureBlobLeaseDistributedLock : IInternalDistributedLock<AzureBlobLeaseDistributedLockHandle>
    {
        /// <summary>
        /// Metadata marker used to indicate that a blob was created for distributed locking and therefore 
        /// should be destroyed upon release
        /// </summary>
        private static readonly string CreatedMetadataKey = $"__DistributedLock";

        private readonly BlobClientWrapper _blobClient;
        private readonly (TimeoutValue duration, TimeoutValue renewalCadence, TimeSpan minBusyWaitSleepTime, TimeSpan maxBusyWaitSleepTime) _options;

        public AzureBlobLeaseDistributedLock(BlobBaseClient blobClient, Action<AzureBlobLeaseOptionsBuilder>? options = null)
        {
            this._blobClient = new BlobClientWrapper(blobClient ?? throw new ArgumentNullException(nameof(blobClient)));
            this._options = AzureBlobLeaseOptionsBuilder.GetOptions(options);
        }

        public AzureBlobLeaseDistributedLock(BlobContainerClient blobContainerClient, string name, Action<AzureBlobLeaseOptionsBuilder>? options = null)
        {
            if (blobContainerClient == null) { throw new ArgumentNullException(nameof(blobContainerClient)); }
            if (name == null) { throw new ArgumentNullException(nameof(name)); }

            this._blobClient = new BlobClientWrapper(blobContainerClient.GetBlobClient(GetSafeName(name, blobContainerClient)));
            this._options = AzureBlobLeaseOptionsBuilder.GetOptions(options);
        }

        public string Name => this._blobClient.Name;

        bool IDistributedLock.IsReentrant => false;

        // implementation based on https://docs.microsoft.com/en-us/rest/api/storageservices/naming-and-referencing-containers--blobs--and-metadata#blob-names
        internal static string GetSafeName(string name, BlobContainerClient blobContainerClient)
        {
            var maxLength = IsStorageEmulator() ? 256 : 1024;

            return DistributedLockHelpers.ToSafeName(name, maxLength, s => ConvertToValidName(s));

            // check based on 
            // https://docs.microsoft.com/en-us/azure/storage/common/storage-use-emulator#connect-to-the-emulator-account-using-the-well-known-account-name-and-key
            bool IsStorageEmulator() => blobContainerClient.Uri.IsAbsoluteUri
                && blobContainerClient.Uri.AbsoluteUri.StartsWith("http://127.0.0.1:10000/devstoreaccount1", StringComparison.Ordinal);

            static string ConvertToValidName(string name)
            {
                const int MaxSlashes = 253; // allowed to have up to 254 segments, which means 253 slashes

                if (name.Length == 0) { return "__EMPTY__"; }

                StringBuilder? builder = null;
                var slashCount = 0;
                for (var i = 0; i < name.Length; ++i)
                {
                    var @char = name[i];

                    // enforce cap on # path segments and note that trailing slash or DOT are
                    // discouraged

                    if ((@char == '/' || @char == '\\')
                        && (++slashCount > MaxSlashes || i == name.Length - 1))
                    {
                        EnsureBuilder().Append("SLASH");
                    }
                    else if (@char == '.' && i == name.Length - 1)
                    {
                        EnsureBuilder().Append("DOT");
                    }
                    else
                    {
                        builder?.Append(@char);
                    }

                    StringBuilder EnsureBuilder() => builder ??= new StringBuilder().Append(name, startIndex: 0, count: i);
                }

                return builder?.ToString() ?? name;
            }
        }

        ValueTask<AzureBlobLeaseDistributedLockHandle?> IInternalDistributedLock<AzureBlobLeaseDistributedLockHandle>.InternalTryAcquireAsync(TimeoutValue timeout, CancellationToken cancellationToken) =>
            BusyWaitHelper.WaitAsync(
                (@lock: this, leaseClient: this._blobClient.GetBlobLeaseClient()),
                (state, token) => state.@lock.TryAcquireAsync(state.leaseClient, token, isRetryAfterCreate: false),
                timeout,
                minSleepTime: this._options.minBusyWaitSleepTime,
                maxSleepTime: this._options.maxBusyWaitSleepTime,
                cancellationToken
            );

        private async ValueTask<AzureBlobLeaseDistributedLockHandle?> TryAcquireAsync(
            BlobLeaseClientWrapper leaseClient, 
            CancellationToken cancellationToken,
            bool isRetryAfterCreate)
        {
            try  { await leaseClient.AcquireAsync(this._options.duration, cancellationToken).ConfigureAwait(false); }
            catch (RequestFailedException acquireException)
            {
                if (acquireException.ErrorCode == AzureErrors.LeaseAlreadyPresent) { return null; }

                if (acquireException.ErrorCode == AzureErrors.BlobNotFound)
                {
                    // if we just created and it already doesn't exist again, just return null and retry later
                    if (isRetryAfterCreate) { return null; }

                    // create the blob
                    var metadata = new Dictionary<string, string> { [CreatedMetadataKey] = DateTime.UtcNow.ToString("o") }; // date value is just for debugging
                    try { await this._blobClient.CreateIfNotExistsAsync(metadata, cancellationToken).ConfigureAwait(false); }
                    catch (RequestFailedException createException)
                    {
                        // handle the race condition where we try to create and someone else creates it first
                        return createException.ErrorCode == AzureErrors.LeaseIdMissing
                            ? default(AzureBlobLeaseDistributedLockHandle?)
                            : throw new AggregateException($"Blob {this._blobClient.Name} does not exist and could not be created. See inner exceptions for details", acquireException, createException);
                    }

                    try { return await this.TryAcquireAsync(leaseClient, cancellationToken, isRetryAfterCreate: true).ConfigureAwait(false); }
                    catch (Exception retryException)
                    {
                        // if the retry fails and we created, attempt deletion to clean things up
                        try { await this._blobClient.DeleteIfExistsAsync().ConfigureAwait(false); }
                        catch (Exception deletionException)
                        {
                            throw new AggregateException(retryException, deletionException);
                        }

                        throw;
                    }
                }

                throw;
            }

            var shouldDeleteBlob = isRetryAfterCreate
                || (await this._blobClient.GetMetadataAsync(leaseClient.LeaseId, cancellationToken).ConfigureAwait(false)).ContainsKey(CreatedMetadataKey);

            var internalHandle = new InternalHandle(leaseClient, ownsBlob: shouldDeleteBlob, @lock: this);
            return new AzureBlobLeaseDistributedLockHandle(internalHandle);
        }

        // todo remove
        public bool WillGoAsync(TimeoutValue timeout, CancellationToken cancellationToken) => false;

        internal sealed class InternalHandle : IDistributedLockHandle
        {
            private readonly CancellationTokenSource _renewalCancellationSource = new CancellationTokenSource();
            private readonly BlobLeaseClientWrapper _leaseClient;
            private readonly bool _ownsBlob;
            private readonly AzureBlobLeaseDistributedLock _lock;
            private readonly Task _renewalTask;
            
            public InternalHandle(BlobLeaseClientWrapper leaseClient, bool ownsBlob, AzureBlobLeaseDistributedLock @lock)
            {
                this._leaseClient = leaseClient;
                this._ownsBlob = ownsBlob;
                this._lock = @lock;
                var handleLostSource = new CancellationTokenSource();
                this.HandleLostToken = handleLostSource.Token;
                this._renewalTask = StartRenewalOrHandleCheckTask(new WeakReference<InternalHandle>(this), handleLostSource, this._renewalCancellationSource.Token);

                // static function to make sure we don't capture this
                static Task StartRenewalOrHandleCheckTask(WeakReference<InternalHandle> weakThis, CancellationTokenSource handleLostSource, CancellationToken cancellationToken) =>
                    Task.Run(() => RenewalOrHandleCheckLoop(weakThis, handleLostSource, cancellationToken));
            }

            public CancellationToken HandleLostToken { get; }

            private bool RenewalEnabled => !this._lock._options.renewalCadence.IsInfinite;

            public string LeaseId => this._leaseClient.LeaseId;

            public void Dispose() => SyncOverAsync.Run(@this => @this.DisposeAsync(), this, false);

            public async ValueTask DisposeAsync()
            {
                if (this._renewalCancellationSource.IsCancellationRequested)
                {
                    return; // already disposed
                }

                this._renewalCancellationSource.Cancel();
                if (SyncOverAsync.IsSynchronous) { this._renewalTask.GetAwaiter().GetResult(); }
                else { await this._renewalTask.ConfigureAwait(false); }
                this._renewalCancellationSource.Dispose();

                // if we own the blob, release by just deleting it
                if (this._ownsBlob)
                {
                    await this._lock._blobClient.DeleteIfExistsAsync(leaseId: this._leaseClient.LeaseId).ConfigureAwait(false);
                }
                else 
                {
                    await this._leaseClient.ReleaseAsync().ConfigureAwait(false);
                }
            }

            private static async Task RenewalOrHandleCheckLoop(
                WeakReference<InternalHandle> weakThis,
                CancellationTokenSource handleLostSource,
                CancellationToken cancellationToken)
            {
                if (!TryGetCadence(weakThis, out var cadence)) 
                {
                    handleLostSource.Dispose();
                    return;
                }

                while (true)
                {
                    // avoid throwing since this will be canceled very commonly and since this task is awaited on dispose
                    await Task.Delay(cadence.InMilliseconds, cancellationToken).TryAwait();
                    if (cancellationToken.IsCancellationRequested)
                    {
                        handleLostSource.Dispose();
                        return;
                    }

                    var renewOrCheckStatus = await TryRenewOrCheckHandleAsync(weakThis, handleLostSource, cancellationToken).ConfigureAwait(false);
                    if (renewOrCheckStatus != RenewOrCheckStatus.Continue)
                    {
                        if (renewOrCheckStatus == RenewOrCheckStatus.HandleLost)
                        {
                            // offload cancel to a background thread to avoid hangs or errors
                            var ignored = Task.Run(() =>
                            {
                                try { handleLostSource.Cancel(); }
                                finally { handleLostSource.Dispose(); }
                            });
                        }
                        else
                        {
                            handleLostSource.Dispose();
                        }
                        return;
                    }
                }

                // separate function to avoid taking a strong reference
                static bool TryGetCadence(WeakReference<InternalHandle> weakThis, out TimeoutValue renewalCadence)
                {
                    if (weakThis.TryGetTarget(out var @this))
                    {
                        renewalCadence = @this.RenewalEnabled ? @this._lock._options.renewalCadence : @this._lock._options.duration;
                        return true;
                    }

                    renewalCadence = default;
                    return false;
                }
            }

            private static async ValueTask<RenewOrCheckStatus> TryRenewOrCheckHandleAsync(
                WeakReference<InternalHandle> weakThis, 
                CancellationTokenSource handleLostSource,
                CancellationToken cancellationToken)
            {
                if (!weakThis.TryGetTarget(out var @this)) { return RenewOrCheckStatus.HandleGarbageCollected; }

                var task = @this.RenewalEnabled
                    ? @this._leaseClient.RenewAsync(cancellationToken).AsTask()
                    // if we're not renewing, then just touch the blob using the lease to see if someone else has renewed it
                    : @this._lock._blobClient.GetMetadataAsync(@this._leaseClient.LeaseId, cancellationToken).AsTask();

                await task.TryAwait();
                return task.Status == TaskStatus.RanToCompletion ? RenewOrCheckStatus.Continue
                    : cancellationToken.IsCancellationRequested ? RenewOrCheckStatus.Canceled
                    : RenewOrCheckStatus.HandleLost;
            }

            private enum RenewOrCheckStatus
            {
                Continue,
                Canceled,
                HandleLost,
                HandleGarbageCollected,
            }
        }
    }
}
