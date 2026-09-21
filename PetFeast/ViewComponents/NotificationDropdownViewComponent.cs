using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PetFeast.Models.Identity;
using PetFeast.Models.Interfaces;
using PetFeast.Models.Notifications;

namespace PetFeast.ViewComponents
{
    public class NotificationDropdownViewComponent : ViewComponent
    {
        private readonly INotificationRepository _notificationRepo;
        private readonly UserManager<ApplicationUser> _userManager;

        public NotificationDropdownViewComponent(
            INotificationRepository notificationRepo,
            UserManager<ApplicationUser> userManager)
        {
            _notificationRepo = notificationRepo;
            _userManager = userManager;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var user = await _userManager.GetUserAsync(
                HttpContext.User
            );

            var notifications = new List<Notification>();

            if (user != null)
            {
                notifications =
                    await _notificationRepo.GetByUserIdAsync(user.Id);
            }

            return View("Default", notifications);
        }
    }
}