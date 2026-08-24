using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PetFeast.Models.Identity;
using PetFeast.Models.Orders;
using PetFeast.Models.Points;
using PetFeast.Data;
using Microsoft.EntityFrameworkCore;
namespace PetFeast.Models.Services
{
    public class PointsRepository
    {
        private readonly PetFeastDBContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public PointsRepository(
            PetFeastDBContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<int> AddPointsForOrderAsync(Order order)
        {
            // Đơn hàng chưa có UserId
            if (string.IsNullOrEmpty(order.UserId))
                return 0;

            // Kiểm tra đơn hàng đã được cộng điểm chưa
            bool alreadyEarned = await _context.PointTransactions
                .AnyAsync(x =>
                    x.OrderId == order.OrderId &&
                    x.Type == "Earn");

            if (alreadyEarned)
                return 0;

            // Tìm user
            var user = await _userManager.FindByIdAsync(order.UserId);

            if (user == null)
                return 0;

            // Mỗi đơn hàng hoàn thành = 1 điểm
            int points = 1;

            // Cộng điểm cho User
            user.Points += points;

            // Tạo lịch sử giao dịch điểm
            var transaction = new PointTransaction
            {
                UserId = user.Id,
                Points = points,
                Type = "Earn",
                OrderId = order.OrderId,
                Description = $"Tích {points} điểm từ đơn hàng #{order.OrderId}",
                CreatedAt = DateTime.Now
            };

            _context.PointTransactions.Add(transaction);

            await _context.SaveChangesAsync();

            return points;
        }
    }
}
