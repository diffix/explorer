namespace Explorer.Api
{
    using System;

    public class ExplorerConfig
    {
        public const string AircloakApiUrlEnvironmentVariable = "AIRCLOAK_API_URL";

        public const string ApiKeyEnvironmentVariable = "AIRCLOAK_API_KEY";

        public Uri? AircloakApiUrlDefault { get; set; }

        public uint PollFrequency { get; set; } = 2000;

        public TimeSpan PollFrequencyTimeSpan => TimeSpan.FromMilliseconds(PollFrequency);

        public Uri AircloakApiUrl()
        {
            var fromEnv =
                Environment.GetEnvironmentVariable(AircloakApiUrlEnvironmentVariable);

            if (!string.IsNullOrEmpty(fromEnv))
            {
                // If there is no trailing slash, add one, otherwise it messes up api request routing
                return new Uri(fromEnv.EndsWith('/') ? fromEnv : fromEnv + '/');
            }

            return AircloakApiUrlDefault ??
                    throw new Exception(
                        $"Aircloak Api Url needs to be set either using the {AircloakApiUrlEnvironmentVariable} " +
                        $"environment variable or the {nameof(AircloakApiUrlDefault)} config item.");
        }
    }
}