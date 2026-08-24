using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PetFeast.Data;
using PetFeast.Models.Identity;
using PetFeast.Models.Interfaces;
using PetFeast.Models.ShoppingCart;

namespace PetFeast.Models.Services
{
    public class ShoppingCartRepository : IShoppingCartRepository
    {
        private readonly PetFeastDBContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IHttpContextAccessor _httpContext;

        public ShoppingCartRepository(
            PetFeastDBContext context,
            UserManager<ApplicationUser> userManager,
            IHttpContextAccessor httpContext)
        {
            _context = context;
            _userManager = userManager;
            _httpContext = httpContext;
        }

        // ==========================================
        // LẤY USER ID HIỆN TẠI
        // ==========================================

        private string? GetCurrentUserId()
        {
            var user = _httpContext.HttpContext?.User;

            if (user == null || !user.Identity?.IsAuthenticated == true)
            {
                return null;
            }

            return _userManager.GetUserId(user);
        }


        // ==========================================
        // LẤY CART CỦA USER
        // ==========================================

        private Cart? GetUserCart()
        {
            var userId = GetCurrentUserId();

            if (string.IsNullOrEmpty(userId))
            {
                return null;
            }

            return _context.Carts
                .Include(c => c.Items)
                .FirstOrDefault(c => c.UserId == userId);
        }


        // ==========================================
        // TẠO CART NẾU CHƯA CÓ
        // ==========================================

        private Cart GetOrCreateUserCart()
        {
            var userId = GetCurrentUserId();

            if (string.IsNullOrEmpty(userId))
            {
                throw new InvalidOperationException(
                    "Người dùng chưa đăng nhập.");
            }

            var cart = _context.Carts
                .Include(c => c.Items)
                .FirstOrDefault(c => c.UserId == userId);

            if (cart == null)
            {
                cart = new Cart
                {
                    UserId = userId
                };

                _context.Carts.Add(cart);
                _context.SaveChanges();
            }

            return cart;
        }


        // ==========================================
        // GET CART
        // ==========================================

        public List<ShoppingCartItem> GetCart()
        {
            var userId = GetCurrentUserId();

            if (string.IsNullOrEmpty(userId))
            {
                return new List<ShoppingCartItem>();
            }

            var cartItems = _context.CartItems
                .Include(x => x.Product)
                .Include(x => x.Cart)
                .Where(x => x.Cart.UserId == userId)
                .ToList();

            return cartItems.Select(x => new ShoppingCartItem
            {
                ProductId = x.ProductId,

                ProductName = x.Product.ProductName,

                ImageUrl = x.Product.ImageUrl,

                Price = x.Product.DiscountPercent > 0
                    ? x.Product.DiscountPrice
                    : x.Product.Price,

                Quantity = x.Quantity,

                IsSelected = x.IsSelected

            }).ToList();
        }


        // ==========================================
        // ADD TO CART
        // ==========================================

        public void AddToCart(ShoppingCartItem item)
        {
            var cart = GetOrCreateUserCart();

            var existingItem = cart.Items
                .FirstOrDefault(x =>
                    x.ProductId == item.ProductId);

            if (existingItem != null)
            {
                existingItem.Quantity += item.Quantity;
            }
            else
            {
                cart.Items.Add(new CartItem
                {
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    IsSelected = true
                });
            }

            _context.SaveChanges();
        }


        // ==========================================
        // REMOVE
        // ==========================================

        public void Remove(int productId)
        {
            RemoveFromCart(productId);
        }


        public void RemoveFromCart(int productId)
        {
            var cart = GetUserCart();

            if (cart == null)
                return;

            var item = cart.Items
                .FirstOrDefault(x =>
                    x.ProductId == productId);

            if (item != null)
            {
                _context.CartItems.Remove(item);
                _context.SaveChanges();
            }
        }


        // ==========================================
        // UPDATE QUANTITY
        // ==========================================

        public void UpdateQuantity(
            int productId,
            int quantity)
        {
            var cart = GetUserCart();

            if (cart == null)
                return;

            var item = cart.Items
                .FirstOrDefault(x =>
                    x.ProductId == productId);

            if (item != null)
            {
                item.Quantity = quantity;

                _context.SaveChanges();
            }
        }


        // ==========================================
        // CLEAR CART
        // ==========================================

        public void ClearCart()
        {
            var cart = GetUserCart();

            if (cart == null)
                return;

            _context.CartItems.RemoveRange(cart.Items);

            _context.SaveChanges();
        }


        // ==========================================
        // UPDATE SELECT
        // ==========================================

        public void UpdateSelect(
            int productId,
            bool isSelected)
        {
            var cart = GetUserCart();

            if (cart == null)
                return;

            var item = cart.Items
                .FirstOrDefault(x =>
                    x.ProductId == productId);

            if (item != null)
            {
                item.IsSelected = isSelected;

                _context.SaveChanges();
            }
        }


        // ==========================================
        // INCREASE
        // ==========================================

        public void IncreaseQuantity(int productId)
        {
            var cart = GetUserCart();

            if (cart == null)
                return;

            var item = cart.Items
                .FirstOrDefault(x =>
                    x.ProductId == productId);

            if (item != null)
            {
                item.Quantity++;

                _context.SaveChanges();
            }
        }


        // ==========================================
        // DECREASE
        // ==========================================

        public void DecreaseQuantity(int productId)
        {
            var cart = GetUserCart();

            if (cart == null)
                return;

            var item = cart.Items
                .FirstOrDefault(x =>
                    x.ProductId == productId);

            if (item == null)
                return;

            item.Quantity--;

            if (item.Quantity <= 0)
            {
                _context.CartItems.Remove(item);
            }

            _context.SaveChanges();
        }


        // ==========================================
        // CART COUNT
        // ==========================================

        public int GetCartCount()
        {
            var userId = GetCurrentUserId();

            if (string.IsNullOrEmpty(userId))
            {
                return 0;
            }

            return _context.CartItems
                .Where(x => x.Cart.UserId == userId)
                .Sum(x => (int?)x.Quantity) ?? 0;
        }
    }
}
