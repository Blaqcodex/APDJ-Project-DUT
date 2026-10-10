using CampusPulse.Application.Interfaces;
using CampusPulse.Domain.Entities;
using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace CampusPulse.Application.Services
{
    public class RegistrationService : IRegistrationService
    {
        private readonly IUserRepository userRepository;
        private readonly IStudentRepository studentRepository;
        private readonly IInstitutionRepository institutionRepository;
        private readonly IPasswordService passwordService;
        private readonly IRegistrationTransaction registrationTransaction;

        public RegistrationService(
            IUserRepository userRepository,
            IStudentRepository studentRepository,
            IInstitutionRepository institutionRepository,
            IPasswordService passwordService,
            IRegistrationTransaction registrationTransaction)
        {
            this.userRepository = userRepository;
            this.studentRepository = studentRepository;
            this.institutionRepository = institutionRepository;
            this.passwordService = passwordService;
            this.registrationTransaction = registrationTransaction;
        }

        public bool RegisterStudent(
            string firstName,
            string lastName,
            string email,
            string studentNumber,
            string password,
            string recoveryEmail)
        {
            if (string.IsNullOrWhiteSpace(firstName) ||
                string.IsNullOrWhiteSpace(lastName) ||
                string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(studentNumber) ||
                string.IsNullOrWhiteSpace(password) ||
                string.IsNullOrWhiteSpace(recoveryEmail))
            {
                return false;
            }

            if (studentNumber.Length != 8 ||
                !studentNumber.All(char.IsDigit))
            {
                return false;
            }

            email = email.Trim().ToLowerInvariant();
            recoveryEmail = recoveryEmail.Trim().ToLowerInvariant();

            string expectedEmail = $"{studentNumber}@dut4life.ac.za";

            if (!string.Equals(
                email,
                expectedEmail,
                StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (!Regex.IsMatch(
                recoveryEmail,
                @"^[^@\s]+@gmail\.com$",
                RegexOptions.IgnoreCase))
            {
                return false;
            }

            if (password.Length < 8)
            {
                return false;
            }

            if (userRepository.GetByEmail(email) != null)
            {
                return false;
            }

            if (studentRepository.GetByStudentNumber(studentNumber) != null)
            {
                return false;
            }

            string emailDomain = email.Substring(
                email.IndexOf('@') + 1);

            Institution? institution =
                institutionRepository.GetByStudentEmailDomain(emailDomain);

            if (institution == null)
            {
                return false;
            }

            string passwordHash =
                passwordService.HashPassword(password);

            User user = new User
            {
                FirstName = firstName.Trim(),
                LastName = lastName.Trim(),
                Email = email,
                RecoveryEmail = recoveryEmail,
                RecoveryEmailVerified = false,
                PasswordHash = passwordHash,
                Role = "Student"
            };

            Student student = new Student
            {
                InstitutionId = institution.InstitutionId,
                StudentNumber = studentNumber,
                ProgrammeId = 0,
                YearLevel = 0
            };

            registrationTransaction.Register(user, student);

            return true;
        }
    }
}