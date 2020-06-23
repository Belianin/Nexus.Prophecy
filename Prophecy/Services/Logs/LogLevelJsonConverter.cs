using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Nexus.Logging.Utils;

namespace Nexus.Prophecy.Services.Logs
{
    public class LogLevelJsonConverter : JsonConverter<LogLevel>
    {
        public override LogLevel Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return (LogLevel) Enum.Parse(typeof(LogLevel), reader.GetString());
        }

        public override void Write(Utf8JsonWriter writer, LogLevel value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString());
        }
    }
}