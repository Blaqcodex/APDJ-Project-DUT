
using CampusPulse.Domain.Entities;

namespace CampusPulse.Application.Interfaces
{
    public interface IJwtTokenService
    {
        string GenerateToken(User user);
    }
}
