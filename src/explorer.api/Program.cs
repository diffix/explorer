namespace Explorer.Api
{
    using Lamar.Microsoft.DependencyInjection;

    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;

    public static class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            var config = new ConfigurationBuilder()
                .AddCommandLine(args)
                .Build();

            return Host.CreateDefaultBuilder(args)
                .UseLamar()
                .ConfigureWebHostDefaults(builder =>
                {
                    builder.UseStartup<Startup>();
                    builder.UseConfiguration(config);
                    builder.ConfigureLogging(loggerBuilder =>
                        loggerBuilder.AddConsole(opts => opts.TimestampFormat = "[yyyy'-'MM'-'dd' 'HH':'mm':'ss] "));
                    builder.UseSentry();

                    builder.ConfigureServices((_, services) => services.AddControllers());
                });
        }
    }
}
