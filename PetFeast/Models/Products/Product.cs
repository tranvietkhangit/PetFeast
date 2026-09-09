using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PetFeast.Models.Products
{
    public class Product
    {
        [Key]
        public int ProductId { get; set; }

        [Required]
        [StringLength(200)]
        public string ProductName { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        public int Quantity { get; set; }

        public string? Description { get; set; }

        public string? ImageUrl { get; set; }

        [Display(Name = "Danh mục")]
        public int CategoryId { get; set; }

        [ForeignKey("CategoryId")]
        public Category? Category { get; set; }
        // % Giảm giá
        public int DiscountPercent { get; set; }
        // Giá sau khi giảm
        [NotMapped]
        public decimal DiscountPrice
        {
            get
            {
                return Price - (Price * DiscountPercent / 100m);
            }
        }
        [NotMapped]
        public IFormFile? ImageFile { get; set; }
    }
}
