using BitVid11.Data;
using BitVid11.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;

namespace BitVid11.Pages.Store
{
    public class EditProductModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public EditProductModel(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        [BindProperty]
        public int Id { get; set; }

        [Required]
        [BindProperty]
        public int CharacterId { get; set; }

        [Required]
        [BindProperty]
        public string Name { get; set; }

        [Required]
        [Range(0.01, 10000)]
        [BindProperty]
        public decimal Price { get; set; }

        [BindProperty]
        public IFormFile? UploadImage { get; set; }  // nullable: optional upload

        [Required]
        [BindProperty]
        public string Description { get; set; }

        public string ExistingImageFile { get; set; }
        public List<Character> Characters { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();

            Id = product.Id;
            CharacterId = product.CharacterId;
            Name = product.Name;
            Price = product.Price;
            Description = product.Description;
            ExistingImageFile = product.ImageFile;

            Characters = await _context.Characters.ToListAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                Characters = await _context.Characters.ToListAsync();
                return Page();
            }

            var product = await _context.Products.FindAsync(Id);
            if (product == null) return NotFound();

            // Character cannot change
            product.CharacterId = CharacterId;

            product.Name = Name;
            product.Price = Price;
            product.Description = Description;

            if (UploadImage != null)
            {
                // Delete old image
                if (!string.IsNullOrEmpty(product.ImageFile))
                {
                    var oldPath = Path.Combine(_env.WebRootPath, "product/images", product.ImageFile);
                    if (System.IO.File.Exists(oldPath)) System.IO.File.Delete(oldPath);
                }

                // Save new image
                var uploadDir = Path.Combine(_env.WebRootPath, "product/images");
                if (!Directory.Exists(uploadDir)) Directory.CreateDirectory(uploadDir);

                var fileName = Path.GetFileNameWithoutExtension(UploadImage.FileName);
                var ext = Path.GetExtension(UploadImage.FileName);
                var uniqueFileName = $"{fileName}_{Guid.NewGuid()}{ext}";

                using var stream = new FileStream(Path.Combine(uploadDir, uniqueFileName), FileMode.Create);
                await UploadImage.CopyToAsync(stream);

                product.ImageFile = uniqueFileName;
            }

            _context.Products.Update(product);
            await _context.SaveChangesAsync();

            return RedirectToPage("/Store/AdminProducts");
        }
    }
}
