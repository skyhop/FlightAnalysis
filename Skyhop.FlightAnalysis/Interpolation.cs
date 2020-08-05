using System;
using System.Collections.Generic;
using System.Linq;

namespace Skyhop.FlightAnalysis
{
    public static class Interpolation
    {
        public static List<InterpolationContainer<T>> Interpolate<T>(
            IEnumerable<T> t1,
            IEnumerable<T> t2,
            Func<T, long> getTime,
            Func<T, T, long, T> interpolation)
            where T : class
        {
            var combination = new List<InterpolationContainer<T>>();

            //if (!(T1.HasAtLeast(2) && T2.HasAtLeast(2))) throw new Exception("Lists too short");
            if (t1.Count() < 2 || t2.Count() < 2) throw new Exception("Lists too short");

            combination.AddRange(t1.Select(t => new InterpolationContainer<T>
            {
                Time = getTime.Invoke(t),
                T1 = t,
                T2 = default
            }));

            combination.AddRange(t2.Select(t => new InterpolationContainer<T>
            {
                Time = getTime.Invoke(t),
                T1 = default,
                T2 = t
            }));

            combination.Sort();

            for (var i = 0; i < combination.Count - 1; i++)
            {
                combination[i].T1 = _interpolateSet(combination, interpolation, q => q.T1, i);
                combination[i].T2 = _interpolateSet(combination, interpolation, q => q.T2, i);
            }

            return combination
                .Where(q => q.T1 != null && q.T2 != null)
                .ToList();
        }

        private static T _interpolateSet<T>(
            List<InterpolationContainer<T>> list,
            Func<T, T, long, T> interpolation,
            Func<InterpolationContainer<T>, T> valueAccessor,
            int i)
            where T : class
        {
            var va = valueAccessor.Invoke(list[i]);

            if (va != default) return va;

            // ToDo: Look before and after
            int before = i == 0 ? 0 : i - 1;
            int after = i + 1;

            while (before > 0)
            {
                if (valueAccessor.Invoke(list[before]) != null) break;
                before--;
            }

            var listCount = list.Count;

            while (after < listCount - 1)
            {
                if (valueAccessor.Invoke(list[after]) != null) break;
                after++;
            }

            // ToDo: Block interpolation on objects with only one data point.
            if (valueAccessor.Invoke(list[before]) == null && valueAccessor.Invoke(list[after]) == null)
            {
                throw new Exception("Not enough data");
            }

            if (valueAccessor.Invoke(list[before]) == null)
            {
                before = after;
                after++;
                while (after < listCount)
                {
                    if (valueAccessor.Invoke(list[after]) != null) break;
                    after++;
                }
            }
            else if (valueAccessor.Invoke(list[after]) == null)
            {
                after = before;
                before--;

                while (before > 0)
                {
                    if (list[before].T1 != null) break;
                    before--;
                }
            }

            return interpolation.Invoke(
                valueAccessor(list[before])!,
                valueAccessor(list[after])!,
                list[i].Time);
        }
    }

    public class InterpolationContainer<T> : IComparable<InterpolationContainer<T>>
        where T : class
    {
        public long Time { get; set; }
        public T T1 { get; set; }
        public T T2 { get; set; }

        public int CompareTo(InterpolationContainer<T>? other)
        {
            if (other == null) return 1;

            return Time.CompareTo(other.Time);
        }
    }
}
