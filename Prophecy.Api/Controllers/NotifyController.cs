using Microsoft.AspNetCore.Mvc;
using Nexus.Logging;
using Nexus.Prophecy.Services.Notifications;

namespace Nexus.Prophecy.Api.Controllers
{
    [Route("api/v1")]
    public class NotifyController : Controller
    {
        private readonly INotificationService service;
        private readonly ILog log;

        public NotifyController(INotificationService service, ILog log)
        {
            this.service = service;
            this.log = log;
        }

        [HttpPost("notify")]
        public IActionResult Notify([FromBody] NotificationRequest request)
        {
            log.Info($"Received a request {Request.Path}");
            var notification = new Notification(request.LogLevel, request.Message);
            
            log.Info("Notification sending");
            service.Notify(notification);

            return Ok();
        }
    }
}