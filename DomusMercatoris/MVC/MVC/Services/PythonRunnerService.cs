using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MVC.Services
{
    public class PythonRunnerService : IHostedService, IDisposable
    {
        private readonly ILogger<PythonRunnerService> _logger;
        private Process? _pythonProcess;

        public PythonRunnerService(ILogger<PythonRunnerService> logger)
        {
            _logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting Python AI Service...");

            var currentDir = Directory.GetCurrentDirectory();
            // Assuming we are in MVC/MVC, we need to go up two levels to find the root where 'AI' and 'venv' are.
            // However, let's try to locate the 'AI' folder dynamically to be safer.
            
            var rootDir = FindProjectRoot(currentDir);
            if (rootDir == null)
            {
                _logger.LogError("Could not find project root containing 'AI' folder. Python service will not start.");
                return Task.CompletedTask;
            }

            var pythonExecutable = Path.Combine(rootDir, "venv", "bin", "python");
            var scriptPath = Path.Combine(rootDir, "AI", "api.py");

            if (!File.Exists(pythonExecutable))
            {
                // Fallback: try to use system python if venv not found (though venv is preferred)
                // Or maybe the venv is named differently? user created 'venv' in previous step.
                _logger.LogWarning($"Virtual environment python not found at {pythonExecutable}. Checking for 'python3'...");
                pythonExecutable = "python3"; 
            }

            if (!File.Exists(scriptPath))
            {
                _logger.LogError($"Python script not found at {scriptPath}");
                return Task.CompletedTask;
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = pythonExecutable,
                Arguments = scriptPath,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = rootDir // Run from root so relative paths in python script work
            };

            // Set environment variables if needed, e.g., PYTHONUNBUFFERED
            startInfo.EnvironmentVariables["PYTHONUNBUFFERED"] = "1";

            try
            {
                _pythonProcess = new Process { StartInfo = startInfo };
                
                _pythonProcess.OutputDataReceived += (sender, args) => 
                {
                    if (!string.IsNullOrEmpty(args.Data)) _logger.LogInformation($"[Python API]: {args.Data}");
                };
                
                _pythonProcess.ErrorDataReceived += (sender, args) => 
                {
                    if (!string.IsNullOrEmpty(args.Data))
                    {
                        if (args.Data.Contains("INFO:"))
                        {
                            _logger.LogInformation($"[Python API]: {args.Data}");
                        }
                        else if (args.Data.Contains("WARNING:"))
                        {
                            _logger.LogWarning($"[Python API]: {args.Data}");
                        }
                        else
                        {
                            _logger.LogError($"[Python API Error]: {args.Data}");
                        }
                    }
                };

                _pythonProcess.Start();
                _pythonProcess.BeginOutputReadLine();
                _pythonProcess.BeginErrorReadLine();

                _logger.LogInformation($"Python API started with PID {_pythonProcess.Id}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start Python API");
            }

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Stopping Python AI Service...");
            KillProcess();
            return Task.CompletedTask;
        }

        private void KillProcess()
        {
            if (_pythonProcess != null && !_pythonProcess.HasExited)
            {
                try
                {
                    _pythonProcess.Kill();
                    _pythonProcess.WaitForExit(); // Wait for it to verify
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error stopping Python process");
                }
                finally
                {
                    _pythonProcess.Dispose();
                    _pythonProcess = null;
                }
            }
        }

        public void Dispose()
        {
            KillProcess();
        }

        private string? FindProjectRoot(string startPath)
        {
            var dir = new DirectoryInfo(startPath);
            while (dir != null)
            {
                if (Directory.Exists(Path.Combine(dir.FullName, "AI")))
                {
                    return dir.FullName;
                }
                dir = dir.Parent;
            }
            return null;
        }
    }
}
