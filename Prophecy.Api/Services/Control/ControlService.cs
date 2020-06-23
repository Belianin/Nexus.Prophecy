using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Nexus.Core;
using Nexus.Prophecy.Configuration;

namespace Nexus.Prophecy.Services.Control
{
    public class ControlService : IControlService
    {
        private readonly ProphecySettings settings;
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

        public IEnumerable<string> ListServices()
        {
            return settings.Services.Keys;
        }

        public Result<IEnumerable<string>> ListCommands(string service)
        {
            if (!settings.Services.TryGetValue(service, out var commands))
                return $"Unknown service \"{service}\"";

            return commands.Commands.Keys;
        }

        public Result<string> ShowCommand(string service, string command)
        {
            var filename = TryGetCommandFilename(service, command);
            if (filename.IsFail)
                return filename;

            return Result<string>.Ok(File.ReadAllText(filename));
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