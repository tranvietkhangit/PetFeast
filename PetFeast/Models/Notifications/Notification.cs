using PetFeast.Models.Identity;
using PetFeast.Models.Orders;

namespace PetFeast.Models.Notifications
{
    public class Notification
    {
        public int Id { get; set; }

        public string UserId { get; set; } = string.Empty;

        public string Title { get; set; } = string.Empty;

        public string Message { get; set; } = string.Empty;

        public int? OrderId { get; set; }

        public bool IsRead { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public ApplicationUser? User { get; set; }

        public Order? Order { get; set; }
    }
}
