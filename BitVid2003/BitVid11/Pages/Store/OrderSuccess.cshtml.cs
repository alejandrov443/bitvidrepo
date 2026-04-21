using BitVid11.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Stripe.Checkout;

namespace BitVid11.Pages.Store
{
    public class OrderSuccessModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public OrderSuccessModel(ApplicationDbContext context)
        {
            _context = context;
        }

        // Product info
        public string ProductName { get; set; }
        public decimal ProductPrice { get; set; }

        // Shipping info
        public string FullName { get; set; }
        public string AddressLine1 { get; set; }
        public string AddressLine2 { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string PostalCode { get; set; }
        public string Country { get; set; }

        // Additional info
        public string TrackingNumber { get; set; }
        public string OrderStatus { get; set; }
        public int CharacterId { get; set; }

        public IActionResult OnGet(string sessionId)
        {
            if (string.IsNullOrEmpty(sessionId))
                return RedirectToPage("/Index");

            var service = new SessionService();
            var session = service.Get(sessionId);

            // Include product for display
            var order = _context.Orders
                .Include(o => o.Product)
                .FirstOrDefault(o => o.StripeSessionId == sessionId);

            if (order == null || order.Product == null)
                return RedirectToPage("/Index");

            // ✅ Update payment status
            order.PaymentStatus = session.PaymentStatus == "paid" ? "Paid" : "Failed";

            // ✅ Set order status if paid
            if (order.PaymentStatus == "Paid")
            {
                order.OrderStatus = "Order Received";
            }

            // ✅ Assign display properties
            ProductName = order.Product.Name;
            ProductPrice = order.Product.Price;
            TrackingNumber = order.TrackingNumber;
            OrderStatus = order.OrderStatus;
            CharacterId = order.Product.CharacterId;

            if (session.CustomerDetails?.Address != null)
            {
                order.FullName = session.CustomerDetails.Name;
                order.AddressLine1 = session.CustomerDetails.Address.Line1;
                order.AddressLine2 = session.CustomerDetails.Address.Line2;
                order.City = session.CustomerDetails.Address.City;
                order.State = session.CustomerDetails.Address.State;
                order.PostalCode = session.CustomerDetails.Address.PostalCode;
                order.Country = session.CustomerDetails.Address.Country;
            }

            // ✅ Update local display fields
            FullName = order.FullName;
            AddressLine1 = order.AddressLine1;
            AddressLine2 = order.AddressLine2;
            City = order.City;
            State = order.State;
            PostalCode = order.PostalCode;
            Country = order.Country;

            _context.SaveChanges();

            return Page();
        }
    }
}
