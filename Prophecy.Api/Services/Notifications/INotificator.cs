using System.Threading.Tasks;
using Nexus.Core;

namespace Nexus.Prophecy.Services.Notifications
{
    public interface INotificator
    {
        public Task<Result> NotifyAsync(Notification notification);
    }
}