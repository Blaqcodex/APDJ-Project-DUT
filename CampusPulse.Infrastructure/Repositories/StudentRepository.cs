using CampusPulse.Application.Interfaces;
using CampusPulse.Domain.Entities;
using CampusPulse.Infrastructure.Data;

namespace CampusPulse.Infrastructure.Repositories
{
    public class StudentRepository : IStudentRepository
    {
        private readonly CampusPulseDbContext dbContext;

        public StudentRepository(CampusPulseDbContext context)
        {
            dbContext = context;
        }

        public Student? GetByStudentNumber(string studentNumber)
        {
            return dbContext.Students
                .FirstOrDefault(student => student.StudentNumber == studentNumber);
        }

        public void Add(Student student)
        {
            dbContext.Students.Add(student);
            dbContext.SaveChanges();
        }
    }
}