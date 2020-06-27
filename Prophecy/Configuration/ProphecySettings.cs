using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;

namespace Nexus.Prophecy.Configuration
{
    public class ProphecySettings
    {
        public StartUpSettings StartUp { get; set; }
        public Dictionary<string, ServiceSettings> Services { get; set; }
        public InterfaceSettings Interface { get; set; }
        
        public class ServiceSettings
        {
            public string Path { get; set; }
            public ServiceMetaInfo MetaInfo { get; set; }
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

        public class StartUpSettings
        {
            public string[] Services { get; set; }
        }

        public ServicePaths GetServiceFullPath(string serviceName)
        {
            var serviceInfo = Services[serviceName];
            var project = serviceInfo.MetaInfo.Project ?? serviceName;
            var csprojPath = Path.Combine(serviceInfo.Path, project, $"{project}.csproj");

            var targetFramework = GetTargetFramework(csprojPath);
            var assemblyInfo = GetAssemblyInfo(csprojPath);

            var root = Path.Combine(serviceInfo.Path, project, "bin", "Release", targetFramework);

            return new ServicePaths
            {
                Root = root,
                Executable = Path.Combine(root, $"{assemblyInfo ?? serviceName}.exe"),
                Logs = Path.Combine(root, "Logs")
            };
        }

        private static string GetAssemblyInfo(string csprojPath)
        {
            var reader = XmlReader.Create(File.OpenRead(csprojPath));
                
            return reader.ReadToFollowing("AssemblyInfo") 
                ? reader.Value
                : null;
        }

        private static string GetTargetFramework(string csprojPath)
        {
            var reader = XmlReader.Create(File.OpenRead(csprojPath));
                
            return reader.ReadToFollowing("TargetFramework") 
                ? reader.Value.Split(",").FirstOrDefault(f => f.Contains("netcoreapp"))
                : null;
        }
    }
    
    
    public class ServicePaths
    {
        public string Root { get; set; }
        public string Executable { get; set; }
        public string Logs { get; set; }
    }

    public class ServiceMetaInfo
    {
        public string Url { get; set; }
        public string Project { get; set; }
    }

    public static class ProphecySettingsExtensions
    {
        public static Dictionary<string, string> GetLogPaths(this ProphecySettings settings)
        {
            return settings.Services
                .ToDictionary(
                    k => k.Key,
                    s => settings.GetServiceFullPath(s.Key).Logs);
        }
    }
}