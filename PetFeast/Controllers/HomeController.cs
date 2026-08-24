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
        public IActionResult Discount()
        {
            var products = _productRepository
                .GetAll()
                .Where(x => x.DiscountPercent > 0)
                .ToList();


            return View(products);
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
