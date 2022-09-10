using Medallion.Threading.Internal;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Medallion.Threading
{
	/// <summary>
	/// A version of <see cref="ReaderWriterLockSlim"/> which supports both 
	/// synchronous (via <see cref="SyncViaAsync"/>) and asynchronous locking.
	/// </summary>
    internal sealed class AsyncReaderWriterLock
    {
		private readonly AsyncLock _upgradeableReadLock = AsyncLock.Create();
		/// <summary>
		/// Fires when the last reader releases OR the writer releases
		/// </summary>
		private readonly AsyncManualResetEvent _releasedEvent = new(set: true);

		/// <summary>
		/// <see cref="_readerCount"/> is the number of readers who have acquired or are waiting to acquire the read lock.
		/// <see cref="_writerCount"/> is the number of writers who have acquired or are waiting to acquire the write lock.
		/// Neither counter includes the upgradeable read lock.
		/// </summary>
		private ulong _readerCount, _writerCount;
		/// <summary>
		/// Whether the lock is held by a reader or a writer or neither. Ignores the upgradeable read lock.
		/// </summary>
		private HeldState _state;

		private object Lock => this._releasedEvent;

		public async ValueTask<IDisposable?> TryAcquireReadLockAsync(TimeoutValue timeout, CancellationToken cancellationToken)
        {
			cancellationToken.ThrowIfCancellationRequested();

			lock (this.Lock)
			{
				checked { ++this._readerCount; }
				if (this._writerCount == 0) { return SetStateAndCreateHandleNoLock(); }
			}
			try
            {
				TimeoutTracker timeoutTracker = new(timeout);
				while (true)
				{
					if (!await this._releasedEvent.WaitAsync(timeoutTracker.Remaining, cancellationToken).ConfigureAwait(false))
					{
						this.ReleaseReadLock();
						return null;
					}
					
					lock (this.Lock)
                    {
						if (this._writerCount == 0) { return SetStateAndCreateHandleNoLock(); }
                    }
				}
			}
            catch
            {
				this.ReleaseReadLock();
				throw;
            }

			Handle SetStateAndCreateHandleNoLock()
			{
				Invariant.Require(this._state is HeldState.None or HeldState.Reader);
				this._state = HeldState.Reader;
				this._releasedEvent.Reset(); // writers will now block
				return new Handle(this, state: null, static (l, _) => l.ReleaseReadLock());
			}
		}

		private void ReleaseReadLock()
		{
			lock (this.Lock)
			{
				if (checked(--this._readerCount) == 0 && this._state == HeldState.Reader)
                {
					this._state = HeldState.None;
					this._releasedEvent.Set(); // must be last in the lock block in case continuations run inline
				}
			}
		}

		public async ValueTask<UpgradeableHandle?> TryAcquireUpgradeableReadLockAsync(TimeoutValue timeout, CancellationToken cancellationToken)
        {
			var upgradeableReadLockHandle = await this._upgradeableReadLock.TryAcquireAsync(timeout, cancellationToken).ConfigureAwait(false);
			return upgradeableReadLockHandle is null ? null : new(this, upgradeableReadLockHandle);
        }

		private async ValueTask<bool> TryUpgradeToWriteLockAsync(TimeoutValue timeout, CancellationToken cancellationToken)
        {
			cancellationToken.ThrowIfCancellationRequested();

			lock (this.Lock)
            {
				this.BeginAcquiringWriteLockNoLock();

				if (this._readerCount == 0) 
				{
					this.SetStateOnWriteLockAcquiredNoLock();
					return true; 
				}
            }
			try
            {
				TimeoutTracker timeoutTracker = new(timeout);
				while (true)
                {
					if (!await this._releasedEvent.WaitAsync(timeoutTracker.Remaining, cancellationToken).ConfigureAwait(false))
                    {
						lock (this.Lock)
                        {
							this.ReleaseWriteLock(null);
                        }
						return false;
                    }
					
					lock (this.Lock)
                    {
						if (this._readerCount == 0) 
						{
							this.SetStateOnWriteLockAcquiredNoLock();
							return true; 
						}
                    }
                }
            }
			catch
            {
				this.ReleaseWriteLock(null);
				throw;
            }
        }

		public async ValueTask<IDisposable?> TryAcquireWriteLockAsync(TimeoutValue timeout, CancellationToken cancellationToken)
        {
			lock (this.Lock)
            {
				this.BeginAcquiringWriteLockNoLock();
			}

			IDisposable? upgradeableReadLockHandle = null;
			try
			{
				TimeoutTracker timeoutTracker = new(timeout);
				upgradeableReadLockHandle = await this._upgradeableReadLock.TryAcquireAsync(timeoutTracker.Remaining, cancellationToken).ConfigureAwait(false);
				if (upgradeableReadLockHandle is null)
				{
					this.ReleaseWriteLock(upgradeableReadLockHandle);
					return null;
				}

				while (true)
				{
					lock (this.Lock)
					{
						if (this._readerCount == 0)
						{
							this.SetStateOnWriteLockAcquiredNoLock();
							return new Handle(this, upgradeableReadLockHandle, static (l, s) => l.ReleaseWriteLock((IDisposable?)s));
						}
					}

					if (!await this._releasedEvent.WaitAsync(timeoutTracker.Remaining, cancellationToken).ConfigureAwait(false))
                    {
						this.ReleaseWriteLock(upgradeableReadLockHandle);
						return null;
					}
				}
			}
			catch
            {
				this.ReleaseWriteLock(upgradeableReadLockHandle);
				throw;
			}
        }

		private void BeginAcquiringWriteLockNoLock()
        {
			checked { ++this._writerCount; }
			this._releasedEvent.Reset(); // readers will now block
		}

		private void SetStateOnWriteLockAcquiredNoLock()
        {
			Invariant.Require(this._state == HeldState.None);
			this._state = HeldState.Writer;
		}

		private void ReleaseWriteLock(IDisposable? upgradeableReadLockHandle)
        {
			lock (this.Lock)
            {
				if (checked(--this._writerCount) == 0 && this._state == HeldState.Writer)
                {
					this._state = HeldState.None;
					this._releasedEvent.Set(); // must be last in the lock block in case continuations run inline
				}
            }
			upgradeableReadLockHandle?.Dispose();
        }

		private enum HeldState { None, Reader, Writer, }

		private sealed class Handle : IDisposable
        {
			private AsyncReaderWriterLock? _lock;
			private readonly object? _state;
			private readonly Action<AsyncReaderWriterLock, object?> _release;

			public Handle(AsyncReaderWriterLock @lock, object? state, Action<AsyncReaderWriterLock, object?> release)
            {
				this._lock = @lock;
				this._state = state;
				this._release = release;
            }

			public void Dispose()
			{
				if (Interlocked.Exchange(ref this._lock, null) is { } @lock)
                {
					this._release(@lock, this._state);
                }
			}
        }

        public sealed class UpgradeableHandle : IDisposable
        {
			private AsyncReaderWriterLock? _lock;
			private readonly IDisposable _upgradeableReadLockHandle;
			private UpgradeState _state;

			public UpgradeableHandle(AsyncReaderWriterLock @lock, IDisposable upgradeableReadLockHandle)
            {
				this._lock = @lock;
				this._upgradeableReadLockHandle = upgradeableReadLockHandle;
            }

			private object Mutex => this._upgradeableReadLockHandle;

			public async ValueTask<bool> TryUpgradeToWriteLockAsync(TimeoutValue timeout, CancellationToken cancellationToken)
            {
				lock (this.Mutex)
                {
					if (this._lock is null) { throw new ObjectDisposedException(this.GetType().ToString()); }
					this._state = this._state switch
					{
						UpgradeState.NotUpgraded => UpgradeState.Upgrading,
						UpgradeState.Upgrading => throw new InvalidOperationException("Already upgrading to write lock"),
						UpgradeState.Upgraded => throw new InvalidOperationException("Already upgraded to write lock"),
						_ => throw new InvalidOperationException("Should never get here"),
					};
                }
				var state = UpgradeState.NotUpgraded;
				try
                {
					if (await this._lock.TryUpgradeToWriteLockAsync(timeout, cancellationToken).ConfigureAwait(false))
                    {
						state = UpgradeState.Upgraded;
						return true;
                    }
					return false;
                }
				finally
                {
					lock (this.Mutex)
                    {
						this._state = state;
                    }
                }
            }

			public void Dispose()
            {
				lock (this.Mutex)
                {
					if (this._lock is { } @lock)
                    {
						if (this._state == UpgradeState.Upgrading)
						{
							throw new InvalidOperationException("Cannot dispose during an upgrade operation");
						}

						if (this._state == UpgradeState.Upgraded) { @lock.ReleaseWriteLock(this._upgradeableReadLockHandle); }
						else { this._upgradeableReadLockHandle.Dispose(); }
						this._lock = null;
					}
                }
            }

			private enum UpgradeState { NotUpgraded, Upgrading, Upgraded }
        }

        private sealed class AsyncManualResetEvent
		{
			private static readonly Task<bool> TrueTask = Task.FromResult(true);

			private readonly object _lock = new();
			private TaskCompletionSource<bool>? _task;
			private bool _set;

			public AsyncManualResetEvent(bool set)
            {
				this._set = set;
            }

			public bool IsSet => Volatile.Read(ref this._set);

			public void Set()
            {
				TaskCompletionSource<bool>? taskToComplete;
				lock (this._lock)
                {
					if (this._set) { return; }

					taskToComplete = this._task;
					this._task = null;
					this._set = false;
                }

				// true is important for allowing us to directly return this task in InternalWaitAsync
				taskToComplete?.SetResult(true);
			}

			public void Reset()
            {
				lock (this._lock) { this._set = false; }
            }

			private Task<bool> GetTask()
            {
				lock (this._lock)
                {
					return this._set
						? TrueTask
						: (this._task ??= new()).Task;
                }
            }

			public ValueTask<bool> WaitAsync(TimeoutValue timeout, CancellationToken cancellationToken) =>
				SyncViaAsync.IsSynchronous
					? this.InternalWait(timeout, cancellationToken).AsValueTask()
					: this.InternalWaitAsync(timeout, cancellationToken).AsValueTask();

			private bool InternalWait(TimeoutValue timeout, CancellationToken cancellationToken)
			{
				cancellationToken.ThrowIfCancellationRequested();
				return this.GetTask().Wait(timeout.InMilliseconds, cancellationToken);
			}

			private Task<bool> InternalWaitAsync(TimeoutValue timeout, CancellationToken cancellationToken)
			{
				if (cancellationToken.IsCancellationRequested) { return Task.FromCanceled<bool>(cancellationToken); }
				var task = this.GetTask();
				if (task.IsCompleted || (timeout.IsInfinite && !cancellationToken.CanBeCanceled)) { return task; }
				return WaitHelperAsync(task, timeout, cancellationToken);

				static async Task<bool> WaitHelperAsync(Task task, TimeoutValue timeout, CancellationToken cancellationToken)
                {
					var completed = await Task.WhenAny(task, Task.Delay(timeout.InMilliseconds, cancellationToken)).ConfigureAwait(false);
					await completed.ConfigureAwait(false); // propagate OperationCanceledException
					return completed == task;
				}
			}
		}
	}
}
