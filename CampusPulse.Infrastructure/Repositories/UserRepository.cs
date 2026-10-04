using CampusPulse.Application.Interfaces;
using CampusPulse.Domain.Entities;
using CampusPulse.Infrastructure.Data;

namespace CampusPulse.Infrastructure.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly CampusPulseDbContext dbContext;

        public UserRepository(CampusPulseDbContext context)
        {
            dbContext = context;
        }

        public User? GetByEmail(string email)
        {
            return dbContext.Users
                .FirstOrDefault(user => user.Email == email);
        }

        public void Add(User user)
        {
            dbContext.Users.Add(user);
            dbContext.SaveChanges();
        }
    }
}