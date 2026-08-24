using PetFeast.Models.Identity;

namespace PetFeast.Models.ShoppingCart
{
    public class Cart
    {
        public int CartId { get; set; }

        public string UserId { get; set; } = string.Empty;

        public ApplicationUser User { get; set; } = null!;

        public ICollection<CartItem> Items { get; set; }
            = new List<CartItem>();
    }
}
