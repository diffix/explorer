namespace Explorer.Api.Tests
{
    using System;
    using System.Threading.Tasks;
    using Aircloak.JsonApi;

    internal class TestConfig : IAircloakAuthenticationProvider
    {
        public string AircloakApiKey { get; set; } = string.Empty;

        public string DefaultApiUrl { get; set; } = string.Empty;

        public string VcrCassettePath { get; set; } = string.Empty;

        public int PollFrequency { get; set; } = 2000;

        public TimeSpan PollFrequencyTimeSpan => TimeSpan.FromMilliseconds(PollFrequency);

        public VcrSharp.VCRMode VcrMode { get; set; } = VcrSharp.VCRMode.Cache;

        public Task<string> GetAuthToken() => Task.FromResult(AircloakApiKey);
    }
}