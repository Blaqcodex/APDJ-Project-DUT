using System.ComponentModel.DataAnnotations;
using CampusPulse.Application.Interfaces;
using CampusPulse.Domain.Entities;
using CampusPulse.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CampusPulse.API.Controllers;

public sealed class CreateLecturerRequest
{
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string FirstName { get; set; } = "";

    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string LastName { get; set; } = "";

    [Required]
    [EmailAddress]
    [StringLength(254)]
    public string Email { get; set; } = "";

    [Required]
    [StringLength(50, MinimumLength = 1)]
    public string StaffNumber { get; set; } = "";

    [Required]
    [StringLength(150, MinimumLength = 1)]
    public string Department { get; set; } = "";

    [Required]
    [StringLength(128, MinimumLength = 12)]
    public string TemporaryPassword { get; set; } = "";
}

[ApiController]
[Route("api/admin/lecturers")]
[Authorize(Roles = "Admin")]
public sealed class AdminLecturerProvisioningController : ControllerBase
{
    private readonly CampusPulseDbContext db;
    private readonly IPasswordService passwordService;

    public AdminLecturerProvisioningController(
        CampusPulseDbContext db,
        IPasswordService passwordService)
    {
        this.db = db;
        this.passwordService = passwordService;
    }

    [HttpPost]
    public async Task<IActionResult> CreateLecturer(
        [FromBody] CreateLecturerRequest request,
        CancellationToken cancellationToken)
    {
        string firstName = request.FirstName?.Trim() ?? "";
        string lastName = request.LastName?.Trim() ?? "";
        string email = request.Email?.Trim().ToLowerInvariant() ?? "";
        string staffNumber = request.StaffNumber?.Trim() ?? "";
        string department = request.Department?.Trim() ?? "";

        if (string.IsNullOrWhiteSpace(firstName) ||
            string.IsNullOrWhiteSpace(lastName) ||
            string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(staffNumber) ||
            string.IsNullOrWhiteSpace(department) ||
            firstName.Length > 100 ||
            lastName.Length > 100 ||
            email.Length > 254 ||
            staffNumber.Length > 50 ||
            department.Length > 150 ||
            !new EmailAddressAttribute().IsValid(email))
        {
            return BadRequest(new
            {
                message = "Valid lecturer details are required."
            });
        }

        if (string.IsNullOrWhiteSpace(request.TemporaryPassword) ||
            request.TemporaryPassword.Length < 12 ||
            request.TemporaryPassword.Length > 128)
        {
            return BadRequest(new
            {
                message = "Temporary password must contain 12-128 characters."
            });
        }

        bool emailExists = await db.Users
            .AnyAsync(user => user.Email.ToLower() == email,
                cancellationToken);

        if (emailExists)
        {
            return Conflict(new
            {
                message = "An account with this email already exists."
            });
        }

        bool staffNumberExists = await db.Lecturers
            .AnyAsync(lecturer => lecturer.StaffNumber.ToLower() ==
                staffNumber.ToLower(), cancellationToken);

        if (staffNumberExists)
        {
            return Conflict(new
            {
                message = "A lecturer with this staff number already exists."
            });
        }

        string passwordHash = passwordService.HashPassword(
            request.TemporaryPassword);

        await using var transaction =
            await db.Database.BeginTransactionAsync(cancellationToken);

        var user = new User
        {
            FirstName = firstName,
            LastName = lastName,
            Email = email,
            PasswordHash = passwordHash,
            Role = "Lecturer",
            IsActive = true,
            MustChangePassword = true,
            CreatedAt = DateTime.UtcNow
        };

        db.Users.Add(user);
        await db.SaveChangesAsync(cancellationToken);

        var lecturer = new Lecturer
        {
            UserId = user.UserId,
            StaffNumber = staffNumber,
            Department = department
        };

        db.Lecturers.Add(lecturer);
        await db.SaveChangesAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        return Created(
            $"/api/admin/lecturers/{lecturer.LecturerId}",
            new
            {
                lecturerId = lecturer.LecturerId,
                userId = user.UserId,
                firstName = user.FirstName,
                lastName = user.LastName,
                email = user.Email,
                staffNumber = lecturer.StaffNumber,
                department = lecturer.Department,
                role = user.Role,
                isActive = user.IsActive,
                mustChangePassword = user.MustChangePassword,
                message = "Lecturer account created successfully."
            });
    }
}