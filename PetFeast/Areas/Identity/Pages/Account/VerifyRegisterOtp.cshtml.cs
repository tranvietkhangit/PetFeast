using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PetFeast.Models.Identity;
using PetFeast.Models.Services;
using System.Security.Cryptography;

namespace PetFeast.Areas.Identity.Pages.Account
{
    public class VerifyRegisterOtpModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly EmailService _emailService;

        public VerifyRegisterOtpModel(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            EmailService emailService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _emailService = emailService;
        }

        [BindProperty]
        public string Otp { get; set; } = "";

        public string Email { get; set; } = "";

        public string ReturnUrl { get; set; } = "~/";

        // ==========================================
        // HIỂN THỊ TRANG OTP
        // ==========================================

        public void OnGet(string returnUrl = null)
        {
            Email =
                HttpContext.Session.GetString("RegisterEmail")
                ?? "";

            ReturnUrl = returnUrl ?? "~/";
        }

        // ==========================================
        // XÁC NHẬN OTP
        // ==========================================

        public async Task<IActionResult> OnPostAsync(
            string returnUrl = null)
        {
            ReturnUrl = returnUrl ?? "~/";

            var storedOtp =
                HttpContext.Session.GetString("RegisterOtp");

            var userId =
                HttpContext.Session.GetString("RegisterUserId");

            var email =
                HttpContext.Session.GetString("RegisterEmail");

            var expiryString =
                HttpContext.Session.GetString("RegisterOtpExpiry");

            // ==========================================
            // KIỂM TRA SESSION
            // ==========================================

            if (string.IsNullOrEmpty(storedOtp) ||
                string.IsNullOrEmpty(userId) ||
                string.IsNullOrEmpty(email) ||
                string.IsNullOrEmpty(expiryString))
            {
                ModelState.AddModelError(
                    "",
                    "Mã OTP không tồn tại hoặc đã hết hạn.");

                Email = email ?? "";

                return Page();
            }

            // ==========================================
            // KIỂM TRA THỜI GIAN
            // ==========================================

            if (!DateTime.TryParse(
                    expiryString,
                    out DateTime expiry))
            {
                ModelState.AddModelError(
                    "",
                    "Mã OTP không hợp lệ.");

                Email = email;

                return Page();
            }

            // ==========================================
            // KIỂM TRA OTP HẾT HẠN
            // ==========================================

            if (DateTime.Now > expiry)
            {
                HttpContext.Session.Remove("RegisterOtp");
                HttpContext.Session.Remove("RegisterOtpExpiry");

                ModelState.AddModelError(
                    "",
                    "Mã OTP đã hết hạn. Vui lòng gửi lại mã mới.");

                Email = email;

                return Page();
            }

            // ==========================================
            // KIỂM TRA OTP RỖNG
            // ==========================================

            if (string.IsNullOrWhiteSpace(Otp))
            {
                ModelState.AddModelError(
                    "",
                    "Vui lòng nhập mã OTP.");

                Email = email;

                return Page();
            }

            // ==========================================
            // KIỂM TRA OTP
            // ==========================================

            Otp = Otp.Trim();

            if (Otp != storedOtp)
            {
                ModelState.AddModelError(
                    "",
                    "Mã OTP không chính xác.");

                Email = email;

                return Page();
            }

            // ==========================================
            // TÌM USER
            // ==========================================

            var user =
                await _userManager.FindByIdAsync(userId);

            if (user == null)
            {
                ModelState.AddModelError(
                    "",
                    "Không tìm thấy tài khoản.");

                Email = email;

                return Page();
            }

            // ==========================================
            // NẾU EMAIL CHƯA XÁC NHẬN
            // ==========================================

            if (!user.EmailConfirmed)
            {
                user.EmailConfirmed = true;

                var result =
                    await _userManager.UpdateAsync(user);

                if (!result.Succeeded)
                {
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError(
                            "",
                            error.Description);
                    }

                    Email = email;

                    return Page();
                }
            }

            // ==========================================
            // XÓA OTP KHỎI SESSION
            // ==========================================

            HttpContext.Session.Remove("RegisterOtp");
            HttpContext.Session.Remove("RegisterUserId");
            HttpContext.Session.Remove("RegisterEmail");
            HttpContext.Session.Remove("RegisterOtpExpiry");
            HttpContext.Session.Remove("RegisterOtpLastSent");

            // ==========================================
            // ĐĂNG NHẬP
            // ==========================================

            await _signInManager.SignInAsync(
                user,
                isPersistent: false);

            return LocalRedirect(ReturnUrl);
        }

        // ==========================================
        // GỬI LẠI OTP
        // ==========================================

        public async Task<IActionResult> OnPostResendAsync(
            string returnUrl = null)
        {
            ReturnUrl = returnUrl ?? "~/";

            var userId =
                HttpContext.Session.GetString("RegisterUserId");

            var email =
                HttpContext.Session.GetString("RegisterEmail");

            // ==========================================
            // KIỂM TRA SESSION
            // ==========================================

            if (string.IsNullOrEmpty(userId) ||
                string.IsNullOrEmpty(email))
            {
                return RedirectToPage("./Register");
            }

            // ==========================================
            // CHỐNG SPAM - 60 GIÂY
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

                    TempData["Error"] =
                        $"Vui lòng chờ {remainingSeconds} giây trước khi gửi lại OTP.";

                    return RedirectToPage(
                        "./VerifyRegisterOtp",
                        new
                        {
                            returnUrl = ReturnUrl
                        });
                }
            }

            // ==========================================
            // TÌM USER
            // ==========================================

            var user =
                await _userManager.FindByIdAsync(userId);

            if (user == null)
            {
                return RedirectToPage("./Register");
            }

            // ==========================================
            // NẾU USER ĐÃ XÁC NHẬN
            // ==========================================

            if (user.EmailConfirmed)
            {
                HttpContext.Session.Remove("RegisterOtp");
                HttpContext.Session.Remove("RegisterUserId");
                HttpContext.Session.Remove("RegisterEmail");
                HttpContext.Session.Remove("RegisterOtpExpiry");
                HttpContext.Session.Remove("RegisterOtpLastSent");

                await _signInManager.SignInAsync(
                    user,
                    isPersistent: false);

                return LocalRedirect(ReturnUrl);
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
            // LƯU OTP
            // ==========================================

            HttpContext.Session.SetString(
                "RegisterOtp",
                otp);

            HttpContext.Session.SetString(
                "RegisterOtpExpiry",
                expiry.ToString("O"));

            // ==========================================
            // GỬI EMAIL
            // ==========================================

            try
            {
                await _emailService.SendRegisterOtpEmailAsync(
                    email,
                    otp,
                    user.UserName);

                // Chỉ ghi thời gian gửi khi gửi thành công
                HttpContext.Session.SetString(
                    "RegisterOtpLastSent",
                    DateTime.Now.ToString("O"));

                TempData["RegisterSuccess"] =
                    "Mã OTP mới đã được gửi đến email của bạn.";
            }
            catch
            {
                // Nếu gửi thất bại thì xóa OTP mới
                HttpContext.Session.Remove("RegisterOtp");
                HttpContext.Session.Remove("RegisterOtpExpiry");

                TempData["Error"] =
                    "Không thể gửi OTP. Vui lòng thử lại.";
            }

            return RedirectToPage(
                "./VerifyRegisterOtp",
                new
                {
                    returnUrl = ReturnUrl
                });
        }
    }
}