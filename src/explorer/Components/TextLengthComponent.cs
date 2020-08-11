namespace Explorer.Components
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.Json;
    using System.Threading.Tasks;

    using Diffix;
    using Explorer.Common;
    using Explorer.Metrics;
    using Explorer.Queries;

    public class TextLengthComponent
        : ExplorerComponent<TextLengthComponent.Result>, PublisherComponent
    {
        public async IAsyncEnumerable<ExploreMetric> YieldMetrics()
        {
            var result = await ResultAsync;
            if (result == null)
            {
                yield break;
            }

            yield return new UntypedMetric(
                "text.length.values",
                result.DistinctRows
                    .Where(r => r.HasValue)
                    .OrderBy(r => r.Value.GetInt32())
                    .Select(r => new { r.Value, r.Count })
                    .ToList());
            yield return new UntypedMetric("text.length.counts", result.ValueCounts);
        }

        protected override async Task<Result?> Explore()
        {
            if (Context.ColumnInfo.Isolating)
            {
                return null;
            }

            var distinctResult = await Context.Exec(new DistinctLengths());
            return new Result(distinctResult.Rows);
        }

        public sealed class Result
        {
            public Result(IEnumerable<ValueWithCount<JsonElement>> distinctRows)
            {
                DistinctRows = distinctRows;
                ValueCounts = ValueCounts.Compute(distinctRows);
            }

            public IEnumerable<ValueWithCount<JsonElement>> DistinctRows { get; }

            public ValueCounts ValueCounts { get; }
        }
    }
}