#nullable enable
namespace VcrSharp
{
    using System.Net.Http;
    using Microsoft.Extensions.Options;

    public class VcrApiHttpClientFactory : IHttpClientFactory
    {
        private readonly VcrOptions vcrOptions;

        public VcrApiHttpClientFactory(Cassette cassette, IOptions<VcrOptions> vcrOptions)
        {
            Cassette = cassette;
            this.vcrOptions = vcrOptions.Value;
        }

        public Cassette Cassette { get; }

        public VCRMode VcrMode => vcrOptions.VcrMode;

        public RecordingOptions RecordingOptions => vcrOptions.RecordingOptions;

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
                    BaseAddress = vcrOptions.HttpClientBaseAddress,
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
                    BaseAddress = vcrOptions.HttpClientBaseAddress,
                };
            }
        }
    }
}
#nullable disable