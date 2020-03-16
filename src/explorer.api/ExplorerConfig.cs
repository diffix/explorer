namespace Explorer.Api
{
    using System;

    public class ExplorerConfig
    {
        public Uri? AircloakApiUrl { get; set; }

        public string? ApiKeyEnvironmentVariable { get; set; }

        public uint PollFrequency { get; set; } = 2000;

        public TimeSpan PollFrequencyTimeSpan => TimeSpan.FromMilliseconds(PollFrequency);
    }
}