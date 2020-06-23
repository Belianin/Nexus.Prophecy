using System.Collections.Generic;

namespace Nexus.Prophecy.Services.Control
{
    public class ServiceInfo
    {
        public string Name { get; set; }
        public bool IsRunning { get; set; }
        public IEnumerable<string> Commands { get; set; }
    }
}