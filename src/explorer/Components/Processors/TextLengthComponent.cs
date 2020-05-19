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
        private readonly DConnection conn;
        private readonly ExplorerContext ctx;
        private readonly ResultProvider<IsolatorCheckComponent.Result> isolatorCheck;

        public TextLengthComponent(
            DConnection conn,
            ExplorerContext ctx,
            ResultProvider<IsolatorCheckComponent.Result> isolatorCheck)
        {
            this.ctx = ctx;
            this.conn = conn;
            this.isolatorCheck = isolatorCheck;
        }

        public async IAsyncEnumerable<ExploreMetric> YieldMetrics()
        {
            var result = await ResultAsync;

            if (result.Success)
            {
                yield return new UntypedMetric("text.length.success", "true");

                yield return new UntypedMetric(
                    "text.length.histogram",
                    result.Histogram!.Buckets.Values.Select(b => new
                    {
                        Length = b.LowerBound,
                        b.Count,
                    }));

                yield return new UntypedMetric("text.length.quartiles", result.Quartiles!);
            }
            else
            {
                yield return new UntypedMetric("text.length.success", "false");
            }
        }

        protected override async Task<Result> Explore()
        {
            var isolator = await isolatorCheck.ResultAsync;

            if (isolator.IsIsolatorColumn)
            {
                return Result.Failed();
            }

            var histogramQuery = new SingleColumnHistogram(
                ctx.Table, $"length{ctx.Column}", new List<decimal> { 1 });
            var histogramResult = await conn.Exec(histogramQuery);

            var histogram = Histogram.FromQueryRows(histogramResult.Rows).First();
            var quartiles = await QuartileEstimator.EstimateQuartiles(histogram);

            return Result.Ok(histogram, quartiles);
        }

        public class Result
        {
            private Result(bool success)
            {
                Success = success;
            }

            public Histogram? Histogram { get; private set; }

            public List<double>? Quartiles { get; private set; }

            public bool Success { get; }

            public static Result Failed()
            {
                return new Result(false);
            }

            public static Result Ok(
                Histogram histogram,
                List<double> quartiles)
            {
                return new Result(true)
                {
                    Histogram = histogram,
                    Quartiles = quartiles,
                };
            }
        }
    }
}