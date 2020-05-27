namespace Explorer.Tests
{
    using System;
    using System.Net.Http;
    using VcrSharp;

    public class VcrApiHttpClientFactory : IHttpClientFactory
    {
        private const string UrlEnvironmentVariable = "AIRCLOAK_API_URL";
        private const string DefaultTestUrl = "https://attack.aircloak.com/api/";
        private const VCRMode DefaultVcrMode = VCRMode.Cache;
        private const RecordingOptions DefaultRecordingOptions = RecordingOptions.SuccessOnly;

        public VcrApiHttpClientFactory(Cassette cassette)
        {
            Cassette = cassette;
            VcrMode = DefaultVcrMode;
            RecordingOptions = DefaultRecordingOptions;
            var urlString = Environment.GetEnvironmentVariable(UrlEnvironmentVariable)
                    ?? DefaultTestUrl;
            ApiBaseAddress = new Uri(urlString);
        }

        public Cassette Cassette { get; }

        public VCRMode VcrMode { get; set; }

        public RecordingOptions RecordingOptions { get; set; }

        public Uri ApiBaseAddress { get; set; }

        public HttpClient? HttpClient { get; private set; }

        public HttpClient CreateClient(string name)
        {
            return HttpClient ??= CreateClient();
        }

        private HttpClient CreateClient()
        {
            // Note: This will create a new HttpClient per test - normally we wouldn't want this
            // (see https://docs.microsoft.com/en-us/aspnet/core/fundamentals/http-requests?view=aspnetcore-3.1)
            // ie. it's recommended to use a single HttpClient for the entire lifetime of the application.
            // However in this case, we want to attach a custom handler (the vcr) for each test scope. There
            // doesn't seem to be a way to do this with a singleton HttpClient. Instead, we attach the lifetime of
            // HttpClient to the lifetime of this factory.
            if (Cassette is null)
            {
                return new HttpClient()
                {
                    BaseAddress = ApiBaseAddress,
                };
            }
            else
            {
#pragma warning disable CA2000 // Call System.IDisposable.Dispose on object
                // lifetime is managed by HttpClient object
                var vcr = new ReplayingHandler(new SocketsHttpHandler(), VcrMode, Cassette, RecordingOptions);
#pragma warning restore CA2000 // Call System.IDisposable.Dispose on object
                return new HttpClient(vcr)
                {
                    BaseAddress = ApiBaseAddress,
                };
            }
        }
    }
}
