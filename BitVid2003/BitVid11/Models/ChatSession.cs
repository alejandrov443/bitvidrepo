namespace BitVid11.Models
{
    public class ChatSession
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public DateTime CreatedAt { get; set; }

        // Foreign key to the User
        public int UserId { get; set; }
    }
}
