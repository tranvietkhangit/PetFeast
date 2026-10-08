#nullable disable

using System;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PetFeast.Models.Identity;
using PetFeast.Models.Services;

namespace PetFeast.Areas.Identity.Pages.Account
{
    public class ResendRegisterOtpModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly EmailService _emailService;

        public ResendRegisterOtpModel(
            UserManager<ApplicationUser> userManager,
            EmailService emailService)
        {
            _userManager = userManager;
            _emailService = emailService;
        }

        // ==========================================
        // INPUT
        // ==========================================

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "Vui lòng nhập email.")]
            [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
            [Display(Name = "Email")]
            public string Email { get; set; }
        }

        // ==========================================
        // RETURN URL
        // ==========================================

        public string ReturnUrl { get; set; } = "~/";

        // ==========================================
        // GET
        // ==========================================

        public void OnGet(string email = null, string returnUrl = null)
        {
            ReturnUrl = returnUrl ?? "~/";

            // Nếu Login truyền email sang thì tự điền
            if (!string.IsNullOrWhiteSpace(email))
            {
                Input = new InputModel
                {
                    Email = email
                };
            }
        }

        // ==========================================
        // POST
        // ==========================================

        public async Task<IActionResult> OnPostAsync(
            string returnUrl = null)
        {
            ReturnUrl = returnUrl ?? "~/";

            if (!ModelState.IsValid)
            {
                return Page();
            }

            var email = Input.Email.Trim();

            // ==========================================
            // TÌM USER
            // ==========================================

            var user =
                await _userManager.FindByEmailAsync(email);

            if (user == null)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Không tìm thấy tài khoản với email này.");

                return Page();
            }

            // ==========================================
            // EMAIL ĐÃ XÁC NHẬN
            // ==========================================

            if (user.EmailConfirmed)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Email này đã được xác nhận. Bạn có thể đăng nhập.");

                return Page();
            }

            // ==========================================
            // CHỐNG SPAM OTP - 60 GIÂY
            // ==========================================

            var lastSentString =
                HttpContext.Session.GetString(
                    "RegisterOtpLastSent");

            if (DateTime.TryParse(
                    lastSentString,
                    out DateTime lastSent))
            {
                var secondsPassed =
                    (DateTime.Now - lastSent).TotalSeconds;

                if (secondsPassed < 60)
                {
                    int remainingSeconds =
                        60 - (int)secondsPassed;

                    ModelState.AddModelError(
                        string.Empty,
                        $"Vui lòng chờ {remainingSeconds} giây trước khi gửi lại OTP.");

                    return Page();
                }
            }

            // ==========================================
            // TẠO OTP MỚI
            // ==========================================

            string otp =
                RandomNumberGenerator
                    .GetInt32(100000, 1000000)
                    .ToString();

            DateTime expiry =
                DateTime.Now.AddMinutes(5);

            // ==========================================
            // LƯU OTP VÀO SESSION
            // ==========================================

            HttpContext.Session.SetString(
                "RegisterOtp",
                otp);

            HttpContext.Session.SetString(
                "RegisterUserId",
                user.Id);

            HttpContext.Session.SetString(
                "RegisterEmail",
                user.Email);

            HttpContext.Session.SetString(
                "RegisterOtpExpiry",
                expiry.ToString("O"));

            // ==========================================
            // GỬI OTP QUA EMAIL
            // ==========================================

            try
            {
                await _emailService.SendRegisterOtpEmailAsync(
                    user.Email,
                    otp,
                    user.UserName);

                // Lưu thời gian gửi OTP
                HttpContext.Session.SetString(
                    "RegisterOtpLastSent",
                    DateTime.Now.ToString("O"));
            }
            catch
            {
                // Nếu gửi thất bại thì xóa OTP mới

                HttpContext.Session.Remove(
                    "RegisterOtp");

                HttpContext.Session.Remove(
                    "RegisterOtpExpiry");

                ModelState.AddModelError(
                    string.Empty,
                    "Không thể gửi mã OTP. Vui lòng thử lại sau.");

                return Page();
            }

            // ==========================================
            // CHUYỂN SANG TRANG OTP
            // ==========================================

            return RedirectToPage(
                "./VerifyRegisterOtp",
                new
                {
                    returnUrl = ReturnUrl
                });
        }
    }
}