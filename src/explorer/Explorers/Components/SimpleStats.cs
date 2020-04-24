namespace Explorer.Explorers.Components
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using Diffix;
    using Explorer.Common;
    using Explorer.Queries;

    internal class SimpleStats<T> : ExplorerComponent<SimpleStats<T>.Result>
    {
        public SimpleStats(DConnection conn, ExplorerContext ctx)
        : base(conn, ctx)
        {
        }

        protected override async Task<SimpleStats<T>.Result> Explore()
        {
            var statsQ = await Conn.Exec(new BasicColumnStats<T>(Ctx.Table, Ctx.Column));

            return new Result(statsQ.Rows.Single());
        }

        public class Result : Metrics.MetricsProvider
        {
            public Result(BasicColumnStats<T>.Result stats)
            {
                Stats = stats;
            }

            public BasicColumnStats<T>.Result Stats { get; }

            public long Count { get => Stats.Count; }

            public T Min { get => Stats.Min; }

            public T Max { get => Stats.Max; }

            public IEnumerable<ExploreMetric> Metrics()
            {
                yield return new UntypedMetric("count", Count);
                yield return new UntypedMetric("naive_min", Min!);
                yield return new UntypedMetric("naive_max", Max!);
            }
        }
    }
}