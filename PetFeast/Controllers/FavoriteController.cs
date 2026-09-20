using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PetFeast.Data;
using PetFeast.Models.Products;
using System.Security.Claims;

namespace PetFeast.Controllers
{
    public class FavoriteController : Controller
    {
        private readonly PetFeastDBContext _context;

        public FavoriteController(PetFeastDBContext context)
        {
            _context = context;
        }

        // =========================
        // DANH SÁCH SẢN PHẨM YÊU THÍCH
        // =========================
        [Authorize]
        public async Task<IActionResult> Index(int page = 1)
        {
            const int pageSize = 6;

            var userId = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var query = _context.Favorites
                .Include(f => f.Product)
                .Where(f => f.UserId == userId)
                .OrderByDescending(f => f.FavoriteId);

            int totalFavorites = await query.CountAsync();

            int totalPages = (int)Math.Ceiling(
                totalFavorites / (double)pageSize);

            if (page < 1)
                page = 1;

            if (totalPages > 0 && page > totalPages)
                page = totalPages;

            var favorites = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;

            return View(favorites);
        }

        // =========================
        // TOGGLE YÊU THÍCH - AJAX
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ToggleAjax(int productId)
        {
            if (User.Identity?.IsAuthenticated != true)
            {
                return Json(new
                {
                    success = false,
                    requireLogin = true
                });
            }

            string userId =
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier)!;

            var favorite = _context.Favorites
                .FirstOrDefault(f =>
                    f.UserId == userId &&
                    f.ProductId == productId);

            bool isFavorite;

            if (favorite == null)
            {
                // Thêm yêu thích
                _context.Favorites.Add(
                    new Favorite
                    {
                        UserId = userId,
                        ProductId = productId
                    });

                isFavorite = true;
            }
            else
            {
                // Bỏ yêu thích
                _context.Favorites.Remove(favorite);

                isFavorite = false;
            }

            _context.SaveChanges();

            return Json(new
            {
                success = true,
                isFavorite = isFavorite
            });
        }


        // =========================
        // TOGGLE CŨ
        // DÙNG CHO DETAIL / FAVORITE
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Toggle(
            int productId,
            string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated != true)
            {
                return RedirectToPage(
                    "/Account/Login",
                    new
                    {
                        area = "Identity",
                        returnUrl =
                            returnUrl ??
                            $"/Product/Detail/{productId}"
                    });
            }

            string userId =
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier)!;

            var favorite = _context.Favorites
                .FirstOrDefault(f =>
                    f.UserId == userId &&
                    f.ProductId == productId);

            if (favorite == null)
            {
                _context.Favorites.Add(
                    new Favorite
                    {
                        UserId = userId,
                        ProductId = productId
                    });
            }
            else
            {
                _context.Favorites.Remove(favorite);
            }

            _context.SaveChanges();

            if (!string.IsNullOrEmpty(returnUrl) &&
                Url.IsLocalUrl(returnUrl))
            {
                return LocalRedirect(returnUrl);
            }

            return RedirectToAction(
                "Index",
                "Product");
        }
    }
}