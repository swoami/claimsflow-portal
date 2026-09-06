using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace ClaimsFlow.Portal
{
    // Classic two-file hosting: Program builds the host, Startup configures it.
    // Minimal hosting (WebApplicationBuilder) arrived in .NET 6 but this app was
    // scaffolded before that and nobody went back.
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}
