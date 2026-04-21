using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BitVid11.Pages.Accounts
{
    public class LogoutModel : PageModel
    {
        public async Task<IActionResult> OnGetAsync()
        {
            // deletes cookies
            HttpContext.Response.Cookies.Delete("UserAuth");
            HttpContext.Response.Cookies.Delete("UserId");
            // for page access
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            // Redirect the user to the home page or login page
            return RedirectToPage("/Index");  // or specify another page
        }
    }
}
