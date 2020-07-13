using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Nexus.Core;
using Nexus.Logging;
using Nexus.Prophecy.Configuration;
using Nexus.Prophecy.Services.Control;
using Nexus.Prophecy.Worker.Telegram.Nodes;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
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

            client.OnMessage += async (s, e) => await Task.Run(() => OnMessageAsync(e.Message)).ConfigureAwait(false);
            client.OnCallbackQuery += async (s, e) => await Task.Run(() => OnCallbackAsync(e.CallbackQuery)).ConfigureAwait(false);
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
            log.Info($"[{message.Chat.Id}]: Received a callback: {callback.Data} from {AuthorToString(message)}");
            if (!settings.Interface.Telegram.Admins.Contains(message.Chat.Id))
            {
                log.Important($"Forbidden access to telegram user \"{message.Chat.Id}\" {message.From.Username ?? "(no nickname)"}");
                await client.SendTextMessageAsync(message.Chat.Id, $"Доступ с id \"{message.Chat.Id}\" не разрешён")
                    .ConfigureAwait(false);
                return;
            }

            var callbackData = CallbackParser.ParseCallback(callback.Data);

            var response = await ReplyOnCallback(callbackData).ConfigureAwait(false);

            if (response.Markup is InlineKeyboardMarkup markup)
                await EditMessageAsync(response.Text, markup, message).ConfigureAwait(false);
            else
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
            callback.Command switch
            {
                Commands.ServiceInfo => GetServiceInfo(callback.Service),
                Commands.ListServices => ListServices(),
                Commands.RunScript => await RunCommandAsync(callback.Service, callback.Script).ConfigureAwait(false),
                Commands.Start => await StartServiceAsync(callback.Service).ConfigureAwait(false),
                Commands.Stop => await StopServiceAsync(callback.Service).ConfigureAwait(false),
                Commands.Build => await BuildServiceAsync(callback.Service).ConfigureAwait(false),
                Commands.Rebuild => await RebuildAsync(callback.Service).ConfigureAwait(false),
                Commands.Restart => await RestartAsync(callback.Service).ConfigureAwait(false),
                _ => ListServices()
            };

        private async Task<NodeResponse> BuildServiceAsync(string service)
        {
            var serviceInfo = controlService.GetServiceInfo(service);
            if (serviceInfo.IsFail)
                return serviceInfo.Error;

            if (serviceInfo.Value.IsRunning)
                return new NodeResponse
                {
                    Text = $"{service} сейчас запущен. Можно перезапустить автоматически или остановить вручную",
                    Markup = new InlineKeyboardMarkup(
                        new[]
                        {
                            new []
                            {
                                RebuildButton(service), 
                                StopButton(service)
                            },
                            new []
                            {
                                BackButton()
                            }
                        })
                };
            
            var result = await controlService.BuildAsync(service).ConfigureAwait(false);
            var newServiceInfo = GetServiceInfo(service);

            var maxOutputLength = 256;
            var output = result.Value.Length < maxOutputLength 
                ? result.Value 
                : "..." + result.Value.Substring(result.Value.Length - maxOutputLength);
            if (result.IsSuccess)
            {
                return new NodeResponse
                {
                    Text = $"{service} successfully built\n*OUTPUT:*\n{output}\n\n{newServiceInfo.Text}",
                    Markup = newServiceInfo.Markup
                };
            }

            return new NodeResponse
            {
                Text = $"Unable to build {service}\n*ERROR:*\n{result.Error}\n\n{newServiceInfo.Text}",
                Markup = newServiceInfo.Markup
            };
        }

        private async Task<NodeResponse> RebuildAsync(string service)
        {
            var rebuildResult = await controlService.RebuildAsync(service).ConfigureAwait(false);
            if (rebuildResult.IsFail)
                return new NodeResponse
                {
                    Text = $"_REBUILD_ упал с ошибкой.\n*СКОРЕЕ ВСЕГО СЕРВИС ЛЕЖИТ*\n_Ошибка_:{rebuildResult.Error}"
                };
            
            return GetServiceInfo(service);
        }

        private async Task<NodeResponse> RestartAsync(string service)
        {
            var restartResult = await controlService.RestartAsync(service).ConfigureAwait(false);
            if (restartResult.IsFail)
                return new NodeResponse
                {
                    Text = $"_RESTART_ упал с ошибкой.\n*СКОРЕЕ ВСЕГО СЕРВИС ЛЕЖИТ*\n_Ошибка_:{restartResult.Error}"
                };
            
            return GetServiceInfo(service);
        }

        private async Task OnMessageAsync(Message message)
        {
            log.Info($"[{message.Chat.Id}]: Received a request: {message.Text} from {AuthorToString(message)}");
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
        
        private string FormatServiceInfo(ServiceInfo info)
        {
            var sb = new StringBuilder();
            sb.Append(string.IsNullOrEmpty(info.MetaInfo.Url) ? $"{info.Name}: " : $"[{info.Name}]({info.MetaInfo.Url}): ");
            sb.Append(info.IsRunning ? $"*ON*👌 {info.MemoryUsage / (1024f * 1024)}mb" : "*OFF*❌");
                
            return sb.ToString();
        }

        private NodeResponse ListServices()
        {
            var services = controlService.ListServices().ToArray();

            var text = $"Выберите сервис:\n{string.Join("\n", services.Select(FormatServiceInfo))}";
            return new NodeResponse
            {
                Text = text,
                Markup = new InlineKeyboardMarkup(services.Select(s => new InlineKeyboardButton
                {
                    Text = s.Name,
                    CallbackData = CallbackParser.CreateCallbackData(s.Name, null, Commands.ServiceInfo)
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

            var firstLineButtons = new List<InlineKeyboardButton>();
            firstLineButtons.Add(result.Value.IsRunning
                ? StopButton(service)
                : StartButton(service));
            
            if (result.Value.IsRunning)
                firstLineButtons.Add(RestartButton(service));
            
            firstLineButtons.Add(BuildButton(service));

            var markup = new[]
            {
                firstLineButtons,
                result.Value.Commands.Select(c => new InlineKeyboardButton
                {
                    Text = c.Key,
                    CallbackData = CallbackParser.CreateCallbackData(service, c.Key, Commands.RunScript)
                }),
                new[] {BackButton()}
            };
            
            return new NodeResponse
            {
                Text = $"{FormatServiceInfo(result.Value)}\n\nСписок доступных комманд для {service}",
                Markup = new InlineKeyboardMarkup(markup)
            };
        }
        
        private Task SendResponseAsync(NodeResponse response, Message message)
        {
            return SendResponseAsync(response.Text, response.Markup, message);
        }

        private Task EditMessageAsync(string text, InlineKeyboardMarkup markup, Message message)
        {
            try
            {
                return client.EditMessageTextAsync(
                    message.Chat.Id.ToString(),
                    message.MessageId,
                    text,
                    ParseMode.Markdown,
                    false,
                    markup);
            }
            catch (MessageIsNotModifiedException)
            {
                return Task.CompletedTask;
            }
        }
        
        private async Task SendResponseAsync(string text, IReplyMarkup markup, Message message)
        {
            foreach (var chunk in Chunk(text, 4096))
            {
                await client.SendTextMessageAsync(
                    message.Chat.Id,
                    chunk,
                    ParseMode.Markdown,
                    true,
                    replyMarkup:
                    markup)
                    .ConfigureAwait(false);   
            }
        }
        
        
        private static IEnumerable<string> Chunk(string str, int chunkSize)
        {
            if (str.Length < chunkSize)
                return new[] {str};
            return Enumerable.Range(0, str.Length / chunkSize)
                .Select(i => str.Substring(i * chunkSize, chunkSize));
        }

        private static string AuthorToString(Message message)
        {
            var sb = new StringBuilder()
                .Append(message.From.Id);
            if (message.From.Username != null)
                sb.Append($" @{message.From.Username}");

            return sb.ToString();
        }

        // Buttons.Start(service)
        private InlineKeyboardButton StopButton(string service) => new InlineKeyboardButton
        {
            Text = "Stop ⛔️",
            CallbackData = CallbackParser.CreateCallbackData(service, null, Commands.Stop)
        };

        private InlineKeyboardButton BackButton() => new InlineKeyboardButton
        {
            Text = "🔙",
            CallbackData = CallbackParser.CreateCallbackData(null, null, Commands.ListServices)
        };

        private InlineKeyboardButton RebuildButton(string service) => new InlineKeyboardButton
        {
            Text = "Rebuild 🛎",
            CallbackData = CallbackParser.CreateCallbackData(service, null, Commands.Rebuild)
        };

        private InlineKeyboardButton BuildButton(string service) => new InlineKeyboardButton
        {
            Text = "Build 🔨",
            CallbackData = CallbackParser.CreateCallbackData(service, null, Commands.Build)
        };
        
        private InlineKeyboardButton StartButton(string service) => new InlineKeyboardButton
        {
            Text = "Start 🚀",
            CallbackData = CallbackParser.CreateCallbackData(service, null, Commands.Start)
        };
        
        private InlineKeyboardButton RestartButton(string service) => new InlineKeyboardButton
        {
            Text = "Restart ♻️",
            CallbackData = CallbackParser.CreateCallbackData(service, null, Commands.Restart)
        };
    }
}