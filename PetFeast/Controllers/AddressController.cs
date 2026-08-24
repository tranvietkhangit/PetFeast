using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PetFeast.Data;
using PetFeast.Models.Identity;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace PetFeast.Controllers
{
    [Authorize]
    public class AddressController : Controller
    {
        private readonly PetFeastDBContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public AddressController(
            PetFeastDBContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // ==============================
        // DANH SÁCH ĐỊA CHỈ
        // ==============================

        [HttpGet]
        public async Task<IActionResult> Index(bool fromCheckout = false)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return Challenge();

            var addresses = await _context.UserAddresses
                .Where(x => x.UserId == user.Id)
                .OrderByDescending(x => x.IsDefault)
                .ThenByDescending(x => x.UserAddressId)
                .ToListAsync();

            ViewBag.FromCheckout = fromCheckout;

            return View(addresses);
        }


        // ==============================
        // THÊM ĐỊA CHỈ
        // ==============================

        [HttpGet]
        public IActionResult Create()
        {
            return View(new UserAddress());
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(UserAddress address)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return Challenge();

            if (!ModelState.IsValid)
                return View(address);

            address.UserId = user.Id;

            var hasAddress = await _context.UserAddresses
                .AnyAsync(x => x.UserId == user.Id);

            // Địa chỉ đầu tiên luôn là mặc định
            if (!hasAddress)
            {
                address.IsDefault = true;
            }

            // Nếu chọn làm mặc định
            if (address.IsDefault)
            {
                var oldDefaults = await _context.UserAddresses
                    .Where(x =>
                        x.UserId == user.Id &&
                        x.IsDefault)
                    .ToListAsync();

                foreach (var oldAddress in oldDefaults)
                {
                    oldAddress.IsDefault = false;
                }
            }

            _context.UserAddresses.Add(address);

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Thêm địa chỉ thành công.";

            return RedirectToAction(nameof(Index));
        }


        // ==============================
        // CHỌN ĐỊA CHỈ CHO CHECKOUT
        // ==============================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SelectForCheckout(int id)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return Challenge();

            var addresses = await _context.UserAddresses
                .Where(x => x.UserId == user.Id)
                .ToListAsync();

            var selectedAddress = addresses
                .FirstOrDefault(x => x.UserAddressId == id);

            if (selectedAddress == null)
                return NotFound();

            // Bỏ mặc định cũ
            foreach (var address in addresses)
            {
                address.IsDefault =
                    address.UserAddressId == id;
            }

            await _context.SaveChangesAsync();

            return RedirectToAction(
                "CheckOut",
                "Order");
        }


        // ==============================
        // ĐẶT MẶC ĐỊNH
        // ==============================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetDefault(int id)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return Challenge();

            var addresses = await _context.UserAddresses
                .Where(x => x.UserId == user.Id)
                .ToListAsync();

            var selectedAddress = addresses
                .FirstOrDefault(x => x.UserAddressId == id);

            if (selectedAddress == null)
                return NotFound();

            foreach (var address in addresses)
            {
                address.IsDefault =
                    address.UserAddressId == id;
            }

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Đã chọn địa chỉ này làm mặc định.";

            return RedirectToAction(nameof(Index));
        }


        // ==============================
        // XÓA
        // ==============================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return Challenge();

            var address = await _context.UserAddresses
                .FirstOrDefaultAsync(x =>
                    x.UserAddressId == id &&
                    x.UserId == user.Id);

            if (address == null)
                return NotFound();

            if (address.IsDefault)
            {
                TempData["Error"] =
                    "Không thể xóa địa chỉ mặc định. " +
                    "Vui lòng chọn địa chỉ khác làm mặc định trước.";

                return RedirectToAction(nameof(Index));
            }

            _context.UserAddresses.Remove(address);

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Đã xóa địa chỉ.";

            return RedirectToAction(nameof(Index));
        }


        // ==============================
        // CHỈNH SỬA
        // ==============================

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return Challenge();

            var address = await _context.UserAddresses
                .FirstOrDefaultAsync(x =>
                    x.UserAddressId == id &&
                    x.UserId == user.Id);

            if (address == null)
                return NotFound();

            return View(address);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            UserAddress address)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return Challenge();

            var existingAddress =
                await _context.UserAddresses
                    .FirstOrDefaultAsync(x =>
                        x.UserAddressId == id &&
                        x.UserId == user.Id);

            if (existingAddress == null)
                return NotFound();

            if (!ModelState.IsValid)
            {
                address.UserAddressId = id;
                return View(address);
            }

            existingAddress.ReceiverName =
                address.ReceiverName;

            existingAddress.PhoneNumber =
                address.PhoneNumber;

            existingAddress.City =
                address.City;

            existingAddress.Ward =
                address.Ward;

            existingAddress.AddressDetail =
                address.AddressDetail;

            // Nếu chọn làm mặc định
            if (address.IsDefault)
            {
                var addresses = await _context.UserAddresses
                    .Where(x => x.UserId == user.Id)
                    .ToListAsync();

                foreach (var item in addresses)
                {
                    item.IsDefault =
                        item.UserAddressId == id;
                }
            }

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Cập nhật địa chỉ thành công.";

            return RedirectToAction(nameof(Index));
        }
    }
}
