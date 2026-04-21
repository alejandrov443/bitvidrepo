using BitVid11.Data;
using BitVid11.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BitVid11.Pages.Orders
{
    public class MyOrdersModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public MyOrdersModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<Order> UserOrders { get; set; } = new();

        public IActionResult OnGet()
        {
            // ✅ Read UserId from cookie
            var userIdCookie = Request.Cookies["UserId"];

            if (string.IsNullOrEmpty(userIdCookie))
            {
                // User is not logged in
                return RedirectToPage("/Accounts/Login");
            }

            if (!int.TryParse(userIdCookie, out int userId))
            {
                return RedirectToPage("/Accounts/Login");
            }

            // ✅ Pull all orders for that user
            UserOrders = _context.Orders
                .Where(o => o.UserId == userId)
                .ToList();

            return Page();
        }
    }
}
