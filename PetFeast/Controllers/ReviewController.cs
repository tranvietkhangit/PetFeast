using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PetFeast.Data;
using PetFeast.Models.Identity;
using PetFeast.Models.Products;
using PetFeast.Models.Reviews;

namespace PetFeast.Controllers
{
    [Authorize]
    public class ReviewController : Controller
    {
        private readonly PetFeastDBContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ReviewController(
            PetFeastDBContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // =========================================================
        // HIỂN THỊ FORM ĐÁNH GIÁ
        // /Review/Create?orderDetailId=5
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Create(int orderDetailId)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return Challenge();
            }

            // -----------------------------------------------------
            // Lấy OrderDetail
            // Đồng thời lấy Order và Product
            // -----------------------------------------------------

            var orderDetail = await _context.OrderDetails
    .Include(od => od.Order)
        .ThenInclude(o => o.ReturnRequest)
    .Include(od => od.Product)
    .FirstOrDefaultAsync(od => od.OrderDetailId == orderDetailId);

            if (orderDetail == null)
            {
                return NotFound();
            }

            // -----------------------------------------------------
            // Kiểm tra Order có tồn tại không
            // -----------------------------------------------------

            if (orderDetail.Order == null)
            {
                return NotFound();
            }

            // -----------------------------------------------------
            // Kiểm tra đơn hàng có thuộc user hiện tại không
            // -----------------------------------------------------

            if (orderDetail.Order.UserId != user.Id)
            {
                return Forbid();
            }

            // -----------------------------------------------------
            // Chỉ đơn hàng "Hoàn thành" mới được đánh giá
            // -----------------------------------------------------
            if (orderDetail.Order.Status != "Hoàn thành")
            {
                TempData["ReviewError"] =
                    "Bạn chỉ có thể đánh giá sản phẩm sau khi đơn hàng hoàn thành.";

                return RedirectToAction(
                    nameof(OrderController.UserOrderDetail),
                    "Order",
                    new { id = orderDetail.Order.OrderId });
            }
            if (orderDetail.Order.ReturnRequest?.Status == "Đã hoàn tiền")
            {
                TempData["ReviewError"] =
                    "Bạn không thể đánh giá sản phẩm của đơn hàng đã hoàn trả.";

                return RedirectToAction(
                    nameof(OrderController.UserOrderDetail),
                    "Order",
                    new { id = orderDetail.Order.OrderId });
            }

            // -----------------------------------------------------
            // Kiểm tra đã đánh giá chưa
            // -----------------------------------------------------

            var existingReview =
                await _context.ProductReviews
                    .FirstOrDefaultAsync(r =>
                        r.OrderDetailId == orderDetailId);

            if (existingReview != null)
            {
                TempData["ReviewError"] =
                    "Bạn đã đánh giá sản phẩm này rồi.";

                return RedirectToAction(
                    nameof(OrderController.UserOrderDetail),
                    "Order",
                    new { id = orderDetail.Order.OrderId });
            }

            // -----------------------------------------------------
            // Tạo model cho form
            // -----------------------------------------------------
            var review = new ProductReview
            {
                OrderDetailId = orderDetail.OrderDetailId,
                ProductId = orderDetail.ProductId,
                UserId = user.Id,
                Rating = 5,
                Product = orderDetail.Product,
                OrderDetail = orderDetail
            };

            return View(review);
        }


        // =========================================================
        // XỬ LÝ GỬI ĐÁNH GIÁ
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProductReview review)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return Challenge();

            // =========================================================
            // LẤY ORDER DETAIL
            // =========================================================

            var orderDetail = await _context.OrderDetails
    .Include(od => od.Order)
        .ThenInclude(o => o.ReturnRequest)
    .Include(od => od.Product)
    .FirstOrDefaultAsync(
        od => od.OrderDetailId == review.OrderDetailId);

            if (orderDetail == null)
                return NotFound();

            if (orderDetail.Order == null)
                return NotFound();

            // =========================================================
            // KIỂM TRA QUYỀN SỞ HỮU ĐƠN HÀNG
            // =========================================================

            if (orderDetail.Order.UserId != user.Id)
                return Forbid();

            // =========================================================
            // CHỈ ĐƯỢC ĐÁNH GIÁ ĐƠN ĐÃ HOÀN THÀNH
            // =========================================================

            if (orderDetail.Order.Status != "Hoàn thành")
            {
                TempData["ReviewError"] =
                    "Bạn chỉ có thể đánh giá sản phẩm sau khi đơn hàng hoàn thành.";

                return RedirectToAction(
                    nameof(OrderController.UserOrderDetail),
                    "Order",
                    new
                    {
                        id = orderDetail.Order.OrderId
                    });
            }
            if (orderDetail.Order.ReturnRequest?.Status == "Đã hoàn tiền")
            {
                TempData["ReviewError"] =
                    "Bạn không thể đánh giá sản phẩm của đơn hàng đã hoàn trả.";

                return RedirectToAction(
                    nameof(OrderController.UserOrderDetail),
                    "Order",
                    new { id = orderDetail.Order.OrderId });
            }

            // =========================================================
            // KIỂM TRA ĐÃ ĐÁNH GIÁ CHƯA
            // =========================================================

            var existingReview = await _context.ProductReviews
                .AnyAsync(r =>
                    r.OrderDetailId == review.OrderDetailId);

            if (existingReview)
            {
                TempData["ReviewError"] =
                    "Bạn đã đánh giá sản phẩm này rồi.";

                return RedirectToAction(
                    nameof(OrderController.UserOrderDetail),
                    "Order",
                    new
                    {
                        id = orderDetail.Order.OrderId
                    });
            }

            // =========================================================
            // GÁN DỮ LIỆU TỪ DATABASE
            // =========================================================

            review.ProductId = orderDetail.ProductId;
            review.UserId = user.Id;
            review.CreatedAt = DateTime.Now;

            // =========================================================
            // BỎ VALIDATION CHO CÁC PROPERTY
            // KHÔNG ĐƯỢC GỬI TỪ FORM
            // =========================================================

            ModelState.Remove(nameof(ProductReview.UserId));
            ModelState.Remove(nameof(ProductReview.Product));
            ModelState.Remove(nameof(ProductReview.User));
            ModelState.Remove(nameof(ProductReview.OrderDetail));

            // =========================================================
            // KIỂM TRA MODEL
            // =========================================================

            if (!ModelState.IsValid)
            {
                review.Product = orderDetail.Product;
                review.OrderDetail = orderDetail;

                return View(review);
            }

            // =========================================================
            // LƯU DATABASE
            // =========================================================

            _context.ProductReviews.Add(review);

            await _context.SaveChangesAsync();

            // =========================================================
            // THÔNG BÁO THÀNH CÔNG
            // =========================================================

            TempData["ReviewSuccess"] =
                "Đánh giá sản phẩm thành công!";

            // =========================================================
            // QUAY VỀ CHI TIẾT ĐƠN HÀNG
            // =========================================================

            return RedirectToAction(
                nameof(OrderController.UserOrderDetail),
                "Order",
                new
                {
                    id = orderDetail.Order.OrderId
                });
        }
        [HttpGet]
        public async Task<IActionResult> MyReviews()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return Challenge();

            var reviews = await _context.ProductReviews
                .Include(r => r.Product)
                .Include(r => r.OrderDetail)
                    .ThenInclude(od => od.Order)
                .Where(r => r.UserId == user.Id)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return View(reviews);
        }
        // =========================
        // SỬA ĐÁNH GIÁ - GET
        // =========================
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return Challenge();

            var review = await _context.ProductReviews
    .Include(r => r.Product)
    .Include(r => r.OrderDetail)
        .ThenInclude(od => od.Order)
    .FirstOrDefaultAsync(r =>
        r.ProductReviewId == id &&
        r.UserId == user.Id);

            if (review == null)
                return NotFound();

            if (review.IsDeleted)
            {
                TempData["ReviewError"] =
                    "Đánh giá này đã bị xóa do vi phạm quy định cộng đồng.";

                return RedirectToAction(nameof(MyReviews));
            }

            return View(review);
        }


        // =========================
        // SỬA ĐÁNH GIÁ - POST
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
    int id,
    ProductReview model)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return Challenge();

            // Tìm review thuộc về user hiện tại
            var review = await _context.ProductReviews
                .Include(r => r.Product)
                .Include(r => r.OrderDetail)
                    .ThenInclude(od => od.Order)
                .FirstOrDefaultAsync(r =>
                    r.ProductReviewId == id &&
                    r.UserId == user.Id);

            if (review == null)
                return NotFound();

            if (review.IsDeleted)
            {
                TempData["ReviewError"] =
                    "Đánh giá này đã bị xóa do vi phạm quy định cộng đồng.";

                return RedirectToAction(nameof(MyReviews));
            }

            // Các thuộc tính này không được nhập từ form
            ModelState.Remove(nameof(ProductReview.UserId));
            ModelState.Remove(nameof(ProductReview.ProductId));
            ModelState.Remove(nameof(ProductReview.Product));
            ModelState.Remove(nameof(ProductReview.User));
            ModelState.Remove(nameof(ProductReview.OrderDetail));
            ModelState.Remove(nameof(ProductReview.CreatedAt));

            if (!ModelState.IsValid)
            {
                model.Product = review.Product;
                model.OrderDetail = review.OrderDetail;

                return View(model);
            }

            // Chỉ cập nhật những gì người dùng được phép sửa
            review.Rating = model.Rating;
            review.Comment = model.Comment;

            await _context.SaveChangesAsync();

            TempData["ReviewSuccess"] =
                "Cập nhật đánh giá thành công!";

            return RedirectToAction(nameof(MyReviews));
        }
        // =========================
        // XÓA ĐÁNH GIÁ - GET
        // =========================
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return Challenge();

            var review = await _context.ProductReviews
                .Include(r => r.Product)
                .FirstOrDefaultAsync(r =>
                    r.ProductReviewId == id &&
                    r.UserId == user.Id);

            if (review == null)
                return NotFound();
            if (review.IsDeleted)
            {
                TempData["ReviewError"] =
                    "Đánh giá này đã bị xóa do vi phạm quy định cộng đồng.";

                return RedirectToAction(nameof(MyReviews));
            }
            return View(review);
        }


        // =========================
        // XÓA ĐÁNH GIÁ - POST
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return Challenge();

            var review = await _context.ProductReviews
                .FirstOrDefaultAsync(r =>
                    r.ProductReviewId == id &&
                    r.UserId == user.Id);

            if (review == null)
                return NotFound();
            if (review.IsDeleted)
            {
                TempData["ReviewError"] =
                    "Đánh giá này đã bị xóa do vi phạm quy định cộng đồng.";

                return RedirectToAction(nameof(MyReviews));
            }
            _context.ProductReviews.Remove(review);

            await _context.SaveChangesAsync();

            TempData["ReviewSuccess"] =
                "Đã xóa đánh giá thành công!";

            return RedirectToAction(nameof(MyReviews));
        }
        [HttpGet]
        public async Task<IActionResult> Report(int id)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return Challenge();

            var review = await _context.ProductReviews
                .Include(r => r.Product)
                .Include(r => r.User)
                .FirstOrDefaultAsync(r =>
                    r.ProductReviewId == id);

            if (review == null)
                return NotFound();

            // Không cho người dùng tự tố cáo review của chính mình
            if (review.UserId == user.Id)
            {
                TempData["ReviewError"] =
                    "Bạn không thể tố cáo đánh giá của chính mình.";

                return RedirectToAction(
                    "Detail",
                    "Product",
                    new { id = review.ProductId });
            }

            // Kiểm tra đã tố cáo review này chưa
            var existingReport = await _context.ReviewReports
                .FirstOrDefaultAsync(r =>
                    r.ProductReviewId == id &&
                    r.ReporterUserId == user.Id &&
                    r.Status == "Chờ xử lý");

            if (existingReport != null)
            {
                TempData["ReviewError"] =
                    "Bạn đã tố cáo đánh giá này rồi.";

                return RedirectToAction(
                    "Detail",
                    "Product",
                    new { id = review.ProductId });
            }

            var report = new ReviewReport
            {
                ProductReviewId = review.ProductReviewId,
                ReporterUserId = user.Id,
                ProductReview = review
            };

            return View(report);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Report(
    int id,
    ReviewReport model)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return Challenge();

            var review = await _context.ProductReviews
                .Include(r => r.Product)
                .FirstOrDefaultAsync(r =>
                    r.ProductReviewId == id);

            if (review == null)
                return NotFound();
            if (review.IsDeleted)
            {
                TempData["ReviewError"] =
                    "Đánh giá này không còn tồn tại.";

                return RedirectToAction(
                    nameof(ProductController.Detail),
                    "Product",
                    new { id = review.ProductId });
            }
            // Không cho tự tố cáo đánh giá của mình
            if (review.UserId == user.Id)
            {
                return Forbid();
            }

            // Không cho tố cáo trùng khi đang chờ xử lý
            var existingReport = await _context.ReviewReports
                .FirstOrDefaultAsync(r =>
                    r.ProductReviewId == id &&
                    r.ReporterUserId == user.Id &&
                    r.Status == "Chờ xử lý");

            if (existingReport != null)
            {
                TempData["ReviewError"] =
                    "Bạn đã tố cáo đánh giá này rồi.";

                return RedirectToAction(
                    "Detail",
                    "Product",
                    new { id = review.ProductId });
            }

            // Bỏ validation cho các navigation property
            ModelState.Remove(nameof(ReviewReport.ProductReview));
            ModelState.Remove(nameof(ReviewReport.ReporterUser));
            ModelState.Remove(nameof(ReviewReport.ReporterUserId));
            ModelState.Remove(nameof(ReviewReport.ProductReviewId));
            ModelState.Remove(nameof(ReviewReport.Status));
            ModelState.Remove(nameof(ReviewReport.CreatedAt));

            if (!ModelState.IsValid)
            {
                model.ProductReview = review;
                return View(model);
            }

            var report = new ReviewReport
            {
                ProductReviewId = review.ProductReviewId,
                ReporterUserId = user.Id,
                Reason = model.Reason,
                Description = model.Description,
                Status = "Chờ xử lý",
                CreatedAt = DateTime.Now
            };

            _context.ReviewReports.Add(report);

            await _context.SaveChangesAsync();

            TempData["ReportSuccess"] =
     "Đã gửi tố cáo. Cảm ơn bạn đã giúp PetFeast kiểm duyệt đánh giá.";

            return RedirectToAction(
                nameof(ProductController.Detail),
                "Product",
                new { id = review.ProductId });
        }
    }
}