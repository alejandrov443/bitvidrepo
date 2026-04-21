using BitVid11.Data;
using BitVid11.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using System.Linq;

namespace BitVid11.Pages.Store
{
    public class CharacterProductsModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public CharacterProductsModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<Product> Products { get; set; } = new();
        public Character Character { get; set; }

        public IActionResult OnGet(int characterId)
        {
            Character = _context.Characters.FirstOrDefault(c => c.Id == characterId);
            if (Character == null) return NotFound();

            Products = _context.Products
                               .Where(p => p.CharacterId == characterId)
                               .ToList();

            return Page();
        }
    }
}
