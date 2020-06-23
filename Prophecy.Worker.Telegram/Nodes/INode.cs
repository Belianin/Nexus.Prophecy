using System.Threading.Tasks;
using Telegram.Bot.Types;

namespace Nexus.Prophecy.Worker.Telegram.Nodes
{
    public interface INode
    {
        Task<NodeResponse> ReplyAsync(Message message);
        NodeResponse Response { get; } 
    }
}