using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PetFeast.Data;
using PetFeast.Models.Interfaces;
using PetFeast.Models.Orders;
using System.Security.Claims;
using PetFeast.Models.Points;
using PetFeast.Models.Identity;
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

            return View(cart);
        }
        // LƯU ĐƠN HÀNG
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckOut(
    Order order,
    int usedPoints = 0)
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

            // Lấy user hiện tại
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return Unauthorized();
            }

            // ==========================
            // LẤY ĐỊA CHỈ MẶC ĐỊNH
            // ==========================

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


            // ==========================
            // THÔNG TIN KHÁCH HÀNG
            // ==========================

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


            // ==========================
            // ĐIỂM
            // ==========================

            // Không cho sử dụng điểm âm
            if (usedPoints < 0)
            {
                usedPoints = 0;
            }

            // Không cho sử dụng quá số điểm đang có
            if (usedPoints > user.Points)
            {
                TempData["Error"] =
                    "Số điểm sử dụng vượt quá số điểm hiện có.";

                return RedirectToAction(nameof(CheckOut));
            }


            // ==========================
            // TỔNG TIỀN SẢN PHẨM
            // ==========================

            var productTotal =
                cart.Sum(x => x.TotalPrice);


            // ==========================
            // PHÍ SHIP
            // ==========================

            if (order.DeliveryMethod == "Ship")
            {
                if (productTotal >= 199000)
                {
                    order.ShippingFee = 0;
                }
                else
                {
                    order.ShippingFee = 20000;
                }
            }
            else
            {
                order.ShippingFee = 0;
            }


            // ==========================
            // TỔNG TRƯỚC KHI DÙNG ĐIỂM
            // ==========================

            var totalBeforePoints =
                productTotal + order.ShippingFee;


            // ==========================
            // 1 ĐIỂM = 1.000Đ
            // ==========================

            decimal pointDiscount =
                usedPoints * 1000m;


            // Không cho giảm quá tổng tiền
            if (pointDiscount > totalBeforePoints)
            {
                pointDiscount = totalBeforePoints;

                usedPoints =
                    (int)(pointDiscount / 1000m);
            }


            // ==========================
            // LƯU ĐIỂM VÀO ORDER
            // ==========================

            order.UsedPoints = usedPoints;

            order.PointDiscount =
                pointDiscount;


            // ==========================
            // TỔNG THANH TOÁN
            // ==========================

            order.TotalAmount =
                totalBeforePoints - pointDiscount;


            // ==========================
            // ORDER DETAIL
            // ==========================

            order.OrderDetails =
                new List<OrderDetail>();

            foreach (var item in cart)
            {
                var product =
                    _productRepo.GetById(item.ProductId);

                if (product == null)
                {
                    return BadRequest();
                }

                if (product.Quantity < item.Quantity)
                {
                    TempData["Error"] =
                        $"Sản phẩm {product.ProductName} chỉ còn {product.Quantity} sản phẩm.";

                    return RedirectToAction(
                        "Index",
                        "ShoppingCart");
                }

                product.Quantity -= item.Quantity;

                order.OrderDetails.Add(
                    new OrderDetail
                    {
                        ProductId = item.ProductId,
                        Quantity = item.Quantity,
                        Price = item.Price
                    });
            }


            // ==========================
            // USER ID
            // ==========================

            order.UserId =
                user.Id;


            // ==========================
            // LƯU ORDER
            // ==========================

            _orderRepo.Add(order);

            _orderRepo.Save();


            // ==========================
            // TRỪ ĐIỂM
            // ==========================

            if (usedPoints > 0)
            {
                user.Points -= usedPoints;

                var pointTransaction =
                    new PointTransaction
                    {
                        UserId = user.Id,
                        Points = -usedPoints,
                        Type = "Spend",
                        OrderId = order.OrderId,
                        Description =
                            $"Sử dụng {usedPoints} điểm cho đơn hàng #{order.OrderId}",
                        CreatedAt = DateTime.Now
                    };

                _context.PointTransactions.Add(
                    pointTransaction);

                await _context.SaveChangesAsync();
            }


            // ==========================
            // XÓA GIỎ HÀNG
            // ==========================

            _cartRepo.ClearCart();

            return RedirectToAction(
                nameof(CheckOutComplete));
        }
        [Authorize]
        public IActionResult CheckOutComplete()
        {
            return View();
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

            var order = await _context.Orders
                .FirstOrDefaultAsync(x =>
                    x.OrderId == id &&
                    x.UserId == userId);

            if (order == null)
            {
                return NotFound();
            }

            // Chỉ được hủy khi đơn chưa được Admin xác nhận
            if (order.Status != "Chờ xác nhận")
            {
                TempData["Error"] =
                    "Đơn hàng đã được xử lý nên không thể hủy.";

                return RedirectToAction(nameof(UserOrderList));
            }

            order.Status = "Đã hủy";

            await _context.SaveChangesAsync();

            TempData["Success"] =
                $"Đã hủy đơn hàng #{order.OrderId} thành công.";

            return RedirectToAction(nameof(UserOrderList));
        }
    }
}
