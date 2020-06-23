using System;
using System.Collections.Generic;
using Nexus.Core;

namespace Nexus.Prophecy.Services.Logs
{
    public interface ILogService
    {
        Result<IEnumerable<string>> GetLogs(string service, DateTime from, DateTime to);
    }
}