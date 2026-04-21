using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BitVid11.Models
{
    public class Order
    {
        [Key]
        public int Id { get; set; }

        public int ProductId { get; set; }

        [ForeignKey("ProductId")]
        public Product Product { get; set; }

        // Shipping information
        public string FullName { get; set; }
        public string AddressLine1 { get; set; }
        public string? AddressLine2 { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string PostalCode { get; set; }
        public string Country { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Stripe / payment info
        public string StripeSessionId { get; set; }
        public string PaymentStatus { get; set; } = "Pending";

        // ✅ New fields
        public string OrderStatus { get; set; } = "Order Received";

        // ✅ Unique tracking number for each order
        [MaxLength(50)]
        public string TrackingNumber { get; set; }

        // ✅ User ID from login cookie
        public int UserId { get; set; }
    }
}
