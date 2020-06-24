using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Nexus.Core;
using Nexus.Prophecy.Configuration;

namespace Nexus.Prophecy.Services.Control
{
    public class ControlService : IControlService
    {
        private readonly ProphecySettings settings;
        private readonly Dictionary<string, Process[]> liveServices = new Dictionary<string, Process[]>();
        // нужен ли словарь вообще
        
        private const string DirectoryName = "Commands";

        public ControlService(ProphecySettings settings)
        {
            this.settings = settings;
            if (!Directory.Exists(DirectoryName))
                Directory.CreateDirectory(DirectoryName);

            foreach (var service in settings.Services)
            {
                if (!Directory.Exists(service.Key))
                    Directory.CreateDirectory(service.Key);
            }
        }

        public async Task<Result<string>> RunCommandAsync(string service, string command)
        {
            var filename = TryGetCommandFilename(service, command);
            if (filename.IsFail)
                return filename;

            var processInfo = new ProcessStartInfo(filename)
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true
            };

            var output = new List<string>();
            await Task.Run(() =>
            {
                var process = Process.Start(processInfo);

                process.OutputDataReceived += (sender, e) => output.Add($"[OUT]: {e.Data}");
                process.BeginOutputReadLine();
                process.ErrorDataReceived += (sender, e) => output.Add($"[ERR]: {e.Data}");
                process.BeginErrorReadLine();
            
                process.WaitForExit();
                process.Close();
            }).ConfigureAwait(false);

            return Result<string>.Ok(string.Join(Environment.NewLine, output));
        }

        public async Task<Result<ServiceInfo>> StartAsync(string service)
        {
            var serviceInfo = GetServiceInfo(service);
            if (serviceInfo.IsFail)
                return serviceInfo;
            
            if (serviceInfo.Value.IsRunning)
                return $"{service} is already running";

            try
            {
                var processInfo = new ProcessStartInfo(settings.Services[service].Path)
                {
                    CreateNoWindow = false,
                    UseShellExecute = true,
                    RedirectStandardError = false,
                    RedirectStandardOutput = false
                };

                var process = Process.Start(processInfo);
                liveServices[service] = new[] {process};

                return GetServiceInfo(service);
            }
            catch (Exception e)
            {
                return e.Message;
            }
        }

        public async Task<Result<ServiceInfo>> StopAsync(string service)
        {
            if (!liveServices.TryGetValue(service, out var processes))
                return $"{service} is already stopped";

            await Task.Run(() =>
            {
                foreach (var process in processes)
                {
                    process.Close();
                    process.WaitForExit();
                }
                
                liveServices.Remove(service);
            }).ConfigureAwait(false);

            return GetServiceInfo(service);
        }

        public IEnumerable<ServiceInfo> ListServices()
        {
            return settings.Services.Select(s => GetServiceInfo(s.Key))
                .Select(s => s.Value); // meh всегда велью у резалта
        }

        public Result<ServiceInfo> GetServiceInfo(string service)
        {
            if (!settings.Services.TryGetValue(service, out var serviceSettings))
                return $"Unknown service \"{service}\"";

            if (!liveServices.ContainsKey(service))
            {
                var processes = Process.GetProcessesByName(service);
                if (processes.Length != 0)
                    liveServices[service] = processes;
            }
            
            return new ServiceInfo
            {
                Commands = serviceSettings.Commands ?? new Dictionary<string, string>(),
                IsRunning = liveServices.ContainsKey(service),
                Name = service,
                MetaInfo = new ServiceMetaInfo
                {
                    Url = serviceSettings.MetaInfo?.Url ?? string.Empty
                }
            };
        }

        public Result RemoveCommand(string service, string command)
        {
            var filename = TryGetCommandFilename(service, command);
            if (filename.IsFail)
                return filename;
            
            File.Delete(filename);
            settings.Services[service].Commands.Remove(command);
            
            return Result.Ok();
        }

        public Result UpdateCommand(string service, string command, string commandBody)
        {
            var filename = TryGetCommandFilename(service, command);
            if (filename.IsFail)
                return filename;
            
            File.WriteAllText(filename, commandBody);
            
            return Result.Ok();
        }

        private Result<string> TryGetCommandFilename(string service, string command)
        {
            if (!settings.Services.TryGetValue(service, out var commands))
                return Result.Fail<string>($"Unknown service \"{service}\"");
            
            if (!commands.Commands.TryGetValue(command, out var filename))
                return Result<string>.Fail($"Unknown command \"{filename}\"");

            var path = Path.Combine(DirectoryName, service, filename);
            return !File.Exists(path) 
                ? Result<string>.Fail($"Missing file \"{path}\"") 
                : Result<string>.Ok(path);
        }
    }
}