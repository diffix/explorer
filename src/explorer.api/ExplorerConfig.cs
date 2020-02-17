namespace Explorer.Api
{
    using System;

    public class ExplorerConfig
    {
        public ExplorerConfig()
        {
            AircloakApiKey = Environment.GetEnvironmentVariable("AIRCLOAK_API_KEY")
                ?? throw new Exception("Environment variable AIRCLOAK_API_KEY must be set.");
        }

        public Uri? AircloakApiUrl { get; set; }

        public string AircloakApiKey { get; set; }
    }
}