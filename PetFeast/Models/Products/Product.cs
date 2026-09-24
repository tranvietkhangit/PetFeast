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
        // THÔNG TIN MÔ TẢ


        public string? Description { get; set; }

        // THÔNG TIN SẢN PHẨM


        [StringLength(100)]
        [Display(Name = "Thương hiệu")]
        public string? Brand { get; set; }

        [StringLength(100)]
        [Display(Name = "Xuất xứ")]
        public string? Origin { get; set; }

        [StringLength(100)]
        [Display(Name = "Đối tượng sử dụng")]
        public string? TargetPet { get; set; }
        // THÔNG TIN CHI TIẾT

        [Display(Name = "Thành phần")]
        public string? Ingredients { get; set; }

        [Display(Name = "Thông tin dinh dưỡng")]
        public string? Nutrition { get; set; }

        [Display(Name = "Hướng dẫn sử dụng")]
        public string? Usage { get; set; }

        // BẢO QUẢN & AN TOÀN

        [Display(Name = "Hướng dẫn bảo quản")]
        public string? Storage { get; set; }

        [Display(Name = "Cảnh báo")]
        public string? Warning { get; set; }

        // HÌNH ẢNH

        public string? ImageUrl { get; set; }

        // DANH MỤC

        [Display(Name = "Danh mục")]
        public int CategoryId { get; set; }

        [ForeignKey("CategoryId")]
        public Category? Category { get; set; }

        // GIẢM GIÁ

        public int DiscountPercent { get; set; }

        [NotMapped]
        public decimal DiscountPrice
        {
            get
            {
                return Price - (Price * DiscountPercent / 100m);
            }
        }
        // UPLOAD ẢNH

        [NotMapped]
        public IFormFile? ImageFile { get; set; }
        [NotMapped]
        public double AverageRating { get; set; }

        [NotMapped]
        public int ReviewCount { get; set; }
    }
}
