using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PetFeast.Models.Identity;
using PetFeast.Models.Interfaces;
using PetFeast.Models.Services;

namespace PetFeast.Controllers
{
    [Authorize]
    public class NotificationController : Controller
    {
        private readonly INotificationRepository _notificationRepo;
        private readonly UserManager<ApplicationUser> _userManager;
        public NotificationController(
            INotificationRepository notificationRepo,
            UserManager<ApplicationUser> userManager)
        {
            _notificationRepo = notificationRepo;
            _userManager = userManager;
        }
        // GET: Notification
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return Challenge();
            }

            var notifications =
                await _notificationRepo.GetByUserIdAsync(user.Id);

            return View(notifications);
        }
        // POST: Notification/MarkAsRead
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var userId = _userManager.GetUserId(User);

            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            await _notificationRepo.MarkAsReadAsync(id, userId);

            return Json(new
            {
                success = true,
                id = id
            });
        }
    }
}
