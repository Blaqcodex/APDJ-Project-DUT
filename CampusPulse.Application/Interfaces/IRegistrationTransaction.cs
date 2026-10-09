
using CampusPulse.Domain.Entities;

namespace CampusPulse.Application.Interfaces
{
    public interface IRegistrationTransaction
    {
        void Register(User user, Student student);
    }
}