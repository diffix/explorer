namespace Explorer.Tests
{
    using System;
    using System.Threading;
    using System.Runtime.CompilerServices;

    using Aircloak.JsonApi;
    using Diffix;
    using Lamar;

    public class TestScope : IDisposable
    {
        protected readonly CancellationTokenSource cts = new CancellationTokenSource();
        private bool disposedValue;

        public INestedContainer Scope { get; }

        public TestScope(Container rootContainer)
        {
            Scope = rootContainer.GetNestedContainer();
            Scope.Inject(cts);
        }

        public TestScope LoadCassette([CallerMemberName] string testName = "")
        {
            Scope.Inject(new VcrSharp.Cassette($"../../../.vcr/{GetType()}.{testName}.yaml"));

            return this;
        }

        public TestScope OverrideVcrOptions(
            VcrSharp.VCRMode vcrMode = VcrSharp.VCRMode.Cache,
            VcrSharp.RecordingOptions recordingOptions = VcrSharp.RecordingOptions.SuccessOnly)
        {
            var vcrFactory = Scope.GetInstance<VcrApiHttpClientFactory>();
            vcrFactory.VcrMode = vcrMode;
            vcrFactory.RecordingOptions = recordingOptions;

            return this;
        }

        public QueryableTestScope WithConnectionParams(
            string dataSourceName,
            int pollFrequencySecs = 2)
        {
            Scope.Inject<DConnection>(
                new AircloakConnection(
                Scope.GetInstance<JsonApiClient>(),
                dataSourceName,
                TimeSpan.FromSeconds(pollFrequencySecs),
                cts.Token));
            return new QueryableTestScope(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    cts.Dispose();
                    Scope.Dispose();
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
