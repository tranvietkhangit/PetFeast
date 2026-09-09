using Microsoft.AspNetCore.Identity;
using PetFeast.Models.Points;
using PetFeast.Models.Voucher;

namespace PetFeast.Models.Identity
{
    public class ApplicationUser : IdentityUser
    {
        public string? FullName { get; set; }
        public int Points { get; set; } = 0;

        public ICollection<PointTransaction> PointTransactions { get; set; } = new List<PointTransaction>();
        public ICollection<UserAddress> UserAddresses { get; set; } = new List<UserAddress>();
        public ICollection<UserVoucher> UserVouchers
        { get; set; } = new List<UserVoucher>();
    }
}
