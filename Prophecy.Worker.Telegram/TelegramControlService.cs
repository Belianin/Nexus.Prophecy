using System;
using System.Linq;
using System.Text;
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

            try
            {
                await client.AnswerCallbackQueryAsync(callback.Id).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                log.Warn(e.Message);
            }
        }

        private async Task<NodeResponse> ReplyOnCallback(CallbackData callback) =>
            callback.Action switch
            {
                Actions.ListCommands => GetServiceInfo(callback.Service),
                Actions.ListServices => ListServices(),
                Actions.RunCommand => await RunCommandAsync(callback.Service, callback.Command).ConfigureAwait(false),
                Actions.Start => await StartServiceAsync(callback.Service).ConfigureAwait(false),
                Actions.Stop => await StopServiceAsync(callback.Service).ConfigureAwait(false),
                _ => ListServices()
            };

        private async Task OnMessageAsync(Message message)
        {
            log.Info($"Received a request: {message.Text} from {AuthorToString(message)}");
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
            else if (message.Text == "/help")
                await SendResponseAsync("/start for list available service", new ReplyKeyboardRemove(), message)
                    .ConfigureAwait(false);
            else
                await SendResponseAsync("/start or /help ?", new ReplyKeyboardRemove(), message).ConfigureAwait(false);
        }

        private async Task<NodeResponse> StartServiceAsync(string service)
        {
            var result = await controlService.StartAsync(service).ConfigureAwait(false);
            if (result.IsFail)
                return new NodeResponse
                {
                    Text = result.Error
                };

            return GetServiceInfo(service);
        }

        private async Task<NodeResponse> StopServiceAsync(string service)
        {
            var result = await controlService.StopAsync(service).ConfigureAwait(false);
            if (result.IsFail)
                return new NodeResponse
                {
                    Text = result.Error
                };

            return GetServiceInfo(service);
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
            var services = controlService.ListServices().ToArray();

            string FormatService(ServiceInfo s)
            {
                var sb = new StringBuilder();
                sb.Append(string.IsNullOrEmpty(s.MetaInfo.Url) ? $"{s.Name}: " : $"[{s.Name}]({s.MetaInfo.Url}): ");
                sb.Append(s.IsRunning ? "*ON*👌" : "*OFF*❌");
                
                return sb.ToString();
            }

            var text = $"Выберите сервис:\n{string.Join("\n", services.Select(FormatService))}";
            return new NodeResponse
            {
                Text = text,
                Markup = new InlineKeyboardMarkup(services.Select(s => new InlineKeyboardButton
                {
                    Text = s.Name,
                    CallbackData = CallbackParser.CreateCallbackData(s.Name, null, Actions.ListCommands)
                }))
            };
        }

        private NodeResponse GetServiceInfo(string service)
        {
            var result = controlService.GetServiceInfo(service);
            if (result.IsFail)
                return new NodeResponse
                {
                    Text = result.Error
                };

            var startStopButton = result.Value.IsRunning
                ? new InlineKeyboardButton
                {
                    Text = "Start",
                    CallbackData = CallbackParser.CreateCallbackData(service, null, Actions.Start)
                }
                : new InlineKeyboardButton
                {
                    Text = "Stop",
                    CallbackData = CallbackParser.CreateCallbackData(service, null, Actions.Stop)
                };

            var markup = new[]
            {
                new[] {startStopButton},
                result.Value.Commands.Select(c => new InlineKeyboardButton
                {
                    Text = c.Key,
                    CallbackData = CallbackParser.CreateCallbackData(service, c.Key, Actions.RunCommand)
                })
            };
            
            return new NodeResponse
            {
                Text = $"Список доступных комманд для {service}",
                Markup = new InlineKeyboardMarkup(markup)
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
                ParseMode.Markdown,
                true,
                replyMarkup:
                markup);
        }

        private static string AuthorToString(Message message)
        {
            var sb = new StringBuilder()
                .Append(message.From.Id);
            if (message.From.Username != null)
                sb.Append($" @{message.From.Username}");

            return sb.ToString();
        }
    }
}