using System;

namespace CampusPulse.Domain.Entities
{
    public class User
    {
        public int UserId { get; set; }

        public string FirstName { get; set; } = "";

        public string LastName { get; set; } = "";

        public string Email { get; set; } = "";

        public string PasswordHash { get; set; } = "";

        public string Role { get; set; } = "";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}