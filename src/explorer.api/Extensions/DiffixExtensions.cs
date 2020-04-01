namespace Explorer.Diffix.Extensions
{
    using System.Collections.Generic;
    using System.Linq;

    using Explorer.Diffix.Interfaces;

    internal static class DiffixExtensions
    {
        public static CountResultType CountTotalAndSuppressed<T>(this IEnumerable<T> valueCounts)
        where T : ICountAggregate, ISuppressible
        => valueCounts.Aggregate(
                default(CountResultType),
                (acc, row) => new CountResultType(acc, row.Count, row.IsSuppressed));
    }

#pragma warning disable CA1815 // Struct type should override Equals
#pragma warning disable SA1201 // A struct should not follow a class
    public struct CountResultType
    {
        public CountResultType(CountResultType cr, long count, bool isSuppressed)
        {
            TotalCount = cr.TotalCount + count;
            TotalRows = cr.TotalRows + 1;
            if (isSuppressed)
            {
                SuppressedCount = cr.SuppressedCount + count;
                SuppressedRows = cr.SuppressedRows + 1;
            }
            else
            {
                SuppressedCount = cr.SuppressedCount;
                SuppressedRows = cr.SuppressedRows;
            }
        }

        public long TotalCount { get; }

        public long SuppressedCount { get; }

        public long TotalRows { get; }

        public long SuppressedRows { get; }

        public long NonSuppressedRows => TotalRows - SuppressedRows;

        public long NonSuppressedCount => TotalCount - SuppressedCount;

        public double SuppressedCountRatio => (double)SuppressedCount / TotalCount;
    }
#pragma warning restore CA1815 // Struct type should override Equals
#pragma warning restore SA1201 // A struct should not follow a class
}