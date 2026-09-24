using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PetFeast.Data;
using PetFeast.Models.Contacts;

namespace PetFeast.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminContactController : Controller
    {
        private readonly PetFeastDBContext _context;

        public AdminContactController(
            PetFeastDBContext context)
        {
            _context = context;
        }

        // =========================
        // DANH SÁCH LIÊN HỆ
        // =========================
        public async Task<IActionResult> Index(
            string? keyword = null,
            string? status = null)
        {
            IQueryable<Contact> query = _context.Contacts
                .AsNoTracking();

            // =========================
            // LỌC TRẠNG THÁI
            // =========================
            if (status == "unread")
            {
                query = query.Where(c => !c.IsRead);
            }
            else if (status == "read")
            {
                query = query.Where(c => c.IsRead);
            }

            // =========================
            // TÌM KIẾM
            // =========================
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                keyword = keyword.Trim();

                query = query.Where(c =>
                    (c.Name != null &&
                     c.Name.Contains(keyword))

                    ||

                    (c.Email != null &&
                     c.Email.Contains(keyword))

                    ||

                    (c.Phone != null &&
                     c.Phone.Contains(keyword))
                );
            }

            ViewBag.Status = status;
            ViewBag.Keyword = keyword;

            ViewBag.UnreadCount = await _context.Contacts
                .CountAsync(c => !c.IsRead);

            var contacts = await query
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            return View(
                "~/Views/Admin/AdminContact/Index.cshtml",
                contacts
            );
        }

        // =========================
        // CHI TIẾT LIÊN HỆ
        // =========================
        public async Task<IActionResult> Details(int id)
        {
            var contact = await _context.Contacts
                .FirstOrDefaultAsync(c => c.Id == id);

            if (contact == null)
            {
                return NotFound();
            }

            if (!contact.IsRead)
            {
                contact.IsRead = true;
                await _context.SaveChangesAsync();
            }

            return View(
                "~/Views/Admin/AdminContact/Details.cshtml",
                contact
            );
        }

        // =========================
        // ĐÁNH DẤU CHƯA ĐỌC
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkUnread(int id)
        {
            var contact = await _context.Contacts
                .FirstOrDefaultAsync(c => c.Id == id);
            if (contact == null)
            {
                return NotFound();
            }
            contact.IsRead = false;
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // =========================
        // XÓA LIÊN HỆ
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var contact = await _context.Contacts
                .FirstOrDefaultAsync(c => c.Id == id);
            if (contact == null)
            {
                return NotFound();
            }
            _context.Contacts.Remove(contact);
            await _context.SaveChangesAsync();
            TempData["Success"] =
                "Xóa liên hệ thành công.";

            return RedirectToAction(nameof(Index));
        }
    }
}
