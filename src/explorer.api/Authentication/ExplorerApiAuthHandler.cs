namespace Explorer.Api.Authentication
{
    using Aircloak.JsonApi;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.DependencyInjection;
    using System.Net.Http;
    using System.Threading.Tasks;

    internal class ExplorerApiAuthHandler : AircloakAuthenticationHandler
    {
        private readonly IHttpContextAccessor ctx;

        public ExplorerApiAuthHandler(IHttpContextAccessor ctx)
        {
            this.ctx = ctx;
        }

        public ExplorerApiAuthHandler(HttpMessageHandler innerHandler, IHttpContextAccessor ctx)
        : base(innerHandler)
        {
            this.ctx = ctx;
        }

        protected override Task<string> GetAuthToken()
        {
            return Task.FromResult((string)ctx.HttpContext.Items["ApiKey"]);
        }

        public static void ConfigureServices(IServiceCollection services)
        {
            services.AddHttpContextAccessor();
        }
    }
}