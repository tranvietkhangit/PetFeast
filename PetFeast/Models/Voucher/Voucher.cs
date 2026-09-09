using System.ComponentModel.DataAnnotations;
namespace PetFeast.Models.Voucher

{
    public class Voucher
    {
        public int VoucherId { get; set; }

        [Required]
        public string Name { get; set; } = "";

        [Required]
        public string Code { get; set; } = "";

        public decimal DiscountAmount { get; set; }

        public int RequiredPoints { get; set; }

        public decimal MinimumOrderAmount { get; set; }

        public int Quantity { get; set; }

        public DateTime ExpiryDate { get; set; }

        public bool IsActive { get; set; } = true;

        public ICollection<UserVoucher> UserVouchers { get; set; }
            = new List<UserVoucher>();
    }
}
