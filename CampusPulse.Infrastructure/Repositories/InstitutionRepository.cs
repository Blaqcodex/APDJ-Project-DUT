using CampusPulse.Application.Interfaces;
using CampusPulse.Domain.Entities;
using CampusPulse.Infrastructure.Data;

namespace CampusPulse.Infrastructure.Repositories
{
    public class InstitutionRepository : IInstitutionRepository
    {
        private readonly CampusPulseDbContext dbContext;

        public InstitutionRepository(CampusPulseDbContext context)
        {
            dbContext = context;
        }

        public Institution? GetByStudentEmailDomain(string emailDomain)
        {
            return dbContext.Institutions
                .FirstOrDefault(institution =>
                    institution.StudentEmailDomain == emailDomain);
        }
    }
}