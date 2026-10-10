using System;

namespace CampusPulse.Domain.Entities
{
    public class User
    {
        public int UserId { get; set; }

        public string FirstName { get; set; } = "";

        public string LastName { get; set; } = "";

        // DUT email used for login.
        public string Email { get; set; } = "";

        // Personal email used for account recovery.
        public string RecoveryEmail { get; set; }

        // Recovery is disabled until email ownership is verified.
        public bool RecoveryEmailVerified { get; set; } = false;

        public string PasswordHash { get; set; } = "";

        public string Role { get; set; } = "";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}