using BitVid11.Data;
using BitVid11.Hubs;
using BitVid11.Models;
using BitVid11.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace BitVid11.Pages
{
    public class GenerateModel : PageModel
    {
        private readonly ApplicationDbContext _dbContext;

        private readonly IWebHostEnvironment _env;

        private readonly IHttpClientFactory _httpClientFactory;

        private readonly HttpClient _httpClient;

        public GenerateModel(
            ApplicationDbContext dbContext,
            IWebHostEnvironment env, IHttpClientFactory httpClientFactory)
        {
            _dbContext = dbContext;
            _env = env;
            _httpClientFactory = httpClientFactory;
            _httpClient = httpClientFactory.CreateClient();
        }

        [BindProperty]
        public string Prompt { get; set; }

        [BindProperty]
        public IFormFile Reference { get; set; }

        public string JobId { get; set; }

        public async Task<IActionResult> OnPostAsync()
        {
            int userId = int.TryParse(Request.Cookies["UserId"], out var id) ? id : 0;

            JobId = Guid.NewGuid().ToString();

            string savedFilePath = null;

            if (Reference != null)
            {
                var ext = Path.GetExtension(Reference.FileName);
                var fileName = $"{Guid.NewGuid()}{ext}";

                var uploadDir = Path.Combine(_env.WebRootPath, "uploadedimages");

                if (!Directory.Exists(uploadDir))
                    Directory.CreateDirectory(uploadDir);

                var physicalPath = Path.Combine(uploadDir, fileName);

                using var stream = new FileStream(physicalPath, FileMode.Create);
                await Reference.CopyToAsync(stream);

                savedFilePath = "/uploadedimages/" + fileName;
            }

            var job = new VideoJobs
            {
                UserId = userId,
                JobId = JobId,
                Prompt = Prompt,
                Status = "Received",
                VideoPath = null,
                FileName = null,
                GalleryType = "private",
                CreatedAt = DateTime.UtcNow,
                uploadedImagePath = savedFilePath

            };

            await QueueJob(savedFilePath, Prompt, job);
            

            _dbContext.VideoJobs.Add(job);
            await _dbContext.SaveChangesAsync();

            return Page();
        }

        public async Task QueueJob(string tempImageFilePath, string prompt, VideoJobs job)
        {

            var client = _httpClientFactory.CreateClient();
            using var content = new MultipartFormDataContent();
            content.Add(new StringContent(prompt), "prompt");

            var physicalPath = Path.Combine(_env.WebRootPath, tempImageFilePath.TrimStart('/'));

            if (System.IO.File.Exists(physicalPath))
            {
                var stream = System.IO.File.OpenRead(physicalPath);
                var fileContent = new StreamContent(stream);
                fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
                content.Add(fileContent, "reference", Path.GetFileName(physicalPath));

            }

            var pythonApiUrl = "http://localhost:8000/generate";
            var apiKey = "supersecretkey";
            client.DefaultRequestHeaders.TryAddWithoutValidation("x-api-key", apiKey);

            // send GENERATE VIDEO REQUEST
            var response = await client.PostAsync(pythonApiUrl, content);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();

            using var doc = JsonDocument.Parse(json);
            var jobId = doc.RootElement.GetProperty("result").GetString();

            Console.WriteLine("JobID: " + jobId);


            job.Status = "Queued";
            job.JobIdentification = jobId;

        }


        public async Task<string> ImprovePrompt2(string prompt, string tempImageFilePath)
        {

            var imageBytes = await System.IO.File.ReadAllBytesAsync(@"C:\BitVidPremium9\BitVid2003\BitVid11\wwwroot" + tempImageFilePath);
            var base64Image = Convert.ToBase64String(imageBytes);

            var requestBody = new { model = "gemma3:12b", prompt = "Improve the prompt for this image for video generation and make it detailed.\r\n\r\nJust Return the new prompt nothing else\r\n\r\nHere is the prompt:" + prompt, images = new[] { base64Image }, stream = false };

            var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost:11434/api/generate")
            {
                Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json")
            };

            var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

            response.EnsureSuccessStatusCode();

            var jsonString = await response.Content.ReadAsStringAsync();

            var result = JsonSerializer.Deserialize<OllamaGenerateResponse>(jsonString);

            return result?.response ?? "";


        }

    }
}
