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

            // Lấy địa chỉ mặc định nếu có
            var defaultAddress = await _context.UserAddresses
                .FirstOrDefaultAsync(x =>
                    x.UserId == user.Id &&
                    x.IsDefault);

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
            if (order.DeliveryMethod != "Ship" &&
    order.DeliveryMethod != "Pickup")
            {
                TempData["Error"] =
                    "Phương thức nhận hàng không hợp lệ.";

                return RedirectToAction(nameof(CheckOut));
            }

            if (order.PaymentMethod != "COD" &&
                order.PaymentMethod != "BankTransfer")
            {
                TempData["Error"] =
                    "Phương thức thanh toán không hợp lệ.";

                return RedirectToAction(nameof(CheckOut));
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
                TempData["Info"] =
                    "Bạn chưa có địa chỉ nhận hàng. Vui lòng thêm địa chỉ.";

                return RedirectToAction(
                    "Create",
                    "Address",
                    new { fromCheckout = true });
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
            // 6. LẤY GIÁ SẢN PHẨM MỚI NHẤT TỪ DATABASE
            // =========================================================

            var productIds = cart
                .Select(x => x.ProductId)
                .Distinct()
                .ToList();

            var products = await _context.Products
                .Where(p => productIds.Contains(p.ProductId))
                .ToDictionaryAsync(p => p.ProductId);

            // Kiểm tra sản phẩm còn tồn tại
            foreach (var item in cart)
            {
                if (!products.ContainsKey(item.ProductId))
                {
                    TempData["Error"] =
                        "Một sản phẩm trong giỏ hàng không còn tồn tại.";

                    return RedirectToAction(
                        "Index",
                        "ShoppingCart");
                }
            }

            // =========================================================
            // TÍNH LẠI TỔNG TIỀN TỪ DATABASE
            // =========================================================

            decimal productTotal = 0;

            foreach (var item in cart)
            {
                var product = products[item.ProductId];

                // Giá hiện tại sau giảm giá
                var currentPrice = product.DiscountPrice;

                productTotal += currentPrice * item.Quantity;
            }

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

            // ==========================================
            // TRẠNG THÁI THANH TOÁN
            // ==========================================

            // Khi khách vừa đặt hàng:
            // COD hoặc chuyển khoản đều chưa được xác nhận thanh toán.
            // Admin sẽ xác nhận thanh toán sau đối với BankTransfer.
            order.PaymentStatus = "Chưa thanh toán";

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
                    var product = products[item.ProductId];

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

                    var currentPrice = product.DiscountPrice;

                    order.OrderDetails.Add(
                        new OrderDetail
                        {
                            ProductId = item.ProductId,
                            Quantity = item.Quantity,
                            Price = currentPrice
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
                .FirstOrDefaultAsync(x =>
                    x.OrderId == orderId &&
                    x.UserId == userId);

            if (order == null)
            {
                return RedirectToAction("Index", "Home");
            }

            return View(order);
        }
        [Authorize]
        public async Task<IActionResult> UserOrderList(int page = 1)
        {
            const int pageSize = 5;

            var userId = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var totalOrders = await _context.Orders
                .CountAsync(x => x.UserId == userId);

            var totalPages = (int)Math.Ceiling(
                totalOrders / (double)pageSize);

            if (page < 1)
                page = 1;

            if (totalPages > 0 && page > totalPages)
                page = totalPages;

            var orders = await _context.Orders
    .Where(x => x.UserId == userId)
    .Include(x => x.OrderDetails)
        .ThenInclude(x => x.Product)
    .Include(x => x.ReturnRequest)
    .OrderByDescending(x => x.OrderDate)
    .Skip((page - 1) * pageSize)
    .Take(pageSize)
    .ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;

            return View(orders);
        }
        [Authorize]
        public async Task<IActionResult> UserOrderDetail(int id)
        {
            // ==========================================
            // 1. LẤY USER HIỆN TẠI
            // ==========================================

            var userId = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
                return Unauthorized();


            // ==========================================
            // 2. LẤY ĐƠN HÀNG
            // ==========================================

            var order = await _context.Orders
                .Include(x => x.OrderDetails)
                    .ThenInclude(x => x.Product)

                .Include(x => x.UserVoucher)
                    .ThenInclude(x => x.Voucher)

                .Include(x => x.ReturnRequest)

                .FirstOrDefaultAsync(x =>
                    x.OrderId == id &&
                    x.UserId == userId);


            // ==========================================
            // 3. KHÔNG TÌM THẤY ĐƠN HÀNG
            // ==========================================

            if (order == null)
            {
                return NotFound();
            }


            // ==========================================
            // 4. LẤY DANH SÁCH SẢN PHẨM ĐÃ ĐÁNH GIÁ
            // ==========================================

            var reviewedOrderDetailIds =
     await _context.ProductReviews
         .Where(r =>
             r.UserId == userId &&
             order.OrderDetails
                 .Select(od => od.OrderDetailId)
                 .Contains(r.OrderDetailId))
         .Select(r => r.OrderDetailId)
         .ToListAsync();




            // ==========================================
            // 5. GỬI DANH SÁCH SANG VIEW
            // ==========================================
            ViewBag.ReviewedOrderDetailIds =
                            reviewedOrderDetailIds;


            // ==========================================
            // 6. HIỂN THỊ VIEW
            // ==========================================

            return View(order);
        }
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelMyOrder(
     int id,
     string cancellationReason)
        {
            var userId = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            try
            {
                var order = await _context.Orders
                    .Include(o => o.UserVoucher)
                        .ThenInclude(uv => uv.Voucher)
                    .Include(o => o.OrderDetails)
                        .ThenInclude(od => od.Product)
                    .FirstOrDefaultAsync(o =>
                        o.OrderId == id &&
                        o.UserId == userId);

                if (order == null)
                {
                    TempData["Error"] =
                        "Không tìm thấy đơn hàng.";

                    return RedirectToAction(nameof(UserOrderList));
                }

                // Chỉ được hủy khi đang chờ xác nhận
                if (order.Status != "Chờ xác nhận")
                {
                    TempData["Error"] =
                        "Chỉ có thể hủy đơn hàng khi đơn đang ở trạng thái Chờ xác nhận.";

                    return RedirectToAction(nameof(UserOrderList));
                }

                if (string.IsNullOrWhiteSpace(cancellationReason))
                {
                    TempData["Error"] =
                        "Vui lòng chọn lý do hủy đơn hàng.";

                    return RedirectToAction(nameof(UserOrderList));
                }

                var user = await _userManager.FindByIdAsync(userId);

                if (user == null)
                {
                    TempData["Error"] =
                        "Không tìm thấy tài khoản.";

                    return RedirectToAction(nameof(UserOrderList));
                }

                // ==========================================
                // 1. HOÀN LẠI TỒN KHO
                // ==========================================

                if (order.OrderDetails != null)
                {
                    foreach (var detail in order.OrderDetails)
                    {
                        if (detail.Product != null)
                        {
                            detail.Product.Quantity += detail.Quantity;
                        }
                    }
                }

                // ==========================================
                // 2. HOÀN LẠI ĐIỂM ĐÃ SỬ DỤNG
                // ==========================================

                if (order.UsedPoints > 0)
                {
                    user.Points += order.UsedPoints;

                    var refundPointTransaction =
                        new PointTransaction
                        {
                            UserId = user.Id,
                            Points = order.UsedPoints,
                            Type = "Refund",
                            Description =
                                $"Hoàn {order.UsedPoints} điểm " +
                                $"do hủy đơn hàng #{order.OrderId}",
                            OrderId = order.OrderId,
                            CreatedAt = DateTime.Now
                        };

                    _context.PointTransactions.Add(
                        refundPointTransaction);
                }

                // ==========================================
                // 3. HOÀN LẠI VOUCHER
                // ==========================================

                if (order.UserVoucher != null &&
                    order.UserVoucher.IsUsed)
                {
                    order.UserVoucher.IsUsed = false;
                    order.UserVoucher.UsedDate = null;

                    if (order.UserVoucher.Voucher != null)
                    {
                        order.UserVoucher.Voucher.Quantity++;
                    }
                }

                // ==========================================
                // 4. LƯU LÝ DO HỦY
                // ==========================================

                order.CancellationReason =
                    cancellationReason.Trim();

                // ==========================================
                // 5. ĐỔI TRẠNG THÁI
                // ==========================================

                order.Status = "Đã hủy";

                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                TempData["Success"] =
                    "Hủy đơn hàng thành công. " +
                    "Sản phẩm, điểm và voucher đã được hoàn lại.";

                return RedirectToAction(nameof(UserOrderList));
            }
            catch
            {
                await transaction.RollbackAsync();

                TempData["Error"] =
                    "Có lỗi xảy ra trong quá trình hủy đơn hàng.";

                return RedirectToAction(nameof(UserOrderList));
            }
        }
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> ReturnRequest(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var order = await _context.Orders
    .AsNoTracking()
    .Include(o => o.OrderDetails)
        .ThenInclude(od => od.Product)
    .Include(o => o.ReturnRequest)
    .FirstOrDefaultAsync(o =>
        o.OrderId == id &&
        o.UserId == userId);

            if (order == null)
                return NotFound();

            // Chỉ được yêu cầu trả hàng khi đơn đã hoàn thành
            if (order.Status != "Hoàn thành")
            {
                TempData["Error"] =
                    "Chỉ có thể yêu cầu trả hàng đối với đơn hàng đã hoàn thành.";

                return RedirectToAction(nameof(UserOrderDetail), new { id });
            }

            // Đã có yêu cầu trả hàng
            if (order.ReturnRequest != null)
            {
                TempData["Error"] =
                    "Đơn hàng này đã có yêu cầu trả hàng.";

                return RedirectToAction(nameof(UserOrderDetail), new { id });
            }

            // Kiểm tra thời hạn 7 ngày
            if (!order.CompletedDate.HasValue ||
                DateTime.Now > order.CompletedDate.Value.AddDays(7))
            {
                TempData["Error"] =
                    "Đơn hàng đã quá thời hạn 7 ngày để yêu cầu trả hàng.";

                return RedirectToAction(nameof(UserOrderDetail), new { id });
            }

            ViewBag.Order = order;

            return View();
        }
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(60 * 1024 * 1024)]
        public async Task<IActionResult> ReturnRequest(
     int id,
     string reason,
     string? description,
     IFormFile? evidenceImage,
     IFormFile? evidenceVideo)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var order = await _context.Orders
                .Include(o => o.ReturnRequest)
                .FirstOrDefaultAsync(o =>
                    o.OrderId == id &&
                    o.UserId == userId);

            if (order == null)
                return NotFound();

            // =========================================================
            // 1. KIỂM TRA TRẠNG THÁI ĐƠN HÀNG
            // =========================================================

            if (order.Status != "Hoàn thành")
            {
                TempData["Error"] =
                    "Chỉ có thể yêu cầu trả hàng đối với đơn hàng đã hoàn thành.";

                return RedirectToAction(
                    nameof(UserOrderDetail),
                    new { id });
            }

            // =========================================================
            // 2. KIỂM TRA ĐÃ CÓ YÊU CẦU TRẢ HÀNG CHƯA
            // =========================================================

            if (order.ReturnRequest != null)
            {
                TempData["Error"] =
                    "Đơn hàng này đã có yêu cầu trả hàng.";

                return RedirectToAction(
                    nameof(UserOrderDetail),
                    new { id });
            }

            // =========================================================
            // 3. KIỂM TRA THỜI HẠN 7 NGÀY
            // =========================================================

            if (!order.CompletedDate.HasValue ||
                DateTime.Now > order.CompletedDate.Value.AddDays(7))
            {
                TempData["Error"] =
                    "Đơn hàng đã quá thời hạn 7 ngày để yêu cầu trả hàng.";

                return RedirectToAction(
                    nameof(UserOrderDetail),
                    new { id });
            }

            // =========================================================
            // 4. KIỂM TRA LÝ DO
            // =========================================================

            if (string.IsNullOrWhiteSpace(reason))
            {
                TempData["Error"] =
                    "Vui lòng chọn lý do trả hàng.";

                return RedirectToAction(
                    nameof(ReturnRequest),
                    new { id });
            }

            // =========================================================
            // 5. PHẢI CÓ ẢNH HOẶC VIDEO
            // =========================================================

            if (evidenceImage == null &&
                evidenceVideo == null)
            {
                TempData["Error"] =
                    "Vui lòng cung cấp ít nhất một hình ảnh hoặc video làm bằng chứng.";

                return RedirectToAction(
                    nameof(ReturnRequest),
                    new { id });
            }

            // =========================================================
            // 6. KIỂM TRA DUNG LƯỢNG
            // =========================================================

            const long maxImageSize = 5 * 1024 * 1024;   // 5 MB
            const long maxVideoSize = 50 * 1024 * 1024;  // 50 MB

            if (evidenceImage != null &&
                evidenceImage.Length > maxImageSize)
            {
                TempData["Error"] =
                    "Hình ảnh không được vượt quá 5 MB.";

                return RedirectToAction(
                    nameof(ReturnRequest),
                    new { id });
            }

            if (evidenceVideo != null &&
                evidenceVideo.Length > maxVideoSize)
            {
                TempData["Error"] =
                    "Video không được vượt quá 50 MB.";

                return RedirectToAction(
                    nameof(ReturnRequest),
                    new { id });
            }

            // =========================================================
            // 7. KIỂM TRA EXTENSION TRƯỚC KHI LƯU FILE
            // =========================================================

            string? imageExtension = null;
            string? videoExtension = null;

            // ---------- IMAGE ----------

            if (evidenceImage != null &&
                evidenceImage.Length > 0)
            {
                var allowedImageExtensions = new[]
                {
            ".jpg",
            ".jpeg",
            ".png",
            ".webp"
        };

                imageExtension =
                    Path.GetExtension(evidenceImage.FileName)
                        .ToLowerInvariant();

                if (!allowedImageExtensions.Contains(imageExtension))
                {
                    TempData["Error"] =
                        "Hình ảnh phải có định dạng JPG, JPEG, PNG hoặc WEBP.";

                    return RedirectToAction(
                        nameof(ReturnRequest),
                        new { id });
                }
            }

            // ---------- VIDEO ----------

            if (evidenceVideo != null &&
                evidenceVideo.Length > 0)
            {
                var allowedVideoExtensions = new[]
                {
            ".mp4",
            ".mov",
            ".avi",
            ".webm"
        };

                videoExtension =
                    Path.GetExtension(evidenceVideo.FileName)
                        .ToLowerInvariant();

                if (!allowedVideoExtensions.Contains(videoExtension))
                {
                    TempData["Error"] =
                        "Video phải có định dạng MP4, MOV, AVI hoặc WEBM.";

                    return RedirectToAction(
                        nameof(ReturnRequest),
                        new { id });
                }
            }

            // =========================================================
            // 8. TẠO THƯ MỤC UPLOAD
            // =========================================================

            var folderPath = Path.Combine(
                Directory.GetCurrentDirectory(),
                "wwwroot",
                "uploads",
                "returns");

            Directory.CreateDirectory(folderPath);

            string? imageUrl = null;
            string? videoUrl = null;

            // =========================================================
            // 9. LƯU IMAGE
            // =========================================================

            if (evidenceImage != null &&
                evidenceImage.Length > 0 &&
                imageExtension != null)
            {
                var fileName =
                    Guid.NewGuid().ToString() +
                    imageExtension;

                var filePath =
                    Path.Combine(folderPath, fileName);

                using (var stream = new FileStream(
                    filePath,
                    FileMode.Create))
                {
                    await evidenceImage.CopyToAsync(stream);
                }

                imageUrl =
                    "/uploads/returns/" + fileName;
            }

            // =========================================================
            // 10. LƯU VIDEO
            // =========================================================

            if (evidenceVideo != null &&
                evidenceVideo.Length > 0 &&
                videoExtension != null)
            {
                var fileName =
                    Guid.NewGuid().ToString() +
                    videoExtension;

                var filePath =
                    Path.Combine(folderPath, fileName);

                using (var stream = new FileStream(
                    filePath,
                    FileMode.Create))
                {
                    await evidenceVideo.CopyToAsync(stream);
                }

                videoUrl =
                    "/uploads/returns/" + fileName;
            }

            // =========================================================
            // 11. TẠO RETURN REQUEST
            // =========================================================

            var returnRequest = new ReturnRequest
            {
                OrderId = order.OrderId,
                UserId = userId!,
                Reason = reason.Trim(),
                Description = description?.Trim(),
                EvidenceImageUrl = imageUrl,
                EvidenceVideoUrl = videoUrl,
                Status = "Chờ xử lý",
                CreatedAt = DateTime.Now
            };

            _context.ReturnRequests.Add(returnRequest);

            await _context.SaveChangesAsync();

            // =========================================================
            // 12. THÔNG BÁO
            // =========================================================

            TempData["Success"] =
                "Yêu cầu trả hàng đã được gửi. PetFeast sẽ kiểm tra và xử lý.";

            return RedirectToAction(
                nameof(UserOrderDetail),
                new { id });
        }
    }
}
