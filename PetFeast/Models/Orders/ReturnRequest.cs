using PetFeast.Models.Identity;
using System.ComponentModel.DataAnnotations;

namespace PetFeast.Models.Orders
{
    public class ReturnRequest
    {
        public int ReturnRequestId { get; set; }

        // Đơn hàng cần trả
        public int OrderId { get; set; }

        public Order Order { get; set; } = null!;

        // Người yêu cầu
        [Required]
        public string UserId { get; set; } = "";

        public ApplicationUser User { get; set; } = null!;

        // Lý do trả hàng
        [Required]
        public string Reason { get; set; } = "";

        // Mô tả thêm
        public string? Description { get; set; }

        // Hình ảnh bằng chứng
        public string? EvidenceImageUrl { get; set; }

        // Video bằng chứng
        public string? EvidenceVideoUrl { get; set; }

        // Trạng thái yêu cầu
        public string Status { get; set; } = "Chờ xử lý";

        // Ghi chú của Admin
        public string? AdminNote { get; set; }

        // Ngày gửi yêu cầu
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Ngày Admin duyệt
        public DateTime? ApprovedAt { get; set; }

        // Ngày hoàn tất trả hàng
        public DateTime? CompletedAt { get; set; }

        // Phí trả hàng
        public decimal ReturnFee { get; set; } = 0;

        // Số tiền thực tế hoàn lại
        public decimal RefundAmount { get; set; } = 0;
    }
}
