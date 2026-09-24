using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using PetFeast.Models.Identity;
using PetFeast.Models.Products;

namespace PetFeast.Models.Reviews
{
    public class ReviewReport
    {
        [Key]
        public int ReviewReportId { get; set; }

        // Review bị tố cáo
        [Required]
        public int ProductReviewId { get; set; }

        [ForeignKey("ProductReviewId")]
        public ProductReview? ProductReview { get; set; }


        // Người tố cáo
        [Required]
        public string ReporterUserId { get; set; } = string.Empty;

        [ForeignKey("ReporterUserId")]
        public ApplicationUser? ReporterUser { get; set; }


        // Lý do tố cáo
        [Required]
        [StringLength(200)]
        public string Reason { get; set; } = string.Empty;


        // Mô tả thêm
        [StringLength(1000)]
        public string? Description { get; set; }


        // Trạng thái xử lý
        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "Chờ xử lý";


        // Thời gian tố cáo
        public DateTime CreatedAt { get; set; } = DateTime.Now;


        // Thời gian admin xử lý
        public DateTime? ResolvedAt { get; set; }


        // Ghi chú của admin
        [StringLength(1000)]
        public string? AdminNote { get; set; }
    }
}