namespace Explorer.Api
{
    using Aircloak.JsonApi;
    using Diffix;
    using Explorer.Api.Authentication;
    using Explorer.Common;
    using Explorer.Components;
    using Explorer.Metrics;
    using Lamar;

    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;

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

            var config = Configuration.GetSection("Explorer").Get<ExplorerConfig>();
            services.AddSingleton(config);

            services.AddAircloakJsonApiServices<ExplorerApiAuthProvider>(config.AircloakApiUrl());

            // Singleton services
            services
                .AddSingleton<MetricsPublisher, SimpleMetricsPublisher>()
                .AddSingleton<ExplorationRegistry>()
                .AddSingleton<ExplorationLauncher>();

            // Scoped services
            services
                .AddScoped<ContextBuilder>()
                .AddScoped<AircloakConnectionBuilder>();

            // Scan for Components
            services.Scan(_ =>
            {
                _.Assembly("explorer");
                _.IncludeNamespace("Explorer.Components");
                _.AddAllTypesOf<PublisherComponent>(ServiceLifetime.Scoped);
                _.ConnectImplementationsToTypesClosing(typeof(ResultProvider<>), ServiceLifetime.Scoped);
                _.ConnectImplementationsToTypesClosing(typeof(ExplorerComponent<>), ServiceLifetime.Scoped);
            });

            // The following are not picked up by the scan for some reason.
            services.AddScoped<SimpleStats<double>>();
            services.AddScoped<SimpleStats<long>>();

            // Services to be injected at runtime
            services.Injectable<ExplorerContext>();
            services.Injectable<DConnection>();

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
                var container = (IContainer)app.ApplicationServices;

                System.Console.WriteLine(container.WhatDoIHave());
                System.Console.WriteLine(container.WhatDidIScan());

                container.AssertConfigurationIsValid();

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
    }
}
