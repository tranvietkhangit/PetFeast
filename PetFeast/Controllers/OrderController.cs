using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PetFeast.Data;
using PetFeast.Models.Identity;
using PetFeast.Models.Interfaces;
using PetFeast.Models.Orders;
using PetFeast.Models.Points;
using PetFeast.Models.Voucher;
using System.Security.Claims;
namespace PetFeast.Controllers
{
    public class OrderController : Controller
    {
        private readonly IShoppingCartRepository _cartRepo;
        private readonly IOrderRepository _orderRepo;
        private readonly IProductRepository _productRepo;
        private readonly PetFeastDBContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        public OrderController(
            IShoppingCartRepository cartRepo,
            IOrderRepository orderRepo,
            IProductRepository productRepo,
            PetFeastDBContext context,
            UserManager<ApplicationUser> userManager)
        {
            _cartRepo = cartRepo;
            _orderRepo = orderRepo;
            _productRepo = productRepo;
            _context = context;
            _userManager = userManager;
        }
        // HIỂN THỊ TRANG CHECKOUT
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> CheckOut()
        {
            var cart = _cartRepo.GetCart()
                .Where(x => x.IsSelected)
                .ToList();

            if (cart.Count == 0)
            {
                return RedirectToAction(
                    "Index",
                    "ShoppingCart");
            }

            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return Unauthorized();
            }

            // Lấy địa chỉ mặc định
            var defaultAddress = await _context.UserAddresses
                .FirstOrDefaultAsync(x =>
                    x.UserId == user.Id &&
                    x.IsDefault);

            // Nếu chưa có địa chỉ
            if (defaultAddress == null)
            {
                TempData["Error"] =
                    "Bạn chưa có địa chỉ nhận hàng. Vui lòng thêm địa chỉ trước khi đặt hàng.";

                return RedirectToAction(
                    "Create",
                    "Address");
            }

            // Số điểm hiện có
            ViewBag.UserPoints = user.Points;

            // Địa chỉ mặc định
            ViewBag.DefaultAddress = defaultAddress;

            // ==========================
            // LẤY VOUCHER CỦA USER
            // ==========================

            var userVouchers = await _context.UserVouchers
                .Include(x => x.Voucher)
                .Where(x =>
                    x.UserId == user.Id &&
                    !x.IsUsed &&
                    x.Voucher.IsActive &&
                    x.Voucher.ExpiryDate >= DateTime.Now)
                .OrderByDescending(x => x.ReceivedDate)
                .ToListAsync();

            ViewBag.UserVouchers = userVouchers;

            return View(cart);
        }
        // LƯU ĐƠN HÀNG
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckOut(
    Order order,
    int usedPoints = 0,
    int? userVoucherId = null)
        {
            // =========================================================
            // 1. LẤY CÁC SẢN PHẨM ĐƯỢC CHỌN TRONG GIỎ
            // =========================================================

            var cart = _cartRepo.GetCart()
                .Where(x => x.IsSelected)
                .ToList();

            if (cart.Count == 0)
            {
                return RedirectToAction(
                    "Index",
                    "ShoppingCart");
            }

            // =========================================================
            // 2. LẤY USER HIỆN TẠI
            // =========================================================

            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return Unauthorized();
            }

            // =========================================================
            // 3. LẤY ĐỊA CHỈ MẶC ĐỊNH
            // =========================================================

            var defaultAddress = await _context.UserAddresses
                .FirstOrDefaultAsync(x =>
                    x.UserId == user.Id &&
                    x.IsDefault);

            if (defaultAddress == null &&
                order.DeliveryMethod == "Ship")
            {
                TempData["Error"] =
                    "Bạn chưa có địa chỉ nhận hàng.";

                return RedirectToAction(
                    "Create",
                    "Address");
            }

            // =========================================================
            // 4. THÔNG TIN KHÁCH HÀNG
            // =========================================================

            if (order.DeliveryMethod == "Ship")
            {
                order.CustomerName =
                    defaultAddress!.ReceiverName;

                order.Phone =
                    defaultAddress.PhoneNumber;

                order.Address =
                    $"{defaultAddress.AddressDetail}, " +
                    $"{defaultAddress.Ward}, " +
                    $"{defaultAddress.City}";
            }
            else
            {
                order.CustomerName =
                    user.FullName ?? "Khách hàng";

                order.Phone =
                    user.PhoneNumber ?? "";

                order.Address =
                    "PetFeast - Đà Lạt, Lâm Đồng";
            }

            // =========================================================
            // 5. KIỂM TRA ĐIỂM
            // 1 ĐIỂM = 100Đ
            // =========================================================

            if (usedPoints < 0)
            {
                usedPoints = 0;
            }

            if (usedPoints > user.Points)
            {
                TempData["Error"] =
                    "Số điểm sử dụng vượt quá số điểm hiện có.";

                return RedirectToAction(nameof(CheckOut));
            }

            // =========================================================
            // 6. TÍNH TỔNG TIỀN SẢN PHẨM
            // =========================================================

            var productTotal =
                cart.Sum(x => x.TotalPrice);

            // =========================================================
            // 7. PHÍ VẬN CHUYỂN
            // Ship = 20.000đ
            // Pickup = 0đ
            // =========================================================

            if (order.DeliveryMethod == "Ship")
            {
                order.ShippingFee = 20000;
            }
            else
            {
                order.ShippingFee = 0;
            }

            // =========================================================
            // 8. TỔNG TRƯỚC GIẢM GIÁ
            // =========================================================

            var totalBeforeDiscount =
                productTotal + order.ShippingFee;

            // =========================================================
            // 9. GIẢM GIÁ BẰNG ĐIỂM
            // 1 POINT = 100Đ
            // =========================================================

            decimal pointDiscount =
                usedPoints * 100m;

            // Không cho điểm giảm vượt quá tổng tiền
            if (pointDiscount > totalBeforeDiscount)
            {
                pointDiscount = totalBeforeDiscount;

                usedPoints =
                    (int)(pointDiscount / 100m);
            }

            // =========================================================
            // 10. KIỂM TRA VOUCHER
            // =========================================================

            UserVoucher? userVoucher = null;

            decimal voucherDiscount = 0;

            if (userVoucherId.HasValue)
            {
                userVoucher = await _context.UserVouchers
                    .Include(x => x.Voucher)
                    .FirstOrDefaultAsync(x =>
                        x.UserVoucherId == userVoucherId.Value &&
                        x.UserId == user.Id);

                // Voucher không tồn tại
                if (userVoucher == null)
                {
                    TempData["Error"] =
                        "Voucher không hợp lệ.";

                    return RedirectToAction(nameof(CheckOut));
                }

                // Voucher đã sử dụng
                if (userVoucher.IsUsed)
                {
                    TempData["Error"] =
                        "Voucher này đã được sử dụng.";

                    return RedirectToAction(nameof(CheckOut));
                }

                // Voucher không hoạt động
                if (!userVoucher.Voucher.IsActive)
                {
                    TempData["Error"] =
                        "Voucher hiện không khả dụng.";

                    return RedirectToAction(nameof(CheckOut));
                }

                // Voucher hết hạn
                if (userVoucher.Voucher.ExpiryDate < DateTime.Now)
                {
                    TempData["Error"] =
                        "Voucher này đã hết hạn.";

                    return RedirectToAction(nameof(CheckOut));
                }

                // Kiểm tra giá trị đơn hàng tối thiểu
                if (productTotal <
                    userVoucher.Voucher.MinimumOrderAmount)
                {
                    TempData["Error"] =
                        $"Đơn hàng phải từ " +
                        $"{userVoucher.Voucher.MinimumOrderAmount:N0}đ " +
                        $"mới sử dụng được voucher này.";

                    return RedirectToAction(nameof(CheckOut));
                }

                // Lấy số tiền giảm
                voucherDiscount =
                    userVoucher.Voucher.DiscountAmount;

                // Không cho voucher giảm vượt quá
                // số tiền còn lại sau khi trừ điểm
                var remainingAfterPoints =
                    totalBeforeDiscount - pointDiscount;

                if (voucherDiscount > remainingAfterPoints)
                {
                    voucherDiscount =
                        remainingAfterPoints;
                }
            }

            // =========================================================
            // 11. GÁN THÔNG TIN GIẢM GIÁ VÀO ORDER
            // =========================================================

            order.UsedPoints =
                usedPoints;

            order.PointDiscount =
                pointDiscount;

            order.UserVoucherId =
                userVoucher?.UserVoucherId;

            order.VoucherDiscount =
                voucherDiscount;

            // =========================================================
            // 12. TÍNH TỔNG THANH TOÁN
            // =========================================================

            order.TotalAmount =
                totalBeforeDiscount
                - pointDiscount
                - voucherDiscount;

            if (order.TotalAmount < 0)
            {
                order.TotalAmount = 0;
            }

            // =========================================================
            // 13. BẮT ĐẦU DATABASE TRANSACTION
            // =========================================================

            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            try
            {
                // =====================================================
                // 14. TẠO ORDER DETAIL + ATOMIC TRỪ KHO
                // =====================================================

                order.OrderDetails =
                    new List<OrderDetail>();

                foreach (var item in cart)
                {
                    // Lấy sản phẩm mới nhất từ database
                    var product = await _context.Products
                        .FirstOrDefaultAsync(p =>
                            p.ProductId == item.ProductId);

                    if (product == null)
                    {
                        await transaction.RollbackAsync();

                        TempData["Error"] =
                            "Một sản phẩm trong giỏ hàng " +
                            "không còn tồn tại.";

                        return RedirectToAction(
                            "Index",
                            "ShoppingCart");
                    }

                    // =================================================
                    // ATOMIC UPDATE
                    //
                    // Chỉ trừ kho nếu database vẫn còn đủ hàng.
                    //
                    // Ví dụ:
                    // Kho = 10
                    // User A mua 10 -> thành công
                    // User B mua 10 -> rowsAffected = 0
                    // =================================================

                    var rowsAffected =
                        await _context.Database
                            .ExecuteSqlInterpolatedAsync($@"
                        UPDATE Products
                        SET Quantity = Quantity - {item.Quantity}
                        WHERE ProductId = {item.ProductId}
                          AND Quantity >= {item.Quantity}
                    ");

                    // Không trừ được kho
                    if (rowsAffected == 0)
                    {
                        await transaction.RollbackAsync();

                        TempData["Error"] =
                            $"Sản phẩm {product.ProductName} " +
                            $"không còn đủ số lượng. " +
                            $"Vui lòng kiểm tra lại giỏ hàng.";

                        return RedirectToAction(
                            "Index",
                            "ShoppingCart");
                    }

                    // =================================================
                    // TẠO ORDER DETAIL
                    // =================================================

                    order.OrderDetails.Add(
                        new OrderDetail
                        {
                            ProductId = item.ProductId,
                            Quantity = item.Quantity,
                            Price = item.Price
                        });
                }

                // =====================================================
                // 15. GÁN USER CHO ORDER
                // =====================================================

                order.UserId =
                    user.Id;

                // =====================================================
                // 16. THÊM ORDER VÀO DATABASE
                // =====================================================

                _context.Orders.Add(order);

                // =====================================================
                // 17. SAVE LẦN 1
                //
                // Mục đích:
                // SQL Server sinh OrderId
                // =====================================================

                await _context.SaveChangesAsync();

                // Lúc này:
                // order.OrderId đã có giá trị thật

                // =====================================================
                // 18. TRỪ ĐIỂM
                // =====================================================

                if (usedPoints > 0)
                {
                    user.Points -= usedPoints;

                    var pointTransaction =
                        new PointTransaction
                        {
                            UserId = user.Id,

                            Points = -usedPoints,

                            Type = "Spend",

                            OrderId =
                                order.OrderId,

                            Description =
                                $"Sử dụng {usedPoints} điểm " +
                                $"cho đơn hàng #{order.OrderId}",

                            CreatedAt =
                                DateTime.Now
                        };

                    _context.PointTransactions.Add(
                        pointTransaction);
                }

                // =====================================================
                // 19. ĐÁNH DẤU VOUCHER ĐÃ SỬ DỤNG
                // =====================================================

                if (userVoucher != null)
                {
                    userVoucher.IsUsed = true;

                    userVoucher.UsedDate =
                        DateTime.Now;
                }

                // =====================================================
                // 20. SAVE LẦN 2
                // =====================================================

                await _context.SaveChangesAsync();

                // =====================================================
                // 21. COMMIT
                // =====================================================

                await transaction.CommitAsync();
            }
            catch
            {
                // Nếu bất kỳ bước nào lỗi:
                // - Trừ kho
                // - Tạo Order
                // - Trừ điểm
                // - Voucher
                //
                // => rollback toàn bộ

                await transaction.RollbackAsync();

                throw;
            }

            // =========================================================
            // 22. XÓA GIỎ HÀNG
            // =========================================================

            _cartRepo.ClearCart();

            // =========================================================
            // 23. CHUYỂN SANG TRANG HOÀN TẤT
            // =========================================================

            return RedirectToAction(
                nameof(CheckOutComplete),
                new
                {
                    orderId = order.OrderId
                });
        }
        [Authorize]
        public async Task<IActionResult> CheckOutComplete(int? orderId)
        {
            if (orderId == null)
            {
                return RedirectToAction("Index", "Home");
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var order = await _context.Orders
                .FirstOrDefaultAsync(x => x.OrderId == orderId && x.UserId == userId);

            if (order == null)
            {
                return RedirectToAction("Index", "Home");
            }

            return View(order);
        }
        [Authorize]
        public async Task<IActionResult> UserOrderList()
        {
            var userId =
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier);

            var orders = await _context.Orders
    .Where(x => x.UserId == userId)
    .Include(x => x.OrderDetails)
        .ThenInclude(x => x.Product)
    .OrderByDescending(x => x.OrderDate)
    .ToListAsync();

            return View(orders);
        }
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelMyOrder(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var order = await _context.Orders
                .Include(o => o.UserVoucher)
                .FirstOrDefaultAsync(o =>
                    o.OrderId == id &&
                    o.UserId == userId);

            if (order == null)
            {
                TempData["Error"] = "Không tìm thấy đơn hàng.";
                return RedirectToAction(nameof(UserOrderList));
            }

            // Chỉ cho phép hủy những đơn chưa hoàn thành / chưa hủy
            if (order.Status == "Hoàn thành")
            {
                TempData["Error"] = "Đơn hàng đã hoàn thành nên không thể hủy.";
                return RedirectToAction(nameof(UserOrderList));
            }

            if (order.Status == "Đã hủy")
            {
                TempData["Error"] = "Đơn hàng này đã được hủy trước đó.";
                return RedirectToAction(nameof(UserOrderList));
            }

            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
            {
                TempData["Error"] = "Không tìm thấy tài khoản.";
                return RedirectToAction(nameof(UserOrderList));
            }

            // ==========================================
            // 1. HOÀN ĐIỂM
            // ==========================================

            if (order.UsedPoints > 0)
            {
                user.Points += order.UsedPoints;

                var refundPointTransaction = new PointTransaction
                {
                    UserId = userId,
                    Points = order.UsedPoints,
                    Type = "Refund",
                    Description = $"Hoàn {order.UsedPoints} điểm do hủy đơn hàng #{order.OrderId}",
                    OrderId = order.OrderId,
                    CreatedAt = DateTime.Now
                };

                _context.PointTransactions.Add(refundPointTransaction);
            }

            // ==========================================
            // 2. HOÀN VOUCHER
            // ==========================================

            if (order.UserVoucherId.HasValue)
            {
                var userVoucher = order.UserVoucher;

                if (userVoucher != null)
                {
                    userVoucher.IsUsed = false;
                    userVoucher.UsedDate = null;
                }
            }

            // ==========================================
            // 3. ĐỔI TRẠNG THÁI ĐƠN
            // ==========================================

            order.Status = "Đã hủy";

            await _context.SaveChangesAsync();

            TempData["Success"] = "Hủy đơn hàng thành công. Điểm và voucher đã được hoàn lại.";

            return RedirectToAction(nameof(UserOrderList));
        }
    }
}
