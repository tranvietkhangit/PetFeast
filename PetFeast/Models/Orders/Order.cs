using PetFeast.Models.Identity;
using PetFeast.Models.Points;
using PetFeast.Models.Voucher;
using System.ComponentModel.DataAnnotations;

namespace PetFeast.Models.Orders
{
    public class Order
    {
        public int OrderId { get; set; }

        [Required]
        public string CustomerName { get; set; } = "";

        [Required]
        public string Phone { get; set; } = "";

        [Required]
        public string Address { get; set; } = "";
        public string DeliveryMethod { get; set; } = "";
        public string PaymentMethod { get; set; } = "COD";
        public string PaymentStatus { get; set; } = "Chưa thanh toán";
        public string? Note { get; set; }
        public decimal ShippingFee { get; set; }
        public decimal TotalAmount { get; set; }
        public int UsedPoints { get; set; } = 0;

        public decimal PointDiscount { get; set; } = 0;
        public DateTime OrderDate { get; set; } = DateTime.Now;
        public DateTime? CompletedDate { get; set; }
        public List<OrderDetail>? OrderDetails { get; set; }
        public string? UserId { get; set; }
        public string Status { get; set; } = "Chờ xác nhận";
        // Lý do hủy đơn
        public string? CancellationReason { get; set; }
        public ApplicationUser? User { get; set; }

        public List<PointTransaction> PointTransactions { get; set; } = new List<PointTransaction>();
        public int? UserVoucherId { get; set; }

        public UserVoucher? UserVoucher { get; set; }

        public decimal VoucherDiscount { get; set; } = 0;
        public ReturnRequest? ReturnRequest { get; set; }
    }
}