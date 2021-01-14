using Medallion.Threading.Internal;
using Medallion.Threading.Redis.Primitives;
using Medallion.Threading.Redis.RedLock;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Medallion.Threading.Redis
{
    /// <summary>
    /// Implements a <see cref="IDistributedReaderWriterLock"/> using Redis. Can leverage multiple servers via the RedLock algorithm.
    /// </summary>
    public sealed partial class RedisDistributedReaderWriterLock : IInternalDistributedReaderWriterLock<RedisDistributedReaderWriterLockHandle>
    {
        private readonly IReadOnlyList<IDatabase> _databases;
        private readonly RedisDistributedLockOptions _options;

        /// <summary>
        /// Constructs a lock named <paramref name="name"/> using the provided <paramref name="database"/> and <paramref name="options"/>.
        /// </summary>
        public RedisDistributedReaderWriterLock(string name, IDatabase database, Action<RedisDistributedSynchronizationOptionsBuilder>? options = null)
            : this(name, new[] { database ?? throw new ArgumentNullException(nameof(database)) }, options)
        {
        }

        /// <summary>
        /// Constructs a lock named <paramref name="name"/> using the provided <paramref name="databases"/> and <paramref name="options"/>.
        /// </summary>
        public RedisDistributedReaderWriterLock(string name, IEnumerable<IDatabase> databases, Action<RedisDistributedSynchronizationOptionsBuilder>? options = null)
        {
            if (name == null) { throw new ArgumentNullException(nameof(name)); }
            this._databases = RedisDistributedLock.ValidateDatabases(databases);

            this.ReaderKey = name + ".readers";
            this.WriterKey = name + ".writer";
            this.Name = name;
            this._options = RedisDistributedSynchronizationOptionsBuilder.GetOptions(options);

            // We insist on this rule to ensure that when we take the writer waiting lock it won't expire between attempts 
            // to upgrade it to the write lock. This avoids the need to extend the writer waiting lock
            if (this._options.RedLockTimeouts.MinValidityTime.CompareTo(this._options.MaxBusyWaitSleepTime) <= 0)
            {
                throw new ArgumentException($"{nameof(RedisDistributedSynchronizationOptionsBuilder.BusyWaitSleepTime)} must be <= {nameof(RedisDistributedSynchronizationOptionsBuilder.MinValidityTime)}", nameof(options));
            }
        }

        internal RedisKey ReaderKey { get; }
        internal RedisKey WriterKey { get; }

        /// <summary>
        /// Implements <see cref="IDistributedReaderWriterLock.Name"/>
        /// </summary>
        public string Name { get; }

        ValueTask<RedisDistributedReaderWriterLockHandle?> IInternalDistributedReaderWriterLock<RedisDistributedReaderWriterLockHandle>.InternalTryAcquireAsync(
            TimeoutValue timeout,
            CancellationToken cancellationToken,
            bool isWrite)
        {
            return isWrite
                ? this.TryAcquireWriteLockAsync(timeout, cancellationToken)
                : BusyWaitHelper.WaitAsync(
                    this,
                    (@lock, cancellationToken) => @lock.TryAcquireAsync(new RedisReadLockPrimitive(@lock.ReaderKey, @lock.WriterKey, @lock._options.RedLockTimeouts), cancellationToken),
                    timeout: timeout,
                    minSleepTime: this._options.MinBusyWaitSleepTime,
                    maxSleepTime: this._options.MaxBusyWaitSleepTime,
                    cancellationToken
                );
        }

        private async ValueTask<RedisDistributedReaderWriterLockHandle?> TryAcquireWriteLockAsync(TimeoutValue timeout, CancellationToken cancellationToken)
        {
            var acquireWriteLockState = new AcquireWriteLockState(canRetry: !timeout.IsZero);
            RedisDistributedReaderWriterLockHandle? handle = null;
            try
            {
                return handle = await BusyWaitHelper.WaitAsync(
                   (Lock: this, State: acquireWriteLockState),
                   (state, cancellationToken) => state.Lock.TryAcquireWriteLockAsync(state.State, cancellationToken),
                   timeout: timeout,
                   minSleepTime: this._options.MinBusyWaitSleepTime,
                   maxSleepTime: this._options.MaxBusyWaitSleepTime,
                   cancellationToken
                ).ConfigureAwait(false);
            }
            finally
            {
                // If we failed to take the write lock but we took the writer waiting lock, release
                // the writer waiting lock on our way out.
                if (handle == null && acquireWriteLockState.WriterWaiting.TryGetValue(out var writerWaiting)) 
                {
                    await new RedLockRelease(writerWaiting.Primitive, writerWaiting.TryAcquireTasks).ReleaseAsync().ConfigureAwait(false);
                }
            }
        }

        private async ValueTask<RedisDistributedReaderWriterLockHandle?> TryAcquireWriteLockAsync(AcquireWriteLockState state, CancellationToken cancellationToken)
        {
            // The first time, through, just try to acquire the write lock. This covers the TryAcquire(0) case and ensures that we 
            // don't bother with taking the writer waiting lock if we don't need to.
            if (state.IsFirstTry)
            {
                state.IsFirstTry = false;
                var firstTryResult =  await TryAcquireWriteLockAsync(RedLockHelper.CreateLockId()).ConfigureAwait(false);
                if (firstTryResult != null) { return firstTryResult; }
                // if we're not going to retry the acquire, don't bother attempting the writer waiting lock
                if (!state.CanRetry) { return null; }
            }

            Invariant.Require(state.CanRetry);

            // Otherwise, if we don't have the writer waiting lock yet, try to take that
            if (!state.WriterWaiting.HasValue)
            {
                var lockId = RedLockHelper.CreateLockId();
                var primitive = new RedisWriterWaitingPrimitive(this.WriterKey, lockId, this._options.RedLockTimeouts);
                var tryAcquireTasks = await new RedLockAcquire(primitive, this._databases, cancellationToken).TryAcquireAsync().ConfigureAwait(false);
                if (tryAcquireTasks == null) { return null; }

                // if we took writer waiting, save off the info and just keep going
                state.WriterWaiting = (primitive, tryAcquireTasks, lockId);
            }

            // If we get here, we have the writer waiting lock. Try to "upgrade" that to an actual writer lock
            return await TryAcquireWriteLockAsync(state.WriterWaiting.Value.LockId).ConfigureAwait(false);

            ValueTask<RedisDistributedReaderWriterLockHandle?> TryAcquireWriteLockAsync(RedisValue lockId) =>
                this.TryAcquireAsync(new RedisWriteLockPrimitive(this.ReaderKey, this.WriterKey, lockId, this._options.RedLockTimeouts), cancellationToken);
        }

        private async ValueTask<RedisDistributedReaderWriterLockHandle?> TryAcquireAsync<TPrimitive>(TPrimitive primitive, CancellationToken cancellationToken)
            where TPrimitive : IRedLockAcquirableSynchronizationPrimitive, IRedLockExtensibleSynchronizationPrimitive
        {
            var tryAcquireTasks = await new RedLockAcquire(primitive, this._databases, cancellationToken).TryAcquireAsync().ConfigureAwait(false);
            return tryAcquireTasks != null
                ? new RedisDistributedReaderWriterLockHandle(new RedLockHandle(primitive, tryAcquireTasks, extensionCadence: this._options.ExtensionCadence, expiry: this._options.RedLockTimeouts.Expiry))
                : null;
        }

        private class AcquireWriteLockState
        {
            public AcquireWriteLockState(bool canRetry)
            {
                this.CanRetry = canRetry;
            }

            public bool CanRetry { get; }

            public bool IsFirstTry { get; set; } = true;

            public (RedisWriterWaitingPrimitive Primitive, IReadOnlyDictionary<IDatabase, Task<bool>> TryAcquireTasks, RedisValue LockId)? WriterWaiting { get; set; }
        }
    }
}
