using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PetFeast.Models.Identity;
using PetFeast.Models.Interfaces;

namespace PetFeast.ViewComponents
{
    public class NotificationCountViewComponent : ViewComponent
    {
        private readonly INotificationRepository _notificationRepo;
        private readonly UserManager<ApplicationUser> _userManager;

        public NotificationCountViewComponent(
            INotificationRepository notificationRepo,
            UserManager<ApplicationUser> userManager)
        {
            _notificationRepo = notificationRepo;
            _userManager = userManager;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            if (!(User?.Identity?.IsAuthenticated ?? false))
            {
                return Content("0");
            }

            var user = await _userManager.GetUserAsync(
                (System.Security.Claims.ClaimsPrincipal)User);

            if (user == null)
            {
                return Content("0");
            }

            var count = await _notificationRepo
                .GetUnreadCountAsync(user.Id);

            return Content(count.ToString());
        }
    }
}
