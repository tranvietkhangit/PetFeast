#nullable disable

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using PetFeast.Models.Identity;
using PetFeast.Models.Services;

namespace PetFeast.Areas.Identity.Pages.Account
{
    public class RegisterModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IUserStore<ApplicationUser> _userStore;
        private readonly IUserEmailStore<ApplicationUser> _emailStore;
        private readonly ILogger<RegisterModel> _logger;
        private readonly EmailService _emailService;

        public RegisterModel(
            UserManager<ApplicationUser> userManager,
            IUserStore<ApplicationUser> userStore,
            SignInManager<ApplicationUser> signInManager,
            ILogger<RegisterModel> logger,
            EmailService emailService)
        {
            _userManager = userManager;
            _userStore = userStore;
            _emailStore = GetEmailStore();
            _signInManager = signInManager;
            _logger = logger;
            _emailService = emailService;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public string ReturnUrl { get; set; }

        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        // ==========================================
        // INPUT MODEL
        // ==========================================

        public class InputModel
        {
            [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập.")]
            [StringLength(
                50,
                MinimumLength = 3,
                ErrorMessage = "Tên đăng nhập phải từ {2} đến {1} ký tự.")]
            [Display(Name = "Tên đăng nhập")]
            public string UserName { get; set; }

            [Required(ErrorMessage = "Vui lòng nhập email.")]
            [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
            [Display(Name = "Email")]
            public string Email { get; set; }

            [Required(ErrorMessage = "Vui lòng nhập mật khẩu.")]
            [StringLength(
                100,
                ErrorMessage = "Mật khẩu phải có ít nhất {2} ký tự.",
                MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "Mật khẩu")]
            public string Password { get; set; }

            [Required(ErrorMessage = "Vui lòng nhập lại mật khẩu.")]
            [DataType(DataType.Password)]
            [Display(Name = "Nhập lại mật khẩu")]
            [Compare(
                "Password",
                ErrorMessage = "Mật khẩu và xác nhận mật khẩu không khớp.")]
            public string ConfirmPassword { get; set; }
        }

        // ==========================================
        // GET REGISTER
        // ==========================================

        public async Task OnGetAsync(string returnUrl = null)
        {
            ReturnUrl = returnUrl;

            ExternalLogins =
                (await _signInManager
                    .GetExternalAuthenticationSchemesAsync())
                .ToList();
        }

        // ==========================================
        // POST REGISTER
        // ==========================================

        public async Task<IActionResult> OnPostAsync(
            string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");

            ReturnUrl = returnUrl;

            ExternalLogins =
                (await _signInManager
                    .GetExternalAuthenticationSchemesAsync())
                .ToList();

            // ==========================================
            // VALIDATE FORM
            // ==========================================

            if (!ModelState.IsValid)
            {
                return Page();
            }

            // ==========================================
            // KIỂM TRA USERNAME
            // ==========================================

            var existingUser =
                await _userManager.FindByNameAsync(
                    Input.UserName);

            if (existingUser != null)
            {
                ModelState.AddModelError(
                    "Input.UserName",
                    "Tên đăng nhập này đã được sử dụng.");

                return Page();
            }

            // ==========================================
            // KIỂM TRA EMAIL
            // ==========================================

            var existingEmail =
                await _userManager.FindByEmailAsync(
                    Input.Email);

            if (existingEmail != null)
            {
                ModelState.AddModelError(
                    "Input.Email",
                    "Email này đã được đăng ký.");

                return Page();
            }

            // ==========================================
            // TẠO USER
            // ==========================================

            var user = CreateUser();
            user.FullName = Input.UserName.Trim();
            await _userStore.SetUserNameAsync(
                user,
                Input.UserName,
                CancellationToken.None);

            await _emailStore.SetEmailAsync(
                user,
                Input.Email,
                CancellationToken.None);

            // Chưa xác nhận email
            user.EmailConfirmed = false;

            var result =
                await _userManager.CreateAsync(
                    user,
                    Input.Password);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        error.Description);
                }

                return Page();
            }

            _logger.LogInformation(
                "User created a new account. Waiting for OTP verification.");

            // ==========================================
            // TẠO OTP 6 SỐ
            // ==========================================

            string otp =
                RandomNumberGenerator
                    .GetInt32(100000, 1000000)
                    .ToString();

            // OTP có hiệu lực 5 phút
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
                Input.Email);

            HttpContext.Session.SetString(
                "RegisterOtpExpiry",
                expiry.ToString("O"));

            // ==========================================
            // GỬI OTP QUA EMAIL
            // ==========================================

            try
            {
                await _emailService.SendRegisterOtpEmailAsync(
                    Input.Email,
                    otp,
                    Input.UserName);

                // Lưu thời gian gửi OTP
                HttpContext.Session.SetString(
                    "RegisterOtpLastSent",
                    DateTime.Now.ToString("O"));
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Không thể gửi OTP đăng ký đến {Email}",
                    Input.Email);

                // Xóa tài khoản nếu gửi OTP thất bại
                await _userManager.DeleteAsync(user);

                // Xóa Session OTP
                HttpContext.Session.Remove("RegisterOtp");
                HttpContext.Session.Remove("RegisterUserId");
                HttpContext.Session.Remove("RegisterEmail");
                HttpContext.Session.Remove("RegisterOtpExpiry");
                HttpContext.Session.Remove("RegisterOtpLastSent");

                ModelState.AddModelError(
                    string.Empty,
                    "Không thể gửi mã OTP. Vui lòng thử lại.");

                return Page();
            }

            // ==========================================
            // CHUYỂN SANG TRANG OTP
            // ==========================================

            return RedirectToPage(
                "./VerifyRegisterOtp",
                new
                {
                    returnUrl = returnUrl
                });
        }

        // ==========================================
        // CREATE USER
        // ==========================================

        private ApplicationUser CreateUser()
        {
            try
            {
                return Activator.CreateInstance<ApplicationUser>();
            }
            catch
            {
                throw new InvalidOperationException(
                    $"Can't create an instance of '{nameof(ApplicationUser)}'. " +
                    $"Ensure that '{nameof(ApplicationUser)}' is not an abstract class " +
                    $"and has a parameterless constructor.");
            }
        }

        // ==========================================
        // EMAIL STORE
        // ==========================================

        private IUserEmailStore<ApplicationUser> GetEmailStore()
        {
            if (!_userManager.SupportsUserEmail)
            {
                throw new NotSupportedException(
                    "The default UI requires a user store with email support.");
            }

            return (IUserEmailStore<ApplicationUser>)_userStore;
        }
    }
}