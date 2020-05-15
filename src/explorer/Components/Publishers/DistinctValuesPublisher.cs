namespace Explorer.Components
{
    using System.Collections.Generic;
    using System.Linq;

    using Explorer.Metrics;

    public class DistinctValuesPublisher : PublisherComponent
    {
        private const double SuppressedRatioThreshold = 0.1;

        private readonly ResultProvider<DistinctValuesComponent.Result> resultProvider;

        public DistinctValuesPublisher(
            MetricsPublisher publisher,
            ResultProvider<DistinctValuesComponent.Result> resultProvider)
        : base(publisher)
        {
            this.resultProvider = resultProvider;
        }

        public override async IAsyncEnumerable<ExploreMetric> YieldMetrics()
        {
            var result = await resultProvider.ResultAsync;
            var valueCounts = result.ValueCounts;

            if (valueCounts.SuppressedRowRatio < SuppressedRatioThreshold)
            {
                // Only few of the values are suppressed. This means the data is already well-segmented and can be
                // considered categorical or quasi-categorical.
                var distinctValues =
                    from row in result.DistinctRows
                    where row.HasValue
                    orderby row.Count descending
                    select new
                    {
                        row.Value,
                        row.Count,
                    };

                yield return new UntypedMetric(name: "distinct.is_categorical", metric: true);
                yield return new UntypedMetric(name: "distinct.values", metric: distinctValues);
                yield return new UntypedMetric(name: "distinct.null_count", metric: valueCounts.NullCount);
                yield return new UntypedMetric(name: "distinct.suppressed_count", metric: valueCounts.SuppressedCount);
                yield return new UntypedMetric(name: "distinct.value_count", metric: valueCounts.TotalCount);
            }
            else
            {
                yield return new UntypedMetric(name: "distinct.is_categorical", metric: false);
            }
        }
    }
}