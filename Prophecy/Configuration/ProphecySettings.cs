using System.Collections.Generic;
using System.Linq;

namespace Nexus.Prophecy.Configuration
{
    public class ProphecySettings
    {
        public Dictionary<string, ServiceSettings> Services { get; set; }
        public InterfaceSettings Interface { get; set; }
        
        public class ServiceSettings
        {
            public string Logs { get; set; }
            public string Path { get; set; }
            public Dictionary<string, string> Commands { get; set; }
        }
        
        public class InterfaceSettings
        {
            public TelegramSettings Telegram { get; set; }

            public class TelegramSettings
            {
                public string Token { get; set; }
                public long[] Admins { get; set; }
                public long[] LogChannels { get; set; }
            }
        }
    }

    public static class ProphecySettingsExtensions
    {
        public static Dictionary<string, string> GetLogPaths(this ProphecySettings settings) =>
            settings.Services.ToDictionary(k => k.Key.ToLower(), v => v.Value.Logs);
    }
}