using System;
using System.Collections.Generic;
using System.Linq;
using Nexus.Core;
using Nexus.Logging.Utils;

namespace Nexus.Prophecy.Services.Logs
{
    public static class LogServiceExtensions
    {
        public static Result<IEnumerable<string>> GetLogs(
            this ILogService logService,
            string service,
            DateTime from,
            DateTime to,
            LogFilterParameters parameters)
        {
            var result = logService.GetLogs(service, from, to);
            if (result.IsFail)
                return result;

            var filtered = result.Value;
            if (parameters != null)
            {
                var comparison = parameters.IsIgnoreCase
                    ? StringComparison.InvariantCultureIgnoreCase :
                    StringComparison.InvariantCulture;
                filtered = filtered.Where(s => s.Contains(s, comparison));
            }

            if (parameters.LogLevels != null && parameters.LogLevels.Length > 0)
            {
                filtered = filtered.Where(s => parameters
                    .LogLevels.Any(l => LogFormatter.IsLogLevel(s, l)));
            }

            return Result<IEnumerable<string>>.Ok(filtered);
        }
    }
}