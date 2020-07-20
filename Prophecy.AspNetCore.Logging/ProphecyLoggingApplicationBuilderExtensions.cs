using Microsoft.Extensions.DependencyInjection;
using Nexus.Logging;
using Nexus.Logging.Prophecy;

namespace Nexus.Prophecy.AspNetCore.Logging
{
    public static class ProphecyLoggingApplicationBuilderExtensions
    {
        public static IServiceCollection AddProphecyLogging(
            this IServiceCollection serviceCollection,
            string prophecyUrl,
            ILog innerLog = null)
        {
            return serviceCollection.AddSingleton<ILog>(sp =>
            {
                var log = innerLog ?? sp.GetService<ILog>();
                
                var prophecyLog = new ProphecyLog(prophecyUrl, log)
                    .OnlyErrors();

                if (log == null)
                    return prophecyLog;
                
                return new AggregationLog(log, prophecyLog);
            });
        }
    }
}