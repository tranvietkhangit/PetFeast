using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PetFeast.Data;
using PetFeast.Models;
using PetFeast.Models.Interfaces;
using System.Diagnostics;
using System.Security.Claims;
namespace PetFeast.Controllers
{
    public class HomeController : Controller
    {
        private readonly IProductRepository _productRepository;
        private readonly PetFeastDBContext _context;
        public HomeController(IProductRepository productRepository, PetFeastDBContext context)
        {
            _productRepository = productRepository;
            _context = context;
        }

        public IActionResult Index()
        {
            var products = _productRepository.GetAll();

            ViewBag.FeaturedProducts =
                _productRepository.GetBestSellingProducts(8);

            // =========================
            // S?N PH?M YÊU THÍCH
            // =========================

            var favoriteProductIds = new HashSet<int>();

            if (User.Identity?.IsAuthenticated == true)
            {
                string? userId =
                    User.FindFirstValue(
                        ClaimTypes.NameIdentifier);

                if (!string.IsNullOrEmpty(userId))
                {
                    favoriteProductIds = _context.Favorites
                        .Where(f => f.UserId == userId)
                        .Select(f => f.ProductId)
                        .ToHashSet();
                }
            }

            ViewBag.FavoriteProductIds = favoriteProductIds;

            return View(products);
        }
        public IActionResult Discount(
    int page = 1,
    int? minPrice = null,
    int? maxPrice = null,
    int? minDiscount = null,
    string? sortOrder = null)
        {
            const int pageSize = 9;

            // Lấy các sản phẩm đang giảm giá
            var products = _productRepository
                .GetAll()
                .Where(x => x.DiscountPercent > 0)
                .ToList();


            // ==========================================
            // LỌC % GIẢM GIÁ
            // ==========================================

            if (minDiscount.HasValue)
            {
                products = products
                    .Where(x => x.DiscountPercent == minDiscount.Value)
                    .ToList();
            }


            // ==========================================
            // LỌC GIÁ SAU KHI GIẢM - TỪ
            // ==========================================

            if (minPrice.HasValue)
            {
                products = products
                    .Where(x => x.DiscountPrice >= minPrice.Value)
                    .ToList();
            }


            // ==========================================
            // LỌC GIÁ SAU KHI GIẢM - ĐẾN
            // ==========================================

            if (maxPrice.HasValue)
            {
                products = products
                    .Where(x => x.DiscountPrice <= maxPrice.Value)
                    .ToList();
            }


            // ==========================================
            // SẮP XẾP
            // ==========================================

            products = sortOrder switch
            {
                "discount_desc" =>
                    products
                        .OrderByDescending(x => x.DiscountPercent)
                        .ToList(),

                "price_asc" =>
                    products
                        .OrderBy(x => x.DiscountPrice)
                        .ToList(),

                "price_desc" =>
                    products
                        .OrderByDescending(x => x.DiscountPrice)
                        .ToList(),

                _ =>
                    products
                        .OrderByDescending(x => x.DiscountPercent)
                        .ToList()
            };


            // ==========================================
            // PHÂN TRANG
            // ==========================================

            int totalProducts = products.Count;

            int totalPages =
                (int)Math.Ceiling(
                    totalProducts / (double)pageSize
                );

            if (page < 1)
                page = 1;

            if (totalPages > 0 && page > totalPages)
                page = totalPages;


            var pagedProducts = products
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();


            // ==========================================
            // FAVORITE
            // ==========================================

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

            ViewBag.FavoriteProductIds = favoriteProductIds;


            // ==========================================
            // GIÁ CAO NHẤT
            // ==========================================

            var discountedProducts = _productRepository
    .GetAll()
    .Where(x => x.DiscountPercent > 0)
    .ToList();

            decimal highestDiscountPrice = discountedProducts.Any()
                ? discountedProducts.Max(x => x.DiscountPrice)
                : 0;

            ViewBag.MaxDiscountPrice = (int)Math.Ceiling(highestDiscountPrice);


            // ==========================================
            // VIEWBAG
            // ==========================================

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;

            ViewBag.MinDiscount = minDiscount;

            ViewBag.MinPrice =
                minPrice ?? 0;

            ViewBag.MaxPrice =
                maxPrice ?? (int)highestDiscountPrice;

            ViewBag.SortOrder =
                sortOrder;

            ViewBag.MaxDiscountPrice =
                (int)Math.Ceiling(highestDiscountPrice);


            return View(pagedProducts);
        }
        public IActionResult AboutUs()
        {
            return View();
        }
        public IActionResult Contact()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
        [Authorize]
        public async Task<IActionResult> UserPoint()
        {
            var userId =
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var transactions = await _context.PointTransactions
                .Where(x => x.UserId == userId)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();

            return View(transactions);
        }
    }
}
