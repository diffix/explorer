namespace Explorer.Common
{
    using System.Collections.Generic;
    using System.Linq;

    public sealed class ValueCounts
    {
        private const double SuppressedRatioThreshold = 0.01;

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

        /// <summary>
        /// Gets a value indicating whether the columns contains categorical data or not.
        /// A high count of suppressed values means that there are many values which are not part
        /// of any bucket, so the column is not categorical.
        /// </summary>
        public bool IsCategorical => SuppressedCountRatio < SuppressedRatioThreshold;

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
