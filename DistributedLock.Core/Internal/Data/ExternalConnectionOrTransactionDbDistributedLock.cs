using Medallion.Threading.Internal;
using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace Medallion.Threading.Internal.Data
{
    /// <summary>
    /// Implements <see cref="IDbDistributedLock"/> using a <see cref="IDbConnection"/>
    /// or <see cref="IDbTransaction"/> created external to the library and passed in
    /// by the user
    /// </summary>
#if DEBUG
    public
#else
    internal
#endif
    sealed class ExternalConnectionOrTransactionDbDistributedLock : IDbDistributedLock
    {
        private readonly string _name;
        private readonly DatabaseConnection _connection;

        public ExternalConnectionOrTransactionDbDistributedLock(string name, DatabaseConnection connection)
        {
            this._name = name;
            this._connection = connection;
        }

        public async ValueTask<IDistributedLockHandle?> TryAcquireAsync<TLockCookie>(
            TimeoutValue timeout, 
            IDbSynchronizationStrategy<TLockCookie> strategy, 
            CancellationToken cancellationToken, 
            IDistributedLockHandle? contextHandle)
            where TLockCookie : class
        {
            if (!this._connection.CanExecuteQueries)
            {
                throw new InvalidOperationException("The connection and/or transaction are disposed or closed");
            }

            var lockCookie = await strategy.TryAcquireAsync(this._connection, this._name, timeout, cancellationToken).ConfigureAwait(false);
            return lockCookie == null ? null : new Handle<TLockCookie>(this, strategy, lockCookie);
        }

        private sealed class Handle<TLockCookie> : IDistributedLockHandle
            where TLockCookie : class
        {
            private RefBox<(ExternalConnectionOrTransactionDbDistributedLock @lock, IDbSynchronizationStrategy<TLockCookie> strategy, TLockCookie lockCookie)>? _box;

            public Handle(
                ExternalConnectionOrTransactionDbDistributedLock @lock,
                IDbSynchronizationStrategy<TLockCookie> strategy,
                TLockCookie lockCookie)
            {
                this._box = RefBox.Create((@lock, strategy, lockCookie));
            }

            public CancellationToken HandleLostToken => throw new NotImplementedException();

            public void Dispose() => SyncOverAsync.Run(@this => this.DisposeAsync(), this, willGoAsync: false);

            public ValueTask DisposeAsync() =>
                RefBox.TryConsume(ref this._box, out var contents)
                    && contents.@lock._connection.CanExecuteQueries
                    ? contents.strategy.ReleaseAsync(contents.@lock._connection, contents.@lock._name, contents.lockCookie)
                    : default;
        }
    }
}
