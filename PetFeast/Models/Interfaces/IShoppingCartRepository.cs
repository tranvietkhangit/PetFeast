using PetFeast.Models.ShoppingCart;

namespace PetFeast.Models.Interfaces
{
    public interface IShoppingCartRepository
    {
        List<ShoppingCartItem> GetCart();

        void AddToCart(ShoppingCartItem item);

        void Remove(int productId);

        void UpdateQuantity(int productId, int quantity);

        void ClearCart();

        void UpdateSelect(int productId, bool isSelected);

        void IncreaseQuantity(int productId);

        void DecreaseQuantity(int productId);

        void RemoveFromCart(int productId);

        int GetCartCount();
    }
}
