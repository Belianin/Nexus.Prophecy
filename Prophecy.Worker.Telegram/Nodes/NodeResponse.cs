using Telegram.Bot.Types.ReplyMarkups;

namespace Nexus.Prophecy.Worker.Telegram.Nodes
{
    public class NodeResponse
    {
        public string Text { get; set; }
        public IReplyMarkup Markup { get; set; }
    }
}