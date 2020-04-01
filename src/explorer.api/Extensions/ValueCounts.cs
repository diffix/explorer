namespace Explorer.Diffix.Extensions
{
    using System.Collections.Generic;
    using System.Linq;
    using Explorer.Diffix.Interfaces;

    internal sealed class ValueCounts
    {
        private ValueCounts()
        {
        }

        public long TotalCount { get; private set; } = 0;

        public long SuppressedCount { get; private set; } = 0;

        public long TotalRows { get; private set; } = 0;

        public long SuppressedRows { get; private set; } = 0;

        public long NonSuppressedRows => TotalRows - SuppressedRows;

        public long NonSuppressedCount => TotalCount - SuppressedCount;

        public double SuppressedCountRatio => (double)SuppressedCount / TotalCount;

        public static ValueCounts Compute<T>(IEnumerable<T> rows)
            where T : ICountAggregate, ISuppressible
        {
            return rows.Aggregate(new ValueCounts(), AccumulateCounts);
        }

        private static ValueCounts AccumulateCounts<T>(ValueCounts vc, T row)
            where T : ICountAggregate, ISuppressible
        {
            vc.TotalCount += row.Count;
            vc.TotalRows++;
            if (row.IsSuppressed)
            {
                vc.SuppressedCount += row.Count;
                vc.SuppressedRows++;
            }
            return vc;
        }
    }
}