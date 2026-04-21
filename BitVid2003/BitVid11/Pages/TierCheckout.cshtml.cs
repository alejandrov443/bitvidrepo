using BitVid11.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BitVid11.Pages.Store
{
    public class TierCheckoutModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly StripeSettings _stripeSettings;

        public TierCheckoutModel(ApplicationDbContext context, IOptions<StripeSettings> stripeSettings)
        {
            _context = context;
            _stripeSettings = stripeSettings.Value;
        }

        [BindProperty(SupportsGet = true)]
        public string Tier { get; set; } // "starter" or "pro"

        public async Task<IActionResult> OnGetAsync()
        {
            // Get logged-in user
            string? userIdCookie = Request.Cookies["UserId"];
            if (string.IsNullOrEmpty(userIdCookie)) return RedirectToPage("/Accounts/Login");

            int userId = int.Parse(userIdCookie);

            // Free tier redirect
            if (Tier == "free") return RedirectToPage("/Tiers");

            // Map tier to price in cents
            var priceMap = new Dictionary<string, long>
            {
                {"starter", 11000 },
                {"pro", 22000 }
            };

            if (!priceMap.ContainsKey(Tier)) return RedirectToPage("/Tiers");

            StripeConfiguration.ApiKey = _stripeSettings.SecretKey;

            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card" },
                LineItems = new List<SessionLineItemOptions>
    {
        new SessionLineItemOptions
        {
            PriceData = new SessionLineItemPriceDataOptions
            {
                UnitAmount = priceMap[Tier],
                Currency = "usd",
                ProductData = new SessionLineItemPriceDataProductDataOptions
                {
                    Name = $"{Tier.ToUpper()} Plan"
                }
            },
            Quantity = 1
        }
    },
                Mode = "payment",
                SuccessUrl = $"{Request.Scheme}://{Request.Host}/TierSuccess?tier={Tier}",
                CancelUrl = $"{Request.Scheme}://{Request.Host}/Tiers"
            };


            var service = new SessionService();
            var session = service.Create(options);

            return Redirect(session.Url);
        }
    }
}
