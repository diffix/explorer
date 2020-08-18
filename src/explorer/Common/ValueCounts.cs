namespace Explorer.Common
{
    using System.Collections.Generic;
    using System.Linq;

    public sealed class ValueCounts
    {
        private const double SuppressedRatioThreshold = 0.1;

        private ValueCounts()
        {
        }

        public long TotalCount { get; private set; } = 0;

        public long SuppressedCount { get; private set; } = 0;

        public long NullCount { get; private set; } = 0;

        public long TotalRows { get; private set; } = 0;

        public long SuppressedRows { get; private set; } = 0;

        public long NullRows { get; private set; } = 0;

        public long NonSuppressedRows => TotalRows - SuppressedRows;

        public long NonSuppressedCount => TotalCount - SuppressedCount;

        public long NonSuppressedNonNullCount => TotalCount - SuppressedCount - NullCount;

        public double SuppressedCountRatio => TotalCount == 0 ? 1 : (double)SuppressedCount / TotalCount;

        // Note the `SuppressedCount / 4` can be seen as a proxy for the number of suppressed rows in the original dataset,
        // where 4 average for the low-value filter, so a good estimate for the average count in a suppressed bucket is in fact 2.
        // We can use this to estimate the proportion of unqiue values that have been suppressed. This may be a better
        // metric for estimating the cardinality of a column than the `SuppressedCountRatio`
        public double SuppressedRowRatio => TotalRows == 0 ? 1 : (double)SuppressedCount / 2 / TotalRows;

        public bool IsCategorical => SuppressedRowRatio < SuppressedRatioThreshold;

        public static ValueCounts Compute(IList<CountableRow> rows)
        {
            return rows.Aggregate(new ValueCounts(), AccumulateRow);
        }

        public static ValueCounts Compute(IEnumerable<CountableRow> rows)
        {
            return Compute(rows.OrderBy(r => r.Count).ToList());
        }

        public ValueCounts AccumulateRow(CountableRow row)
        {
            TotalCount += row.Count;
            TotalRows++;

            if (row.IsSuppressed)
            {
                SuppressedCount += row.Count;
                SuppressedRows++;
            }
            if (row.IsNull)
            {
                NullCount += row.Count;
                NullRows++;
            }

            return this;
        }

        private static ValueCounts AccumulateRow<T>(ValueCounts vc, T row)
        where T : CountableRow
        {
            vc.AccumulateRow(row);
            return vc;
        }
    }
}
