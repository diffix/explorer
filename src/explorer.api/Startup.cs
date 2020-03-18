namespace Explorer.Api
{
    using Aircloak.JsonApi;
    using Explorer.Api.Authentication;
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
        public virtual void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();

            var config = Configuration.GetSection("Explorer").Get<ExplorerConfig>();

            services.AddAircloakJsonApiServices<ExplorerApiAuthProvider>(config.AircloakApiUrl ??
                throw new System.Exception("No Aircloak Api base Url provided in Explorer config."));
            services.AddSingleton(config);

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
