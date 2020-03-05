namespace Explorer.Api.Authentication
{
    using Aircloak.JsonApi;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.DependencyInjection;
    using System.Threading.Tasks;

    internal class ExplorerApiAuthProvider : IAircloakAuthenticationProvider
    {
        private readonly IHttpContextAccessor ctx;

        public ExplorerApiAuthProvider(IHttpContextAccessor ctx)
        {
            this.ctx = ctx;
        }

        public Task<string> GetAuthToken()
        {
            return Task.FromResult((string)ctx.HttpContext.Items["ApiKey"]);
        }

        public static void ConfigureServices(IServiceCollection services)
        {
            services.AddHttpContextAccessor();
        }
    }
}