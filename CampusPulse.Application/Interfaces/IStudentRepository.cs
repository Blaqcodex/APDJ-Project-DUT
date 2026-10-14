using CampusPulse.Domain.Entities;

namespace CampusPulse.Application.Interfaces
{
    public interface IStudentRepository
    {
        Student? GetByStudentNumber(string studentNumber);

        Student? GetByUserId(int userId);

        void Add(Student student);
    }
}