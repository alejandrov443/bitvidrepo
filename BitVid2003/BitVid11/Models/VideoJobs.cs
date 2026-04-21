using Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal;

namespace BitVid11.Models
{
    public class VideoJobs
    {

        public int Id { get; set; }
        public int UserId { get; set; }
        public string JobId { get; set; }
        public string Prompt { get; set; }
        public string Status { get; set; }
        public string? VideoPath {  get; set; }
        public string? FileName { get; set; }
        public string GalleryType { get; set; }
        public string? uploadedImagePath { get; set; }
        public string? JobIdentification { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? CompletedAt { get; set; }
    }
}
