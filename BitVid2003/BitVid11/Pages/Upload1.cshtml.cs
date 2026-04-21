using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.IO;
using System.Threading.Tasks;

namespace BitVid11.Pages
{
    public class Upload1Model : PageModel
    {
        private readonly IWebHostEnvironment _env;

        public Upload1Model(IWebHostEnvironment env)
        {
            _env = env;
        }

        [BindProperty]
        public IFormFile? File { get; set; }

        public string? Message { get; set; }

        public async Task<IActionResult> OnPostAsync()
        {
            if (File == null || File.Length == 0)
            {
                Message = "No file selected.";
                return Page();
            }

            var videosPath = Path.Combine(_env.WebRootPath, "videos");

            if (!Directory.Exists(videosPath))
            {
                Directory.CreateDirectory(videosPath);
            }

            var filePath = Path.Combine(videosPath, Path.GetFileName(File.FileName));

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await File.CopyToAsync(stream);
            }

            Message = $"File uploaded successfully to /videos/{File.FileName}";
            return Page();
        }
    }
}
