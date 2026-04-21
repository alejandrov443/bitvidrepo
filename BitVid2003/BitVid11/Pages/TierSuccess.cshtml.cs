using BitVid11.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Threading.Tasks;

namespace BitVid11.Pages.Store
{
    public class TierSuccessModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public TierSuccessModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty(SupportsGet = true)]
        public string Tier { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            string? userIdCookie = Request.Cookies["UserId"];
            if (string.IsNullOrEmpty(userIdCookie)) return RedirectToPage("/Accounts/Login");

            int userId = int.Parse(userIdCookie);
            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                user.SubscriptionStatus = Tier;
                await _context.SaveChangesAsync();
            }

            return Page();
        }
    }
}
