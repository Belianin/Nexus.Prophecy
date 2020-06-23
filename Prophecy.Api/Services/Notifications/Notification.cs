
using Nexus.Logging.Utils;

namespace Nexus.Prophecy.Services.Notifications
{
    public class Notification
    {
        public LogLevel LogLevel { get; }

        public string Message { get; }
        
        public string[] Context { get; set; } = new string[0];

        public Notification(LogLevel logLevel, string message)
        {
            LogLevel = logLevel;
            Message = message;
        }
    }
}