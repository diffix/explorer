[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("explorer.api.tests")]

namespace Explorer.Api
{
    using Microsoft.AspNetCore;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.Configuration;

    public static class Program
    {
        public static void Main(string[] args)
        {
            CreateWebHostBuilder(args).Build().Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args)
        {
            var config = new ConfigurationBuilder()
                .AddCommandLine(args)
                .Build();
            return WebHost.CreateDefaultBuilder<Startup>(args).UseConfiguration(config);
        }
    }
}
