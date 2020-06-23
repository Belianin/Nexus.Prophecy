using System.Linq;
using System.Threading.Tasks;
using Nexus.Prophecy.Configuration;
using Nexus.Prophecy.Services.Control;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace Nexus.Prophecy.Worker.Telegram.Nodes
{
    public class ServicesNode : INode
    {
        private readonly IControlService service;

        public ServicesNode(IControlService service)
        {
            this.service = service;
        }

        public async Task<NodeResponse> ReplyAsync(Message message)
        {
            return Response;
        }

        public NodeResponse Response => new NodeResponse
        {
            Markup = GetMenu(),
            NextNode = this,
            Text = "Выберите нужный сервис:"
        };

        private IReplyMarkup GetMenu()
        {
            return new InlineKeyboardMarkup(service.ListServices().Select(s => new InlineKeyboardButton
            {
                Text = s,
                CallbackData = CallbackParser.CreateCallbackData(s, null, Actions.ListCommands)
            }));
        }
    }
}