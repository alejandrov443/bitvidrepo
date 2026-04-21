using BitVid11.Data;
using BitVid11.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MyProduct = BitVid11.Models.Product;

namespace BitVid11.Pages.Store
{
    public class CheckoutModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly StripeSettings _stripeSettings;

        public CheckoutModel(ApplicationDbContext context, IOptions<StripeSettings> stripeSettings)
        {
            _context = context;
            _stripeSettings = stripeSettings.Value;
        }

        [BindProperty(SupportsGet = true)]
        public int ProductId { get; set; }

        public MyProduct Product { get; set; }

        // Shipping info
        [BindProperty] public string FullName { get; set; }
        [BindProperty] public string AddressLine1 { get; set; }
        [BindProperty] public string AddressLine2 { get; set; }
        [BindProperty] public string City { get; set; }
        [BindProperty] public string State { get; set; }
        [BindProperty] public string PostalCode { get; set; }
        [BindProperty] public string Country { get; set; }

        public IActionResult OnGet()
        {
            Product = _context.Products.FirstOrDefault(p => p.Id == ProductId);
            if (Product == null) return NotFound();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            Product = _context.Products.FirstOrDefault(p => p.Id == ProductId);
            if (Product == null) return NotFound();

            // ✅ Get logged-in user's ID from cookie
            string? userId = Request.Cookies["UserId"];

            if (string.IsNullOrEmpty(userId))
            {
                // If no user ID cookie, redirect to login page
                return RedirectToPage("/Accounts/Login");
            }

            // ✅ Generate unique tracking number
            string trackingNumber = "TRK-" + Guid.NewGuid().ToString("N").Substring(0, 10).ToUpper();

            // ✅ Create a new order
            var order = new Order
            {
                ProductId = Product.Id,
                StripeSessionId = "",
                PaymentStatus = "Pending",
                OrderStatus = "Order Received", // Set immediately
                TrackingNumber = trackingNumber, // Unique for each order
                UserId = Int32.Parse(userId), // Comes from cookie
                FullName = FullName ?? "",
                AddressLine1 = AddressLine1 ?? "",
                AddressLine2 = AddressLine2 ?? "",
                City = City ?? "",
                State = State ?? "",
                PostalCode = PostalCode ?? "",
                Country = Country ?? ""
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            // ✅ Stripe setup
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
                            UnitAmount = (long)(Product.Price * 100),
                            Currency = "usd",
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = Product.Name
                            }
                        },
                        Quantity = 1
                    }
                },
                Mode = "payment",
                SuccessUrl = $"{Request.Scheme}://{Request.Host}/Store/OrderSuccess?sessionId={{CHECKOUT_SESSION_ID}}",
                CancelUrl = Url.Page("/CharacterProducts", pageHandler: null, values: new { characterId = Product.CharacterId }, protocol: Request.Scheme),
                ShippingAddressCollection = new SessionShippingAddressCollectionOptions
                {
                    AllowedCountries = new List<string> { "US", "CA", "GB", "AU" }
                }
            };

            var service = new SessionService();
            var session = service.Create(options);

            // ✅ Update order with Stripe session ID
            order.StripeSessionId = session.Id;
            await _context.SaveChangesAsync();

            return Redirect(session.Url);
        }
    }
}
