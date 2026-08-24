using PetFeast.Models.Identity;
using PetFeast.Models.Points;
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
        public string? Note { get; set; }
        public decimal ShippingFee { get; set; }
        public decimal TotalAmount { get; set; }
        public int UsedPoints { get; set; } = 0;

        public decimal PointDiscount { get; set; } = 0;
        public DateTime OrderDate { get; set; } = DateTime.Now;

        public List<OrderDetail>? OrderDetails { get; set; }
        public string? UserId { get; set; }
        public string Status { get; set; } = "Chờ xác nhận";
        public ApplicationUser? User { get; set; }

        public List<PointTransaction> PointTransactions { get; set; } = new List<PointTransaction>();
    }
}