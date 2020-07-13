using System.Threading.Tasks;
using Nexus.Core;

namespace Nexus.Prophecy.Services.Control
{
    public static class ControlServiceExtensions
    {
        public static async Task<Result> RebuildAsync(this IControlService controlService, string service)
        {
            var stopResult = await controlService.StopAsync(service).ConfigureAwait(false);
            if (stopResult.IsFail)
                return Result.Fail($"Rebuild fails on \"stop-stage\":\nError: {stopResult.Error}");

            var buildResult = await controlService.BuildAsync(service).ConfigureAwait(false);
            if (buildResult.IsFail)
                return Result.Fail($"Rebuild fails on \"build-stage\". Error: {buildResult.Error}");

            var runResult = await controlService.StartAsync(service).ConfigureAwait(false);
            if (runResult.IsFail)
                return Result.Fail($"Rebuild fails on \"run-stage\". Error: {runResult.Error}");
            
            return Result.Ok();
        }
        
        public static async Task<Result> RestartAsync(this IControlService controlService, string service)
        {
            var stopResult = await controlService.StopAsync(service).ConfigureAwait(false);
            if (stopResult.IsFail)
                return Result.Fail($"Rebuild fails on \"stop-stage\":\nError: {stopResult.Error}");

            var runResult = await controlService.StartAsync(service).ConfigureAwait(false);
            if (runResult.IsFail)
                return Result.Fail($"Rebuild fails on \"run-stage\". Error: {runResult.Error}");
            
            return Result.Ok();
        }
    }
}