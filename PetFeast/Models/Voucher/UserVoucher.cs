using PetFeast.Models.Identity;
using System.ComponentModel.DataAnnotations;

namespace PetFeast.Models.Voucher
{
    public class UserVoucher
    {
        public int UserVoucherId { get; set; }

        [Required]
        public string UserId { get; set; } = "";

        public int VoucherId { get; set; }

        [Required]
        public string VoucherCode { get; set; } = "";

        public DateTime ReceivedDate { get; set; } = DateTime.Now;

        public bool IsUsed { get; set; } = false;

        public DateTime? UsedDate { get; set; }

        public ApplicationUser User { get; set; } = null!;

        public Voucher Voucher { get; set; } = null!;
    }
}
