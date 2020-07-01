using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Nexus.Prophecy.Worker.Telegram.Nodes
{
    public static class CallbackParser
    {
        public static string CreateCallbackData(string service, string script, string command)
        {
            var sb = new StringBuilder();
            return $"service={service};{(script != null ? $"script={script};" : "")}command={command}";
        }

        public static CallbackData ParseCallback(string query)
        {
            var data = query
                .Split(";")
                .Select(s => s.Split("="))
                .ToDictionary(k => k[0], v => v[1]);

            return new CallbackData(data);
        }
    }

    public class CallbackData
    {
        private readonly Dictionary<string, string> arguments;

        public CallbackData(Dictionary<string, string> arguments)
        {
            this.arguments = arguments;
        }

        public string Service => arguments["service"];
        public string Script => arguments["script"];
        public string Command => arguments["command"];
    }
}