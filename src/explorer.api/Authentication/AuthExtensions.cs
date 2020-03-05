namespace Explorer.Api.Authentication
{
    using Microsoft.AspNetCore.Mvc;

    internal static class AuthExtensions
    {
        public static void RegisterApiKey(this ControllerBase controller, string apiKey)
        {
            controller.HttpContext.Items["ApiKey"] = apiKey;
        }
    }
}
