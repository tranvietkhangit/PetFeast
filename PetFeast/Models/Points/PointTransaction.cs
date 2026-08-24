using PetFeast.Models.Identity;
using System.ComponentModel.DataAnnotations;

using PetFeast.Models.Orders;

namespace PetFeast.Models.Points
{
    public class PointTransaction
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        public int Points { get; set; }

        [Required]
        public string Type { get; set; } = string.Empty;

        public string? Description { get; set; }

        public int? OrderId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;


        public ApplicationUser User { get; set; } = null!;

        public Order? Order { get; set; }
    }
}
