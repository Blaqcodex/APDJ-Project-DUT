using CampusPulse.Domain.Entities;

namespace CampusPulse.Application.Interfaces
{
    public interface IInstitutionRepository
    {
        Institution? GetByStudentEmailDomain(string emailDomain);
    }
}