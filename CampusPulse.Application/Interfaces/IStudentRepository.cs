using CampusPulse.Domain.Entities;

namespace CampusPulse.Application.Interfaces
{
    public interface IStudentRepository
    {
        Student? GetByStudentNumber(string studentNumber);

        void Add(Student student);
    }
}