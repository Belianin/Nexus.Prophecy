using System.Collections.Generic;
using System.Threading.Tasks;
using Nexus.Core;

namespace Nexus.Prophecy.Services.Control
{
    public interface IControlService
    {
        IEnumerable<string> ListServices();
        Task<Result<string>> RunCommandAsync(string service, string command);
        Result<IEnumerable<string>> ListCommands(string service);
        Result<string> ShowCommand(string service, string command);
        Result RemoveCommand(string service, string command);
        Result UpdateCommand(string service, string command, string commandBody);
    }
}