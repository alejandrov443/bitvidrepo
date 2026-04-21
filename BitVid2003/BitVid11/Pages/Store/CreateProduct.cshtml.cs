using BitVid11.Data;
using BitVid11.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace BitVid11.Pages.Store
{
    public class CreateProductModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public CreateProductModel(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // Flatten properties
        [Required(ErrorMessage = "Character selection is required.")]
        [Display(Name = "Character")]
        [BindProperty]
        public int CharacterId { get; set; }

        [Required(ErrorMessage = "Product name is required.")]
        [BindProperty]
        public string Name { get; set; }

        [Required(ErrorMessage = "Price is required.")]
        [Range(0.01, 10000)]
        [BindProperty]
        public decimal Price { get; set; }

        [Required(ErrorMessage = "Description is required.")]
        [BindProperty]
        public string Description { get; set; }

        [BindProperty]
        public IFormFile UploadImage { get; set; }

        public List<Character> Characters { get; set; } = new();

        public void OnGet()
        {
            Characters = _context.Characters.ToList();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            Characters = _context.Characters.ToList();

            // Validate image upload
            if (UploadImage == null)
            {
                ModelState.AddModelError("UploadImage", "Product image is required.");
            }

            if (!ModelState.IsValid)
            {
                return Page();
            }

            // Save uploaded image
            var uploadDir = Path.Combine(_env.WebRootPath, "product/images");
            if (!Directory.Exists(uploadDir)) Directory.CreateDirectory(uploadDir);

            var fileName = Path.GetFileNameWithoutExtension(UploadImage.FileName);
            var ext = Path.GetExtension(UploadImage.FileName);
            var uniqueFileName = $"{fileName}_{Guid.NewGuid()}{ext}";

            using (var stream = new FileStream(Path.Combine(uploadDir, uniqueFileName), FileMode.Create))
            {
                await UploadImage.CopyToAsync(stream);
            }

            // Insert into database
            var product = new Product
            {
                CharacterId = CharacterId,
                Name = Name,
                Price = Price,
                Description = Description,
                ImageFile = uniqueFileName
            };

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            return RedirectToPage("/Shop");
        }
    }
}
