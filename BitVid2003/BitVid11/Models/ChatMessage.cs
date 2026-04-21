namespace BitVid11.Models
{
    public class ChatMessage
    {
        public int Id { get; set; }
        public string Sender { get; set; }
        public string Message { get; set; }
        public string? startmsg { get; set; }
        public string? CharacterName { get; set; }
        public string status { get; set; }
        public string username { get; set; }
        public DateTime Timestamp { get; set; }
        public int Order { get; set; }
        // Foreign key for User
        public int UserId { get; set; }
        public string Name { get; set; }
        public string? ImageUrl { get; set; }
        public string? VideoUrl { get; set; }
        public User User { get; set; } // Navigation Property
        public string MessageUid { get; set; } = Guid.NewGuid().ToString();
    }
}
