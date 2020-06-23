using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Nexus.Logging;
using Nexus.Prophecy.Configuration;
using Nexus.Prophecy.Services.Control;
using Nexus.Prophecy.Telegram.Nodes;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace Nexus.Prophecy.Telegram
{
    public class TelegramControlService : BackgroundService
    {
        private readonly ITelegramBotClient client;
        private readonly IControlService service;
        private readonly ILog log;

        private readonly ProphecySettings settings;

        public TelegramControlService(ITelegramBotClient client, IControlService service, ProphecySettings settings, ILog log)
        {
            this.client = client;
            this.log = log;
            this.settings = settings;
            this.service = service;

            client.OnMessage += async (s, e) => await OnMessageAsync(e.Message).ConfigureAwait(false);
            client.OnCallbackQuery += async (s, e) => await OnCallbackAsync(e.CallbackQuery).ConfigureAwait(false);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                if (!client.IsReceiving)
                    client.StartReceiving(cancellationToken: stoppingToken);
            }
            catch (Exception e)
            {
                log?.Fatal($"TelegramControlPanel failed to start: {e.Message}");
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                Thread.Sleep(1000);
            }
        }

        private async Task OnCallbackAsync(CallbackQuery callback)
        {
            var message = callback.Message;
            if (!settings.Interface.Telegram.Admins.Contains(message.Chat.Id))
            {
                log.Important($"Forbidden access to telegram user \"{message.Chat.Id}\" {message.From.Username ?? "(no nickname)"}");
                await client.SendTextMessageAsync(message.Chat.Id, $"Доступ с id \"{message.Chat.Id}\" не разрешён")
                    .ConfigureAwait(false);
                return;
            }

            var callbackData = CallbackParser.ParseCallback(callback.Data);
        }

        private async Task OnMessageAsync(Message message)
        {
            if (!settings.Interface.Telegram.Admins.Contains(message.Chat.Id))
            {
                log.Important($"Forbidden access to telegram user \"{message.Chat.Id}\" {message.From.Username ?? "(no nickname)"}");
                await client.SendTextMessageAsync(message.Chat.Id, $"Доступ с id \"{message.Chat.Id}\" не разрешён")
                    .ConfigureAwait(false);
                return;
            }
            
            throw new NotImplementedException();
        }
    }
}