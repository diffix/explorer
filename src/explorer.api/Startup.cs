namespace Explorer.Api
{
    using Aircloak.JsonApi;
    using Explorer.Api.Authentication;
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

            services.AddAircloakJsonApiServices<ExplorerApiAuthProvider>();

            // Singleton services
            services
                .AddSingleton<ExplorationRegistry>()
                .AddSingleton<ExplorationLauncher>();

            // Scoped services
            services
                .AddScoped<MetricsPublisher, SimpleMetricsPublisher>()
                .AddScoped<ContextBuilder>()
                .AddScoped<AircloakConnectionBuilder>();

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

            System.Console.WriteLine(container.WhatDoIHave());
            System.Console.WriteLine(container.WhatDidIScan());

            container.AssertConfigurationIsValid();
        }
    }
}
