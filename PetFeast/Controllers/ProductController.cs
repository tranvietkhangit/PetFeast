using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PetFeast.Data;
using PetFeast.Models.Interfaces;
using PetFeast.Models.Products;
using PetFeast.Models.Services;
using System.Security.Claims;

namespace PetFeast.Controllers
{
    public class ProductController : Controller
    {
        private readonly IProductRepository _productRepo;
        private readonly CategoryIRepository _categoryRepo;
        private readonly PetFeastDBContext _context;

        public ProductController(
            IProductRepository productRepo,
            CategoryIRepository categoryRepo,
            PetFeastDBContext context)
        {
            _productRepo = productRepo;
            _categoryRepo = categoryRepo;
            _context = context;
        }

        public IActionResult Index( string? keyword, int? categoryId, decimal? minPrice, decimal? maxPrice, string? sortOrder, int page = 1)
        {
            const int pageSize = 9;

            var products = _productRepo.GetAll();
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

            // Giá tối thiểu
            if (minPrice.HasValue && minPrice > 0)
            {
                products = products.Where(p =>
                    p.Price >= minPrice.Value);
            }

            // Giá tối đa
            if (maxPrice.HasValue && maxPrice > 0)
            {
                products = products.Where(p =>
                    p.Price <= maxPrice.Value);
            }

            // Sắp xếp
            switch (sortOrder)
            {
                case "price_asc":
                    products = products.OrderBy(p => p.Price);
                    break;

                case "price_desc":
                    products = products.OrderByDescending(p => p.Price);
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

            // ViewBag
            ViewBag.Keyword = keyword;
            ViewBag.Categories = _categoryRepo.GetAll();

            ViewBag.MinPrice = minPrice;
            ViewBag.MaxPrice = maxPrice;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.FavoriteProductIds = favoriteProductIds;
            return View(products);
        }

        public IActionResult Detail(int id)
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

            if (User.Identity != null && User.Identity.IsAuthenticated)
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

            ViewBag.IsFavorite = isFavorite;
            ViewBag.RelatedProducts = _productRepo.GetAll().Where(x => x.CategoryId == product.CategoryId && x.ProductId != product.ProductId)
        .Take(4)
        .ToList();

            return View(product);
        }

        public IActionResult Create()
        {
            ViewBag.Categories =
                new SelectList(
                    _categoryRepo.GetAll(),
                    "CategoryId",
                    "CategoryName");

            return View();
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
