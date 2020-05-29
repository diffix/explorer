namespace Explorer.Api.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    using Explorer.Metrics;
    using Lamar;
    using VcrSharp;
    using Xunit;

    public class ExplorationTestScope : IDisposable
    {
        private bool disposedValue;

        public ExplorationTestScope(Container rootContainer)
        {
            Scope = rootContainer.GetNestedContainer();
        }

        public INestedContainer Scope { get; }

        public ExplorationTestScope LoadCassette(string testFileName)
        {
#pragma warning disable CA2000 // Call System.IDisposable.Dispose on object (Object lifetime is managed by container.)
            Scope.InjectDisposable(new Cassette($"../../../.vcr/{testFileName}.yaml"));
#pragma warning restore CA2000 // Call System.IDisposable.Dispose on object

            return this;
        }

        public async Task RunExploration(
            string dataSourceName,
            string table,
            string column)
        {
            var exploreParams = new Models.ExploreParams
            {
                DataSourceName = dataSourceName,
                TableName = table,
                ColumnName = column,
            };

            var task = Scope.GetInstance<ExplorationLauncher>()
                .LaunchExploration(exploreParams, CancellationToken.None);

            await task;
            Assert.True(task.IsCompletedSuccessfully);
        }

        public void CheckMetrics(Action<IEnumerable<ExploreMetric>> testMetrics)
        {
            var publisher = Scope.GetInstance<MetricsPublisher>();

            testMetrics(publisher.PublishedMetrics);
        }

        public async Task RunAndCheckMetrics(
            string dataSourceName,
            string table,
            string column,
            Action<IEnumerable<ExploreMetric>> testMetrics)
        {
            await RunExploration(dataSourceName, table, column);
            CheckMetrics(testMetrics);
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Scope.Dispose();
                }

                disposedValue = true;
            }
        }
    }
}
