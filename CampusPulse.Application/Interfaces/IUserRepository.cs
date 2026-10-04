using CampusPulse.Domain.Entities;

namespace CampusPulse.Application.Interfaces
{
    public interface IUserRepository
    {
        User? GetByEmail(string email);

        void Add(User user);
    }
}