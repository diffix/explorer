namespace Explorer.Api
{
    using System;

    public class ExplorerConfig
    {
        public Uri? AircloakApiUrl { get; set; }

        public string? ApiKeyEnvironmentVariable { get; set; }
    }
}