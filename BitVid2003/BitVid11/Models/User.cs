namespace BitVid11.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string Phone { get; set; }
        public ICollection<ChatMessage> ChatMessages { get; set; } // Navigation property
        public string SubscriptionStatus { get; set; } = "Free"; // default
    }
}
