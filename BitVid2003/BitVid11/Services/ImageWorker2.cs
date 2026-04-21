using Org.BouncyCastle.Crypto.Generators;
using System.Diagnostics;

namespace BitVid11.Services
{
    public class ImageWorker2 : IDisposable
    {
        private readonly SemaphoreSlim _gpuLock = new(1, 1); // serialize GPU access
        private readonly ILogger<ImageWorker2> _logger;

        private readonly string _pythonExe;
        private readonly string _scriptPath;

        public ImageWorker2(ILogger<ImageWorker2> logger)
        {
            _logger = logger;

            //_pythonExe = @"C:\BitVidPremium\BitVid2003\BitVid11\zimageturbo\nunchaku\nunchakuturbo\Scripts\python.exe";
            //_scriptPath = @"C:\BitVidPremium\BitVid2003\BitVid11\zimageturbo\nunchaku\zimagegenerate.py";
            _pythonExe = @"C:\zimageturbo\nunchaku\nunchakuturbo\Scripts\python.exe";
            _scriptPath = @"C:\zimageturbo\nunchaku\zimagegenerate.py";


            _logger.LogInformation("TurboImageWorker2 initialized.");
        }

        /// <summary>
        /// Generates an image from the given prompt.
        /// Queues GPU access so only one generation runs at a time.
        /// Returns relative path to image (e.g., "/images/xyz.png") or null if failed.
        /// </summary>
        public async Task<string?> GenerateAsync(string prompt)
        {
            await _gpuLock.WaitAsync(); // queue this request if another is running
            try
            {
                _logger.LogInformation("Starting TurboImage generation for prompt: {Prompt}", prompt);

                var psi = new ProcessStartInfo
                {
                    FileName = _pythonExe,
                    Arguments = $"\"{_scriptPath}\" \"{prompt}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(psi);
                if (process == null)
                {
                    _logger.LogError("Failed to start TurboImage Python process.");
                    return null;
                }

                string output = await process.StandardOutput.ReadToEndAsync();
                string error = await process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync();

                if (process.ExitCode != 0)
                {
                    _logger.LogError("TurboImage failed. ExitCode: {Code}, Error: {Error}", process.ExitCode, error);
                    return null;
                }

                output = output.Trim();
                if (string.IsNullOrEmpty(output))
                {
                    _logger.LogWarning("TurboImage returned empty output.");
                    return null;
                }

                string imagePath = output.Contains("wwwroot")
                    ? "/" + output.Substring(output.IndexOf("wwwroot") + "wwwroot".Length).Replace("\\", "/")
                    : output.Replace("\\", "/");

                if (!imagePath.StartsWith("/"))
                    imagePath = "/" + imagePath;

                _logger.LogInformation("TurboImage generated: {Path}", imagePath);
                return imagePath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception during TurboImage generation");
                return null;
            }
            finally
            {
                _gpuLock.Release();
            }
        }

        public void Dispose()
        {
            // nothing to clean up, no persistent process
        }
    }
}
