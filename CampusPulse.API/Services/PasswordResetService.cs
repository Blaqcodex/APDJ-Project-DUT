
using System.Security.Cryptography;
using System.Text;
using CampusPulse.Application.Interfaces;
using CampusPulse.Infrastructure.Data;
using CampusPulse.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CampusPulse.API.Services
{
    public class PasswordResetService
    {
        private readonly CampusPulseDbContext _dbContext;
        private readonly IPasswordService _passwordService;
        private readonly BrevoEmailService _emailService;

        public PasswordResetService(
            CampusPulseDbContext dbContext,
            IPasswordService passwordService,
            BrevoEmailService emailService)
        {
            _dbContext = dbContext;
            _passwordService = passwordService;
            _emailService = emailService;
        }

        public async Task<bool> SendResetEmailAsync(string email)
        {
            string normalizedEmail = email.Trim().ToLowerInvariant();

            var user = await _dbContext.Users
                .FirstOrDefaultAsync(u =>
                    u.Email == normalizedEmail &&
                    u.RecoveryEmailVerified &&
                    u.RecoveryEmail != null &&
                    u.RecoveryEmail != "");

            if (user == null)
            {
                return false;
            }

            string token = Convert.ToHexString(
                RandomNumberGenerator.GetBytes(32));

            string tokenHash = HashToken(token);
            DateTime now = DateTime.UtcNow;

            var previousTokens = await _dbContext.PasswordResetTokens
                .Where(t =>
                    t.UserId == user.UserId &&
                    t.UsedAt == null)
                .ToListAsync();

            foreach (var previousToken in previousTokens)
            {
                previousToken.UsedAt = now;
            }

            var resetToken = new PasswordResetToken
            {
                UserId = user.UserId,
                TokenHash = tokenHash,
                CreatedAt = now,
                ExpiresAt = now.AddMinutes(15)
            };

            _dbContext.PasswordResetTokens.Add(resetToken);
            await _dbContext.SaveChangesAsync();

            string message =
                "Hello " + user.FirstName + "," +
                "\n\nWe received a request to reset your CampusPulse password." +
                "\n\nYour password reset token is:" +
                "\n\n" + token +
                "\n\nThis token expires in 15 minutes." +
                "\n\nOpen CampusPulse and enter this token on the Reset Password screen." +
                "\n\nIf you did not request this, ignore this email." +
                "\n\nCampusPulse Team";

            await _emailService.SendEmailAsync(
                user.RecoveryEmail!,
                "CampusPulse - Password Reset",
                message);

            return true;
        }

        public async Task<bool> ResetPasswordAsync(
            string token,
            string newPassword)
        {
            if (string.IsNullOrWhiteSpace(token) ||
                string.IsNullOrWhiteSpace(newPassword))
            {
                return false;
            }

            string tokenHash = HashToken(token);
            DateTime now = DateTime.UtcNow;

            var resetToken = await _dbContext.PasswordResetTokens
                .FirstOrDefaultAsync(t =>
                    t.TokenHash == tokenHash &&
                    t.UsedAt == null &&
                    t.ExpiresAt > now);

            if (resetToken == null)
            {
                return false;
            }

            var user = await _dbContext.Users
                .FirstOrDefaultAsync(u =>
                    u.UserId == resetToken.UserId &&
                    u.RecoveryEmailVerified);

            if (user == null)
            {
                return false;
            }

            user.PasswordHash =
                _passwordService.HashPassword(newPassword);

            resetToken.UsedAt = now;

            var otherTokens = await _dbContext.PasswordResetTokens
                .Where(t =>
                    t.UserId == user.UserId &&
                    t.PasswordResetTokenId != resetToken.PasswordResetTokenId &&
                    t.UsedAt == null)
                .ToListAsync();

            foreach (var otherToken in otherTokens)
            {
                otherToken.UsedAt = now;
            }

            await _dbContext.SaveChangesAsync();

            return true;
        }

        private static string HashToken(string token)
        {
            byte[] bytes = SHA256.HashData(
                Encoding.UTF8.GetBytes(token));

            return Convert.ToHexString(bytes);
        }
    }
}
