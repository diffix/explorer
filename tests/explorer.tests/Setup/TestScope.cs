namespace Explorer.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    using Aircloak.JsonApi;
    using Diffix;
    using Explorer.Common;
    using Lamar;
    using VcrSharp;
    using Xunit;

    public class TestScope : IDisposable
    {
        private bool disposedValue;

        public TestScope(
            Container rootContainer,
            Uri apiUri,
            string dataSource,
            string table,
            string column,
            DColumnInfo columnInfo,
            string vcrFileName,
            VCRMode vcrMode = VCRMode.Cache,
            RecordingOptions recordingOptions = RecordingOptions.SuccessOnly,
            int pollFrequencySecs = 2)
        : this(
            rootContainer,
            apiUri,
            dataSource,
            table,
            new[] { column },
            new[] { columnInfo },
            vcrFileName,
            vcrMode,
            recordingOptions,
            pollFrequencySecs)
        {
        }

        public TestScope(
            Container rootContainer,
            Uri apiUri,
            string dataSource,
            string table,
            IEnumerable<string> columns,
            IEnumerable<DColumnInfo> columnInfo,
            string vcrFileName,
            VCRMode vcrMode = VCRMode.Cache,
            RecordingOptions recordingOptions = RecordingOptions.SuccessOnly,
            int pollFrequencySecs = 2)
        {
#pragma warning disable CA2000 // Call System.IDisposable.Dispose on object (lifetime is managed by container.)
            var cts = new CancellationTokenSource();
            var cassette = new Cassette($"../../../.vcr/{vcrFileName}.yaml");
#pragma warning restore CA2000 // Call System.IDisposable.Dispose on object

            Scope = rootContainer.GetNestedContainer();

            Scope.InjectDisposable(cts);
            Scope.InjectDisposable(cassette);

            Connection = new AircloakConnection(
                Scope.GetInstance<JsonApiClient>(),
                apiUri,
                dataSource,
                TimeSpan.FromSeconds(pollFrequencySecs),
                cts.Token);

            Context = new ExplorerTestContext(Connection, dataSource, table, columns, columnInfo);
            Scope.Inject<ExplorerContext>(Context);

            var vcrFactory = Scope.GetInstance<VcrApiHttpClientFactory>();
            vcrFactory.VcrMode = vcrMode;
            vcrFactory.RecordingOptions = recordingOptions;
        }

        public ExplorerTestContext Context { get; }

        private INestedContainer Scope { get; }

        private AircloakConnection Connection { get; }

        public async Task<IEnumerable<TRow>> QueryRows<TQuery, TRow>(TQuery query)
        where TQuery : DQueryStatement, DResultParser<TRow>
        {
            var queryResult = await Context.Exec<TQuery, TRow>(query);
            return queryResult.Rows;
        }

        public async Task<IEnumerable<TRow>> QueryRows<TRow>(DQuery<TRow> query)
        {
            var queryResult = await Context.Exec(query);
            return queryResult.Rows;
        }

        public async Task CancelQueryAfter(int millisecondDelay)
        {
            await Task.Delay(millisecondDelay);
            Scope.GetInstance<CancellationTokenSource>().Cancel();
        }

        public async Task ResultTest<TComponent, TResult>(Action<TResult?> test)
        where TComponent : ResultProvider<TResult>
        where TResult : class
        {
            // Resolve the component using the interface to ensure correct scope
            var c = Scope.GetInstance<ResultProvider<TResult>>();
            Assert.IsType<TComponent>(c);

            var result = await c.ResultAsync;

            test(result);
        }

        public async Task MetricsTest<T>(Action<MetricsDictionary> test)
        where T : PublisherComponent
        {
            var publisher = Scope.ResolvePublisherComponent<T>();

            var metrics = new MetricsDictionary();
            await foreach (var m in publisher.YieldMetrics())
            {
                metrics[m.Name] = m;
            }

            test(metrics);
        }

        public void ConfigurePublisher<T>(Action<T> doSomething)
        where T : PublisherComponent
        {
            var publisher = Scope.ResolvePublisherComponent<T>();
            doSomething(publisher);
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

        public class MetricsDictionary : Dictionary<string, ExploreMetric>
        {
            public T FindMetric<T>(MetricDefinition<T> metricInfo)
            where T : notnull
            {
                if (!TryGetValue(metricInfo.Name, out var metric))
                {
                    throw new ArgumentException($"Value was not found for metric '{metricInfo.Name}`.");
                }
                if (!(metric.Metric is T metricValue))
                {
                    throw new ArgumentException($"Incorrect type specified for metric '{metricInfo.Name}`.");
                }
                return metricValue;
            }
        }
    }
}
