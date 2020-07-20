using Microsoft.AspNetCore.Builder;

namespace Nexus.Prophecy.AspNetCore
{
    public static class ProphecyMiddlewareExtensions
    {
        public static IApplicationBuilder UseProphecy(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<ProphecyMiddleware>();
        }
    }
}