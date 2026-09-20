using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using Microsoft.EntityFrameworkCore;
using PetFeast.Data;

namespace PetFeast.ViewComponents
{
    public class UnreadContactCountViewComponent : ViewComponent
    {
        private readonly PetFeastDBContext _context;
        public UnreadContactCountViewComponent(
            PetFeastDBContext context)
        {
            _context = context;
        }
        public async Task<IViewComponentResult> InvokeAsync()
        {
            int unreadCount = await _context.Contacts
                .CountAsync(c => !c.IsRead);

            if (unreadCount <= 0)
            {
                return new HtmlContentViewComponentResult(
                    new HtmlString(string.Empty)
                );
            }
            string displayCount = unreadCount > 99
                ? "99+"
                : unreadCount.ToString();
            string html = $@"
                <span class=""notification-badge"">
                    {displayCount}
                </span>";
            return new HtmlContentViewComponentResult(
                new HtmlString(html)
            );
        }
    }
}