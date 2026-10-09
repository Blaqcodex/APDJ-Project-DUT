
using CampusPulse.Application.Interfaces;
using CampusPulse.Domain.Entities;
using CampusPulse.Infrastructure.Data;

namespace CampusPulse.Infrastructure.Repositories
{
    public class RegistrationTransaction : IRegistrationTransaction
    {
        private readonly CampusPulseDbContext dbContext;

        public RegistrationTransaction(CampusPulseDbContext context)
        {
            dbContext = context;
        }

        public void Register(User user, Student student)
        {
            using var transaction =
                dbContext.Database.BeginTransaction();

            try
            {
                dbContext.Users.Add(user);
                dbContext.SaveChanges();

                student.UserId = user.UserId;

                dbContext.Students.Add(student);
                dbContext.SaveChanges();

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }
    }
}