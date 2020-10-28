namespace Explorer.Api
{
    using Aircloak.JsonApi;
    using Explorer.Api.Authentication;
    using Explorer.Api.Models;
    using Explorer.Components;
    using Explorer.Metrics;
    using Lamar;

    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;

    using Sentry.Extensibility;

    public class Startup
    {
        public Startup(IConfiguration configuration, IWebHostEnvironment environment)
        {
            Configuration = configuration;
            Environment = environment;
        }

        public IConfiguration Configuration { get; }

        public IWebHostEnvironment Environment { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureContainer(ServiceRegistry services)
        {
            services.AddControllers();
            services.AddApiVersioning();

            const string ConfigSection = "Explorer";
            services.Configure<ExplorerOptions>(Configuration.GetSection(ConfigSection));
            services.Configure<ConnectionOptions>(Configuration.GetSection(ConfigSection));

            services.AddAircloakJsonApiServices<ExplorerApiAuthProvider>();

            // Enriched event logger for sentry
            services.AddScoped<ISentryEventProcessor, ExplorerEventProcessor>();

            // Singleton services
            services
                .AddSingleton<ExplorationRegistry>();

            // Scoped services
            services
                .AddScoped<MetricsPublisher, SimpleMetricsPublisher>();

            // Transient Services
            services
                .AddTransient<ExplorationScopeBuilder, TypeBasedScopeBuilder>();

            // Register Explorer Components
            services.IncludeRegistry<ComponentRegistry>();

            if (Environment.IsDevelopment())
            {
                services.AddCors(options =>
                    options.AddDefaultPolicy(b =>
                        b.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin()));
            }
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public virtual void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                PrintLamarDiagnostics(app);

                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            if (env.IsDevelopment())
            {
                app.UseCors();
            }

            app.UseAuthorization();

            app.UseEndpoints(endpoints => endpoints.MapControllers());
        }

        private static void PrintLamarDiagnostics(IApplicationBuilder app)
        {
            var container = (IContainer)app.ApplicationServices;

            var logger = container.GetInstance<ILogger<Startup>>();

            logger.LogInformation(container.WhatDoIHave());
            logger.LogInformation(container.WhatDidIScan());

            container.AssertConfigurationIsValid();
        }
    }
}
