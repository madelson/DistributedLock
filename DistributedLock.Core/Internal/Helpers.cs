using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Medallion.Threading.Internal
{
#if DEBUG
    public
#else
    internal
#endif
    static class Helpers
    {
        /// <summary>
        /// Performs a type-safe cast
        /// </summary>
        public static T As<T>(this T @this) => @this;

        /// <summary>
        /// Performs a type-safe "cast" of a <see cref="ValueTask{TResult}"/>
        /// </summary>
        public static async ValueTask<TBase> ConvertValueTask<TDerived, TBase>(ValueTask<TDerived> task)
            where TDerived : TBase =>
            await task.ConfigureAwait(false);

        public static ValueTask<T> AsValueTask<T>(this Task<T> task) => new ValueTask<T>(task);
        public static ValueTask AsValueTask(this Task task) => new ValueTask(task);
        public static ValueTask<T> AsValueTask<T>(this T value) => new ValueTask<T>(value);

        public static ObjectDisposedException ObjectDisposed<T>(this T value) where T : IAsyncDisposable =>
            throw new ObjectDisposedException(typeof(T).ToString());
    }
}
