using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CampusPulse.Application.Interfaces;
using CampusPulse.Domain.Entities;
using CampusPulse.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace CampusPulse.API.Services
{
    internal static class AdminProvisioning
    {
        public static async Task<int> RunAsync(
            IServiceProvider services,
            IHostEnvironment environment)
        {
            if (!environment.IsDevelopment())
            {
                Console.Error.WriteLine(
                    "Admin provisioning is available only in Development.");
                return 1;
            }

            Console.WriteLine("CampusPulse local administrator provisioning");
            Console.WriteLine("This operation creates one new Admin account.");

            Console.Write("First name: ");
            string firstName = (Console.ReadLine() ?? "").Trim();

            Console.Write("Last name: ");
            string lastName = (Console.ReadLine() ?? "").Trim();

            Console.Write("Email: ");
            string email = (Console.ReadLine() ?? "")
                .Trim()
                .ToLowerInvariant();

            if (string.IsNullOrWhiteSpace(firstName) ||
                string.IsNullOrWhiteSpace(lastName) ||
                firstName.Length > 100 ||
                lastName.Length > 100 ||
                email.Length > 254 ||
                !Regex.IsMatch(
                    email,
                    @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            {
                Console.Error.WriteLine(
                    "Invalid administrator name or email.");
                return 1;
            }

            using IServiceScope scope = services.CreateScope();

            var db = scope.ServiceProvider
                .GetRequiredService<CampusPulseDbContext>();

            var passwordService = scope.ServiceProvider
                .GetRequiredService<IPasswordService>();

            bool exists = await db.Users.AnyAsync(
                user => user.Email.ToLower() == email);

            if (exists)
            {
                Console.Error.WriteLine(
                    "An account with this email already exists.");
                return 1;
            }

            Console.Write("Password: ");
            string password = ReadPassword();

            Console.Write("Confirm password: ");
            string confirmation = ReadPassword();

            if (password.Length < 12 ||
                password.Length > 128 ||
                password != confirmation)
            {
                Console.Error.WriteLine(
                    "Passwords must match and contain 12-128 characters.");
                return 1;
            }

            Console.Write(
                "Type CREATE ADMIN to confirm account creation: ");

            string confirmationText =
                (Console.ReadLine() ?? "").Trim();

            if (confirmationText != "CREATE ADMIN")
            {
                Console.WriteLine("Cancelled. No account created.");
                return 1;
            }

            var user = new User
            {
                FirstName = firstName,
                LastName = lastName,
                Email = email,
                PasswordHash = passwordService.HashPassword(password),
                Role = "Admin",
                CreatedAt = DateTime.UtcNow
            };

            db.Users.Add(user);
            await db.SaveChangesAsync();

            Console.WriteLine(
                "Administrator account created successfully.");

            return 0;
        }

        private static string ReadPassword()
        {
            var builder = new StringBuilder();

            while (true)
            {
                ConsoleKeyInfo key = Console.ReadKey(intercept: true);

                if (key.Key == ConsoleKey.Enter)
                {
                    Console.WriteLine();
                    return builder.ToString();
                }

                if (key.Key == ConsoleKey.Backspace)
                {
                    if (builder.Length > 0)
                    {
                        builder.Length--;
                    }

                    continue;
                }

                if (!char.IsControl(key.KeyChar) &&
                    builder.Length < 129)
                {
                    builder.Append(key.KeyChar);
                }
            }
        }
    }
}