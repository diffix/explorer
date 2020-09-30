namespace Explorer.Metrics
{
    using System.Collections.Generic;

    public class DateTimeMetric<T>
    {
        public DateTimeMetric(long total, long suppressed, List<ValueWithCountAndNoise<T>> counts)
        {
            Total = total;
            Suppressed = suppressed;
            Counts = counts;
        }

        public long Total { get; }

        public long Suppressed { get; }

        public IReadOnlyList<ValueWithCountAndNoise<T>> Counts { get; }
    }
}