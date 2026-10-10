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

        public PasswordResetService(
            CampusPulseDbContext dbContext,
            IPasswordService passwordService)
        {
            _dbContext = dbContext;
            _passwordService = passwordService;
        }

        public async Task<string?> CreateResetTokenAsync(string email)
        {
            string normalizedEmail = email.Trim().ToLowerInvariant();

            var user = await _dbContext.Users
                .FirstOrDefaultAsync(u => u.Email == normalizedEmail);

            if (user == null)
            {
                return null;
            }

            // Generate a cryptographically secure random token.
            string token = Convert.ToHexString(
                RandomNumberGenerator.GetBytes(32));

            string tokenHash = HashToken(token);

            // Invalidate previous unused reset tokens.
            var previousTokens = await _dbContext.PasswordResetTokens
                .Where(t => t.UserId == user.UserId && t.UsedAt == null)
                .ToListAsync();

            DateTime now = DateTime.UtcNow;

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

            return token;
        }

        public async Task<bool> ResetPasswordAsync(
            string token,
            string newPassword)
        {
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
                    u.UserId == resetToken.UserId);

            if (user == null)
            {
                return false;
            }

            user.PasswordHash =
                _passwordService.HashPassword(newPassword);

            resetToken.UsedAt = now;

            // Invalidate all other unused reset tokens for this user.
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