namespace Explorer.Tests
{
    using System;
    using System.Runtime.CompilerServices;
    using System.Threading;

    using Aircloak.JsonApi;
    using Diffix;
    using Lamar;

    public class TestScope : IDisposable
    {
        private bool disposedValue;

        public TestScope(Container rootContainer)
        {
            Scope = rootContainer.GetNestedContainer();
#pragma warning disable CA2000 // Call System.IDisposable.Dispose on object (Object lifetime is managed by container.)
            Scope.Inject(new CancellationTokenSource());
#pragma warning restore CA2000 // Call System.IDisposable.Dispose on object
        }

        public INestedContainer Scope { get; }

        public TestScope LoadCassette([CallerMemberName] string testName = "")
        {
#pragma warning disable CA2000 // Call System.IDisposable.Dispose on object (Object lifetime is managed by container.)
            Scope.Inject(new VcrSharp.Cassette($"../../../.vcr/{GetType()}.{testName}.yaml"));
#pragma warning restore CA2000 // Call System.IDisposable.Dispose on object

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
                Scope.GetInstance<CancellationTokenSource>().Token));
            return new QueryableTestScope(this);
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
