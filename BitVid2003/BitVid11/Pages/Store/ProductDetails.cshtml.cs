using BitVid11.Data;
using BitVid11.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BitVid11.Pages.Store
{
    public class ProductDetailsModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public ProductDetailsModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public Product Product { get; set; }

        public IActionResult OnGet(int productId)
        {
            Product = _context.Products.FirstOrDefault(p => p.Id == productId);
            if (Product == null) return NotFound();

            return Page();
        }
    }
}
