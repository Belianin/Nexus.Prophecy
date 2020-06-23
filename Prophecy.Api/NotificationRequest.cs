using System.ComponentModel.DataAnnotations;
using Nexus.Logging.Utils;

namespace Nexus.Prophecy.Api
{
    public class NotificationRequest
    {
        [Required] public LogLevel LogLevel { get; set; }
        [Required] public string Message { get; set; }
    }
}