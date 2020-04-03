#pragma warning disable SA1402 // File may only contain a single type
namespace Explorer.Common
{
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Diffix;

    internal abstract class ExplorerBase
    {
        private readonly ConcurrentBag<ExploreMetric> metrics = new ConcurrentBag<ExploreMetric>();

        public IEnumerable<ExploreMetric> Metrics => metrics.ToArray();

        public abstract Task Explore(DConnection conn, ExplorerContext ctx);

        protected void PublishMetric(ExploreMetric metric) =>
            metrics.Add(metric);
    }

    internal abstract class ExplorerBase<Tctx> : ExplorerBase
        where Tctx : ExplorerContext
    {
        public abstract Task Explore(DConnection conn, Tctx ctx);

        public override Task Explore(DConnection conn, ExplorerContext ctx) =>
            this.Explore(conn, (Tctx)ctx);
    }
}
#pragma warning restore SA1402 // File may only contain a single type