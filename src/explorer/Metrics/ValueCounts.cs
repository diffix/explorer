namespace Explorer.Metrics
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Explorer.Common.Utils;

    public sealed class ValueCounts
    {
        private ValueCounts()
        {
        }

        public long TotalCount { get; private set; }

        public long SuppressedCount { get; private set; }

        public long NullCount { get; private set; }

        public long TotalRows { get; private set; }

        public long SuppressedRows { get; private set; }

        public long NullRows { get; private set; }

        public long NonSuppressedRows => TotalRows - SuppressedRows;

        public long NonSuppressedCount => TotalCount - SuppressedCount;

        public long NonSuppressedNonNullCount => TotalCount - SuppressedCount - NullCount;

        public double SuppressedCountRatio => TotalCount == 0 ? 1 : (double)SuppressedCount / TotalCount;

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
