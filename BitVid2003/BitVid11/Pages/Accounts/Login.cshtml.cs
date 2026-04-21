using BitVid11.Data;
using BitVid11.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace BitVid11.Pages.Accounts
{
    public class LoginModel : PageModel
    {
        public User user1 = new User();

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostSubmitAsync(User user)
        {
            string inputUsername = user.Username;
            string inputPassword = user.Password; // Hash this before comparing

            // Validate empty fields
            if (string.IsNullOrWhiteSpace(inputUsername))
                ModelState.AddModelError("Username", "Username is required.");

            if (string.IsNullOrWhiteSpace(inputPassword))
                ModelState.AddModelError("Password", "Password is required.");

            using (var context = new ApplicationDbContext())
            {
                var user1 = context.Users.FirstOrDefault(u => u.Username == inputUsername);
                // verify hashed password
                if (user1 != null && VerifyPassword(inputPassword, user1.Password))
                {
                    // Set cookie to remember the user
                    var options = new CookieOptions
                    {
                        HttpOnly = true,
                        Expires = DateTime.Now.AddHours(1), // You can adjust the expiration time
                        IsEssential = true
                    };
                    Response.Cookies.Append("UserAuth", user1.Username, options);
                    Response.Cookies.Append("UserId", user1.Id.ToString(), options);

                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Name, user1.Username)
                    };

                    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    var authProperties = new AuthenticationProperties { IsPersistent = true };

                    // ✅ Make sure to use the correct scheme
                    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                                                  new ClaimsPrincipal(claimsIdentity),
                                                  authProperties);

                    context.SaveChanges();
                    return RedirectToPage("/Index");
                }

                // Show login failure message
                ModelState.AddModelError(string.Empty, "Invalid username or password.");

            }
            return RedirectToPage("LoginFailed");
        }

        // verify
        private static bool VerifyPassword(string inputPassword, string storedHash)
        {
            return BCrypt.Net.BCrypt.Verify(inputPassword, storedHash);
        }
    }
}
