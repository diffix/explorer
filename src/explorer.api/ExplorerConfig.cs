namespace Explorer.Api
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Aircloak.JsonApi;
    using Explorer.Components;
    using Explorer.Metrics;

    public class ExplorerConfig : IAircloakAuthenticationProvider
    {
        public string AircloakApiKey { get; set; } = string.Empty;

        public uint PollFrequency { get; set; } = 2000;

        public TimeSpan PollFrequencyTimeSpan => TimeSpan.FromMilliseconds(PollFrequency);

        public string? CommitHash { get; set; }

        public string CommitRef { get; set; } = string.Empty;

        public Task<string> GetAuthToken() => Task.FromResult(AircloakApiKey);
    }
}