using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Nexus.Core;
using Nexus.Prophecy.Configuration;

namespace Nexus.Prophecy.Services.Control
{
    public class ControlService : IControlService
    {
        private readonly ProphecySettings settings;

        public ControlService(ProphecySettings settings)
        {
            this.settings = settings;
        }

        public Task<Result<string>> RunCommandAsync(string service, string command)
        {
            var filename = TryGetScriptFilename(service, command);
            
            return filename.IsFail ? Task.FromResult(filename) : RunProcessAsync(command);
        }

        public Task<Result<ServiceInfo>> StartAsync(string service)
        {
            var serviceInfo = GetServiceInfo(service);
            if (serviceInfo.IsFail)
                return Task.FromResult(serviceInfo);
            
            if (serviceInfo.Value.IsRunning)
                return Task.FromResult(Result<ServiceInfo>.Fail($"{service} is already running"));

            var paths = settings.GetServiceFullPath(service);

            return Task.Run(() =>
            {
                try
                {
                    var processInfo = new ProcessStartInfo(paths.Executable)
                    {
                        CreateNoWindow = false,
                        UseShellExecute = true,
                        RedirectStandardError = false,
                        RedirectStandardOutput = false,
                        WorkingDirectory = paths.Root
                    };

                    var process = Process.Start(processInfo);

                    Thread.Sleep(1000 * 5);
                    if (process == null || process.HasExited)
                        return $"Failed to run {service}";
                    
                    return GetServiceInfo(service);
                }
                catch (Exception e)
                {
                    return e.Message;
                }
            });
        }

        public async Task<Result<ServiceInfo>> StopAsync(string service)
        {
            var processes = Process.GetProcessesByName(service);
            if (processes.Length == 0)
                return $"{service} is already stopped";

            void CloseTask(Process process)
            {
                process.CloseMainWindow();
                process.Close();
            }

            try
            {
                await Task.WhenAll(processes.Select(p => Task.Run(() => CloseTask(p)))).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                return $"Failed to stop {service}. Error: {e.Message}";
            }

            await Task.Delay(1000 * 5).ConfigureAwait(false);
            
            return GetServiceInfo(service);
        }

        public Task<Result<string>> BuildAsync(string service)
        {
            var serviceInfoResult = GetServiceInfo(service);
            if (serviceInfoResult.IsFail)
                return Task.FromResult(Result<string>.Fail(serviceInfoResult.Error));
            var serviceInfo = serviceInfoResult.Value;

            var script = new StringBuilder()
                .Append("/c git reset --hard HEAD")
                .Append(" && git pull")
                .Append(" && dotnet build -c Release")
                .ToString();

            return RunProcessAsync("cmd.exe", script, serviceInfo.Path);
        }

        public IEnumerable<ServiceInfo> ListServices()
        {
            return settings.Services.Select(s => GetServiceInfo(s.Key))
                .Select(s => s.Value);
        }

        public Result<ServiceInfo> GetServiceInfo(string service)
        {
            if (!settings.Services.TryGetValue(service, out var serviceSettings))
                return $"Unknown service \"{service}\"";

            var processes = Process.GetProcessesByName(service);
            var isRunning = processes.Length != 0;
            
            return new ServiceInfo
            {
                Commands = serviceSettings.Scripts ?? new Dictionary<string, string>(),
                IsRunning = isRunning,
                Name = service,
                Path = serviceSettings.Path,
                MetaInfo = new ServiceMetaInfo
                {
                    Url = serviceSettings.MetaInfo?.Url ?? string.Empty,
                    Project = serviceSettings.MetaInfo?.Project
                },
                MemoryUsage = GetMemoryUsage(processes)
            };
        }

        public Result RemoveCommand(string service, string command)
        {
            var filename = TryGetScriptFilename(service, command);
            if (filename.IsFail)
                return filename;
            
            File.Delete(filename);
            settings.Services[service].Scripts.Remove(command);
            
            return Result.Ok();
        }

        public Result UpdateCommand(string service, string command, string commandBody)
        {
            var filename = TryGetScriptFilename(service, command);
            if (filename.IsFail)
                return filename;
            
            File.WriteAllText(filename, commandBody);
            
            return Result.Ok();
        }

        public (long total, long free) GetSystemMemoryInfo()
        {
            var winQuery = new ObjectQuery("SELECT * FROM Win32_LogicalMemoryConfiguration");

            var searcher = new ManagementObjectSearcher(winQuery);

            var total = 0L;
            var free = 0L;

            foreach (var o in searcher.Get())
            {
                var item = (ManagementObject) o;
                Console.WriteLine("Total Space = " + item["TotalPageFileSpace"]);
                Console.WriteLine("Total Physical Memory = " + item["TotalPhysicalMemory"]);
                Console.WriteLine("Total Virtual Memory = " + item["TotalVirtualMemory"]);
                Console.WriteLine("Available Virtual Memory = " + item["AvailableVirtualMemory"]);
            }

            return (total, free);
        }

        private Result<string> TryGetScriptFilename(string service, string script)
        {
            if (!settings.Services.TryGetValue(service, out var serviceInfo))
                return Result.Fail<string>($"Unknown service \"{service}\"");
            
            if (!serviceInfo.Scripts.TryGetValue(script, out var filename))
                return Result<string>.Fail($"Unknown command \"{script}\"");

            return !File.Exists(filename) 
                ? Result<string>.Fail($"Missing file \"{filename}\"") 
                : Result<string>.Ok(filename);
        }

        private long GetMemoryUsage(IEnumerable<Process> processes)
        {
            return processes?.Sum(p => p.WorkingSet64) ?? 0;
        }

        private Task<Result<string>> RunProcessAsync(string filename, string arguments = null, string workingDirectory = null)
        {
            var processStartInfo = new ProcessStartInfo
            {
                FileName = filename,
                CreateNoWindow = false,
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true
            };

            if (arguments != null)
                processStartInfo.Arguments = arguments;
            if (workingDirectory != null)
                processStartInfo.WorkingDirectory = workingDirectory;

            return Task.Run(() => RunProcess(processStartInfo));
            
        }

        private static Result<string> RunProcess(ProcessStartInfo processStartInfo)
        {
            try
            {
                var process = Process.Start(processStartInfo);
                if (process == null)
                    return Result.Fail<string>($"Process {processStartInfo.FileName} failed to run");
                
                var output = new List<string>();

                process.OutputDataReceived += (sender, e) => output.Add($"{e.Data}");
                process.BeginOutputReadLine();
                process.ErrorDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrWhiteSpace(e.Data))
                        output.Add($"ERR: {e.Data}");
                };
                process.BeginErrorReadLine();
            
                process.WaitForExit(1000 * 60);
                process.Close();

                return Result<string>.Ok(string.Join(Environment.NewLine, output));
            }
            catch (Exception e)
            {
                return Result<string>.Fail($"Process {processStartInfo.FileName} failed to run with exception: {e.Message}");
            }
        }
    }
}