using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PetFeast.Data;
using PetFeast.Models.Identity;
using PetFeast.Models.Points;
using PetFeast.Models.Voucher;
using System.Security.Claims;

namespace PetFeast.Controllers
{
    [Authorize]
    public class VoucherController : Controller
    {
        private readonly PetFeastDBContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public VoucherController(
            PetFeastDBContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // ==========================
        // DANH SÁCH VOUCHER CÓ THỂ ĐỔI
        // ==========================
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return Unauthorized();

            var vouchers = await _context.Vouchers
                .Where(v =>
                    v.IsActive &&
                    v.Quantity > 0 &&
                    v.ExpiryDate >= DateTime.Now)
                .OrderBy(v => v.RequiredPoints)
                .ToListAsync();

            ViewBag.UserPoints = user.Points;

            return View(vouchers);
        }


        // ==========================
        // ĐỔI ĐIỂM LẤY VOUCHER
        // ==========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Redeem(int id)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return Unauthorized();

            var voucher = await _context.Vouchers
                .FirstOrDefaultAsync(v => v.VoucherId == id);

            if (voucher == null)
            {
                TempData["Error"] = "Voucher không tồn tại.";
                return RedirectToAction(nameof(Index));
            }

            // Voucher không hoạt động
            if (!voucher.IsActive)
            {
                TempData["Error"] = "Voucher hiện không khả dụng.";
                return RedirectToAction(nameof(Index));
            }

            // Voucher hết hạn
            if (voucher.ExpiryDate < DateTime.Now)
            {
                TempData["Error"] = "Voucher đã hết hạn.";
                return RedirectToAction(nameof(Index));
            }

            // Voucher hết số lượng
            if (voucher.Quantity <= 0)
            {
                TempData["Error"] = "Voucher đã hết.";
                return RedirectToAction(nameof(Index));
            }

            // Không đủ điểm
            if (user.Points < voucher.RequiredPoints)
            {
                TempData["Error"] =
                    $"Bạn không đủ điểm. Cần {voucher.RequiredPoints:N0} điểm.";

                return RedirectToAction(nameof(Index));
            }

            // ==========================
            // TẠO MÃ VOUCHER RIÊNG
            // ==========================

            string voucherCode;

            do
            {
                voucherCode =
                    $"{voucher.Code}-{Guid.NewGuid().ToString("N")[..8].ToUpper()}";
            }
            while (await _context.UserVouchers
                .AnyAsync(x => x.VoucherCode == voucherCode));


            // ==========================
            // TẠO USER VOUCHER
            // ==========================

            var userVoucher = new UserVoucher
            {
                UserId = user.Id,
                VoucherId = voucher.VoucherId,
                VoucherCode = voucherCode,
                ReceivedDate = DateTime.Now,
                IsUsed = false
            };

            _context.UserVouchers.Add(userVoucher);


            // ==========================
            // TRỪ ĐIỂM
            // ==========================

            user.Points -= voucher.RequiredPoints;

            var pointTransaction = new PointTransaction
            {
                UserId = user.Id,
                Points = -voucher.RequiredPoints,
                Type = "RedeemVoucher",
                Description =
                    $"Đổi {voucher.RequiredPoints} điểm lấy voucher {voucher.Name}",
                CreatedAt = DateTime.Now
            };

            _context.PointTransactions.Add(pointTransaction);


            // ==========================
            // GIẢM SỐ LƯỢNG VOUCHER
            // ==========================

            voucher.Quantity--;


            await _context.SaveChangesAsync();


            TempData["Success"] =
                $"Đổi voucher thành công! Mã voucher: {voucherCode}";

            return RedirectToAction(nameof(MyVouchers));
        }


        // ==========================
        // VOUCHER CỦA TÔI
        // ==========================
        [HttpGet]
        public async Task<IActionResult> MyVouchers()
        {
            var userId = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            var vouchers = await _context.UserVouchers
                .Include(x => x.Voucher)
                .Where(x => x.UserId == userId)
                .OrderByDescending(x => x.ReceivedDate)
                .ToListAsync();

            return View(vouchers);
        }
    }
}
