
using CampusPulse.Application.Interfaces;
using CampusPulse.Application.Services;
using CampusPulse.Infrastructure.Data;
using CampusPulse.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Connect the API to the CampusPulse database.
var connectionString = builder.Configuration
    .GetConnectionString("CampusPulseDatabase");

builder.Services.AddDbContext<CampusPulseDbContext>(options =>
    options.UseSqlServer(connectionString));

// Register repositories.
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IStudentRepository, StudentRepository>();
builder.Services.AddScoped<IInstitutionRepository, InstitutionRepository>();
builder.Services.AddScoped<IRegistrationTransaction, RegistrationTransaction>();

// Register application services.
builder.Services.AddScoped<IPasswordService, PasswordService>();
builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
builder.Services.AddScoped<IRegistrationService, RegistrationService>();

// Register API controllers and OpenAPI.
builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();