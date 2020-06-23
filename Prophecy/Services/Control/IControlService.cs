using System.Collections.Generic;
using System.Threading.Tasks;
using Nexus.Core;

namespace Nexus.Prophecy.Services.Control
{
    public interface IControlService
    {
        Task<Result<ServiceInfo>> StartAsync(string service);
        Task<Result<ServiceInfo>> StopAsync(string service);
        IEnumerable<ServiceInfo> ListServices();
        Task<Result<string>> RunCommandAsync(string service, string command);
        Result<ServiceInfo> GetServiceInfo(string service);
        Result<string> ShowCommand(string service, string command);
        Result RemoveCommand(string service, string command);
        Result UpdateCommand(string service, string command, string commandBody);
    }
}