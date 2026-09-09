using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PetFeast.Models.Contacts;
using PetFeast.Models.Identity;
using PetFeast.Models.Orders;
using PetFeast.Models.Points;
using PetFeast.Models.Products;
using PetFeast.Models.ShoppingCart;
using PetFeast.Models.Voucher;
using System.Reflection.Emit;
namespace PetFeast.Data
{
    public class PetFeastDBContext : IdentityDbContext<ApplicationUser>
    {
        public PetFeastDBContext(
            DbContextOptions<PetFeastDBContext> options)
            : base(options)
        {
        }
        public DbSet<Product> Products { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderDetail> OrderDetails { get; set; }
        public DbSet<Contact> Contacts { get; set; }
        public DbSet<Favorite> Favorites { get; set; }
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);


            builder.Entity<Product>()
                .Property(p => p.Price)
                .HasPrecision(18, 2);


            builder.Entity<Order>()
                .Property(o => o.TotalAmount)
                .HasPrecision(18, 2);


            builder.Entity<Order>()
                .Property(o => o.ShippingFee)
                .HasPrecision(18, 2);


            builder.Entity<OrderDetail>()
                .Property(o => o.Price)
                .HasPrecision(18, 2);

            builder.Entity<Order>()
    .Property(o => o.PointDiscount)
    .HasPrecision(18, 2);
            builder.Entity<UserVoucher>()
    .HasOne(x => x.User)
    .WithMany(x => x.UserVouchers)
    .HasForeignKey(x => x.UserId)
    .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<UserVoucher>()
                .HasOne(x => x.Voucher)
                .WithMany(x => x.UserVouchers)
                .HasForeignKey(x => x.VoucherId)
                .OnDelete(DeleteBehavior.Restrict);
        }
        public DbSet<PointTransaction> PointTransactions { get; set; }
        public DbSet<Cart> Carts { get; set; }
        public DbSet<CartItem> CartItems { get; set; }
        public DbSet<UserAddress> UserAddresses { get; set; }
        public DbSet<Voucher> Vouchers { get; set; }

        public DbSet<UserVoucher> UserVouchers { get; set; }
    }
}
