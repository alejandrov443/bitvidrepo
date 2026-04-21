using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Newtonsoft.Json;

namespace BitVid11.Pages
{

    public class IndexModel : PageModel
    {

        private readonly IWebHostEnvironment _env;
        public List<string> Videos { get; set; }

        public IndexModel(IWebHostEnvironment env)
        {
            _env = env;
        }

        public void OnGet()
        {
            Videos = GetAllVideos();
        }

        public List<string> GetAllVideos()
        {
            string videoPath = Path.Combine(_env.WebRootPath, "videos"); // e.g., wwwroot/videos
            if (!Directory.Exists(videoPath))
            {
                return new List<string>(); // Return empty if directory doesn't exist
            }

            string[] videoExtensions = { ".mp4", ".avi", ".mkv", ".mov", ".webm" };
            var videos = Directory.GetFiles(videoPath)
                                  .Where(file => videoExtensions.Contains(Path.GetExtension(file).ToLower()))
                                  .Select(file => "/videos/" + Path.GetFileName(file)) // Convert to relative URL
                                  .ToList();

            return videos;
        }
    }

}