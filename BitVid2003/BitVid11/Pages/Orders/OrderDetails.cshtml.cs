using BitVid11.Data;
using BitVid11.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace BitVid11.Pages.Orders
{
    public class OrderDetailsModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public OrderDetailsModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public Order Order { get; set; }

        public IActionResult OnGet(int id)
        {
            // Load the order including the Product
            Order = _context.Orders
                            .Include(o => o.Product)
                            .FirstOrDefault(o => o.Id == id);

            if (Order == null)
            {
                return NotFound();
            }

            return Page();
        }
    }
}
