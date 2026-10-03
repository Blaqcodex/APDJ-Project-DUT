using CampusPulse.Domain.Entities;

namespace CampusPulse.Application.Interfaces
{
    public interface IAuthenticationService
    {
        User? Login(string email, string password);
    }
}