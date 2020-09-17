namespace Explorer.Api
{
    using System;
    using System.Threading.Tasks;

    using Aircloak.JsonApi;

    public class ExplorerConfig : IAircloakAuthenticationProvider
    {
        public string AircloakApiKey { get; set; } = string.Empty;

        public uint PollFrequency { get; set; } = 2000;

        public TimeSpan PollFrequencyTimeSpan => TimeSpan.FromMilliseconds(PollFrequency);

        public Task<string> GetAuthToken() => Task.FromResult(AircloakApiKey);
    }
}