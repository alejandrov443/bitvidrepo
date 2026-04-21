using BitVid11.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace BitVid11.Pages
{
    public class MyVideos1Model : PageModel
    {
        private readonly ApplicationDbContext _dbContext;

        public List<string> Videos { get; set; } = new List<string>();

        public MyVideos1Model(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task OnGetAsync()
        {
            // Get the current logged-in user's ID from cookie or claims
            int userId = 0;

            // Try claim first (preferred)
            var userIdClaim = User.FindFirst("UserId");
            if (userIdClaim != null)
            {
                int.TryParse(userIdClaim.Value, out userId);
            }
            else
            {
                // fallback to cookie
                if (Request.Cookies.ContainsKey("UserId"))
                {
                    int.TryParse(Request.Cookies["UserId"], out userId);
                }
            }

            if (userId == 0)
            {
                // Not logged in
                return;
            }

            // Fetch videos for the user where status = 'Done'
            var videoJobs = await _dbContext.VideoJobs
                .Where(v => v.UserId == userId && v.Status == "Done")
                .ToListAsync();

            // Build relative URLs to wwwroot/ltxvideo folder
            foreach (var job in videoJobs)
            {
                if (!string.IsNullOrEmpty(job.VideoPath))
                {
                    var fileName = job.VideoPath + ".mp4";
                    //var fileName = job.VideoPath;
                    Videos.Add("/ltxvideo/" + fileName);
                }
            }
        }
    }
}
