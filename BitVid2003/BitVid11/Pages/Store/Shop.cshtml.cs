using BitVid11.Data;
using BitVid11.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BitVid11.Pages.Store
{
    public class ShopModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public ShopModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<Character> Characters { get; set; }

        public async Task OnGetAsync()
        {
            Characters = await _context.Characters.ToListAsync();
        }
    }
}
