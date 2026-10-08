// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using PetFeast.Models.Identity;


namespace PetFeast.Areas.Identity.Pages.Account
{
    public class LoginModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<LoginModel> _logger;

        public LoginModel(
    SignInManager<ApplicationUser> signInManager,
    UserManager<ApplicationUser> userManager,
    ILogger<LoginModel> logger)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _logger = logger;
        }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [BindProperty]
        public InputModel Input { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public string ReturnUrl { get; set; }
        public bool EmailNotConfirmed { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [TempData]
        public string ErrorMessage { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public class InputModel
        {
            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [Required]
            [EmailAddress]
            public string Email { get; set; }

            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [Required]
            [DataType(DataType.Password)]
            public string Password { get; set; }

            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [Display(Name = "Remember me?")]
            public bool RememberMe { get; set; }
        }

        public async Task OnGetAsync(string returnUrl = null)
        {
            if (!string.IsNullOrEmpty(ErrorMessage))
            {
                ModelState.AddModelError(string.Empty, ErrorMessage);
            }

            returnUrl ??= Url.Content("~/");

            // Clear the existing external cookie to ensure a clean login process
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            ReturnUrl = returnUrl;
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");

            ExternalLogins =
                (await _signInManager
                    .GetExternalAuthenticationSchemesAsync())
                .ToList();

            if (!ModelState.IsValid)
            {
                return Page();
            }

            var user = await _userManager.FindByEmailAsync(Input.Email);

            if (user == null)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Không tìm thấy tài khoản với email này.");

                return Page();
            }
            // EMAIL CHƯA ĐƯỢC XÁC NHẬN
            if (!user.EmailConfirmed)
            {
                var registerUserId =
                    HttpContext.Session.GetString("RegisterUserId");

                var registerEmail =
                    HttpContext.Session.GetString("RegisterEmail");

                // Nếu Session OTP vẫn thuộc tài khoản này
                if (registerUserId == user.Id &&
                    string.Equals(
                        registerEmail,
                        user.Email,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return RedirectToPage(
                        "./VerifyRegisterOtp",
                        new
                        {
                            returnUrl = returnUrl
                        });
                }

                // Session OTP đã mất hoặc không còn hợp lệ
                EmailNotConfirmed = true;

                ModelState.AddModelError(string.Empty, "Email của tài khoản này chưa được xác nhận.");

                return Page();
            }
            // ĐĂNG NHẬP

            var result = await _signInManager.PasswordSignInAsync(
                user,
                Input.Password,
                Input.RememberMe,
                lockoutOnFailure: true);
            // ĐĂNG NHẬP THÀNH CÔNG

            if (result.Succeeded)
            {
                if (await _userManager.IsInRoleAsync(user, "Admin"))
                {
                    return RedirectToAction(
                        "Dashboard",
                        "Admin");
                }

                return LocalRedirect(returnUrl);
            }
            // TÀI KHOẢN KHÔNG ĐƯỢC PHÉP LOGIN

            if (result.IsNotAllowed)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Tài khoản chưa được phép đăng nhập.");

                return Page();
            }
            // TÀI KHOẢN BỊ KHÓA

            if (result.IsLockedOut)
            {
                _logger.LogWarning(
                    "Tài khoản {Email} đã bị khóa do đăng nhập sai quá nhiều lần.",
                    Input.Email);

                ModelState.AddModelError(
                    string.Empty,
                    "Tài khoản đã bị tạm khóa do nhập sai mật khẩu quá nhiều lần. Vui lòng thử lại sau 5 phút.");

                return Page();
            }
            // TWO FACTOR

            if (result.RequiresTwoFactor)
            {
                return RedirectToPage(
                    "./LoginWith2fa",
                    new
                    {
                        ReturnUrl = returnUrl,
                        RememberMe = Input.RememberMe
                    });
            }
            // ĐĂNG NHẬP THẤT BẠI

            ModelState.AddModelError(
                string.Empty,
                "Email hoặc mật khẩu không chính xác.");

            return Page();
        }
    }
}
