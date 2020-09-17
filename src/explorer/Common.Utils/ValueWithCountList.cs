namespace Explorer.Common.Utils
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;

    internal class ValueWithCountList<T> : List<(T Value, long Count)>
    {
        public long TotalCount => Count == 0 ? 0 : this[^1].Count;

        public static ValueWithCountList<T> FromValueWithCountEnum(IEnumerable<ValueWithCount<T>> valueCounts)
        {
            var ret = new ValueWithCountList<T>();
            foreach (var vc in valueCounts)
            {
                ret.AddValueCount(vc.Value, vc.Count);
            }
            return ret;
        }

        public static ValueWithCountList<T> FromTupleEnum(IEnumerable<(T Value, long Count)> valueCounts)
        {
            var ret = new ValueWithCountList<T>();
            foreach (var vc in valueCounts)
            {
                ret.AddValueCount(vc.Value, vc.Count);
            }
            return ret;
        }

        public void AddValueCount(T value, long count)
        {
            Add((value, TotalCount + count));
        }

        [return: MaybeNull]
        public T GetRandomValue(Random rand)
        {
            if (TotalCount == 0)
            {
                return default;
            }

            var rcount = rand.NextLong(TotalCount);
            return FindValue(rcount);
        }

        public T FindValue(long count)
        {
            if (TotalCount == 0)
            {
                throw new InvalidOperationException("Collection is empty.");
            }
            if (count < 0 || count >= TotalCount)
            {
                throw new ArgumentException($"The {nameof(count)} parameter should have a value between 0 and {TotalCount - 1}, inclusive.");
            }
            var left = 0;
            var right = Count - 1;
            while (true)
            {
                var middle = (left + right) / 2;
                if (count < this[middle].Count)
                {
                    if (middle == left)
                    {
                        return this[middle].Value;
                    }
                    if (count >= this[middle - 1].Count)
                    {
                        return this[middle].Value;
                    }
                    right = middle;
                }
                else if (count >= this[middle].Count)
                {
                    if (middle == right)
                    {
                        return this[middle].Value;
                    }
                    if (count < this[middle + 1].Count)
                    {
                        return this[middle + 1].Value;
                    }
                    left = middle;
                }
            }
        }
    }
}