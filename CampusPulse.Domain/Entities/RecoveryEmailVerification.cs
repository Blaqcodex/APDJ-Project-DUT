
using System;

namespace CampusPulse.Domain.Entities
{
    public class RecoveryEmailVerification
    {
        public int RecoveryEmailVerificationId { get; set; }

        public int UserId { get; set; }

        public string Email { get; set; } = "";

        public string CodeHash { get; set; } = "";

        public DateTime ExpiresAtUtc { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        public int FailedAttempts { get; set; } = 0;

        public bool IsUsed { get; set; } = false;
    }
}
