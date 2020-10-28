namespace VcrSharp
{
    using System;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;

    public enum VCRMode
    {
        /// <summary>
        /// Only use the local fixtures when executing network requests
        /// </summary>
        Playback,
        /// <summary>
        /// Use the cached response if found, otherwise fetch and store the result
        /// </summary>
        Cache,
        /// <summary>
        /// Avoid cached responses - use the network and store the result
        /// </summary>
        Record
    }

    public class ReplayingHandler : DelegatingHandler
    {
        private readonly Cassette cassette;

        private readonly RecordingOptions options;

        private readonly VCRMode vcrMode;

        public ReplayingHandler(
            HttpMessageHandler innerHandler,
            VCRMode vcrMode,
            Cassette cassette,
            RecordingOptions options)
        : base(innerHandler)
        {
            this.cassette = cassette;
            this.options = options;
            this.vcrMode = vcrMode;
        }

        public ReplayingHandler(
            VCRMode vcrMode,
            Cassette cassette,
            RecordingOptions options = RecordingOptions.SuccessOnly)
        : base()
        {
            this.cassette = cassette;
            this.options = options;
            this.vcrMode = vcrMode;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (vcrMode != VCRMode.Record)
            {
                var cachedResponse = await cassette.FindCachedResponse(request);
                if (cachedResponse.Found)
                {
                    return cachedResponse.Response;
                }
            }

            if (vcrMode == VCRMode.Playback)
            {
                throw new PlaybackException("A cached response was not found, and the environment is in playback mode which means the network cannot be accessed.");
            }

            var freshResponse = await base.SendAsync(request, cancellationToken);

            if (options == RecordingOptions.RecordAll ||
                options == RecordingOptions.SuccessOnly && freshResponse.IsSuccessStatusCode ||
                options == RecordingOptions.FailureOnly && !freshResponse.IsSuccessStatusCode)
            {
                await cassette.StoreCachedResponseAsync(request, freshResponse);
            }

            return freshResponse;
        }

        protected override void Dispose(bool disposing)
        {
            cassette.Dispose();
            base.Dispose(disposing);
        }
    }
}
