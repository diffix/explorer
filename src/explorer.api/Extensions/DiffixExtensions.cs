namespace Explorer.Diffix.Extensions
{
    using System.Collections.Generic;
    using System.Linq;

    using Explorer.Diffix.Interfaces;

    internal static class DiffixExtensions
    {
        public static (long, long) CountTotalAndSuppressed<T>(this IEnumerable<T> valueCounts)
        where T : ICountAggregate, INullable, ISuppressible
        => valueCounts.Aggregate(
                (0L, 0L),
                (acc, next) => (
                    acc.Item1 + next.Count,
                    acc.Item2 + (next.IsSuppressed ? next.Count : 0L)));
    }
}