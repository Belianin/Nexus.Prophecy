using Microsoft.Extensions.DependencyInjection;
using Nexus.Logging;
using Nexus.Logging.Prophecy;

namespace Prophecy.AspNetCore.Logging
{
    public static class ProphecyLoggingApplicationBuilderExtensions
    {
        public static IServiceCollection AddProphecyLogging(
            this IServiceCollection serviceCollection, string prophecyUrl)
        {
            return serviceCollection.AddSingleton<ILog>(sp =>
            {
                var log = sp.GetService<ILog>();
                
                return new ProphecyLog(prophecyUrl, log);
            });
        }
    }
}