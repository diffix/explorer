namespace Explorer.Common
{
    using System;
    using System.Collections.Generic;

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

        public void AddValueCount(T value, long count)
        {
            Add((value, TotalCount + count));
        }

        public T GetRandomValue(Random rand, T @default)
        {
            if (Count == 0)
            {
                return @default;
            }
            var rcount = rand.NextLong(TotalCount);
            return FindSubstring(rcount);
        }

        private T FindSubstring(long count)
        {
            var left = 0;
            var right = Count - 1;
            while (true)
            {
                var middle = (left + right) / 2;
                if (middle == 0 || middle == Count - 1)
                {
                    return this[middle].Value;
                }
                if (count < this[middle].Count)
                {
                    if (count >= this[middle - 1].Count)
                    {
                        return this[middle - 1].Value;
                    }
                    right = middle;
                }
                else if (count > this[middle].Count)
                {
                    if (count <= this[middle + 1].Count)
                    {
                        return this[middle].Value;
                    }
                    left = middle;
                }
                else
                {
                    return this[middle].Value;
                }
            }
        }
    }
}