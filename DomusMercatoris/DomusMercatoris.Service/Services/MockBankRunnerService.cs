using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace DomusMercatoris.Service.Services
{
    public class MockBankRunnerService : IHostedService, IDisposable
    {
        private readonly ILogger<MockBankRunnerService> _logger;
        private readonly MockBankInfo _mockBankInfo;
        private int _port;
        private Process? _process;
        private bool _isStopping = false;

        public MockBankRunnerService(ILogger<MockBankRunnerService> logger, IConfiguration configuration, MockBankInfo mockBankInfo)
        {
            _logger = logger;
            _mockBankInfo = mockBankInfo;
            // İlk olarak config'den portu almayı dene, yoksa 5003 kullan
            var portConfig = configuration["MockBank:Port"];
            if (int.TryParse(portConfig, out int parsedPort))
            {
                _port = parsedPort;
            }
            else
            {
                _port = 5003;
            }
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            // Portun uygunluğunu kontrol et, doluysa yeni bir port bul
            if (IsPortInUse(_port))
            {
                _logger.LogWarning($"Port {_port} is in use. Searching for an available port...");
                _port = GetAvailablePort(5003); // 5003'ten başlayarak boş port ara
                _logger.LogInformation($"Found available port: {_port}");
            }

            // Port belirlendikten sonra BaseUrl'i güncelle
            _mockBankInfo.BaseUrl = $"http://localhost:{_port}";

            _logger.LogInformation($"Starting Mock Bank Service on port {_port}...");

            var currentDir = Directory.GetCurrentDirectory();
            var projectRoot = FindProjectRoot(currentDir);
            
            if (projectRoot == null)
            {
                _logger.LogError("Could not find project root. Mock Bank will not start.");
                return Task.CompletedTask;
            }

            var mockBankPath = Path.Combine(projectRoot, "DomusMercatoris.MockBank");
            if (!Directory.Exists(mockBankPath))
            {
                _logger.LogError($"Mock Bank directory not found at {mockBankPath}");
                return Task.CompletedTask;
            }

            StartProcess(mockBankPath);
            return Task.CompletedTask;
        }

        private int GetAvailablePort(int startingPort)
        {
            int port = startingPort;
            while (IsPortInUse(port))
            {
                port++;
                if (port > 65535) throw new Exception("No available ports found.");
            }
            return port;
        }

        private bool IsPortInUse(int port)
        {
            try
            {
                using var client = new TcpClient();
                var result = client.BeginConnect("127.0.0.1", port, null, null);
                bool success = result.AsyncWaitHandle.WaitOne(TimeSpan.FromMilliseconds(100));
                if (success)
                {
                    client.EndConnect(result);
                    return true; // Bağlantı başarılıysa port doludur
                }
                return false;
            }
            catch 
            { 
                return false; // Hata alıyorsak port muhtemelen boştur
            }
        }

        private void StartProcess(string workingDir)
        {
             if (_isStopping) return;

             var startInfo = new ProcessStartInfo
             {
                 FileName = "dotnet",
                 Arguments = $"run --urls \"{_mockBankInfo.BaseUrl}\"",
                 WorkingDirectory = workingDir,
                 RedirectStandardOutput = true,
                 RedirectStandardError = true,
                 UseShellExecute = false,
                 CreateNoWindow = true
             };
             
             try
             {
                 _process = new Process { StartInfo = startInfo };
                 _process.OutputDataReceived += (s, e) => { if (e.Data != null) _logger.LogInformation($"[MockBank] {e.Data}"); };
                 _process.ErrorDataReceived += (s, e) => { if (e.Data != null) _logger.LogError($"[MockBank Error] {e.Data}"); };
                 _process.Start();
                 _process.BeginOutputReadLine();
                 _process.BeginErrorReadLine();
                 _logger.LogInformation($"Mock Bank Service started at {_mockBankInfo.BaseUrl}");
             }
             catch (Exception ex)
             {
                 _logger.LogError(ex, "Failed to start Mock Bank");
             }
        }



        public Task StopAsync(CancellationToken cancellationToken)
        {
            _isStopping = true;
            _process?.Kill();
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _process?.Dispose();
        }

        private string? FindProjectRoot(string startPath)
        {
            var dir = new DirectoryInfo(startPath);
            while (dir != null)
            {
                if (Directory.Exists(Path.Combine(dir.FullName, "DomusMercatoris.MockBank")))
                {
                    return dir.FullName;
                }
                dir = dir.Parent;
            }
            return null;
        }
    }
}
