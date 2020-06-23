using Telegram.Bot.Types.ReplyMarkups;

namespace Nexus.Prophecy.Telegram.Nodes
{
    public class NodeResponse
    {
        public string Text { get; set; }
        public IReplyMarkup Markup { get; set; }
        public INode NextNode { get; set; }
    }
}