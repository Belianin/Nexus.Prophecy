using System.Collections.Generic;
using System.Threading.Tasks;
using Nexus.Core;

namespace Nexus.Prophecy.Services.Control
{
    public interface IControlService
    {
        Task<Result<ServiceInfo>> StartAsync(string service);
        Task<Result<ServiceInfo>> StopAsync(string service);
        Task<Result<string>> BuildAsync(string service, string branch = "master");
        IEnumerable<ServiceInfo> ListServices();
        Task<Result<string>> RunCommandAsync(string service, string command);
        Result<ServiceInfo> GetServiceInfo(string service);
        Result RemoveCommand(string service, string command);
        Result UpdateCommand(string service, string command, string commandBody);
    }
}