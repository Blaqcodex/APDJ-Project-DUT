
using System;
using System.Data;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using CampusPulse.Domain.Entities;
using CampusPulse.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace CampusPulse.API.Services
{
    public class RecoveryEmailVerificationService
    {
        private readonly CampusPulseDbContext _context;
        private readonly BrevoEmailService _emailService;
        private readonly byte[] _hashKey;

        private const int CodeLifetimeMinutes = 10;
        private const int MaximumAttempts = 5;
        private const int ResendCooldownSeconds = 60;

        public RecoveryEmailVerificationService(
            CampusPulseDbContext context,
            BrevoEmailService emailService,
            IConfiguration configuration)
        {
            _context = context;
            _emailService = emailService;

            string secret = configuration["Verification:HashKey"]
                ?? throw new InvalidOperationException(
                    "Verification hash key is not configured.");

            _hashKey = Convert.FromBase64String(secret);

            if (_hashKey.Length < 32)
            {
                throw new InvalidOperationException(
                    "Verification hash key must be at least 32 bytes.");
            }
        }

        public async Task<bool> SendCodeAsync(int userId)
        {
            string? recipientEmail = null;
            string? firstName = null;
            string? code = null;

            await using (var transaction =
                await _context.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable))
            {
                User? user = await _context.Users
                    .FirstOrDefaultAsync(u => u.UserId == userId);

                if (user == null ||
                    string.IsNullOrWhiteSpace(user.RecoveryEmail) ||
                    user.RecoveryEmailVerified)
                {
                    return false;
                }

                DateTime now = DateTime.UtcNow;

                RecoveryEmailVerification? latest =
                    await _context.RecoveryEmailVerifications
                        .Where(v => v.UserId == userId)
                        .OrderByDescending(v => v.CreatedAtUtc)
                        .FirstOrDefaultAsync();

                if (latest != null &&
                    latest.CreatedAtUtc.AddSeconds(
                        ResendCooldownSeconds) > now)
                {
                    return false;
                }

                code = RandomNumberGenerator
                    .GetInt32(0, 1000000)
                    .ToString("D6");

                recipientEmail =
                    user.RecoveryEmail.Trim().ToLowerInvariant();

                firstName = user.FirstName;

                await _context.RecoveryEmailVerifications
                    .Where(v => v.UserId == userId && !v.IsUsed)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(v => v.IsUsed, true));

                _context.RecoveryEmailVerifications.Add(
                    new RecoveryEmailVerification
                    {
                        UserId = userId,
                        Email = recipientEmail,
                        CodeHash = HashCode(
                            userId,
                            recipientEmail,
                            code),
                        CreatedAtUtc = now,
                        ExpiresAtUtc = now.AddMinutes(
                            CodeLifetimeMinutes),
                        FailedAttempts = 0,
                        IsUsed = false
                    });

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }

            string message =
                "Hello " + firstName + "," +
                "\n\nYour CampusPulse recovery email verification code is: " +
                code +
                "\n\nThis code expires in 10 minutes." +
                "\nIf you did not request this code, you can ignore this email." +
                "\n\nCampusPulse Team";

            await _emailService.SendEmailAsync(
                recipientEmail!,
                "CampusPulse - Verify Your Recovery Email",
                message);

            return true;
        }

        public async Task<bool> VerifyCodeAsync(
            int userId,
            string code)
        {
            if (string.IsNullOrWhiteSpace(code) ||
                code.Length != 6 ||
                !code.All(char.IsAsciiDigit))
            {
                return false;
            }

            await using var transaction =
                await _context.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable);

            User? user = await _context.Users
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null ||
                string.IsNullOrWhiteSpace(user.RecoveryEmail) ||
                user.RecoveryEmailVerified)
            {
                return false;
            }

            RecoveryEmailVerification? verification =
                await _context.RecoveryEmailVerifications
                    .Where(v =>
                        v.UserId == userId &&
                        !v.IsUsed)
                    .OrderByDescending(v => v.CreatedAtUtc)
                    .FirstOrDefaultAsync();

            if (verification == null ||
                verification.ExpiresAtUtc <= DateTime.UtcNow ||
                verification.FailedAttempts >= MaximumAttempts ||
                !string.Equals(
                    verification.Email,
                    user.RecoveryEmail.Trim(),
                    StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            string suppliedHash = HashCode(
                userId,
                verification.Email,
                code);

            bool matches =
                CryptographicOperations.FixedTimeEquals(
                    Convert.FromHexString(verification.CodeHash),
                    Convert.FromHexString(suppliedHash));

            if (!matches)
            {
                verification.FailedAttempts++;

                if (verification.FailedAttempts >= MaximumAttempts)
                {
                    verification.IsUsed = true;
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return false;
            }

            verification.IsUsed = true;
            user.RecoveryEmailVerified = true;

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return true;
        }

        private string HashCode(
            int userId,
            string email,
            string code)
        {
            string value =
                userId + ":" +
                email.Trim().ToLowerInvariant() + ":" +
                code;

            byte[] hash = HMACSHA256.HashData(
                _hashKey,
                Encoding.UTF8.GetBytes(value));

            return Convert.ToHexString(hash);
        }
    }
}
