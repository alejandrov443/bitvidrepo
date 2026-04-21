using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BitVid11.Models
{
    public class Product
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Character selection is required.")]
        [ForeignKey("Character")]
        public int CharacterId { get; set; }
        public Character Character { get; set; }

        [Required(ErrorMessage = "Product Name is required.")]
        [StringLength(500)]
        public string Name { get; set; }

        [Required(ErrorMessage = "Price is required.")]
        [Range(0.01, 10000, ErrorMessage = "Price must be greater than 0.")]
        public decimal Price { get; set; }

        [Required(ErrorMessage = "Product image is required.")]
        public string ImageFile { get; set; }

        [Required(ErrorMessage = "Description is required.")]
        [StringLength(2000)]
        public string Description { get; set; }
    }
}
