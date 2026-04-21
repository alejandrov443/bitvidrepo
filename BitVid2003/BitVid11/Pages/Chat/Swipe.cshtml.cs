using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using BitVid11.Data;
using Microsoft.EntityFrameworkCore;

namespace BitVid11.Pages.Chat
{
    public class SwipeModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        public SwipeModel(ApplicationDbContext db) => _db = db;

        public List<CharacterProfile> Profiles { get; set; } = new();

        public async Task OnGetAsync()
        {
            // Grab all characters from DB
            var chars = await _db.Characters.ToListAsync();

            Profiles = chars.Select(c => new CharacterProfile
            {
                Id = c.Id,
                // Format name like Tiles page: first letter uppercase, rest lowercase
                Name = char.ToUpper(c.Name[0]) + c.Name.Substring(1).ToLower(),
                ImageUrl = $"/images/{char.ToUpper(c.Name[0]) + c.Name.Substring(1).ToLower()}.png"
            }).ToList();
        }

        [IgnoreAntiforgeryToken]
        public IActionResult OnPostSwipe([FromBody] SwipeAction action)
        {
            if (action == null) return BadRequest();
            Console.WriteLine($"Swiped CharacterId={action.TargetUserId}, Liked={action.Liked}");
            // TODO: persist swipe in DB
            return new JsonResult(new { success = true });
        }

        public class CharacterProfile
        {
            public int Id { get; set; }
            public string Name { get; set; } = "";
            public string ImageUrl { get; set; } = "";
        }

        public class SwipeAction
        {
            public int TargetUserId { get; set; }
            public bool Liked { get; set; }
        }
    }
}
