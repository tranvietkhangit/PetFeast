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

        public IActionResult Dashboard()
        {
            var totalOrders = _context.Orders.Count();
            var completedOrders = _context.Orders.Count(o => o.Status == "Hoàn thành");
            var completionRate = totalOrders > 0 ? (int)Math.Round((double)completedOrders / totalOrders * 100) : 0;

            ViewBag.TotalProducts = _context.Products.Count();
            ViewBag.TotalCategories = _context.Categories.Count();
            ViewBag.TotalOrders = totalOrders;
            ViewBag.CompletedOrders = completedOrders;
            ViewBag.CompletionRate = completionRate;

            // Tổng doanh thu (không tính đơn hủy)
            ViewBag.TotalRevenue = _context.Orders
                .Where(o => o.Status != "Đã hủy")
                .Sum(x => (decimal?)x.TotalAmount) ?? 0;

            ViewBag.BestProducts = _productRepo.GetBestSellingProducts(5);

            // 5 đơn hàng mới đặt gần đây nhất
            ViewBag.RecentOrders = _context.Orders
                .OrderByDescending(o => o.OrderDate)
                .Take(5)
                .ToList();

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetDashboardRealtimeData()
        {
            var currentYear = DateTime.Now.Year;
            var currentMonth = DateTime.Now.Month;

            var totalProducts = await _context.Products.CountAsync();
            var totalCategories = await _context.Categories.CountAsync();
            var totalOrders = await _context.Orders.CountAsync();
            var completedOrders = await _context.Orders.CountAsync(o => o.Status == "Hoàn thành");
            var completionRate = totalOrders > 0 ? (int)Math.Round((double)completedOrders / totalOrders * 100) : 0;

            // Doanh thu tháng hiện tại
            var monthRevenue = await _context.Orders
                .Where(o => o.OrderDate.Month == currentMonth && o.OrderDate.Year == currentYear && o.Status != "Đã hủy")
                .SumAsync(o => (decimal?)o.TotalAmount) ?? 0;

            // Tổng doanh thu toàn bộ
            var totalRevenue = await _context.Orders
                .Where(o => o.Status != "Đã hủy")
                .SumAsync(o => (decimal?)o.TotalAmount) ?? 0;

            // Biểu đồ doanh thu 9 tháng năm nay vs năm trước
            var revCurrentYear = new decimal[9];
            var revPrevYear = new decimal[9];

            for (int m = 1; m <= 9; m++)
            {
                revCurrentYear[m - 1] = await _context.Orders
                    .Where(o => o.OrderDate.Year == currentYear && o.OrderDate.Month == m && o.Status != "Đã hủy")
                    .SumAsync(o => (decimal?)o.TotalAmount) ?? 0;

                revPrevYear[m - 1] = await _context.Orders
                    .Where(o => o.OrderDate.Year == (currentYear - 1) && o.OrderDate.Month == m && o.Status != "Đã hủy")
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
                totalRevenue = monthRevenue > 0 ? monthRevenue : totalRevenue,
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
                .Where(o => o.Status != "Đã hủy" && o.OrderDate >= start && o.OrderDate <= end)
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
                .GroupBy(od => od.Product?.Category?.CategoryName ?? "Khác")
                .Select(g => new {
                    CategoryName = g.Key,
                    Revenue = g.Sum(x => x.Quantity * (x.Product != null ? x.Product.Price : 0))
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
                .Include(x => x.OrderDetails)
                .ThenInclude(x => x.Product)
                .OrderByDescending(x => x.OrderDate)
                .ToList();

            return View("Order/OrderList", orders);
        }
        public IActionResult OrderDetail(int id)
        {
            var order = _context.Orders
                .Include(x => x.OrderDetails)
                .ThenInclude(x => x.Product)
                .FirstOrDefault(x => x.OrderId == id);

            if (order == null)
                return NotFound();

            return View("Order/OrderDetail", order);
        }
        public IActionResult ConfirmOrder(int id)
        {
            var order = _context.Orders.Find(id);

            if (order != null)
            {
                order.Status = "Đang giao";

                _context.SaveChanges();
            }
            return RedirectToAction(nameof(OrderList));
        }
        public async Task<IActionResult> CompleteOrder(int id)
        {
            var order = await _context.Orders
        .Include(x => x.OrderDetails)
        .FirstOrDefaultAsync(x => x.OrderId == id);

            if (order == null)
                return NotFound();

            // Chỉ xử lý nếu đơn chưa hoàn thành
            if (order.Status != "Hoàn thành")
            {
                order.Status = "Hoàn thành";

                await _context.SaveChangesAsync();

                // Tích điểm theo số lượng sản phẩm
                int points = await _pointsRepository
                    .AddPointsForOrderAsync(order);

                TempData["Success"] =
                    $"Đơn hàng #{order.OrderId} đã hoàn thành. Khách hàng nhận được {points} điểm.";
            }

            return RedirectToAction(nameof(OrderList));
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelOrder(int id)
        {
            var order = await _context.Orders
                .Include(o => o.UserVoucher)
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

            if (order.UserVoucherId.HasValue && order.UserVoucher != null)
            {
                order.UserVoucher.IsUsed = false;
                order.UserVoucher.UsedDate = null;
            }

            // ==========================================
            // 3. HỦY ĐƠN
            // ==========================================

            order.Status = "Đã hủy";

            await _context.SaveChangesAsync();

            TempData["Success"] =
                $"Đã hủy đơn hàng #{order.OrderId}. Điểm và voucher đã được hoàn lại.";

            return RedirectToAction(nameof(OrderList));
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
    }
}