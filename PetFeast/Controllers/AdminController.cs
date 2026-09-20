using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PetFeast.Data;
using PetFeast.Models.Identity;
using PetFeast.Models.Interfaces;
using PetFeast.Models.Points;
using PetFeast.Models.Products;
using PetFeast.Models.Services;
using PetFeast.Models.Voucher;
using PetFeast.Models.Orders;
namespace PetFeast.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly IProductRepository _productRepo;
        private readonly CategoryIRepository _categoryRepo;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly PetFeastDBContext _context;
        private readonly PointsRepository _pointsRepository;

        public AdminController(
            IProductRepository productRepo,
            CategoryIRepository categoryRepo,
            PetFeastDBContext context,
            UserManager<ApplicationUser> userManager,
            PointsRepository pointsRepository)
        {
            _productRepo = productRepo;
            _categoryRepo = categoryRepo;
            _context = context;
            _userManager = userManager;
            _pointsRepository = pointsRepository;
        }

        public async Task<IActionResult> Dashboard()
        {
            var currentYear = DateTime.Now.Year;
            var currentMonth = DateTime.Now.Month;

            var totalOrders = await _context.Orders
                .CountAsync(o => o.Status != "Đã hủy");

            var completedOrders = await _context.Orders
                .CountAsync(o =>
                    o.Status == "Hoàn thành" &&
                    o.PaymentStatus != "Đã hoàn tiền");

            var completionRate = totalOrders > 0
                ? (int)Math.Round(
                    (double)completedOrders / totalOrders * 100)
                : 0;

            var monthRevenue = await _context.Orders
                .Where(o =>
                    o.OrderDate.Year == currentYear &&
                    o.OrderDate.Month == currentMonth &&
                    o.Status != "Đã hủy" &&
                    o.PaymentStatus == "Đã thanh toán")
                .SumAsync(o => (decimal?)o.TotalAmount) ?? 0;

            // ================================
            // DOANH THU 12 THÁNG
            // ================================

            var revenueCurrentYear = new decimal[12];
            var revenuePrevYear = new decimal[12];

            for (int month = 1; month <= 12; month++)
            {
                revenueCurrentYear[month - 1] =
                    await _context.Orders
                        .Where(o =>
                            o.OrderDate.Year == currentYear &&
                            o.OrderDate.Month == month &&
                            o.Status != "Đã hủy" &&
                            o.PaymentStatus == "Đã thanh toán")
                        .SumAsync(o => (decimal?)o.TotalAmount) ?? 0;

                revenuePrevYear[month - 1] =
                    await _context.Orders
                        .Where(o =>
                            o.OrderDate.Year == currentYear - 1 &&
                            o.OrderDate.Month == month &&
                            o.Status != "Đã hủy" &&
                            o.PaymentStatus == "Đã thanh toán")
                        .SumAsync(o => (decimal?)o.TotalAmount) ?? 0;
            }

            ViewBag.TotalProducts =
                await _context.Products.CountAsync();

            ViewBag.TotalCategories =
                await _context.Categories.CountAsync();

            ViewBag.TotalOrders = totalOrders;
            ViewBag.CompletedOrders = completedOrders;
            ViewBag.CompletionRate = completionRate;

            // Doanh thu tháng hiện tại
            ViewBag.TotalRevenue = monthRevenue;

            ViewBag.BestProducts =
                _productRepo.GetBestSellingProducts(5);

            ViewBag.RecentOrders =
                await _context.Orders
                    .OrderByDescending(o => o.OrderDate)
                    .Take(5)
                    .ToListAsync();

            // Dữ liệu biểu đồ
            ViewBag.RevenueCurrentYear = revenueCurrentYear;
            ViewBag.RevenuePrevYear = revenuePrevYear;

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetDashboardRealtimeData()
        {
            var currentYear = DateTime.Now.Year;
            var currentMonth = DateTime.Now.Month;

            var totalProducts = await _context.Products.CountAsync();
            var totalCategories = await _context.Categories.CountAsync();
            var totalOrders = await _context.Orders
     .CountAsync(o => o.Status != "Đã hủy");

            var completedOrders = await _context.Orders
                .CountAsync(o =>
                    o.Status == "Hoàn thành" &&
                    o.PaymentStatus != "Đã hoàn tiền");

            var completionRate = totalOrders > 0
                ? (int)Math.Round(
                    (double)completedOrders / totalOrders * 100)
                : 0;

            // Doanh thu tháng hiện tại
            var monthRevenue = await _context.Orders
    .Where(o => o.OrderDate.Month == currentMonth
             && o.OrderDate.Year == currentYear
             && o.Status != "Đã hủy"
             && o.PaymentStatus == "Đã thanh toán")
    .SumAsync(o => (decimal?)o.TotalAmount) ?? 0;

            // Tổng doanh thu toàn bộ
            var totalRevenue = await _context.Orders
    .Where(o => o.Status != "Đã hủy"
             && o.PaymentStatus == "Đã thanh toán")
    .SumAsync(o => (decimal?)o.TotalAmount) ?? 0;

            // ================================
            // BIỂU ĐỒ TĂNG TRƯỞNG DOANH THU
            // ================================

            var revCurrentYear = new decimal[12];
            var revPrevYear = new decimal[12];

            for (int m = 1; m <= 12; m++)
            {
                revCurrentYear[m - 1] = await _context.Orders
                    .Where(o =>
                        o.OrderDate.Year == currentYear &&
                        o.OrderDate.Month == m &&
                        o.Status != "Đã hủy" &&
                        o.PaymentStatus == "Đã thanh toán")
                    .SumAsync(o => (decimal?)o.TotalAmount) ?? 0;

                revPrevYear[m - 1] = await _context.Orders
                    .Where(o =>
                        o.OrderDate.Year == currentYear - 1 &&
                        o.OrderDate.Month == m &&
                        o.Status != "Đã hủy" &&
                        o.PaymentStatus == "Đã thanh toán")
                    .SumAsync(o => (decimal?)o.TotalAmount) ?? 0;
            }

            var bestProducts = _productRepo.GetBestSellingProducts(5);

            // 5 đơn hàng mới nhất cho chu kỳ Polling Realtime
            var recentOrders = await _context.Orders
    .OrderByDescending(o => o.OrderDate)
    .Take(5)
    .Select(o => new
    {
        orderId = o.OrderId,
        fullName = o.User != null && o.User.FullName != null
            ? o.User.FullName
            : o.CustomerName,
        orderDate = o.OrderDate.ToString("dd/MM/yyyy HH:mm"),
        totalAmount = o.TotalAmount,
        status = o.Status
    })
    .ToListAsync();

            return Json(new
            {
                totalRevenue = monthRevenue,
                totalOrders = totalOrders,
                completedOrders = completedOrders,
                completionRate = completionRate,
                totalProducts = totalProducts,
                totalCategories = totalCategories,

                revenueCurrentYear = revCurrentYear,
                revenuePrevYear = revPrevYear,

                bestProducts = bestProducts,
                recentOrders = recentOrders
            });
        }

        // ================= 2. BÁO CÁO DOANH THU =================
        public IActionResult RevenueReport(DateTime? fromDate, DateTime? toDate)
        {
            var start = fromDate ?? new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            var end = (toDate ?? DateTime.Now.Date).AddDays(1).AddTicks(-1);

            ViewBag.FromDate = start.ToString("yyyy-MM-dd");
            ViewBag.ToDate = (toDate ?? DateTime.Now.Date).ToString("yyyy-MM-dd");

            var ordersList = _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .ThenInclude(p => p.Category)
                .Where(o => o.Status != "Đã hủy"
         && o.PaymentStatus == "Đã thanh toán"
         && o.OrderDate >= start
         && o.OrderDate <= end)
                .OrderByDescending(o => o.OrderDate)
                .ToList();

            var totalRevenue = ordersList.Sum(x => x.TotalAmount);
            var totalOrders = ordersList.Count;
            var avgOrderValue = totalOrders > 0 ? (totalRevenue / totalOrders) : 0;
            var totalProductsSold = ordersList.SelectMany(o => o.OrderDetails).Sum(x => x.Quantity);

            ViewBag.ReportTotalRevenue = totalRevenue;
            ViewBag.ReportTotalOrders = totalOrders;
            ViewBag.ReportAvgOrderValue = avgOrderValue;
            ViewBag.ReportProductsSold = totalProductsSold;

            var dailyRevenue = ordersList
                .GroupBy(o => o.OrderDate.ToString("dd/MM"))
                .Select(g => new { Date = g.Key, Revenue = g.Sum(x => x.TotalAmount) })
                .ToList();

            ViewBag.ChartLabels = dailyRevenue.Select(x => x.Date).ToArray();
            ViewBag.ChartValues = dailyRevenue.Select(x => x.Revenue).ToArray();

            var categoryRevenue = ordersList
    .SelectMany(o => o.OrderDetails)
    .GroupBy(od =>
        od.Product?.Category?.CategoryName ?? "Khác")
    .Select(g => new
    {
        CategoryName = g.Key,
        Revenue = g.Sum(x =>
            x.Quantity * x.Price)
    })
    .OrderByDescending(x => x.Revenue)
    .ToList();

            ViewBag.CatLabels = categoryRevenue.Select(x => x.CategoryName).ToArray();
            ViewBag.CatValues = categoryRevenue.Select(x => x.Revenue).ToArray();

            return View("Revenue/RevenueReport", ordersList);
        }

        // PRODUCT
        public IActionResult ProductList()
        {
            var products = _context.Products
                .Include(x => x.Category)
                .ToList();

            return View("Product/ProductList", products);
        }

        public IActionResult CreateProduct()
        {
            ViewBag.Categories =
                _categoryRepo.GetAll();

            return View("Product/CreateProduct");
        }

        [HttpPost]
        public async Task<IActionResult> CreateProduct(Product product)
        {

            if (!ModelState.IsValid)
            {
                foreach (var error in ModelState.Values.SelectMany(x => x.Errors))
                {
                    Console.WriteLine(error.ErrorMessage);
                }


                ViewBag.Categories = _categoryRepo.GetAll();

                return View("Product/CreateProduct", product);
            }



            if (product.ImageFile != null)
            {
                string folder = Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "wwwroot/img/products");


                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);



                string fileName =
                    Guid.NewGuid() +
                    Path.GetExtension(product.ImageFile.FileName);



                string path =
                    Path.Combine(folder, fileName);



                using (var stream = new FileStream(path, FileMode.Create))
                {
                    await product.ImageFile.CopyToAsync(stream);
                }



                product.ImageUrl =
                    "/img/products/" + fileName;
            }



            _productRepo.Add(product);

            _productRepo.Save();


            return RedirectToAction(nameof(ProductList));
        }

        public IActionResult EditProduct(int id)
        {
            var product =
                _productRepo.GetById(id);

            ViewBag.Categories =
                _categoryRepo.GetAll();

            return View("Product/EditProduct", product);
        }

        [HttpPost]
        public async Task<IActionResult> EditProduct(Product product)
        {
            if (ModelState.IsValid)
            {
                var oldProduct = _productRepo.GetById(product.ProductId);

                if (oldProduct == null)
                    return NotFound();

                if (product.ImageFile != null)
                {
                    string folder = Path.Combine(
                        Directory.GetCurrentDirectory(),
                        "wwwroot/img/products");

                    if (!Directory.Exists(folder))
                        Directory.CreateDirectory(folder);

                    string fileName =
                        Guid.NewGuid() +
                        Path.GetExtension(product.ImageFile.FileName);

                    string path = Path.Combine(folder, fileName);

                    using (var stream =
                        new FileStream(path, FileMode.Create))
                    {
                        await product.ImageFile.CopyToAsync(stream);
                    }

                    oldProduct.ImageUrl =
                        "/img/products/" + fileName;
                }

                oldProduct.ProductName = product.ProductName;
                oldProduct.Price = product.Price;
                oldProduct.Quantity = product.Quantity;
                oldProduct.Description = product.Description;
                oldProduct.CategoryId = product.CategoryId;
                oldProduct.DiscountPercent = product.DiscountPercent;

                _productRepo.Save();

                return RedirectToAction(nameof(ProductList));
            }

            ViewBag.Categories = _categoryRepo.GetAll();

            return View("Product/EditProduct", product);
        }

        public IActionResult DeleteProduct(int id)
        {
            _productRepo.Delete(id);
            _productRepo.Save();

            return RedirectToAction(nameof(ProductList));
        }

        // CATEGORY

        public IActionResult CategoryList()
        {
            return View("Categories/CategoriesList", _categoryRepo.GetAll());
        }

        public IActionResult CreateCategory()
        {
            return View("Categories/CreateCategories");
        }

        [HttpPost]
        public IActionResult CreateCategory(Category category)
        {
            if (ModelState.IsValid)
            {
                _categoryRepo.Add(category);
                _categoryRepo.Save();

                return RedirectToAction(nameof(CategoryList));
            }

            return View("Categories/CreateCategories", category);
        }

        public IActionResult EditCategory(int id)
        {
            return View("Categories/EditCategories", _categoryRepo.GetById(id));
        }

        [HttpPost]
        public IActionResult EditCategory(Category category)
        {
            if (ModelState.IsValid)
            {
                _categoryRepo.Update(category);
                _categoryRepo.Save();

                return RedirectToAction(nameof(CategoryList));
            }

            return View("Categories/EditCategories", category);
        }

        public IActionResult DeleteCategory(int id)
        {
            var category = _context.Categories.Find(id);

            if (category == null)
                return NotFound();

            bool hasProduct =
                _context.Products.Any(x => x.CategoryId == id);

            if (hasProduct)
            {
                TempData["Error"] =
                    "Danh mục đang chứa sản phẩm nên không thể xóa.";

                return RedirectToAction(nameof(CategoryList));
            }

            _categoryRepo.Delete(id);
            _categoryRepo.Save();

            return RedirectToAction(nameof(CategoryList));
        }
        public IActionResult OrderList()
        {
            var orders = _context.Orders
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                .Include(o => o.UserVoucher)
                .OrderByDescending(o => o.OrderDate)
                .ToList();

            return View("Order/OrderList", orders);
        }
        public IActionResult OrderDetail(int id)
        {
            var order = _context.Orders
                .Include(x => x.OrderDetails)
                    .ThenInclude(x => x.Product)
                .Include(x => x.UserVoucher)
                    .ThenInclude(x => x.Voucher)
                .Include(x => x.ReturnRequest)
                .FirstOrDefault(x => x.OrderId == id);

            if (order == null)
                return NotFound();

            return View("Order/OrderDetail", order);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmOrder(int id)
        {
            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.OrderId == id);

            if (order == null)
            {
                TempData["Error"] = "Không tìm thấy đơn hàng.";
                return RedirectToAction(nameof(OrderList));
            }

            // Chỉ xác nhận đơn đang chờ xử lý
            if (order.Status != "Chờ xác nhận")
            {
                TempData["Error"] =
                    $"Đơn hàng #{order.OrderId} không ở trạng thái Chờ xác nhận.";

                return RedirectToAction(nameof(OrderList));
            }

            // Nếu chuyển khoản thì phải thanh toán trước
            if (order.PaymentMethod == "BankTransfer" &&
                order.PaymentStatus != "Đã thanh toán")
            {
                TempData["Error"] =
                    $"Đơn hàng #{order.OrderId} chưa được xác nhận thanh toán chuyển khoản.";

                return RedirectToAction(nameof(OrderList));
            }

            order.Status = "Đang giao";

            await _context.SaveChangesAsync();

            TempData["Success"] =
                $"Đã xác nhận đơn hàng #{order.OrderId}. Đơn hàng đang được giao.";

            return RedirectToAction(nameof(OrderList));
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmPayment(int id)
        {
            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.OrderId == id);

            if (order == null)
            {
                TempData["Error"] = "Không tìm thấy đơn hàng.";
                return RedirectToAction(nameof(OrderList));
            }

            // Chỉ áp dụng cho chuyển khoản
            if (order.PaymentMethod != "BankTransfer")
            {
                TempData["Error"] =
                    "Đơn hàng này không sử dụng phương thức chuyển khoản.";

                return RedirectToAction(nameof(OrderList));
            }

            // Không cho xác nhận thanh toán đơn đã hủy
            if (order.Status == "Đã hủy")
            {
                TempData["Error"] =
                    $"Đơn hàng #{order.OrderId} đã bị hủy nên không thể xác nhận thanh toán.";

                return RedirectToAction(nameof(OrderList));
            }

            // Không xác nhận lại lần 2
            if (order.PaymentStatus == "Đã thanh toán")
            {
                TempData["Error"] =
                    $"Đơn hàng #{order.OrderId} đã được xác nhận thanh toán.";

                return RedirectToAction(nameof(OrderList));
            }
            if (order.Status != "Chờ xác nhận")
            {
                TempData["Error"] =
                    $"Đơn hàng #{order.OrderId} không còn ở trạng thái Chờ xác nhận.";

                return RedirectToAction(nameof(OrderList));
            }
            order.PaymentStatus = "Đã thanh toán";

            await _context.SaveChangesAsync();

            TempData["Success"] =
                $"Đã xác nhận thanh toán cho đơn hàng #{order.OrderId}.";

            return RedirectToAction(nameof(OrderList));
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CompleteOrder(int id)
        {
            var order = await _context.Orders
                .Include(x => x.OrderDetails)
                .FirstOrDefaultAsync(x => x.OrderId == id);

            if (order == null)
            {
                TempData["Error"] = "Không tìm thấy đơn hàng.";
                return RedirectToAction(nameof(OrderList));
            }

            // ==========================================
            // 1. KIỂM TRA TRẠNG THÁI ĐƠN
            // ==========================================

            if (order.Status == "Hoàn thành")
            {
                TempData["Error"] =
                    $"Đơn hàng #{order.OrderId} đã hoàn thành trước đó.";

                return RedirectToAction(nameof(OrderList));
            }

            if (order.Status != "Đang giao")
            {
                TempData["Error"] =
                    $"Không thể hoàn thành đơn hàng #{order.OrderId}. " +
                    "Đơn hàng phải ở trạng thái Đang giao.";

                return RedirectToAction(nameof(OrderList));
            }

            // ==========================================
            // 2. KIỂM TRA THANH TOÁN
            // ==========================================

            if (order.PaymentMethod == "BankTransfer" &&
                order.PaymentStatus != "Đã thanh toán")
            {
                TempData["Error"] =
                    $"Không thể hoàn thành đơn hàng #{order.OrderId} " +
                    "vì chưa được xác nhận thanh toán.";

                return RedirectToAction(nameof(OrderList));
            }

            // ==========================================
            // 3. HOÀN THÀNH ĐƠN
            // ==========================================
            if (order.PaymentMethod == "COD")
            {
                order.PaymentStatus = "Đã thanh toán";
            }
            order.Status = "Hoàn thành";
            order.CompletedDate = DateTime.Now;

            // ==========================================
            // 4. CỘNG 1 ĐIỂM
            // ==========================================

            int points = await _pointsRepository
                .AddPointsForOrderAsync(order);

            await _context.SaveChangesAsync();

            TempData["Success"] =
                $"Đơn hàng #{order.OrderId} đã hoàn thành. " +
                $"Khách hàng nhận được {points} điểm.";

            return RedirectToAction(nameof(OrderList));
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelOrder(int id)
        {
            await using var transaction =
    await _context.Database.BeginTransactionAsync();

            try
            {
                var order = await _context.Orders
   .Include(o => o.UserVoucher)
       .ThenInclude(uv => uv.Voucher)
   .FirstOrDefaultAsync(o => o.OrderId == id);

                if (order == null)
                {
                    TempData["Error"] = "Không tìm thấy đơn hàng.";
                    return RedirectToAction(nameof(OrderList));
                }

                // Không cho hủy đơn đã hoàn thành
                if (order.Status == "Hoàn thành")
                {
                    TempData["Error"] = "Đơn hàng đã hoàn thành nên không thể hủy.";
                    return RedirectToAction(nameof(OrderList));
                }

                // Không cho hủy lại đơn đã hủy
                if (order.Status == "Đã hủy")
                {
                    TempData["Error"] = "Đơn hàng này đã được hủy trước đó.";
                    return RedirectToAction(nameof(OrderList));
                }

                // ==========================================
                // 1. HOÀN ĐIỂM ĐÃ SỬ DỤNG
                // ==========================================

                if (!string.IsNullOrEmpty(order.UserId) && order.UsedPoints > 0)
                {
                    var user = await _userManager.FindByIdAsync(order.UserId);

                    if (user != null)
                    {
                        user.Points += order.UsedPoints;

                        // Lưu lịch sử hoàn điểm
                        var refundTransaction = new PointTransaction
                        {
                            UserId = user.Id,
                            Points = order.UsedPoints,
                            Type = "Refund",
                            Description = $"Hoàn {order.UsedPoints} điểm do hủy đơn hàng #{order.OrderId}",
                            OrderId = order.OrderId,
                            CreatedAt = DateTime.Now
                        };

                        _context.PointTransactions.Add(refundTransaction);
                    }
                }

                // ==========================================
                // 2. HOÀN VOUCHER
                // ==========================================

                if (order.UserVoucherId.HasValue &&
    order.UserVoucher != null &&
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
                // 3. HOÀN LẠI TỒN KHO
                // ==========================================

                var orderDetails = await _context.OrderDetails
        .Include(od => od.Product)
        .Where(od => od.OrderId == order.OrderId)
        .ToListAsync();

                foreach (var detail in orderDetails)
                {
                    if (detail.Product != null)
                    {
                        detail.Product.Quantity += detail.Quantity;
                    }
                }
                // ==========================================
                // 4. HỦY ĐƠN
                // ==========================================

                order.Status = "Đã hủy";

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                TempData["Success"] =
                    $"Đã hủy đơn hàng #{order.OrderId}. Điểm và voucher đã được hoàn lại.";

                return RedirectToAction(nameof(OrderList));
            }
            catch
            {
                await transaction.RollbackAsync();

                TempData["Error"] =
                    "Có lỗi xảy ra trong quá trình hủy đơn.";

                return RedirectToAction(nameof(OrderList));
            }
           
        }

        public IActionResult ProductSearch(string keyword)
        {

            var products = _context.Products
                .Include(x => x.Category)
                .Where(x => x.ProductName.Contains(keyword))
                .ToList();


            return View("Product/ProductList", products);

        }
        // =========================
        // VOUCHER
        // =========================

        // DANH SÁCH VOUCHER
        public async Task<IActionResult> VoucherList()
        {
            var vouchers = await _context.Vouchers
                .Include(v => v.UserVouchers)
                .OrderByDescending(v => v.VoucherId)
                .ToListAsync();

            return View("Voucher/VoucherList", vouchers);
        }


        // =========================
        // THÊM VOUCHER - GET
        // =========================
        [HttpGet]
        public IActionResult CreateVoucher()
        {
            return View("Voucher/CreateVoucher");
        }


        // =========================
        // THÊM VOUCHER - POST
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateVoucher(Voucher voucher)
        {
            if (!ModelState.IsValid)
            {
                return View("Voucher/CreateVoucher", voucher);
            }

            // Kiểm tra mã voucher đã tồn tại
            bool exists = await _context.Vouchers
                .AnyAsync(v => v.Code == voucher.Code);

            if (exists)
            {
                ModelState.AddModelError(
                    "Code",
                    "Mã voucher này đã tồn tại.");

                return View("Voucher/CreateVoucher", voucher);
            }

            _context.Vouchers.Add(voucher);

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Thêm voucher thành công.";

            return RedirectToAction(nameof(VoucherList));
        }


        // =========================
        // SỬA VOUCHER - GET
        // =========================
        [HttpGet]
        public async Task<IActionResult> EditVoucher(int id)
        {
            var voucher = await _context.Vouchers
                .FirstOrDefaultAsync(v => v.VoucherId == id);

            if (voucher == null)
                return NotFound();

            return View("Voucher/EditVoucher", voucher);
        }


        // =========================
        // SỬA VOUCHER - POST
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditVoucher(
            int id,
            Voucher voucher)
        {
            if (id != voucher.VoucherId)
                return NotFound();

            if (!ModelState.IsValid)
            {
                return View("Voucher/EditVoucher", voucher);
            }

            // Kiểm tra mã voucher trùng với voucher khác
            bool exists = await _context.Vouchers
                .AnyAsync(v =>
                    v.Code == voucher.Code &&
                    v.VoucherId != id);

            if (exists)
            {
                ModelState.AddModelError(
                    "Code",
                    "Mã voucher này đã tồn tại.");

                return View("Voucher/EditVoucher", voucher);
            }

            var existingVoucher = await _context.Vouchers
                .FirstOrDefaultAsync(v => v.VoucherId == id);

            if (existingVoucher == null)
                return NotFound();

            existingVoucher.Name =
                voucher.Name;

            existingVoucher.Code =
                voucher.Code;

            existingVoucher.DiscountAmount =
                voucher.DiscountAmount;

            existingVoucher.RequiredPoints =
                voucher.RequiredPoints;

            existingVoucher.MinimumOrderAmount =
                voucher.MinimumOrderAmount;

            existingVoucher.Quantity =
                voucher.Quantity;

            existingVoucher.ExpiryDate =
                voucher.ExpiryDate;

            existingVoucher.IsActive =
                voucher.IsActive;

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Cập nhật voucher thành công.";

            return RedirectToAction(nameof(VoucherList));
        }


        // =========================
        // XÓA VOUCHER
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteVoucher(int id)
        {
            var voucher = await _context.Vouchers
                .Include(v => v.UserVouchers)
                .FirstOrDefaultAsync(v => v.VoucherId == id);

            if (voucher == null)
            {
                TempData["Error"] =
                    "Voucher không tồn tại.";

                return RedirectToAction(nameof(VoucherList));
            }

            // Nếu đã có người nhận voucher
            // thì không cho xóa
            if (voucher.UserVouchers.Any())
            {
                TempData["Error"] =
                    "Voucher đã được người dùng nhận nên không thể xóa. Hãy tắt voucher thay vì xóa.";

                return RedirectToAction(nameof(VoucherList));
            }

            _context.Vouchers.Remove(voucher);

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Xóa voucher thành công.";

            return RedirectToAction(nameof(VoucherList));
        }


        // =========================
        // BẬT / TẮT VOUCHER
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleVoucher(int id)
        {
            var voucher = await _context.Vouchers
                .FirstOrDefaultAsync(v => v.VoucherId == id);

            if (voucher == null)
                return NotFound();

            voucher.IsActive =
                !voucher.IsActive;

            await _context.SaveChangesAsync();

            TempData["Success"] =
                voucher.IsActive
                    ? "Đã bật voucher."
                    : "Đã tắt voucher.";

            return RedirectToAction(nameof(VoucherList));
        }
        // =========================
        // RETURN REQUEST
        // =========================

        public async Task<IActionResult> ReturnRequestList()
        {
            var requests = await _context.ReturnRequests
                .Include(r => r.Order)
                .Include(r => r.User)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return View("Return/ReturnRequestList", requests);
        }
        public async Task<IActionResult> ReturnRequestDetail(int id)
        {
            var request = await _context.ReturnRequests
                .Include(r => r.Order)
                    .ThenInclude(o => o.OrderDetails)
                        .ThenInclude(od => od.Product)
                .Include(r => r.Order)
                    .ThenInclude(o => o.UserVoucher)
                        .ThenInclude(uv => uv.Voucher)
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.ReturnRequestId == id);

            if (request == null)
                return NotFound();

            return View("Return/ReturnRequestDetail", request);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveReturnRequest(int id)
        {
            var request = await _context.ReturnRequests
                .Include(r => r.Order)
                .FirstOrDefaultAsync(r => r.ReturnRequestId == id);

            if (request == null)
            {
                TempData["Error"] = "Không tìm thấy yêu cầu trả hàng.";
                return RedirectToAction(nameof(ReturnRequestList));
            }

            if (request.Status != "Chờ xử lý")
            {
                TempData["Error"] =
                    "Yêu cầu này không còn ở trạng thái chờ xử lý.";

                return RedirectToAction(nameof(ReturnRequestDetail),
                    new { id });
            }

            request.Status = "Đã duyệt";
            request.ApprovedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["Success"] =
                $"Đã duyệt yêu cầu trả hàng #{request.ReturnRequestId}.";

            return RedirectToAction(nameof(ReturnRequestDetail),
                new { id });
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectReturnRequest(
    int id,
    string adminNote)
        {
            var request = await _context.ReturnRequests
                .FirstOrDefaultAsync(r => r.ReturnRequestId == id);

            if (request == null)
            {
                TempData["Error"] =
                    "Không tìm thấy yêu cầu trả hàng.";

                return RedirectToAction(nameof(ReturnRequestList));
            }

            if (request.Status != "Chờ xử lý")
            {
                TempData["Error"] =
                    "Yêu cầu này không thể từ chối ở trạng thái hiện tại.";

                return RedirectToAction(nameof(ReturnRequestDetail),
                    new { id });
            }

            if (string.IsNullOrWhiteSpace(adminNote))
            {
                TempData["Error"] =
                    "Vui lòng nhập lý do từ chối.";

                return RedirectToAction(nameof(ReturnRequestDetail),
                    new { id });
            }

            request.Status = "Từ chối";
            request.AdminNote = adminNote.Trim();

            await _context.SaveChangesAsync();

            TempData["Success"] =
                $"Đã từ chối yêu cầu trả hàng #{request.ReturnRequestId}.";

            return RedirectToAction(nameof(ReturnRequestDetail),
                new { id });
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetReturnWaitingForProduct(int id)
        {
            var request = await _context.ReturnRequests
                .FirstOrDefaultAsync(r => r.ReturnRequestId == id);

            if (request == null)
            {
                TempData["Error"] =
                    "Không tìm thấy yêu cầu trả hàng.";

                return RedirectToAction(nameof(ReturnRequestList));
            }

            if (request.Status != "Đã duyệt")
            {
                TempData["Error"] =
                    "Yêu cầu chưa được duyệt.";

                return RedirectToAction(nameof(ReturnRequestDetail),
                    new { id });
            }

            request.Status = "Đang chờ nhận hàng";

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Đã chuyển yêu cầu sang trạng thái đang chờ nhận hàng.";

            return RedirectToAction(nameof(ReturnRequestDetail),
                new { id });
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetReturnReceived(int id)
        {
            var request = await _context.ReturnRequests
                .FirstOrDefaultAsync(r => r.ReturnRequestId == id);

            if (request == null)
            {
                TempData["Error"] =
                    "Không tìm thấy yêu cầu trả hàng.";

                return RedirectToAction(nameof(ReturnRequestList));
            }

            if (request.Status != "Đang chờ nhận hàng")
            {
                TempData["Error"] =
                    "Trạng thái yêu cầu không hợp lệ.";

                return RedirectToAction(nameof(ReturnRequestDetail),
                    new { id });
            }

            request.Status = "Đã nhận hàng";

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Đã xác nhận PetFeast nhận được sản phẩm.";

            return RedirectToAction(nameof(ReturnRequestDetail),
                new { id });
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetReturnInspecting(int id)
        {
            var request = await _context.ReturnRequests
                .FirstOrDefaultAsync(r => r.ReturnRequestId == id);

            if (request == null)
            {
                TempData["Error"] =
                    "Không tìm thấy yêu cầu trả hàng.";

                return RedirectToAction(nameof(ReturnRequestList));
            }

            if (request.Status != "Đã nhận hàng")
            {
                TempData["Error"] =
                    "Sản phẩm chưa được xác nhận đã nhận.";

                return RedirectToAction(nameof(ReturnRequestDetail),
                    new { id });
            }

            request.Status = "Đang kiểm tra";

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Đã chuyển sản phẩm sang trạng thái đang kiểm tra.";

            return RedirectToAction(nameof(ReturnRequestDetail),
                new { id });
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetReturnRefunded(int id)
        {
            // =========================================================
            // 1. LẤY YÊU CẦU TRẢ HÀNG
            // =========================================================

            var request = await _context.ReturnRequests
                .Include(r => r.Order)
                    .ThenInclude(o => o.OrderDetails)
                        .ThenInclude(od => od.Product)
                .Include(r => r.Order)
                    .ThenInclude(o => o.UserVoucher)
                        .ThenInclude(uv => uv.Voucher)
                .FirstOrDefaultAsync(r =>
                    r.ReturnRequestId == id);

            if (request == null)
            {
                TempData["Error"] =
                    "Không tìm thấy yêu cầu trả hàng.";

                return RedirectToAction(
                    nameof(ReturnRequestList));
            }

            // =========================================================
            // 2. KIỂM TRA TRẠNG THÁI
            // =========================================================

            if (request.Status != "Đang kiểm tra")
            {
                TempData["Error"] =
                    "Chỉ có thể hoàn tiền khi sản phẩm đang ở trạng thái Đang kiểm tra.";

                return RedirectToAction(
                    nameof(ReturnRequestDetail),
                    new { id });
            }

            // =========================================================
            // 3. KIỂM TRA ĐƠN HÀNG
            // =========================================================

            var order = request.Order;

            if (order == null)
            {
                TempData["Error"] =
                    "Không tìm thấy đơn hàng.";

                return RedirectToAction(
                    nameof(ReturnRequestDetail),
                    new { id });
            }

            // Đơn hàng phải là đơn đã hoàn thành
            if (order.Status != "Hoàn thành")
            {
                TempData["Error"] =
                    "Chỉ có thể hoàn tiền cho đơn hàng đã hoàn thành.";

                return RedirectToAction(
                    nameof(ReturnRequestDetail),
                    new { id });
            }

            // =========================================================
            // 4. TÌM USER
            // =========================================================

            ApplicationUser? user = null;

            if (!string.IsNullOrEmpty(order.UserId))
            {
                user = await _userManager.FindByIdAsync(
                    order.UserId);
            }

            // =========================================================
            // 5. TÍNH TIỀN HOÀN
            //
            // Không hoàn phí vận chuyển.
            //
            // Ví dụ:
            // Tổng đơn       = 300.000đ
            // Phí vận chuyển = 20.000đ
            // Hoàn lại       = 280.000đ
            // =========================================================

            decimal refundAmount =
                order.TotalAmount - order.ShippingFee;

            if (refundAmount < 0)
            {
                refundAmount = 0;
            }

            // =========================================================
            // 6. BẮT ĐẦU TRANSACTION
            // =========================================================

            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            try
            {
                // =====================================================
                // 7. HOÀN LẠI ĐIỂM ĐÃ SỬ DỤNG
                //
                // Ví dụ:
                // Khách dùng 10 điểm
                // → hoàn lại 10 điểm
                // =====================================================

                if (user != null &&
                    order.UsedPoints > 0)
                {
                    user.Points += order.UsedPoints;

                    var refundPointTransaction =
                        new PointTransaction
                        {
                            UserId = user.Id,

                            Points = order.UsedPoints,

                            Type = "Refund",

                            OrderId = order.OrderId,

                            Description =
                                $"Hoàn lại {order.UsedPoints} điểm " +
                                $"do trả hàng đơn #{order.OrderId}",

                            CreatedAt = DateTime.Now
                        };

                    _context.PointTransactions.Add(
                        refundPointTransaction);
                }

                // =====================================================
                // 8. THU HỒI 1 ĐIỂM ĐÃ TÍCH
                //
                // Khi đơn hoàn thành:
                // → +1 điểm
                //
                // Khi trả hàng:
                // → thu hồi lại 1 điểm
                // =====================================================

                if (user != null)
                {
                    await _pointsRepository
                        .RemovePointsForReturnedOrderAsync(order);
                }

                // =====================================================
                // 9. HOÀN LẠI VOUCHER
                // =====================================================

                if (order.UserVoucher != null &&
                    order.UserVoucher.IsUsed)
                {
                    order.UserVoucher.IsUsed = false;

                    order.UserVoucher.UsedDate = null;

                    // Voucher đã được lấy bằng điểm
                    // nên khi trả hàng sẽ đưa lại số lượng voucher
                    if (order.UserVoucher.Voucher != null)
                    {
                        order.UserVoucher.Voucher.Quantity++;
                    }
                }

                // =====================================================
                // 10. HOÀN LẠI TỒN KHO
                // =====================================================

                if (order.OrderDetails != null)
                {
                    foreach (var detail in order.OrderDetails)
                    {
                        if (detail.Product != null)
                        {
                            detail.Product.Quantity +=
                                detail.Quantity;
                        }
                    }
                }

                // =====================================================
                // 11. CẬP NHẬT THÔNG TIN HOÀN TRẢ
                // =====================================================

                request.RefundAmount = refundAmount;

                request.Status = "Đã hoàn tiền";

                request.CompletedAt = DateTime.Now;

                // Đánh dấu đơn hàng đã hoàn tiền
                order.PaymentStatus = "Đã hoàn tiền";

                // =====================================================
                // 12. LƯU TẤT CẢ
                // =====================================================

                await _context.SaveChangesAsync();

                // =====================================================
                // 13. COMMIT
                // =====================================================

                await transaction.CommitAsync();

                TempData["Success"] =
                    $"Đã hoàn tiền {refundAmount:N0}đ. " +
                    "Điểm, voucher và tồn kho đã được cập nhật.";

                return RedirectToAction(
                    nameof(ReturnRequestDetail),
                    new { id });
            }
            catch
            {
                // =====================================================
                // CÓ LỖI → ROLLBACK TOÀN BỘ
                // =====================================================

                await transaction.RollbackAsync();

                TempData["Error"] =
                    "Có lỗi xảy ra trong quá trình hoàn tiền.";

                return RedirectToAction(
                    nameof(ReturnRequestDetail),
                    new { id });
            }
            
        }
        [HttpGet]
        public IActionResult GetAdminNotificationCount()
        {
            var newOrderCount = _context.Orders
                .Count(o => o.Status == "Chờ xác nhận");

            var pendingReturnCount = _context.ReturnRequests
                .Count(r => r.Status == "Chờ xử lý");

            return Json(new
            {
                newOrderCount,
                pendingReturnCount
            });
        }
    }
}