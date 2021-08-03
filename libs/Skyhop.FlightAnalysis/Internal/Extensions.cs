using System;
using System.Collections.Generic;

namespace Skyhop.FlightAnalysis.Internal
{
    internal static class Extensions
    {
        public static IEnumerable<double> Delta<T>(this IList<T> list, Func<T, double> exp)
        {
            for (var i = 1; i < list.Count; i++)
            {
                yield return exp.Invoke(list[i]) - exp.Invoke(list[i - 1]);
            }
        }
    }
}
