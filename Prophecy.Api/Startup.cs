using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Nexus.Logging;
using Nexus.Logging.Console;
using Nexus.Prophecy.Api.DI;
using Nexus.Prophecy.Configuration;
using Nexus.Prophecy.Services.Control;
using Nexus.Prophecy.Services.Logs;
using Telegram.Bot;

namespace Nexus.Prophecy.Api
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            var settings = SettingsManager.GetSettings();

            services
                .AddSingleton(settings)
                .AddSingleton<ILog, ColourConsoleLog>()
                .AddSingleton<ITelegramBotClient>(new TelegramBotClient(settings.Interface.Telegram.Token))
                .AddSingleton<ILogService>(new LogService(settings.GetLogPaths()))
                .AddSingleton<IControlService, ControlService>()
                .AddNotificatorService()
                .AddControllers();
            
            services.AddMvc(options => options.EnableEndpointRouting = false).AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.PropertyNamingPolicy = null;
                options.JsonSerializerOptions.Converters.Add(new LogLevelJsonConverter());
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(
            IApplicationBuilder app,
            IHostApplicationLifetime lifetime,
            IWebHostEnvironment env,
            ILog log,
            ITelegramBotClient telegramClient)
        {
            log.Info("Starting Prophecy.Api");
            
            lifetime.ApplicationStopping.Register(SettingsManager.SaveSettings);
            lifetime.ApplicationStopping.Register(() => telegramClient?.StopReceiving());
            
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();

            app.UseRouting();
            app.UseMvc();
        }
    }
}