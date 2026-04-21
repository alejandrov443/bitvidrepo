using BitVid11.Data;
using BitVid11.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BitVid11.Pages
{
    public class TiersModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public TiersModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<Tier> Tiers { get; set; }
        public string CurrentSubscription { get; set; }

        public async Task OnGetAsync()
        {
            // 1. Define the tiers
            Tiers = new List<Tier>
            {
                new Tier { Name = "Free", Slug = "free", Price = 0, ShortDescription = "Get started with BitVid at no cost.", Features = new List<string> { "Unlimited Chat", "Up to 5 uploads / month", "Basic streaming quality", "Community support", "Access to Creator Dashboard" }, IsPopular = false },
                new Tier { Name = "Starter", Slug = "starter", Price = 110, ShortDescription = "More Access to Features", Features = new List<string> { "Everything in Free Plan", "Unlimited Text To Speech", "Unlimited Image Generation", "Access to Exclusive Items", "Unlimited Video Uploads" }, IsPopular = false },
                new Tier { Name = "Pro", Slug = "pro", Price = 220, ShortDescription = "The Everything Bundle", Features = new List<string> { "Everything in Starter Plan", "Unlimited Video Generation", "Free Gifts Monthly", "Faster Chat/Image/Video Generation" }, IsPopular = true }
            };

            // 2. Get UserId from cookie
            string userIdStr = Request.Cookies["UserId"];
            if (!string.IsNullOrEmpty(userIdStr) && int.TryParse(userIdStr, out int userId))
            {
                // 3. Pull SubscriptionStatus directly from MySQL
                var user = await _context.Users
                                         .FromSqlRaw("SELECT * FROM Users WHERE Id = {0}", userId)
                                         .AsNoTracking()
                                         .FirstOrDefaultAsync();

                CurrentSubscription = user?.SubscriptionStatus?.Trim() ?? "None";
            }
            else
            {
                CurrentSubscription = "None";
            }

            // Debug
            Console.WriteLine($"[DEBUG] UserId from cookie: {userIdStr}");
            Console.WriteLine($"[DEBUG] Current subscription: {CurrentSubscription}");
        }
    }

    public class Tier
    {
        public string Name { get; set; }
        public string Slug { get; set; }
        public int Price { get; set; }
        public string ShortDescription { get; set; }
        public List<string> Features { get; set; }
        public bool IsPopular { get; set; }
    }
}
