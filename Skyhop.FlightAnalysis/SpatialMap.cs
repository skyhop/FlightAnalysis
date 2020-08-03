using System;
using System.Collections.Generic;
using System.Reflection;

namespace Skyhop.FlightAnalysis
{
    /*
     * The spatial map should be a type easy to use to find nearby data.
     * 
     * Given all data is ordered both based on X and Y axis, it should be
     * fairly simple to retrieve the closest points based on O(log N) performance
     * if run on a single core.
     * 
     * The goals are to;
     * 
     * - Dynamically add data
     * - Dynamically remove data
     * - Find nearby elements based on a maximum distance
     */

    public class SpatialMap<T>
    {
        private readonly object _mutationLock = new object();
        private readonly Func<T, double> _xAccessor;
        private readonly Func<T, double> _yAccessor;

        public SpatialMap(
            Func<T, double> xAccessor,
            Func<T, double> yAccessor)
        {
            _xAccessor = xAccessor;
            _yAccessor = yAccessor;
        }

        private CustomSortedList<double, T> _x { get; set; } = new CustomSortedList<double, T>();
        private CustomSortedList<double, T> _y { get; set; } = new CustomSortedList<double, T>();

        public void Add(T element)
        {
            lock (_mutationLock)
            {
                if (!_x.TryAdd(_xAccessor(element), element))
                {
                    return;
                }

                if (!_y.TryAdd(_yAccessor(element), element))
                {
                    _x.Remove(_xAccessor(element));
                }
            }
        }

        public void Remove(T element)
        {
            lock (_mutationLock)
            {
                _x.Remove(_xAccessor(element));
                _y.Remove(_yAccessor(element));
            }
        }

        public T Nearest(T element)
        {
            var x = _xAccessor(element);
            var y = _yAccessor(element);

            T nearest = default;

            var approxX = _x.IndexOfKey(x);
            var approxY = _y.IndexOfKey(y);

            var lowerX = approxX;
            var upperX = approxX;

            var lowerY = approxY;
            var upperY = approxY;

            var hashSet = new HashSet<T>();

            var minDistance = double.MaxValue;

            for (var i = 0;
                ((_xAccessor(_x.GetByIndex(upperX)) - x) < minDistance)
                   || ((x - _xAccessor(_x.GetByIndex(lowerX))) < minDistance);
                i++)
            {
                if (upperX < _x.Count)
                {
                    var el = _x.GetByIndex(upperX);

                    if (hashSet.Add(el) && Distance(
                        x, y,
                        _xAccessor(el),
                        _yAccessor(el)) < minDistance)
                    {
                        nearest = el;
                        minDistance = Distance(
                        x, y,
                        _xAccessor(el),
                        _yAccessor(el));
                    }

                    upperX = approxX + i;
                }

                if (lowerX >= 0)
                {
                    var el = _x.GetByIndex(lowerX);
                    if (hashSet.Add(el) && Distance(
                        x, y,
                        _xAccessor(el),
                        _yAccessor(el)) < minDistance)
                    {
                        nearest = el;
                        minDistance = Distance(
                            x, y,
                            _xAccessor(el),
                            _yAccessor(el));
                    }

                    lowerX = approxX - i;
                }
            }

            for (var i = 0;
                ((_yAccessor(_y.GetByIndex(upperY)) - y) < minDistance)
                   || ((y - _yAccessor(_y.GetByIndex(lowerY))) < minDistance);
                i++)
            {
                if (upperY < _y.Count)
                {
                    var el = _y.GetByIndex(upperY);

                    if (hashSet.Add(el) && Distance(
                        x, y,
                        _xAccessor(el),
                        _yAccessor(el)) < minDistance)
                    {
                        nearest = el;
                        minDistance = Distance(
                        x, y,
                        _xAccessor(el),
                        _yAccessor(el));
                    }

                    upperY = approxY + i;
                }

                if (lowerY >= 0)
                {
                    var el = _y.GetByIndex(lowerY);
                    if (hashSet.Add(el) && Distance(
                        x, y,
                        _xAccessor(el),
                        _yAccessor(el)) < minDistance)
                    {
                        nearest = el;
                        minDistance = Distance(
                            x, y,
                            _xAccessor(el),
                            _yAccessor(el));
                    }

                    lowerY = approxY - i;
                }
            }

            return nearest;
        }

        public static double Distance(double x1, double y1, double x2, double y2)
        {
            var x = x2 - x1;
            var y = y2 - y1;

            return Math.Sqrt((x * x) + (y * y));
        }

        public IEnumerable<T> Nearby(T element, double distance)
        {
            var x = _xAccessor(element);
            var y = _yAccessor(element);

            // Compensating the inner distance like this reduces the potential
            // number of potential matches by 10% in an evenly distributed scatter
            var innerDistance = Math.Sqrt(Math.Pow(distance, 2) / 2);

            var lowerXIndex = _x.IndexOfKey(x - innerDistance);
            var upperXIndex = _x.IndexOfKey(x + innerDistance);
            var lowerYIndex = _y.IndexOfKey(y - innerDistance);
            var upperYIndex = _y.IndexOfKey(y + innerDistance);

            var hashSet = new HashSet<T>();

            for (var i = lowerXIndex; i < upperXIndex; i++)
            {
                var el = _x.GetByIndex(i);
                if (hashSet.Add(el)
                    && Distance(
                    x, y,
                    _xAccessor(el),
                    _yAccessor(el)) < distance)
                {
                    yield return el;
                }
            }

            for (var i = lowerYIndex; i < upperYIndex; i++)
            {
                var el = _y.GetByIndex(i);
                if (hashSet.Add(el)
                    && Distance(
                        x, y,
                        _xAccessor(el),
                        _yAccessor(el)) < distance)
                {
                    yield return el;
                }
            }
        }
    }

    internal class CustomSortedList<TKey, TValue> : SortedList<TKey, TValue>
        where TKey : notnull
    {
        private readonly FieldInfo _keysField = typeof(CustomSortedList<TKey, TValue>).BaseType.GetField("keys", BindingFlags.Instance | BindingFlags.NonPublic);
        private readonly FieldInfo _valuesField = typeof(CustomSortedList<TKey, TValue>).BaseType.GetField("values", BindingFlags.Instance | BindingFlags.NonPublic);
        private readonly FieldInfo _comparerField = typeof(CustomSortedList<TKey, TValue>).BaseType.GetField("comparer", BindingFlags.Instance | BindingFlags.NonPublic);

        // Returns the index of the entry with a given key in this sorted list. The
        // key is located through a binary search, and thus the average execution
        // time of this method is proportional to Log2(size), where
        // size is the size of this sorted list. The returned value is -1 if
        // the given key does not occur in this sorted list. Null is an invalid 
        // key value.
        // 
        public new int IndexOfKey(TKey key)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            int ret = Array.BinarySearch<TKey>(
                (TKey[])_keysField.GetValue(this),
                0,
                Count,
                key,
                (IComparer<TKey>)_comparerField.GetValue(this));

            return ret >= 0 ? ret : ~ret;
        }

        // Returns the value of the entry at the given index.
        // 
        public TValue GetByIndex(int index)
        {
            if (index < 0 || index > Count) return default;
            return ((TValue[])_valuesField.GetValue(this))[index];
        }
    }
}
