namespace Explorer.Api
{
    using System;
    using System.Net.Http;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;

    public static class Startup
    {
        public static void ConfigureExplorer(this IWebHostBuilder builder)
        {
            ConfigureExplorer(builder, null);
        }

        public static void ConfigureExplorer(this IWebHostBuilder builder, Func<DelegatingHandler>? configureHandler)
        {
            var eb = new ExplorerWebHostBuilderConfig { ConfigureHandler = configureHandler };
            builder.ConfigureServices(eb.ConfigureServices)
                .ConfigureServices(eb.ConfigureHttpClient)
                .Configure(eb.Configure);
        }

        internal class ExplorerWebHostBuilderConfig
        {
            public Func<DelegatingHandler>? ConfigureHandler { get; set; }

            public void ConfigureServices(WebHostBuilderContext ctx, IServiceCollection services)
            {
                _ = ctx;
                services.AddControllers();
            }

            public void Configure(WebHostBuilderContext ctx, IApplicationBuilder app)
            {
                if (ctx.HostingEnvironment.IsDevelopment())
                {
                    app.UseDeveloperExceptionPage();
                }
                app.UseRouting();
                app.UseAuthorization();
                app.UseEndpoints(endpoints => endpoints.MapControllers());
            }

            public void ConfigureHttpClient(WebHostBuilderContext ctx, IServiceCollection services)
            {
                var config = ctx.Configuration.GetSection("Explorer").Get<ExplorerConfig>();

                var clientBuilder = services.AddHttpClient<Aircloak.JsonApi.JsonApiClient>(client =>
                {
                    client.BaseAddress = config.AircloakApiUrl;
                    if (!client.DefaultRequestHeaders.TryAddWithoutValidation("auth-token", config.AircloakApiKey))
                    {
                        throw new Exception($"Failed to add Http header 'auth-token'");
                    }
                });
                if (ConfigureHandler != null)
                {
                    clientBuilder.AddHttpMessageHandler(ConfigureHandler);
                }
            }
        }
    }
}
