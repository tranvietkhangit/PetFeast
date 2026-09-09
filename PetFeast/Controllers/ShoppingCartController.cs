using Microsoft.AspNetCore.Mvc;
using PetFeast.Models.Interfaces;
using PetFeast.Models.Services;
using PetFeast.Models.ShoppingCart;
using Microsoft.AspNetCore.Authorization;

namespace PetFeast.Controllers
{
    [Authorize]
    public class ShoppingCartController : Controller
    {
        private readonly IShoppingCartRepository _cartRepo;
        private readonly IProductRepository _productRepo;

        public ShoppingCartController(
            IShoppingCartRepository cartRepo,
            IProductRepository productRepo)
        {
            _cartRepo = cartRepo;
            _productRepo = productRepo;
        }

        // =========================================================
        // CART
        // =========================================================

        public IActionResult Index()
        {
            var cart = _cartRepo.GetCart();

            return View(cart);
        }

        // =========================================================
        // ADD TO CART - NORMAL
        // =========================================================

        [HttpPost]
        public IActionResult AddToCart(
            int productId,
            int quantity)
        {
            var product =
                _productRepo.GetById(productId);

            if (product == null)
            {
                return NotFound();
            }

            // Không cho số lượng <= 0
            if (quantity < 1)
            {
                quantity = 1;
            }

            if (product.Quantity <= 0)
            {
                TempData["Error"] =
                    "Sản phẩm hiện đã hết hàng.";

                return RedirectToAction(
                    "Detail",
                    "Product",
                    new { id = productId });
            }

            // =====================================================
            // LẤY GIỎ HIỆN TẠI
            // =====================================================

            var cart = _cartRepo.GetCart();

            var exist = cart.FirstOrDefault(
                x => x.ProductId == productId);

            // Số lượng hiện đã có trong giỏ
            int currentCartQuantity =
                exist?.Quantity ?? 0;

            // =====================================================
            // KIỂM TRA TỔNG SỐ LƯỢNG
            //
            // Ví dụ:
            // Kho = 17
            // Giỏ = 1
            // Muốn thêm = 16
            //
            // 1 + 16 = 17 -> OK
            //
            // Giỏ = 1
            // Muốn thêm = 17
            //
            // 1 + 17 = 18 -> KHÔNG OK
            // =====================================================

            int totalQuantity =
                currentCartQuantity + quantity;

            if (totalQuantity > product.Quantity)
            {
                int canAdd =
                    product.Quantity - currentCartQuantity;

                if (canAdd <= 0)
                {
                    TempData["Error"] =
                        "Sản phẩm này đã đạt số lượng tối đa trong giỏ hàng.";

                }
                else
                {
                    TempData["Error"] =
                        $"Bạn đã có {currentCartQuantity} sản phẩm trong giỏ. " +
                        $"Bạn chỉ có thể thêm tối đa {canAdd} sản phẩm nữa.";
                }

                return RedirectToAction(
                    "Detail",
                    "Product",
                    new { id = productId });
            }

            // =====================================================
            // TẠO ITEM
            // =====================================================

            var item = new ShoppingCartItem
            {
                ProductId =
                    product.ProductId,

                ProductName =
                    product.ProductName,

                ImageUrl =
                    product.ImageUrl,

                Price =
                    product.DiscountPercent > 0
                        ? product.DiscountPrice
                        : product.Price,

                Quantity =
                    quantity,

                IsSelected =
                    true
            };

            _cartRepo.AddToCart(item);

            return RedirectToAction(nameof(Index));
        }

        // =========================================================
        // ADD TO CART - AJAX
        // =========================================================

        [AllowAnonymous]
        [HttpPost]
        public IActionResult AddToCartAjax(
            int productId,
            int quantity = 1)
        {
            // =====================================================
            // KIỂM TRA ĐĂNG NHẬP
            // =====================================================

            if (!User.Identity?.IsAuthenticated ?? true)
            {
                return Json(new
                {
                    success = false,
                    requireLogin = true
                });
            }

            // =====================================================
            // LẤY PRODUCT
            // =====================================================

            var product =
                _productRepo.GetById(productId);

            if (product == null)
            {
                return Json(new
                {
                    success = false,
                    message =
                        "Không tìm thấy sản phẩm."
                });
            }

            // =====================================================
            // KIỂM TRA QUANTITY
            // =====================================================

            if (quantity < 1)
            {
                quantity = 1;
            }

            if (product.Quantity <= 0)
            {
                return Json(new
                {
                    success = false,
                    message =
                        "Sản phẩm hiện đã hết hàng."
                });
            }

            // =====================================================
            // LẤY GIỎ HIỆN TẠI
            // =====================================================

            var cart =
                _cartRepo.GetCart();

            var exist =
                cart.FirstOrDefault(
                    x => x.ProductId == productId);

            // Số lượng hiện có trong giỏ
            int currentCartQuantity =
                exist?.Quantity ?? 0;

            // =====================================================
            // TÍNH TỔNG
            // =====================================================

            int totalQuantity =
                currentCartQuantity + quantity;

            // =====================================================
            // KIỂM TRA TỒN KHO
            // =====================================================

            if (totalQuantity > product.Quantity)
            {
                int canAdd =
                    product.Quantity -
                    currentCartQuantity;

                if (canAdd <= 0)
                {
                    return Json(new
                    {
                        success = false,
                        message =
                            "Sản phẩm này đã đạt số lượng tối đa trong giỏ hàng."
                    });
                }

                return Json(new
                {
                    success = false,
                    message =
                        $"Bạn đã có {currentCartQuantity} sản phẩm trong giỏ. " +
                        $"Bạn chỉ có thể thêm tối đa {canAdd} sản phẩm nữa."
                });
            }

            // =====================================================
            // TẠO ITEM
            // =====================================================

            var item =
                new ShoppingCartItem
                {
                    ProductId =
                        product.ProductId,

                    ProductName =
                        product.ProductName,

                    ImageUrl =
                        product.ImageUrl,

                    Price =
                        product.DiscountPercent > 0
                            ? product.DiscountPrice
                            : product.Price,

                    Quantity =
                        quantity,

                    IsSelected =
                        true
                };

            _cartRepo.AddToCart(item);

            // =====================================================
            // CART COUNT
            // =====================================================

            var cartCount =
                _cartRepo.GetCartCount();

            return Json(new
            {
                success = true,
                message =
                    "Đã thêm sản phẩm vào giỏ hàng!",
                cartCount = cartCount
            });
        }

        // =========================================================
        // REMOVE
        // =========================================================

        public IActionResult Remove(int id)
        {
            _cartRepo.Remove(id);

            return RedirectToAction(nameof(Index));
        }

        // =========================================================
        // UPDATE CART
        // =========================================================

        [HttpPost]
        public IActionResult UpdateCart(
            int productId,
            int quantity)
        {
            var product =
                _productRepo.GetById(productId);

            if (product == null)
            {
                return NotFound();
            }

            if (quantity < 1)
            {
                quantity = 1;
            }

            if (quantity > product.Quantity)
            {
                quantity = product.Quantity;
            }

            _cartRepo.UpdateQuantity(
                productId,
                quantity);

            return RedirectToAction(nameof(Index));
        }

        // =========================================================
        // SELECT
        // =========================================================

        [HttpPost]
        public IActionResult UpdateSelect(
            int productId,
            bool isSelected)
        {
            _cartRepo.UpdateSelect(
                productId,
                isSelected);

            return Ok();
        }

        // =========================================================
        // INCREASE
        // =========================================================

        [HttpPost]
        public IActionResult IncreaseQuantity(
            int productId)
        {
            var product = _productRepo.GetById(productId);

            if (product == null)
            {
                return Json(new
                {
                    success = false,
                    message = "Không tìm thấy sản phẩm."
                });
            }

            var cart = _cartRepo.GetCart();
            var item = cart.FirstOrDefault(x => x.ProductId == productId);

            if (item == null)
            {
                return Json(new
                {
                    success = false,
                    message = "Sản phẩm không có trong giỏ hàng."
                });
            }

            if (item.Quantity >= product.Quantity)
            {
                return Json(new
                {
                    success = false,
                    message = $"Chỉ còn {product.Quantity} sản phẩm trong kho.",
                    quantity = item.Quantity,
                    totalPrice = item.TotalPrice
                });
            }

            _cartRepo.IncreaseQuantity(productId);

            // Lấy lại item sau khi tăng
            cart = _cartRepo.GetCart();
            item = cart.FirstOrDefault(x => x.ProductId == productId);

            return Json(new
            {
                success = true,
                quantity = item!.Quantity,
                totalPrice = item.TotalPrice,
                message = "Đã tăng số lượng."
            });
        }

        // =========================================================
        // DECREASE
        // =========================================================

        [HttpPost]
        public IActionResult DecreaseQuantity(
            int productId)
        {
            var cart = _cartRepo.GetCart();
            var item = cart.FirstOrDefault(x => x.ProductId == productId);

            if (item == null)
            {
                return Json(new
                {
                    success = false,
                    message = "Sản phẩm không có trong giỏ hàng."
                });
            }

            if (item.Quantity <= 1)
            {
                return Json(new
                {
                    success = false,
                    message = "Số lượng tối thiểu là 1.",
                    quantity = item.Quantity,
                    totalPrice = item.TotalPrice
                });
            }

            _cartRepo.DecreaseQuantity(productId);

            // Lấy lại item sau khi giảm
            cart = _cartRepo.GetCart();
            item = cart.FirstOrDefault(x => x.ProductId == productId);

            return Json(new
            {
                success = true,
                quantity = item!.Quantity,
                totalPrice = item.TotalPrice,
                message = "Đã giảm số lượng."
            });
        }

        // =========================================================
        // REMOVE FROM CART
        // =========================================================

        [HttpPost]
        public IActionResult RemoveFromCart(
            int productId)
        {
            _cartRepo.RemoveFromCart(
                productId);

            return RedirectToAction(nameof(Index));
        }
    }
}