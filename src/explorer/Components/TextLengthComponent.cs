namespace Explorer.Components
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using Diffix;
    using Explorer.Common;
    using Explorer.Metrics;
    using Explorer.Queries;

    public class TextLengthComponent
        : ExplorerComponent<TextLengthComponent.Result>, PublisherComponent
    {
        private readonly ExplorerContext ctx;

        public TextLengthComponent(ExplorerContext ctx)
        {
            this.ctx = ctx;
        }

        public async IAsyncEnumerable<ExploreMetric> YieldMetrics()
        {
            var result = await ResultAsync;

            if (result.Success)
            {
                yield return new UntypedMetric("text.length.success", "true");

                yield return new UntypedMetric(
                    "text.length.values",
                    result.DistinctResult!.DistinctRows
                        .Where(r => r.HasValue)
                        .OrderBy(r => r.Value.GetInt32())
                        .Select(r => new { r.Value, r.Count }));
                yield return new UntypedMetric("text.length.counts", result.DistinctResult.ValueCounts);
            }
            else
            {
                yield return new UntypedMetric("text.length.success", "false");
            }
        }

        protected override async Task<Result> Explore()
        {
            if (ctx.ColumnInfo.Isolating)
            {
                return Result.Failed();
            }

            var distinctResult = await ctx.Exec(new DistinctLengths());

            return Result.Ok(new DistinctValuesComponent.Result(distinctResult.Rows));
        }

        public sealed class Result
        {
            private Result(bool success)
            {
                Success = success;
            }

            public DistinctValuesComponent.Result? DistinctResult { get; private set; }

            public bool Success { get; }

            public static Result Failed()
            {
                return new Result(false);
            }

            public static Result Ok(DistinctValuesComponent.Result distinctResult)
            {
                return new Result(true)
                {
                    DistinctResult = distinctResult,
                };
            }
        }
    }
}