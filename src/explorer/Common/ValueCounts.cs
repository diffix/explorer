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

        public double SuppressedCountRatio => (double)SuppressedCount / TotalCount;

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
