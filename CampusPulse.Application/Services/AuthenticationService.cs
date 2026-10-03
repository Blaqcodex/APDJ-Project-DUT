using CampusPulse.Application.Interfaces;
using CampusPulse.Domain.Entities;

namespace CampusPulse.Application.Services
{
    public class AuthenticationService : IAuthenticationService
    {
        private readonly IUserRepository userRepository;
        private readonly IPasswordService passwordService;

        public AuthenticationService(
            IUserRepository repository,
            IPasswordService passwordService)
        {
            userRepository = repository;
            this.passwordService = passwordService;
        }

        public User? Login(string email, string password)
        {
            User? user = userRepository.GetByEmail(email);

            if (user == null)
            {
                return null;
            }

            bool passwordValid = passwordService.VerifyPassword(
                password,
                user.PasswordHash
            );

            if (!passwordValid)
            {
                return null;
            }

            return user;
        }
    }
}