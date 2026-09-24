using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PetFeast.Data;
using PetFeast.Models.Interfaces;
using PetFeast.Models.Products;
using PetFeast.Models.Services;
using PetFeast.Models.ViewModels;
using System.Security.Claims;

namespace PetFeast.Controllers
{
    public class ProductController : Controller
    {
        private readonly IProductRepository _productRepo;
        private readonly CategoryIRepository _categoryRepo;
        private readonly PetFeastDBContext _context;
        private readonly IShoppingCartRepository _cartRepo;

        public ProductController(
    IProductRepository productRepo,
    CategoryIRepository categoryRepo,
    PetFeastDBContext context,
    IShoppingCartRepository cartRepo)
        {
            _productRepo = productRepo;
            _categoryRepo = categoryRepo;
            _context = context;
            _cartRepo = cartRepo;
        }

        public IActionResult Index(
    string? keyword,
    int? categoryId,
    decimal? minPrice,
    decimal? maxPrice,
    double? minRating,
    string? sortOrder,
    int page = 1)
        {
            const int pageSize = 9;
            var products = _productRepo.GetAll();

            // Lấy giá bán cao nhất để làm giới hạn bộ lọc
            decimal maxProductPrice = products.Any()
                ? products.Max(p =>
                    p.DiscountPercent > 0
                        ? p.Price * (1m - p.DiscountPercent / 100m)
                        : p.Price)
                : 0;
            // =========================
            // LẤY SẢN PHẨM YÊU THÍCH
            // =========================

            var favoriteProductIds = new HashSet<int>();

            if (User.Identity?.IsAuthenticated == true)
            {
                string? userId =
                    User.FindFirstValue(ClaimTypes.NameIdentifier);

                if (!string.IsNullOrEmpty(userId))
                {
                    favoriteProductIds = _context.Favorites
                        .Where(f => f.UserId == userId)
                        .Select(f => f.ProductId)
                        .ToHashSet();
                }
            }
            // Tìm kiếm
            if (!string.IsNullOrEmpty(keyword))
            {
                products = products.Where(p =>
                    p.ProductName.Contains(
                        keyword,
                        StringComparison.OrdinalIgnoreCase));
            }

            // Danh mục
            if (categoryId.HasValue)
            {
                products = products.Where(p =>
                    p.CategoryId == categoryId.Value);
            }

            // =========================
            // LỌC THEO GIÁ SAU GIẢM
            // =========================

            if (minPrice.HasValue && minPrice > 0)
            {
                products = products.Where(p =>
                    (p.DiscountPercent > 0
                        ? p.Price * (1m - p.DiscountPercent / 100m)
                        : p.Price) >= minPrice.Value);
            }

            if (maxPrice.HasValue && maxPrice > 0)
            {
                products = products.Where(p =>
                    (p.DiscountPercent > 0
                        ? p.Price * (1m - p.DiscountPercent / 100m)
                        : p.Price) <= maxPrice.Value);
            }

            // =========================
            // LỌC THEO ĐÁNH GIÁ
            // =========================

            if (minRating.HasValue)
            {
                var ratingProductIds = _context.ProductReviews
                    .Where(r => !r.IsDeleted)
                    .GroupBy(r => r.ProductId)
                    .Where(g => g.Average(r => r.Rating) >= minRating.Value)
                    .Select(g => g.Key);

                products = products.Where(p =>
                    ratingProductIds.Contains(p.ProductId));
            }
            // =========================
            // SẮP XẾP THEO GIÁ SAU GIẢM
            // =========================

            switch (sortOrder)
            {
                case "price_asc":

                    products = products.OrderBy(p =>
                        p.DiscountPercent > 0
                            ? p.Price * (1m - p.DiscountPercent / 100m)
                            : p.Price);

                    break;

                case "price_desc":

                    products = products.OrderByDescending(p =>
                        p.DiscountPercent > 0
                            ? p.Price * (1m - p.DiscountPercent / 100m)
                            : p.Price);

                    break;
            }

            // Tổng số sản phẩm sau khi lọc
            int totalProducts = products.Count();

            // Tổng số trang
            int totalPages =
                (int)Math.Ceiling(
                    totalProducts / (double)pageSize);

            // Đảm bảo page hợp lệ
            if (page < 1)
                page = 1;

            if (totalPages > 0 && page > totalPages)
                page = totalPages;

            // Lấy 9 sản phẩm của trang hiện tại
            products = products
                .Skip((page - 1) * pageSize)
                .Take(pageSize);
            // =========================
            // LẤY ĐÁNH GIÁ CHO PRODUCT CARD
            // =========================

            var productList = products.ToList();

            var productIds = productList
                .Select(p => p.ProductId)
                .ToList();

            var productRatings = _context.ProductReviews
                .Where(r =>
                    productIds.Contains(r.ProductId) &&
                    !r.IsDeleted)
                .GroupBy(r => r.ProductId)
                .Select(g => new
                {
                    ProductId = g.Key,
                    AverageRating = g.Average(r => r.Rating),
                    ReviewCount = g.Count()
                })
                .ToDictionary(
                    x => x.ProductId,
                    x => new ProductRatingViewModel
                    {
                        AverageRating = x.AverageRating,
                        ReviewCount = x.ReviewCount
                    });


            // ViewBag
            ViewBag.ProductRatings = productRatings;
            ViewBag.Keyword = keyword;
            ViewBag.Categories = _categoryRepo.GetAll();

            ViewBag.MinPrice = minPrice;
            ViewBag.MaxPrice = maxPrice;
            ViewBag.MinRating = minRating;
            ViewBag.MaxProductPrice = maxProductPrice;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.FavoriteProductIds = favoriteProductIds;
            return View(productList);
        }

        public async Task<IActionResult> Detail(int id)
        {
            var product = _productRepo.GetById(id);

            if (product == null)
            {
                return NotFound();
            }

            // =========================
            // KIỂM TRA SẢN PHẨM YÊU THÍCH
            // =========================

            bool isFavorite = false;

            if (User.Identity != null &&
                User.Identity.IsAuthenticated)
            {
                string? userId =
                    User.FindFirstValue(ClaimTypes.NameIdentifier);

                if (!string.IsNullOrEmpty(userId))
                {
                    isFavorite = _context.Favorites.Any(f =>
                        f.UserId == userId &&
                        f.ProductId == id);
                }
            }

            // =========================
            // LẤY SỐ LƯỢNG ĐÃ CÓ TRONG GIỎ
            // =========================

            int currentCartQuantity = 0;

            if (User.Identity?.IsAuthenticated == true)
            {
                var cart = _cartRepo.GetCart();

                var cartItem = cart.FirstOrDefault(
                    x => x.ProductId == id);

                if (cartItem != null)
                {
                    currentCartQuantity = cartItem.Quantity;
                }
            }

            // =========================
            // TÍNH SỐ LƯỢNG CÓ THỂ THÊM
            // =========================

            int maxAddQuantity =
                product.Quantity - currentCartQuantity;

            if (maxAddQuantity < 0)
            {
                maxAddQuantity = 0;
            }

            ViewBag.IsFavorite = isFavorite;

            ViewBag.CurrentCartQuantity =
                currentCartQuantity;

            ViewBag.MaxAddQuantity =
                maxAddQuantity;

            // =========================
            // SẢN PHẨM LIÊN QUAN
            // =========================

            ViewBag.RelatedProducts =
                _productRepo.GetAll()
                    .Where(x =>
                        x.CategoryId == product.CategoryId &&
                        x.ProductId != product.ProductId)
                    .Take(4)
                    .ToList();

            // =========================
            // LẤY ĐÁNH GIÁ SẢN PHẨM
            // =========================

            var reviews = await _context.ProductReviews
    .Include(r => r.User)
    .Where(r =>
        r.ProductId == id &&
        !r.IsDeleted)
    .OrderByDescending(r => r.CreatedAt)
    .ToListAsync();

            ViewBag.ProductReviews = reviews;

            // =========================
            // THỐNG KÊ ĐÁNH GIÁ
            // =========================

            ViewBag.ReviewCount = reviews.Count;

            ViewBag.AverageRating = reviews.Any()
                ? reviews.Average(r => r.Rating)
                : 0;

            return View(product);
        }

        [HttpPost]
        public IActionResult Create(Product product)
        {
            if (ModelState.IsValid)
            {
                _productRepo.Add(product);
                _productRepo.Save();

                return RedirectToAction(nameof(Index));
            }

            return View(product);
        }
    }
}
