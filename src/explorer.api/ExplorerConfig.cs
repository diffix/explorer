namespace Explorer.Api
{
    using Aircloak.JsonApi;
    using System;
    using System.Threading.Tasks;

    public class ExplorerConfig : IAircloakAuthenticationProvider
    {
        public string AircloakApiKey { get; set; } = string.Empty;

        public uint PollFrequency { get; set; } = 2000;

        public TimeSpan PollFrequencyTimeSpan => TimeSpan.FromMilliseconds(PollFrequency);

        public string CommitHash { get; set; } = string.Empty;

        public string CommitRef { get; set; } = string.Empty;

        public Task<string> GetAuthToken() => Task.FromResult(AircloakApiKey);
    }
}