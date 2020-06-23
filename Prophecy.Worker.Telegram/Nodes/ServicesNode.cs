using System.Linq;
using System.Threading.Tasks;
using Nexus.Prophecy.Configuration;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace Nexus.Prophecy.Worker.Telegram.Nodes
{
    public class ServicesNode : INode
    {
        private readonly ProphecySettings settings;

        public ServicesNode(ProphecySettings settings)
        {
            this.settings = settings;
        }

        public Task<NodeResponse> ReplyAsync(Message message)
        {
            throw new System.NotImplementedException();
        }

        public NodeResponse Response => new NodeResponse
        {
            Markup = GetMenu(),
            NextNode = this,
            Text = "Выберите нужный сервис:"
        };

        private IReplyMarkup GetMenu()
        {
            return new InlineKeyboardMarkup(settings.Services.Keys.Select(s => new InlineKeyboardButton
            {
                Text = s,
                CallbackData = CallbackParser.CreateCallbackData(s, null, "list")
            }));
        }
    }
}