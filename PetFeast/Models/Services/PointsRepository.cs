using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PetFeast.Data;
using PetFeast.Models.Identity;
using PetFeast.Models.Orders;
using PetFeast.Models.Points;

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

        // =========================================================
        // CỘNG ĐIỂM KHI ĐƠN HÀNG HOÀN THÀNH
        // MỖI ĐƠN HÀNG = 1 ĐIỂM
        // =========================================================
        public async Task<int> AddPointsForOrderAsync(Order order)
        {
            if (string.IsNullOrEmpty(order.UserId))
                return 0;

            var user = await _userManager.FindByIdAsync(order.UserId);

            if (user == null)
                return 0;

            // Kiểm tra đơn hàng đã được cộng điểm chưa
            bool alreadyEarned = await _context.PointTransactions
                .AnyAsync(p =>
                    p.OrderId == order.OrderId &&
                    p.Type == "Earn");

            if (alreadyEarned)
                return 0;

            // Mỗi sản phẩm theo số lượng = 1 điểm
            int points = order.OrderDetails
                .Sum(detail => detail.Quantity);

            if (points <= 0)
                return 0;

            user.Points += points;

            var transaction = new PointTransaction
            {
                UserId = user.Id,
                Points = points,
                Type = "Earn",
                OrderId = order.OrderId,
                Description =
                    $"Tích {points} điểm từ đơn hàng hoàn thành #{order.OrderId}",
                CreatedAt = DateTime.Now
            };

            _context.PointTransactions.Add(transaction);

            await _context.SaveChangesAsync();

            return points;
        }

        // =========================================================
        // THU HỒI ĐIỂM KHI ĐƠN HÀNG ĐƯỢC TRẢ HÀNG
        // =========================================================
        public async Task<int> RemovePointsForReturnedOrderAsync(Order order)
        {
            // Đơn hàng chưa có UserId
            if (string.IsNullOrEmpty(order.UserId))
                return 0;

            // Tìm user
            var user = await _userManager.FindByIdAsync(order.UserId);

            if (user == null)
                return 0;

            // =====================================================
            // KIỂM TRA ĐƠN HÀNG ĐÃ TỪNG ĐƯỢC CỘNG ĐIỂM CHƯA
            // =====================================================

            var earnTransaction = await _context.PointTransactions
                .FirstOrDefaultAsync(x =>
                    x.OrderId == order.OrderId &&
                    x.Type == "Earn");

            if (earnTransaction == null)
                return 0;

            // =====================================================
            // KIỂM TRA ĐÃ THU HỒI ĐIỂM CHƯA
            // =====================================================

            bool alreadyRemoved = await _context.PointTransactions
                .AnyAsync(x =>
                    x.OrderId == order.OrderId &&
                    x.Type == "Return");

            if (alreadyRemoved)
                return 0;

            // Số điểm cần thu hồi
            int points = earnTransaction.Points;

            // Không cho điểm user bị âm
            int removedPoints =
                Math.Min(points, user.Points);

            if (removedPoints <= 0)
                return 0;

            // =====================================================
            // TRỪ ĐIỂM
            // =====================================================

            user.Points -= removedPoints;

            // =====================================================
            // TẠO LỊCH SỬ
            // =====================================================

            var transaction = new PointTransaction
            {
                UserId = user.Id,
                Points = -removedPoints,
                Type = "Return",
                OrderId = order.OrderId,
                Description =
                    $"Thu hồi {removedPoints} điểm " +
                    $"do trả hàng đơn #{order.OrderId}",
                CreatedAt = DateTime.Now
            };

            _context.PointTransactions.Add(transaction);

            // Không SaveChanges ở đây.
            // Controller sẽ SaveChanges().
            return removedPoints;
        }
    }
}