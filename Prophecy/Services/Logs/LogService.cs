using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Nexus.Core;
using Nexus.Logging.File;
using Nexus.Logging.Utils;

namespace Nexus.Prophecy.Services.Logs
{
    public class LogService : ILogService
    {
        private readonly IDictionary<string, string> registeredServices;

        public LogService(IDictionary<string, string> registeredServices)
        {
            this.registeredServices = registeredServices.ToDictionary(
                x => x.Key.ToLower(), x => x.Value);
        }

        public Result<IEnumerable<string>> GetLogs(string service, DateTime @from, DateTime to)
        {
            if (!registeredServices.TryGetValue(service.ToLower(), out var serviceFolder))
                return $"Service \"{service}\" not found";

            var filenames = GetMonthsBetweenPeriod(from, to)
                .Select(d => Path.Combine(serviceFolder, FileLog.GetFileName(d)))
                .ToArray();
            
            if (filenames.All(f => !File.Exists(f)))
                return $"No logs for this period";

            var logs = filenames
                .Where(File.Exists)
                .SelectMany(File.ReadLines)
                .SkipWhile(s => LogFormatter.GetLogTime(s) < from)
                .TakeWhile(s => LogFormatter.GetLogTime(s) <= to);
            
            return Result.Ok(logs);
        }

        private static IEnumerable<DateTime> GetMonthsBetweenPeriod(DateTime @from, DateTime to)
        {
            var dateTime = from;
            do
            {
                yield return dateTime;
                dateTime = dateTime.AddMonths(1);
            } while (dateTime.Year <= to.Year && dateTime.Month < to.Month);
        }
    }
}