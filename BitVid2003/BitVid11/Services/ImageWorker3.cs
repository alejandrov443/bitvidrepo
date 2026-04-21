using Org.BouncyCastle.Crypto.Generators;
using System.Diagnostics;
using System.Text.Json;

namespace BitVid11.Services
{
    public class ImageWorker3 : IDisposable
    {
        private readonly SemaphoreSlim _gpuLock = new(1, 1); // serialize access
        private readonly ILogger<ImageWorker2> _logger;
        private readonly HttpClient _http;

        // Replace this with your actual public tunnel URL
        private const string BaseUrl = "https://flu-floral-saturday-joel.trycloudflare.com";
        private const string ApiKey = "supersecretkey"; // must match FastAPI API_KEY

        public ImageWorker3(ILogger<ImageWorker2> logger, HttpClient? httpClient = null)
        {
            _logger = logger;
            _http = httpClient ?? new HttpClient();
            _logger.LogInformation("TurboImageWorker3 initialized (HTTP mode).");
        }

        /// <summary>
        /// Generates an image from the given prompt by calling FastAPI POST /generate
        /// Returns full public URL to the generated image or null if failed.
        /// </summary>
        public async Task<string?> GenerateAsync(string prompt)
        {
            await _gpuLock.WaitAsync();
            try
            {
                _logger.LogInformation("Starting TurboImage generation for prompt: {Prompt}", prompt);

                var request = new
                {
                    prompt = prompt
                };

                var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/generate")
                {
                    Content = JsonContent.Create(request)
                };
                httpRequest.Headers.Add("x-api-key", ApiKey);

                var response = await _http.SendAsync(httpRequest);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("TurboImage API failed. StatusCode: {StatusCode}", response.StatusCode);
                    return null;
                }

                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                if (!doc.RootElement.TryGetProperty("path", out var pathProp))
                {
                    _logger.LogWarning("TurboImage API response missing 'path': {Json}", json);
                    return null;
                }

                string relativePath = pathProp.GetString()!;
                if (!relativePath.StartsWith("/"))
                    relativePath = "/" + relativePath;

                string publicUrl = BaseUrl.TrimEnd('/') + relativePath;
                _logger.LogInformation("TurboImage generated: {Url}", publicUrl);

                return publicUrl;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception during TurboImage HTTP generation");
                return null;
            }
            finally
            {
                _gpuLock.Release();
            }
        }

        public void Dispose()
        {
            _http.Dispose();
        }
    }
}
