using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nexus.Prophecy.Worker.Telegram;

namespace Nexus.Prophecy.Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder => webBuilder
                    .ConfigureLogging(config => { config.ClearProviders(); })
                    .UseStartup<Startup>()
                    .UseUrls("http://*:5080"))
                .ConfigureServices(services =>
                    services.AddHostedService<TelegramControlService>())
                .ConfigureLogging(config => { config.ClearProviders(); });
    }
}