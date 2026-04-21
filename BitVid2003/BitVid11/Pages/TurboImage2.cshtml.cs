using BitVid11.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Json;

namespace BitVid11.Pages
{
    public class TurboImage2Model : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IWebHostEnvironment _env;

        public TurboImage2Model(IHttpClientFactory httpClientFactory, IWebHostEnvironment env)
        {
            _httpClientFactory = httpClientFactory;
            _env = env;
        }

        [BindProperty]
        public string Prompt { get; set; }

        public string ImagePath { get; set; }

        // --------------------
        // Read JSON config
        // --------------------
        private (string url, string apiKey) ReadZImageConfig()
        {
            string path = Path.Combine(_env.ContentRootPath, "Generator.json");
            if (!System.IO.File.Exists(path))
                return (null!, null!);

            string json = System.IO.File.ReadAllText(path);
            using var doc = JsonDocument.Parse(json);
            string url = doc.RootElement.GetProperty("ZImageApiUrl").GetString()!;
            string apiKey = doc.RootElement.GetProperty("ApiKey").GetString()!;
            return (url, apiKey);
        }

        // --------------------
        // GET request
        // --------------------
        public void OnGet()
        {
            Prompt = "a young male cooking in the kitchen";
        }

        // --------------------
        // POST request (generate image)
        // --------------------
        public async Task OnPostAsync()
        {
            if (string.IsNullOrWhiteSpace(Prompt))
            {
                ImagePath = null;
                return;
            }

            var (fastApiUrl, apiKey) = ReadZImageConfig();
            if (string.IsNullOrEmpty(fastApiUrl))
            {
                ImagePath = null;
                return;
            }

            try
            {
                var client = _httpClientFactory.CreateClient();
                var requestData = new { prompt = Prompt };

                var request = new HttpRequestMessage(HttpMethod.Post, fastApiUrl)
                {
                    Content = JsonContent.Create(requestData)
                };
                request.Headers.Add("x-api-key", apiKey);

                var response = await client.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    ImagePath = null;
                    return;
                }

                var json = await response.Content.ReadAsStringAsync();
                Console.WriteLine("Response: " + json);
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("path", out var pathElement))
                {
                    string path = pathElement.GetString()!;
                    // Full URL to display image
                    ImagePath = fastApiUrl.Replace("/generate", "") + path;
                }
            }
            catch
            {
                ImagePath = null;
            }
        }

        // --------------------
        // GET request to download image
        // --------------------
        public async Task<IActionResult> OnGetDownloadAsync(string url)
        {
            if (string.IsNullOrEmpty(url))
                return BadRequest("Missing URL");

            try
            {
                var client = _httpClientFactory.CreateClient();
                var response = await client.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                    return NotFound();

                var contentType = response.Content.Headers.ContentType?.ToString() ?? "application/octet-stream";
                var data = await response.Content.ReadAsByteArrayAsync();

                // Return the image as downloadable file
                return File(data, contentType, "generated_image.png");
            }
            catch
            {
                return StatusCode(500);
            }
        }
    }

    // --------------------
    // JSON config model
    // --------------------
    public class ZImageConfig
    {
        public string ZImageApiUrl { get; set; } = string.Empty;
        public string ApiKey { get; set; } = string.Empty;
    }
}
