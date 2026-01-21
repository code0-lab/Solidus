using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Sockets;

namespace DomusMercatoris.Service.Services
{
    public class PythonRunnerService : IHostedService, IDisposable
    {
        private readonly ILogger<PythonRunnerService> _logger;
        private Process? _pythonProcess;
        private bool _isStopping = false;
        private string? _pythonExecutable;
        private string? _scriptPath;
        private string? _workingDirectory;
        private int _restartCount = 0;
        private const int MaxRestarts = 5;
        private const int ServicePort = 5001;
        private DateTime _lastRestartTime = DateTime.MinValue;

        public PythonRunnerService(ILogger<PythonRunnerService> logger)
        {
            _logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting Python AI Service...");

            // 0. Check if service is already running (e.g. started by another app instance)
            if (IsServiceRunning(ServicePort))
            {
                _logger.LogInformation($"Python AI Service appears to be already running on port {ServicePort}. Skipping start.");
                return Task.CompletedTask;
            }

            // Ensure cleanup on unexpected exit
            AppDomain.CurrentDomain.ProcessExit += (s, e) => KillProcess();

            var currentDir = Directory.GetCurrentDirectory();
            var rootDir = FindProjectRoot(currentDir);
            if (rootDir == null)
            {
                _logger.LogError("Could not find project root containing 'AI' folder. Python service will not start.");
                return Task.CompletedTask;
            }

            _workingDirectory = rootDir;
            _scriptPath = Path.Combine(rootDir, "AI", "api.py");

            // 1. Cross-Platform Python Path Logic
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                _pythonExecutable = Path.Combine(rootDir, "venv", "Scripts", "python.exe");
            }
            else
            {
                _pythonExecutable = Path.Combine(rootDir, "venv", "bin", "python");
            }

            if (!File.Exists(_pythonExecutable))
            {
                var fallback = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "python" : "python3";
                _logger.LogWarning($"Virtual environment python not found at {_pythonExecutable}. Checking for '{fallback}'...");
                _pythonExecutable = fallback;
            }

            if (!File.Exists(_scriptPath))
            {
                _logger.LogError($"Python script not found at {_scriptPath}");
                return Task.CompletedTask;
            }

            // 2. Kill Zombies (Best Effort)
            KillZombieProcesses();

            // 3. Start with Resilience
            StartPythonProcess();

            return Task.CompletedTask;
        }

        private bool IsServiceRunning(int port)
        {
            try
            {
                using var client = new TcpClient();
                var result = client.BeginConnect("127.0.0.1", port, null, null);
                var success = result.AsyncWaitHandle.WaitOne(TimeSpan.FromMilliseconds(500));
                if (success)
                {
                    client.EndConnect(result);
                    return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        private void StartPythonProcess()
        {
            if (_isStopping) return;

            // Reset restart count if last restart was > 1 minute ago
            if ((DateTime.UtcNow - _lastRestartTime).TotalMinutes > 1)
            {
                _restartCount = 0;
            }

            if (_restartCount > MaxRestarts)
            {
                _logger.LogError("Python service crashed too many times. Giving up.");
                return;
            }

            _restartCount++;
            _lastRestartTime = DateTime.UtcNow;

            var startInfo = new ProcessStartInfo
            {
                FileName = _pythonExecutable,
                Arguments = _scriptPath,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = _workingDirectory
            };

            startInfo.EnvironmentVariables["PYTHONUNBUFFERED"] = "1";

            try
            {
                _pythonProcess = new Process { StartInfo = startInfo };
                _pythonProcess.EnableRaisingEvents = true;

                _pythonProcess.OutputDataReceived += (sender, args) =>
                {
                    if (!string.IsNullOrEmpty(args.Data)) _logger.LogInformation($"[Python API]: {args.Data}");
                };

                _pythonProcess.ErrorDataReceived += (sender, args) =>
                {
                    if (!string.IsNullOrEmpty(args.Data))
                    {
                        if (args.Data.Contains("INFO:"))
                            _logger.LogInformation($"[Python API]: {args.Data}");
                        else if (args.Data.Contains("WARNING:"))
                            _logger.LogWarning($"[Python API]: {args.Data}");
                        else
                            _logger.LogError($"[Python API Error]: {args.Data}");
                    }
                };

                _pythonProcess.Exited += OnProcessExited;

                _pythonProcess.Start();
                _pythonProcess.BeginOutputReadLine();
                _pythonProcess.BeginErrorReadLine();

                _logger.LogInformation($"Python API started with PID {_pythonProcess.Id}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start Python API");
            }
        }

        private void OnProcessExited(object? sender, EventArgs e)
        {
            if (_isStopping) return;

            var exitCode = _pythonProcess?.ExitCode ?? -1;
            _logger.LogWarning($"Python process exited unexpectedly with code {exitCode}. Restarting...");

            // Cleanup old instance reference
            _pythonProcess?.Dispose();
            _pythonProcess = null;

            // Use Task.Run to avoid blocking the event thread and allow async delay
            Task.Run(async () =>
            {
                await Task.Delay(2000);
                StartPythonProcess();
            });
        }

        private void KillZombieProcesses()
        {
            try
            {
                // Best effort to kill previous instances of api.py
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    // Use PowerShell instead of deprecated WMIC
                    // Get-CimInstance is the modern replacement for Get-WmiObject
                    var psCommand = "Get-CimInstance Win32_Process | Where-Object { $_.CommandLine -like '*AI\\\\api.py*' } | ForEach-Object { Stop-Process -Id $_.ProcessId -Force }";
                    
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "powershell",
                        Arguments = $"-NoProfile -Command \"{psCommand}\"",
                        UseShellExecute = false,
                        CreateNoWindow = true
                    })?.WaitForExit();
                }
                else
                {
                    // Unix pkill
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "pkill",
                        Arguments = "-f AI/api.py",
                        UseShellExecute = false,
                        CreateNoWindow = true
                    })?.WaitForExit();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Failed to cleanup zombie processes: {ex.Message}");
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _isStopping = true;
            _logger.LogInformation("Stopping Python AI Service...");
            KillProcess();
            return Task.CompletedTask;
        }

        private void KillProcess()
        {
            if (_pythonProcess != null)
            {
                try
                {
                    // Detach event handler to prevent restart logic during intentional stop
                    _pythonProcess.Exited -= OnProcessExited;
                    
                    if (!_pythonProcess.HasExited)
                    {
                        _pythonProcess.Kill();
                        _pythonProcess.WaitForExit(3000); // Wait up to 3s
                    }
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