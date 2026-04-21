using BitVid11.Data;
using BitVid11.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace BitVid11.Pages.Store
{
    public class AdminProductsModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public AdminProductsModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<Product> Products { get; set; }

        public async Task OnGetAsync()
        {
            Products = await _context.Products
                                     .Include(p => p.Character)
                                     .ToListAsync();
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product != null)
            {
                // Optionally, delete the image from wwwroot
                if (!string.IsNullOrEmpty(product.ImageFile))
                {
                    var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/product/images", product.ImageFile);
                    if (System.IO.File.Exists(filePath))
                    {
                        System.IO.File.Delete(filePath);
                    }
                }

                _context.Products.Remove(product);
                await _context.SaveChangesAsync();
            }

            return RedirectToPage();
        }
    }
}
