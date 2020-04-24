namespace Explorer.Common
{
    using System.Collections.Generic;
    using System.Linq;

    internal sealed class ValueCounts
    {
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

        public double SuppressedCountRatio => (double)SuppressedCount / TotalCount;

        // Note the `SuppressedCount` can be seen as a proxy for the number of suppressed rows in the original dataset.
        // We can use this to estimate the proportion of unqiue values that have been suppressed. This may be a better
        // metric for estimating the cardinality of a column than the `SuppressedCountRatio`
        public double SuppressedRowRatio => (double)SuppressedCount / TotalRows;

        public static ValueCounts Compute(IEnumerable<CountableRow> rows)
        {
            return rows.Aggregate(new ValueCounts(), AccumulateRow);
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

        private static ValueCounts AccumulateRow(ValueCounts vc, CountableRow row)
        {
            vc.AccumulateRow(row);
            return vc;
        }
    }
}
