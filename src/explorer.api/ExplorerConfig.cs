namespace Explorer.Api
{
    using System;

    public class ExplorerConfig
    {
        public const string ApiKeyEnvironmentVariable = "AIRCLOAK_API_KEY";

        public uint PollFrequency { get; set; } = 2000;

        public TimeSpan PollFrequencyTimeSpan => TimeSpan.FromMilliseconds(PollFrequency);
    }
}