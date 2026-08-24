using PetFeast.Models.Products;

namespace PetFeast.Models.ShoppingCart
{
    public class CartItem
    {
        public int CartItemId { get; set; }

        public int CartId { get; set; }

        public int ProductId { get; set; }

        public int Quantity { get; set; }

        public bool IsSelected { get; set; } = true;

        public Cart Cart { get; set; } = null!;

        public Product Product { get; set; } = null!;
    }
}
