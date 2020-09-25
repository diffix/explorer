namespace Explorer.Components
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.Json;
    using System.Threading.Tasks;

    using Explorer.Common;
    using Explorer.Common.Utils;
    using Explorer.Metrics;
    using Explorer.Queries;

    public class DistinctValuesComponent
        : ExplorerComponent<DistinctValuesComponent.Result>, PublisherComponent
    {
        private const int DefaultNumValuesToPublish = 10;
        private const double SuppressedRatioThreshold = 0.01;

        public int NumValuesToPublish { get; set; } = DefaultNumValuesToPublish;

        public IEnumerable<ExploreMetric> YieldMetrics(Result result)
        {
            if (!result.IsCategorical)
            {
                yield return ExploreMetric.Create(MetricDefinitions.IsCategorical, false);
            }
            else
            {
                // Only few of the values are suppressed. This means the data is already well-segmented and can be
                // considered categorical or quasi-categorical.
                var distinctValues =
                    from row in result.DistinctRows
                    where row.HasValue
                    orderby row.Count descending
                    select new ValueWithCount<JsonElement>(row.Value, row.Count);

                var toPublish = distinctValues.Take(NumValuesToPublish);
                var remaining = distinctValues.Skip(NumValuesToPublish);

                if (remaining.Any())
                {
                    using var jdoc = JsonDocument.Parse("\"--OTHER--\"");
                    toPublish = toPublish.Append(new ValueWithCount<JsonElement>(
                        jdoc.RootElement.Clone(),
                        remaining.Sum(distinct => distinct.Count)));
                }

                var categoricalData = new CategoricalData(new CategoricalData.ValuesListType(toPublish), result.ValueCounts);
                yield return ExploreMetric.Create(MetricDefinitions.CategoricalData, categoricalData);
                yield return ExploreMetric.Create(MetricDefinitions.IsCategorical, true);
            }
        }

        public async IAsyncEnumerable<ExploreMetric> YieldMetrics()
        {
            var result = await ResultAsync;
            if (result == null)
            {
                yield break;
            }

            foreach (var m in YieldMetrics(result))
            {
                yield return m;
            }
        }

        protected override async Task<Result?> Explore()
        {
            if (Context.ColumnInfo.UserId)
            {
                return new Result(Enumerable.Empty<ValueWithCountRow<JsonElement>>());
            }
            var distinctValueResult = await Context.Exec(new DistinctColumnValues());
            return new Result(distinctValueResult.Rows.OrderByDescending(r => r.Count));
        }

        public class Result
        {
            public Result(IEnumerable<ValueWithCountRow<JsonElement>> distinctRows)
            {
                DistinctRows = distinctRows.ToList();
                ValueCounts = ValueCounts.Compute(DistinctRows);
            }

            public List<ValueWithCountRow<JsonElement>> DistinctRows { get; }

            public ValueCounts ValueCounts { get; }

            /// <summary>
            /// Gets a value indicating whether the columns contains categorical data or not.
            /// A high count of suppressed values means that there are many values which are not part
            /// of any bucket, so the column is not categorical.
            /// The maximum number of categories is also limited logarithmically by the total number of values, i.e.:
            /// 100 values - 37 categories; 10_000 values - 55 categories; 1_000_000 values - 72 categories; 1_000_000_000 values - 98 categories.
            /// </summary>
            public bool IsCategorical =>
                ValueCounts.SuppressedCountRatio < SuppressedRatioThreshold &&
                ValueCounts.NonSuppressedRows <= 20 + System.Math.Log(ValueCounts.NonSuppressedCount, 1.3);
        }
    }
}