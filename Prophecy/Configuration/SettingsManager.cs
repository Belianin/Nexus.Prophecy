using System.IO;
using Newtonsoft.Json;

namespace Nexus.Prophecy.Configuration
{
    public static class SettingsManager
    {
        private static ProphecySettings settings = null!;
        public static string Filename { get; set; } = "prophecy.json"; 
        
        public static ProphecySettings GetSettings()
        {
            if (settings == null)
            {
                if (!File.Exists(Filename))
                    throw new IOException($"No Prophecy.Api settings file \"{Filename}\"");

                settings = JsonConvert.DeserializeObject<ProphecySettings>(File.ReadAllText(Filename));
            }

            return settings;
        }

        public static void SaveSettings()
        {
            File.WriteAllText(Filename, JsonConvert.SerializeObject(settings));
        }
    }
}