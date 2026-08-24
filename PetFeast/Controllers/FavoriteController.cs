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

        // Danh sách sản phẩm yêu thích
        public IActionResult Index()
        {
            string userId =
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier)!;

            var favorites = _context.Favorites
                .Include(f => f.Product)
                .Where(f => f.UserId == userId)
                .ToList();

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
                // =========================
                // THÊM YÊU THÍCH
                // =========================

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
                // =========================
                // BỎ YÊU THÍCH
                // =========================

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
