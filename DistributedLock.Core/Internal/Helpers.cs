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
    }
}
