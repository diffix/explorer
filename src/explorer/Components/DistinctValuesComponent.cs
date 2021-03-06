namespace Explorer.Components
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.Json;
    using System.Threading.Tasks;

    using Accord.Statistics.Distributions.Univariate;
    using Explorer;
    using Explorer.Common;
    using Explorer.Metrics;
    using Explorer.Queries;

    using Microsoft.Extensions.Options;

    public class DistinctValuesComponent
        : ExplorerComponent<DistinctValuesComponent.Result>, PublisherComponent
    {
        private static readonly JsonElement JsonNull = Utilities.MakeJsonNull();

        private readonly ExplorerOptions options;

        public DistinctValuesComponent(IOptions<ExplorerOptions> options)
        {
            this.options = options.Value;
        }

        private int NumValuesToPublish => options.DistinctValuesToPublish;

        public IEnumerable<ExploreMetric> YieldMetrics(Result result)
        {
            // Only few of the values are suppressed. This means the data is already well-segmented and can be
            // considered categorical or quasi-categorical.
            var distinctValues =
                from row in result.DistinctRows
                where !row.IsSuppressed
                orderby row.Count descending
                select new
                {
                    Value = row.IsNull ? JsonNull : row.Value,
                    row.Count,
                };

            var toPublish = distinctValues.Take(NumValuesToPublish);
            var remaining = distinctValues.Skip(NumValuesToPublish);

            if (remaining.Any())
            {
                using var jdoc = JsonDocument.Parse("\"--OTHER--\"");
                toPublish = toPublish.Append(new
                {
                    Value = jdoc.RootElement.Clone(),
                    Count = remaining.Sum(distinct => distinct.Count),
                });
            }

            var valueCounts = result.ValueCounts;
            yield return new UntypedMetric(name: "distinct.is_categorical", metric: result.ValueCounts.IsCategorical);
            yield return new UntypedMetric(name: "distinct.values", metric: toPublish.ToList());
            yield return new UntypedMetric(name: "distinct.null_count", metric: valueCounts.NullCount);
            yield return new UntypedMetric(name: "distinct.suppressed_count", metric: valueCounts.SuppressedCount);
            yield return new UntypedMetric(name: "distinct.value_count", metric: valueCounts.TotalCount);
            if (result.DecimalsCountDistribution != null)
            {
                yield return new UntypedMetric(
                    name: "distinct.decimals_count_distribution",
                    metric: result.DecimalsCountDistribution,
                    invisible: true);
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
                return new Result(Enumerable.Empty<ValueWithCount<JsonElement>>(), null);
            }
            var distinctValueResult = await Context.Exec(new DistinctColumnValues());
            var decimalsCountDistribution = GetDecimalsCountDistribution(distinctValueResult.Rows);
            return new Result(distinctValueResult.Rows.OrderByDescending(r => r.Count), decimalsCountDistribution);
        }

        private NumericDistribution? GetDecimalsCountDistribution(IEnumerable<ValueWithCount<JsonElement>> distinctRows)
        {
            if (Context.ColumnInfo.Type != Diffix.DValueType.Real)
            {
                return null;
            }

            static int DecimalsCount(decimal val)
            {
                val = Math.Abs(val);
                var i = 0;
                while (Math.Abs(Math.Round(val, i) - val) > 1e-8m)
                {
                    i++;
                }
                return i;
            }

            var decimalCounts = distinctRows
                    .Where(r => r.HasValue)
                    .Select(r => (double)DecimalsCount(r.Value.GetDecimal()))
                    .DefaultIfEmpty(2)
                    .ToArray();

            return new NumericDistribution(new EmpiricalDistribution(decimalCounts));
        }

        public class Result
        {
            public Result(IEnumerable<ValueWithCount<JsonElement>> distinctRows, NumericDistribution? decimalsCountDistribution)
            {
                DistinctRows = distinctRows.ToList();
                ValueCounts = ValueCounts.Compute(DistinctRows);
                DecimalsCountDistribution = decimalsCountDistribution;
            }

            public List<ValueWithCount<JsonElement>> DistinctRows { get; }

            public ValueCounts ValueCounts { get; }

            public NumericDistribution? DecimalsCountDistribution { get; }
        }
    }
}