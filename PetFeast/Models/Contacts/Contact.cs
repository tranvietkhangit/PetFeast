namespace PetFeast.Models.Contacts
{
    public class Contact
    {
        public int Id { get; set; }

        public string? Name { get; set; }

        public string? Email { get; set; }

        public string? Phone { get; set; }

        public string? Message { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public bool IsRead { get; set; } = false;
    }
}
