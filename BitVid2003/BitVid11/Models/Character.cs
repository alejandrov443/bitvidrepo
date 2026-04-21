using System.ComponentModel.DataAnnotations.Schema;

namespace BitVid11.Models
{
    public class Character
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Message { get; set; }

        [Column("voiceurl")]  // map C# property to lowercase DB column
        public string? VoiceUrl { get; set; } // Nullable to support optional audio
        public string? Origin { get; set; }

        public List<Product> Products { get; set; } = new();
    }
}
