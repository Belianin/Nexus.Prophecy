using Telegram.Bot.Types.ReplyMarkups;

namespace Nexus.Prophecy.Worker.Telegram
{
    internal static class Buttons
    {
        public static InlineKeyboardButton Stop(string service) => new InlineKeyboardButton
        {
            Text = "Stop ⛔️",
            CallbackData = CallbackParser.CreateCallbackData(service, null, Commands.Stop)
        };

        public static InlineKeyboardButton Back() => new InlineKeyboardButton
        {
            Text = "🔙",
            CallbackData = CallbackParser.CreateCallbackData(null, null, Commands.ListServices)
        };

        public static InlineKeyboardButton Rebuild(string service) => new InlineKeyboardButton
        {
            Text = "Rebuild 🛎",
            CallbackData = CallbackParser.CreateCallbackData(service, null, Commands.Rebuild)
        };

        public static InlineKeyboardButton Build(string service) => new InlineKeyboardButton
        {
            Text = "Build 🔨",
            CallbackData = CallbackParser.CreateCallbackData(service, null, Commands.Build)
        };
        
        public static InlineKeyboardButton Start(string service) => new InlineKeyboardButton
        {
            Text = "Start 🚀",
            CallbackData = CallbackParser.CreateCallbackData(service, null, Commands.Start)
        };
        
        public static InlineKeyboardButton Restart(string service) => new InlineKeyboardButton
        {
            Text = "Restart ♻️",
            CallbackData = CallbackParser.CreateCallbackData(service, null, Commands.Restart)
        };
    }
}