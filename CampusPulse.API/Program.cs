
using System.Text;
using System.Threading.RateLimiting;
using CampusPulse.API.Services;
using CampusPulse.Application.Interfaces;
using CampusPulse.Application.Services;
using CampusPulse.Infrastructure.Data;
using CampusPulse.Infrastructure.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// ADMIN_PROVISIONING_EARLY_GUARD
// Reject provisioning outside Development before JWT configuration is read.
if (args.Contains("--provision-admin", StringComparer.Ordinal) &&
    !builder.Environment.IsDevelopment())
{
    Console.Error.WriteLine(
        "Admin provisioning is available only in Development.");
    Environment.ExitCode = 1;
    return;
}

// Database connection
var connectionString = builder.Configuration
    .GetConnectionString("CampusPulseDatabase");

builder.Services.AddDbContext<CampusPulseDbContext>(options =>
    options.UseSqlServer(connectionString));

// Repositories
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IStudentRepository, StudentRepository>();
builder.Services.AddScoped<IInstitutionRepository, InstitutionRepository>();
builder.Services.AddScoped<IRegistrationTransaction, RegistrationTransaction>();

// Application services
builder.Services.AddScoped<IPasswordService, PasswordService>();
builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
builder.Services.AddScoped<IRegistrationService, RegistrationService>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();

// Password recovery services
builder.Services.AddScoped<PasswordResetService>();
builder.Services.AddScoped<RecoveryEmailVerificationService>();
builder.Services.AddHttpClient<BrevoEmailService>();

// JWT configuration
string jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("JWT signing key is missing.");

string jwtIssuer = builder.Configuration["Jwt:Issuer"]
    ?? throw new InvalidOperationException("JWT issuer is missing.");

string jwtAudience = builder.Configuration["Jwt:Audience"]
    ?? throw new InvalidOperationException("JWT audience is missing.");

if (Encoding.UTF8.GetByteCount(jwtKey) < 32)
{
    throw new InvalidOperationException(
        "JWT signing key must be at least 32 bytes.");
}

// JWT authentication
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var userIdClaim = context.Principal?
                    .FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?
                    .Value;

                if (!int.TryParse(userIdClaim, out var userId))
                {
                    context.Fail("Invalid account.");
                    return;
                }

                var db = context.HttpContext.RequestServices
                    .GetRequiredService<CampusPulseDbContext>();

                var isActive = await db.Users
                    .AsNoTracking()
                    .AnyAsync(user =>
                        user.UserId == userId &&
                        user.IsActive);

                if (!isActive)
                {
                    context.Fail("Account unavailable.");
                }
            }
        };
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,

            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,

            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtKey)),

            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

// Rate limiting
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode =
        StatusCodes.Status429TooManyRequests;

    // Recovery email verification
    options.AddPolicy("recovery-email", context =>
    {
        string userId = context.User.FindFirst(
            System.Security.Claims.ClaimTypes.NameIdentifier
        )?.Value ?? "anonymous";

        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: userId,
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(10),
                QueueLimit = 0,
                AutoReplenishment = true
            });
    });

    // Forgot password requests
    options.AddPolicy("forgot-password", context =>
    {
        string clientIp =
            context.Connection.RemoteIpAddress?.ToString()
            ?? "unknown";

        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: clientIp,
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 3,
                Window = TimeSpan.FromMinutes(10),
                QueueLimit = 0,
                AutoReplenishment = true
            });
    });
});

// Controllers and OpenAPI
builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

if (args.Contains("--provision-admin", StringComparer.Ordinal))
{
    Environment.ExitCode = await AdminProvisioning.RunAsync(
        app.Services,
        app.Environment);
    return;
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Middleware
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();

app.MapControllers();

app.Run();
