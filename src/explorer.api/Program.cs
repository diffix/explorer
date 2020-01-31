[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("explorer.api.tests")]

namespace Explorer.Api
{
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.Hosting;

    public static class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(Startup.ConfigureExplorer);
    }
}
