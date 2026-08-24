using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages;
using PetFeast.Data;
using PetFeast.Models.Identity;
using PetFeast.Models.Services;

namespace PetFeast.Controllers
{
    [Authorize]
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly PetFeastDBContext _context;
        private readonly EmailService _emailService;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            PetFeastDBContext context,
            EmailService emailService)
        {
            _userManager = userManager;
            _context = context;
            _emailService = emailService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return Challenge();

            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(
            string fullName,
            string phoneNumber)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return Challenge();

            user.FullName = fullName;
            user.PhoneNumber = phoneNumber;

            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                TempData["Success"] = "Cập nhật thông tin thành công.";
            }
            else
            {
                TempData["Error"] =
                    "Không thể cập nhật thông tin. Vui lòng thử lại.";
            }

            return RedirectToAction(nameof(Index));
        }
        [HttpGet]
        public IActionResult ChangePassword()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(
            string currentPassword,
            string newPassword,
            string confirmPassword)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return Challenge();

            if (string.IsNullOrWhiteSpace(currentPassword) ||
                string.IsNullOrWhiteSpace(newPassword) ||
                string.IsNullOrWhiteSpace(confirmPassword))
            {
                ModelState.AddModelError("", "Vui lòng nhập đầy đủ thông tin.");
                return View();
            }

            if (newPassword != confirmPassword)
            {
                ModelState.AddModelError(
                    "confirmPassword",
                    "Mật khẩu xác nhận không khớp.");

                return View();
            }

            var result = await _userManager.ChangePasswordAsync(
                user,
                currentPassword,
                newPassword);

            if (result.Succeeded)
            {
                TempData["Success"] =
                    "Đổi mật khẩu thành công.";

                return RedirectToAction(nameof(Index));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }

            return View();
        }
        // ==============================
        // QUÊN MẬT KHẨU
        // ==============================

        [AllowAnonymous]
        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }


        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                ModelState.AddModelError(
                    "email",
                    "Vui lòng nhập email.");

                return View();
            }

            var user = await _userManager.FindByEmailAsync(email);

            // Không tiết lộ email có tồn tại hay không
            if (user == null)
            {
                return RedirectToAction(
                    nameof(ForgotPasswordConfirmation));
            }

            var token =
                await _userManager.GeneratePasswordResetTokenAsync(user);

            var resetLink = Url.Action(
                nameof(ResetPassword),
                "Account",
                new
                {
                    userId = user.Id,
                    token = token
                },
                Request.Scheme);

            try
            {
                await _emailService.SendResetPasswordEmailAsync(
                    user.Email!,
                    resetLink!);
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    "Lỗi gửi email: " + ex.Message);
            }

            return RedirectToAction(
                nameof(ForgotPasswordConfirmation));
        }


        // ==============================
        // XÁC NHẬN QUÊN MẬT KHẨU
        // ==============================

        [AllowAnonymous]
        [HttpGet]
        public IActionResult ForgotPasswordConfirmation()
        {
            return View();
        }


        // ==============================
        // ĐẶT LẠI MẬT KHẨU
        // ==============================

        [AllowAnonymous]
        [HttpGet]
        public IActionResult ResetPassword(
            string userId,
            string token)
        {
            if (string.IsNullOrEmpty(userId) ||
                string.IsNullOrEmpty(token))
            {
                return BadRequest();
            }

            ViewBag.UserId = userId;
            ViewBag.Token = token;

            return View();
        }


        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(
            string userId,
            string token,
            string newPassword,
            string confirmPassword)
        {
            if (string.IsNullOrWhiteSpace(newPassword) ||
                string.IsNullOrWhiteSpace(confirmPassword))
            {
                ModelState.AddModelError(
                    "",
                    "Vui lòng nhập đầy đủ thông tin.");

                ViewBag.UserId = userId;
                ViewBag.Token = token;

                return View();
            }

            if (newPassword != confirmPassword)
            {
                ModelState.AddModelError(
                    "confirmPassword",
                    "Mật khẩu xác nhận không khớp.");

                ViewBag.UserId = userId;
                ViewBag.Token = token;

                return View();
            }

            var user =
                await _userManager.FindByIdAsync(userId);

            if (user == null)
            {
                return RedirectToAction(
                    nameof(Login));
            }

            var result =
                await _userManager.ResetPasswordAsync(
                    user,
                    token,
                    newPassword);

            if (result.Succeeded)
            {
                TempData["Success"] =
                    "Đặt lại mật khẩu thành công. Bạn có thể đăng nhập bằng mật khẩu mới.";

                return RedirectToAction(
                    nameof(Login));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(
                    "",
                    error.Description);
            }

            ViewBag.UserId = userId;
            ViewBag.Token = token;

            return View();
        }

    }
}
