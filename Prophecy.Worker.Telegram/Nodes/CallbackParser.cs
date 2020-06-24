using System.Linq;

namespace Nexus.Prophecy.Worker.Telegram.Nodes
{
    public static class CallbackParser
    {
        public static string CreateCallbackData(string service, string command, string action)
        {
            // не хардкодить
            return $"service={service};command={command ?? string.Empty};action={action}";
        }

        public static CallbackData ParseCallback(string query)
        {
            var data = query
                .Split(";")
                .Select(s => s.Split("="))
                .ToDictionary(k => k[0], v => v[1]);

            return new CallbackData
            {
                Service = data["service"],
                Command = data["command"],
                Action = data["action"]
            };
        }
    }

    public class CallbackData
    {
        public string Service { get; set; }
        public string Command { get; set; }
        public string Action { get; set; }
    }
}