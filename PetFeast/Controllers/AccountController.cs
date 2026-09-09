using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
        public async Task<IActionResult> ForgotPassword(string account)
        {
            // ==============================
            // KIỂM TRA EMAIL
            // ==============================

            if (string.IsNullOrWhiteSpace(account))
            {
                ModelState.AddModelError(
                    "account",
                    "Vui lòng nhập email.");

                return View();
            }

            account = account.Trim();

            // ==============================
            // TÌM USER THEO EMAIL
            // ==============================

            var user =
                await _userManager.FindByEmailAsync(account);

            // ==============================
            // KHÔNG TÌM THẤY USER
            // ==============================

            if (user == null)
            {
                ModelState.AddModelError(
                    "account",
                    "Email không tồn tại trong hệ thống.");

                return View();
            }

            // ==============================
            // KIỂM TRA EMAIL
            // ==============================

            if (string.IsNullOrEmpty(user.Email))
            {
                ModelState.AddModelError(
                    "account",
                    "Tài khoản này chưa có email.");

                return View();
            }

            // ==============================
            // TẠO OTP
            // ==============================

            var otp = GenerateOtp();

            // ==============================
            // LƯU OTP VÀO SESSION
            // ==============================

            HttpContext.Session.SetString(
                "ResetOtp",
                otp);

            HttpContext.Session.SetString(
                "ResetUserId",
                user.Id);

            HttpContext.Session.SetString(
                "ResetOtpExpiry",
                DateTime.UtcNow
                    .AddMinutes(5)
                    .ToString("O"));

            // OTP chưa được xác thực
            HttpContext.Session.SetString(
                "ResetVerified",
                "false");

            // ==============================
            // GỬI OTP QUA EMAIL
            // ==============================

            try
            {
                await _emailService.SendResetOtpEmailAsync(
                    user.Email,
                    otp);
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    "Lỗi gửi OTP: " + ex.Message);

                // Xóa Session nếu gửi thất bại
                HttpContext.Session.Remove("ResetOtp");
                HttpContext.Session.Remove("ResetUserId");
                HttpContext.Session.Remove("ResetOtpExpiry");
                HttpContext.Session.Remove("ResetVerified");

                ModelState.AddModelError(
                    "",
                    "Không thể gửi email OTP. Vui lòng thử lại sau.");

                return View();
            }

            // ==============================
            // CHUYỂN SANG CONFIRMATION
            // ==============================

            return RedirectToAction(
                nameof(ForgotPasswordConfirmation));
        }


        // ==============================
        // XÁC NHẬN ĐÃ GỬI OTP
        // ==============================

        [AllowAnonymous]
        [HttpGet]
        public IActionResult ForgotPasswordConfirmation()
        {
            var userId =
                HttpContext.Session.GetString(
                    "ResetUserId");

            // Không có yêu cầu reset password
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction(
                    nameof(ForgotPassword));
            }

            return View();
        }


        // ==============================
        // NHẬP OTP - GET
        // ==============================

        [AllowAnonymous]
        [HttpGet]
        public IActionResult VerifyOtp()
        {
            var userId =
                HttpContext.Session.GetString(
                    "ResetUserId");

            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction(
                    nameof(ForgotPassword));
            }

            ViewBag.UserId = userId;

            return View();
        }


        // ==============================
        // GỬI LẠI OTP
        // ==============================

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResendOtp()
        {
            // ==============================
            // LẤY USER ID TỪ SESSION
            // ==============================

            var userId =
                HttpContext.Session.GetString(
                    "ResetUserId");

            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction(
                    nameof(ForgotPassword));
            }

            // ==============================
            // TÌM USER
            // ==============================

            var user =
                await _userManager.FindByIdAsync(userId);

            if (user == null ||
                string.IsNullOrEmpty(user.Email))
            {
                return RedirectToAction(
                    nameof(ForgotPassword));
            }

            // ==============================
            // TẠO OTP MỚI
            // ==============================

            var otp = GenerateOtp();

            // ==============================
            // LƯU OTP MỚI
            // ==============================

            HttpContext.Session.SetString(
                "ResetOtp",
                otp);

            HttpContext.Session.SetString(
                "ResetOtpExpiry",
                DateTime.UtcNow
                    .AddMinutes(5)
                    .ToString("O"));

            // OTP mới chưa xác thực
            HttpContext.Session.SetString(
                "ResetVerified",
                "false");

            // ==============================
            // GỬI EMAIL OTP MỚI
            // ==============================

            try
            {
                await _emailService.SendResetOtpEmailAsync(
                    user.Email,
                    otp);
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    "Lỗi gửi lại OTP: " + ex.Message);

                TempData["Error"] =
                    "Không thể gửi lại mã OTP. Vui lòng thử lại.";

                return RedirectToAction(
                    nameof(VerifyOtp));
            }

            // ==============================
            // THÀNH CÔNG
            // ==============================

            TempData["Success"] =
                "Mã OTP mới đã được gửi đến email của bạn.";

            // QUAN TRỌNG:
            // Vẫn ở trang nhập OTP
            return RedirectToAction(
                nameof(VerifyOtp));
        }


        // ==============================
        // KIỂM TRA OTP - POST
        // ==============================

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult VerifyOtp(
            string userId,
            string otp)
        {
            // ==============================
            // LẤY SESSION
            // ==============================

            var savedOtp =
                HttpContext.Session.GetString(
                    "ResetOtp");

            var savedUserId =
                HttpContext.Session.GetString(
                    "ResetUserId");

            var expiryString =
                HttpContext.Session.GetString(
                    "ResetOtpExpiry");

            // ==============================
            // KIỂM TRA SESSION
            // ==============================

            if (string.IsNullOrEmpty(savedOtp) ||
                string.IsNullOrEmpty(savedUserId) ||
                string.IsNullOrEmpty(expiryString))
            {
                ModelState.AddModelError(
                    "",
                    "Mã OTP đã hết hạn. Vui lòng yêu cầu mã mới.");

                ViewBag.UserId = userId;

                return View();
            }

            // ==============================
            // KIỂM TRA USER
            // ==============================

            if (savedUserId != userId)
            {
                ModelState.AddModelError(
                    "",
                    "Yêu cầu không hợp lệ.");

                ViewBag.UserId = userId;

                return View();
            }

            // ==============================
            // KIỂM TRA THỜI GIAN
            // ==============================

            if (!DateTime.TryParse(
                    expiryString,
                    out DateTime expiry))
            {
                ModelState.AddModelError(
                    "",
                    "Mã OTP không hợp lệ.");

                ViewBag.UserId = userId;

                return View();
            }

            if (DateTime.UtcNow > expiry)
            {
                ModelState.AddModelError(
                    "",
                    "Mã OTP đã hết hạn. Vui lòng yêu cầu mã mới.");

                ViewBag.UserId = userId;

                return View();
            }

            // ==============================
            // KIỂM TRA OTP
            // ==============================

            if (string.IsNullOrWhiteSpace(otp) ||
                otp.Trim() != savedOtp)
            {
                ModelState.AddModelError(
                    "",
                    "Mã OTP không chính xác.");

                ViewBag.UserId = userId;

                return View();
            }

            // ==============================
            // OTP CHÍNH XÁC
            // ==============================

            HttpContext.Session.SetString(
                "ResetVerified",
                "true");

            // OTP chỉ sử dụng một lần
            HttpContext.Session.Remove(
                "ResetOtp");

            // ==============================
            // CHUYỂN SANG ĐẶT MẬT KHẨU
            // ==============================

            return RedirectToAction(
                nameof(ResetPasswordAfterOtp),
                new
                {
                    userId = userId
                });
        }


        // ==============================
        // TRANG ĐẶT MẬT KHẨU MỚI
        // ==============================

        [AllowAnonymous]
        [HttpGet]
        public IActionResult ResetPasswordAfterOtp(
            string userId)
        {
            var verified =
                HttpContext.Session.GetString(
                    "ResetVerified");

            var sessionUserId =
                HttpContext.Session.GetString(
                    "ResetUserId");

            // ==============================
            // KIỂM TRA ĐÃ XÁC THỰC OTP
            // ==============================

            if (verified != "true" ||
                string.IsNullOrEmpty(sessionUserId) ||
                sessionUserId != userId)
            {
                return RedirectToAction(
                    nameof(ForgotPassword));
            }

            ViewBag.UserId = userId;

            return View("ResetPassword");
        }


        // ==============================
        // ĐẶT LẠI MẬT KHẨU
        // ==============================

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(
            string userId,
            string newPassword,
            string confirmPassword)
        {
            // ==============================
            // KIỂM TRA OTP ĐÃ XÁC THỰC
            // ==============================

            var verified =
                HttpContext.Session.GetString(
                    "ResetVerified");

            var sessionUserId =
                HttpContext.Session.GetString(
                    "ResetUserId");

            if (verified != "true" ||
                string.IsNullOrEmpty(sessionUserId) ||
                sessionUserId != userId)
            {
                return RedirectToAction(
                    nameof(ForgotPassword));
            }

            // ==============================
            // KIỂM TRA PASSWORD
            // ==============================

            if (string.IsNullOrWhiteSpace(newPassword) ||
                string.IsNullOrWhiteSpace(confirmPassword))
            {
                ModelState.AddModelError(
                    "",
                    "Vui lòng nhập đầy đủ thông tin.");

                ViewBag.UserId = userId;

                return View("ResetPassword");
            }

            // ==============================
            // KIỂM TRA PASSWORD KHỚP
            // ==============================

            if (newPassword != confirmPassword)
            {
                ModelState.AddModelError(
                    "",
                    "Mật khẩu xác nhận không khớp.");

                ViewBag.UserId = userId;

                return View("ResetPassword");
            }

            // ==============================
            // TÌM USER
            // ==============================

            var user =
                await _userManager.FindByIdAsync(userId);

            if (user == null)
            {
                return RedirectToAction(
                    nameof(ForgotPassword));
            }

            // ==============================
            // TẠO IDENTITY RESET TOKEN
            // ==============================

            var resetToken =
                await _userManager
                    .GeneratePasswordResetTokenAsync(user);

            // ==============================
            // RESET PASSWORD
            // ==============================

            var result =
                await _userManager.ResetPasswordAsync(
                    user,
                    resetToken,
                    newPassword);

            // ==============================
            // THÀNH CÔNG
            // ==============================

            if (result.Succeeded)
            {
                // Xóa toàn bộ Session reset password

                HttpContext.Session.Remove(
                    "ResetOtp");

                HttpContext.Session.Remove(
                    "ResetUserId");

                HttpContext.Session.Remove(
                    "ResetOtpExpiry");

                HttpContext.Session.Remove(
                    "ResetVerified");

                TempData["Success"] =
                    "Đặt lại mật khẩu thành công. Bạn có thể đăng nhập bằng mật khẩu mới.";

                // ==============================
                // CHUYỂN ĐẾN LOGIN
                // ==============================

                return RedirectToPage(
                    "/Account/Login",
                    new
                    {
                        area = "Identity"
                    });
            }

            // ==============================
            // LỖI IDENTITY
            // ==============================

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(
                    "",
                    error.Description);
            }

            ViewBag.UserId = userId;

            return View("ResetPassword");
        }


        // ==============================
        // TẠO OTP
        // ==============================

        private string GenerateOtp()
        {
            return Random.Shared
                .Next(100000, 1000000)
                .ToString();
        }
    }
    }
