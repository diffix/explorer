namespace Explorer.Components
{
    using System.Collections.Generic;
    using System.Linq;

    using Explorer.Metrics;

    public class DistinctValuesPublisher : PublisherComponent<DistinctValuesComponent.Result>
    {
        private const double SuppressedRatioThreshold = 0.1;

        public DistinctValuesPublisher(ResultProvider<DistinctValuesComponent.Result> resultProvider)
        : base(resultProvider)
        {
        }

        public override IEnumerable<ExploreMetric> YieldMetrics(DistinctValuesComponent.Result result)
        {
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