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

        public ReplayingHandler(
            HttpMessageHandler innerHandler,
            Cassette cassette,
            RecordingOptions options)
        : base(innerHandler)
        {
            this.cassette = cassette;
            this.options = options;
        }

        public ReplayingHandler(
            Cassette cassette,
            RecordingOptions options = RecordingOptions.SuccessOnly)
        : this(new HttpClientHandler(), cassette, options)
        {
        }

        static VCRMode Parse(string mode)
        {
            if (string.IsNullOrWhiteSpace(mode))
            {
                return VCRMode.Cache;
            }

            var text = mode.Trim();
            if (text.Equals("playback", StringComparison.OrdinalIgnoreCase))
            {
                return VCRMode.Playback;
            }

            if (text.Equals("cache", StringComparison.OrdinalIgnoreCase))
            {
                return VCRMode.Cache;
            }

            if (text.Equals("record", StringComparison.OrdinalIgnoreCase))
            {
                return VCRMode.Record;
            }

            return VCRMode.Playback;
        }

        VCRMode? vcrMode;
        public VCRMode CurrentVCRMode
        {
            get
            {
                if (!vcrMode.HasValue)
                {
                    vcrMode = Parse(Environment.GetEnvironmentVariable("VCR_MODE"));
                }
                return vcrMode.Value;
            }
            set => vcrMode = value;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (CurrentVCRMode != VCRMode.Record)
            {
                var cachedResponse = await cassette.FindCachedResponse(request);
                if (cachedResponse.Found)
                {
                    return cachedResponse.Response;
                }
            }

            if (CurrentVCRMode == VCRMode.Playback)
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
    }
}
