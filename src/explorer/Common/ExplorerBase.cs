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

        public virtual void PublishMetric(ExploreMetric metric) =>
            metrics.Add(metric);
    }
}