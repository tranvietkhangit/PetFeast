using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PetFeast.Models.Identity;

namespace PetFeast.ViewComponents
{
    public class UserPointsViewComponent : ViewComponent
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public UserPointsViewComponent(
            UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            if (!UserClaimsPrincipal.Identity?.IsAuthenticated ?? true)
            {
                return View(0);
            }

            var user = await _userManager
                .GetUserAsync(UserClaimsPrincipal);

            if (user == null)
            {
                return View(0);
            }

            return View(user.Points);
        }
    }
}
