using BitVid11.Data;
using BitVid11.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using BCrypt.Net;
using Newtonsoft.Json.Linq;

namespace BitVid11.Pages.Accounts
{
    public class RegisterModel : PageModel
    {
        // Strongly typed properties for binding
        [BindProperty]
        public string Username { get; set; }

        [BindProperty]
        public string Password { get; set; }

        [BindProperty]
        public string Email { get; set; }

        [BindProperty]
        public string Phone { get; set; }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostSubmitAsync()
        {
            // Validate empty fields
            if (string.IsNullOrWhiteSpace(Username))
                ModelState.AddModelError("Username", "Username is required.");

            if (string.IsNullOrWhiteSpace(Password))
                ModelState.AddModelError("Password", "Password is required.");

            if (string.IsNullOrWhiteSpace(Email))
                ModelState.AddModelError("Email", "Email is required.");

            if (string.IsNullOrWhiteSpace(Phone))
                ModelState.AddModelError("Phone", "Phone number is required.");

            if (!ModelState.IsValid)
                return Page();

            // Hash the password
            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(Password);

            // Validate phone number (placeholder logic)
            bool validNumber = true;

            if (validNumber)
            {
                using (var context = new ApplicationDbContext())
                {
                    // Check if username already exists
                    if (context.Users.Any(u => u.Username == Username))
                    {
                        ModelState.AddModelError("Username", "Username is already taken.");
                        return Page();
                    }

                    context.Database.EnsureCreated();
                    context.Users.Add(new User
                    {
                        Username = Username,
                        Password = hashedPassword,
                        Email = Email,
                        Phone = Phone
                    });

                    context.SaveChanges();
                }

                return RedirectToPage("/Accounts/Login");
            }
            else
            {
                ModelState.AddModelError("Phone", "Phone number is not valid.");
                return Page();
            }
        }
    }
}
