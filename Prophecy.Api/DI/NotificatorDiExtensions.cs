using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Nexus.Logging;
using Nexus.Prophecy.Configuration;
using Nexus.Prophecy.Services.Notifications;
using Nexus.Prophecy.Services.Notifications.Telegram;
using Telegram.Bot;

namespace Nexus.Prophecy.Api.DI
{
    public static class NotificatorDiExtensions
    {
        public static IServiceCollection AddNotificatorService(this IServiceCollection services)
        {
            services.AddSingleton<INotificationService>(sp =>
            {
                var log = sp.GetRequiredService<ILog>();
                var telegramClient = sp.GetRequiredService<ITelegramBotClient>();
                var settings = sp.GetRequiredService<ProphecySettings>().Interface;

                var notifiers = new List<INotificator>();
                notifiers.Add(new TelegramNotificator(telegramClient, settings.Telegram.LogChannels, log));

                return new NotificationService(notifiers);
            });

            return services;
        }
    }
}