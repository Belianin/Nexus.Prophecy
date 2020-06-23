using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Nexus.Logging;
using Nexus.Prophecy.Configuration;
using Nexus.Prophecy.Services.Control;
using Nexus.Prophecy.Worker.Telegram.Nodes;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace Nexus.Prophecy.Worker.Telegram
{
    public class TelegramControlService : BackgroundService
    {
        private readonly ITelegramBotClient client;
        private readonly IControlService controlService;
        private readonly ILog log;

        private readonly ProphecySettings settings;

        public TelegramControlService(ITelegramBotClient client, IControlService controlService, ProphecySettings settings, ILog log)
        {
            this.client = client;
            this.log = log;
            this.settings = settings;
            this.controlService = controlService;

            client.OnMessage += async (s, e) => await OnMessageAsync(e.Message).ConfigureAwait(false);
            client.OnCallbackQuery += async (s, e) => await OnCallbackAsync(e.CallbackQuery).ConfigureAwait(false);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            log.Info("Telegram worker is starting");
            try
            {
                if (!client.IsReceiving)
                    client.StartReceiving(cancellationToken: stoppingToken);
            }
            catch (Exception e)
            {
                log?.Fatal($"Telegram failed to start: {e.Message}");
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

            var response = await ReplyOnCallback(callbackData).ConfigureAwait(false);
            
            await SendResponseAsync(response, message).ConfigureAwait(false);
        }

        private async Task<NodeResponse> ReplyOnCallback(CallbackData callback) =>
            callback.Action switch
            {
                Actions.ListCommands => ListCommands(callback.Service),
                Actions.ListServices => ListServices(),
                Actions.RunCommand => await RunCommandAsync(callback.Service, callback.Action).ConfigureAwait(false),
                _ => ListServices()
            };

        private async Task OnMessageAsync(Message message)
        {
            if (!settings.Interface.Telegram.Admins.Contains(message.Chat.Id))
            {
                log.Important($"Forbidden access to telegram user \"{message.Chat.Id}\" {message.From.Username ?? "(no nickname)"}");
                await client.SendTextMessageAsync(message.Chat.Id, $"Доступ с id \"{message.Chat.Id}\" не разрешён")
                    .ConfigureAwait(false);
                return;
            }

            // help и все остальное
            if (message.Text == "/start")
                await SendResponseAsync(ListServices(), message).ConfigureAwait(false);
            else
                await SendResponseAsync("/start ?", new ReplyKeyboardRemove(), message).ConfigureAwait(false);
        }

        private async Task<NodeResponse> RunCommandAsync(string service, string command)
        {
            var result = await controlService.RunCommandAsync(service, command).ConfigureAwait(false);
            if (result.IsFail)
                return new NodeResponse
                {
                    Text = result.Error
                };

            return new NodeResponse
            {
                Text = result.Value,
            };
        }

        private NodeResponse ListServices()
        {
            return new NodeResponse
            {
                Text = "Выберите сервис",
                Markup = new InlineKeyboardMarkup(controlService.ListServices().Select(s => new InlineKeyboardButton
                {
                    Text = s,
                    CallbackData = CallbackParser.CreateCallbackData(s, null, Actions.ListCommands)
                }))
            };
        }

        private NodeResponse ListCommands(string service)
        {
            var result = controlService.ListCommands(service);
            if (result.IsFail)
                return new NodeResponse
                {
                    Text = result.Error
                };

            return new NodeResponse()
            {
                Text = $"Список доступных комманд для {service}",
                Markup = new InlineKeyboardMarkup(result.Value.Select(c => new InlineKeyboardButton
                {
                    Text = c,
                    CallbackData = CallbackParser.CreateCallbackData(service, c, Actions.RunCommand)
                }))
            };
        }
        
        private Task SendResponseAsync(NodeResponse response, Message message)
        {
            return SendResponseAsync(response.Text, response.Markup, message);
        }
        
        private Task SendResponseAsync(string text, IReplyMarkup markup, Message message)
        {
            return client.SendTextMessageAsync(
                message.Chat.Id,
                text,
                ParseMode.Default,
                true,
                replyMarkup:
                markup);
        }
    }
}