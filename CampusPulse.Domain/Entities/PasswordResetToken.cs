using System;

namespace CampusPulse.Domain.Entities
{
    public class PasswordResetToken
    {
        public int PasswordResetTokenId { get; set; }

        public int UserId { get; set; }

        public string TokenHash { get; set; } = "";

        public DateTime ExpiresAt { get; set; }

        public DateTime? UsedAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}