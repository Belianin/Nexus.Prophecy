using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Nexus.Prophecy.Services.Notifications
{
    public class NotificationService : INotificationService, IDisposable
    {
        private readonly ICollection<INotificator> notificators;
        private readonly ConcurrentQueue<Notification> notificationQueue = new ConcurrentQueue<Notification>();
        private readonly CancellationTokenSource cts = new CancellationTokenSource();
        

        public NotificationService(ICollection<INotificator> notificators)
        {
            this.notificators = notificators;
            
            Task.Run(() => NotifyAsync(cts.Token), cts.Token);
        }

        public void Notify(Notification notification)
        {
            notificationQueue.Enqueue(notification);
        }
        
        private async Task NotifyAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                if (notificationQueue.IsEmpty)
                    await Task.Delay(1000, token).ConfigureAwait(false);
                else
                {
                    while (notificationQueue.TryDequeue(out var notification))
                    {
                        foreach (var notificator in notificators)
                        {
                            await notificator.NotifyAsync(notification);
                        }
                    }
                }
            }
        }

        public void Dispose()
        {
            cts.Dispose();
        }
    }
}