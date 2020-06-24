using System.Collections.Generic;
using Nexus.Prophecy.Configuration;

namespace Nexus.Prophecy.Services.Control
{
    public class ServiceInfo
    {
        public string Name { get; set; }
        public bool IsRunning { get; set; }
        public ServiceMetaInfo MetaInfo { get; set; }
        public Dictionary<string, string> Commands { get; set; }
    }
}