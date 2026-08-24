using Microsoft.AspNetCore.Mvc;
using PetFeast.Models.Interfaces;


namespace PetFeast.ViewComponents
{
    public class CartCountViewComponent : ViewComponent
    {
        private readonly IShoppingCartRepository _cartRepository;

        public CartCountViewComponent(
            IShoppingCartRepository cartRepository)
        {
            _cartRepository = cartRepository;
        }

        public IViewComponentResult Invoke()
        {
            var count = _cartRepository.GetCartCount();

            return Content(count.ToString());
        }
    }
}
