using System.Data;
using System.Security.Cryptography;
using System.Text;
using CampusPulse.Application.Interfaces;
using CampusPulse.Domain.Entities;
using CampusPulse.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CampusPulse.API.Services
{
    public class PasswordResetService
    {
        private const int CodeLifetimeMinutes = 10;
        private const int MaximumAttempts = 5;
        private const int ResendCooldownSeconds = 60;

        private readonly CampusPulseDbContext _dbContext;
        private readonly IPasswordService _passwordService;
        private readonly BrevoEmailService _emailService;
        private readonly byte[] _hashKey;

        public PasswordResetService(
            CampusPulseDbContext dbContext,
            IPasswordService passwordService,
            BrevoEmailService emailService,
            IConfiguration configuration)
        {
            _dbContext = dbContext;
            _passwordService = passwordService;
            _emailService = emailService;

            string configuredKey = configuration["Verification:HashKey"]
                ?? throw new InvalidOperationException(
                    "Verification:HashKey is not configured.");

            _hashKey = Convert.FromBase64String(configuredKey);

            if (_hashKey.Length < 32)
            {
                throw new InvalidOperationException(
                    "Verification:HashKey must contain at least 32 bytes.");
            }
        }

        public async Task<bool> SendResetEmailAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return false;
            }

            string normalizedEmail = email.Trim().ToLowerInvariant();

            await using var transaction =
                await _dbContext.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable);

            var user = await _dbContext.Users
                .FirstOrDefaultAsync(u =>
                    u.Email == normalizedEmail &&
                    u.RecoveryEmailVerified &&
                    u.RecoveryEmail != null &&
                    u.RecoveryEmail != "");

            if (user == null)
            {
                await transaction.CommitAsync();
                return false;
            }

            DateTime now = DateTime.UtcNow;

            var latestToken = await _dbContext.PasswordResetTokens
                .Where(t => t.UserId == user.UserId)
                .OrderByDescending(t => t.CreatedAt)
                .ThenByDescending(t => t.PasswordResetTokenId)
                .FirstOrDefaultAsync();

            if (latestToken != null &&
                latestToken.CreatedAt > now.AddSeconds(-ResendCooldownSeconds))
            {
                await transaction.CommitAsync();
                return false;
            }

            string code = RandomNumberGenerator
                .GetInt32(0, 1000000)
                .ToString("D6");

            string codeHash = HashCode(user.UserId, normalizedEmail, code);

            var previousTokens = await _dbContext.PasswordResetTokens
                .Where(t => t.UserId == user.UserId && t.UsedAt == null)
                .ToListAsync();

            foreach (var previousToken in previousTokens)
            {
                previousToken.UsedAt = now;
            }

            var resetToken = new PasswordResetToken
            {
                UserId = user.UserId,
                TokenHash = codeHash,
                CreatedAt = now,
                ExpiresAt = now.AddMinutes(CodeLifetimeMinutes),
                FailedAttempts = 0
            };

            _dbContext.PasswordResetTokens.Add(resetToken);

            await _dbContext.SaveChangesAsync();
            await transaction.CommitAsync();

            string message =
                "Hello " + user.FirstName + "," +
                "\n\nWe received a request to reset your CampusPulse password." +
                "\n\nYour six-digit password reset code is:" +
                "\n\n" + code +
                "\n\nThis code expires in 10 minutes." +
                "\n\nEnter this code in CampusPulse to reset your password." +
                "\n\nIf you did not request this, ignore this email." +
                "\n\nCampusPulse Team";

            await _emailService.SendEmailAsync(
                user.RecoveryEmail!,
                "CampusPulse - Password Reset Code",
                message);

            return true;
        }

        public async Task<bool> ResetPasswordAsync(
            string email,
            string code,
            string newPassword)
        {
            if (string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(code) ||
                string.IsNullOrWhiteSpace(newPassword) ||
                code.Length != 6 ||
                !code.All(char.IsAsciiDigit))
            {
                return false;
            }

            string normalizedEmail = email.Trim().ToLowerInvariant();

            await using var transaction =
                await _dbContext.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable);

            var user = await _dbContext.Users
                .FirstOrDefaultAsync(u =>
                    u.Email == normalizedEmail &&
                    u.RecoveryEmailVerified);

            if (user == null)
            {
                return false;
            }

            DateTime now = DateTime.UtcNow;

            var resetToken = await _dbContext.PasswordResetTokens
                .Where(t =>
                    t.UserId == user.UserId &&
                    t.UsedAt == null &&
                    t.ExpiresAt > now)
                .OrderByDescending(t => t.CreatedAt)
                .ThenByDescending(t => t.PasswordResetTokenId)
                .FirstOrDefaultAsync();

            if (resetToken == null ||
                resetToken.FailedAttempts >= MaximumAttempts)
            {
                return false;
            }

            string submittedHash = HashCode(
                user.UserId,
                normalizedEmail,
                code);

            byte[] expectedBytes =
                Convert.FromHexString(resetToken.TokenHash);

            byte[] submittedBytes =
                Convert.FromHexString(submittedHash);

            if (!CryptographicOperations.FixedTimeEquals(
                expectedBytes,
                submittedBytes))
            {
                resetToken.FailedAttempts++;

                if (resetToken.FailedAttempts >= MaximumAttempts)
                {
                    resetToken.UsedAt = now;
                }

                await _dbContext.SaveChangesAsync();
                await transaction.CommitAsync();

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
            await transaction.CommitAsync();

            return true;
        }

        private string HashCode(int userId, string email, string code)
        {
            string data =
                "password-reset:" +
                userId + ":" +
                email.Trim().ToLowerInvariant() + ":" +
                code;

            using var hmac = new HMACSHA256(_hashKey);

            return Convert.ToHexString(
                hmac.ComputeHash(Encoding.UTF8.GetBytes(data)));
        }
    }
}
