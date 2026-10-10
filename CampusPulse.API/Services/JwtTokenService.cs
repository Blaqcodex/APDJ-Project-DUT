
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CampusPulse.Application.Interfaces;
using CampusPulse.Domain.Entities;
using Microsoft.IdentityModel.Tokens;

namespace CampusPulse.API.Services
{
    public class JwtTokenService : IJwtTokenService
    {
        private readonly IConfiguration configuration;

        public JwtTokenService(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        public string GenerateToken(User user)
        {
            string jwtKey = configuration["Jwt:Key"]
                ?? throw new InvalidOperationException("JWT key is missing.");

            string jwtIssuer = configuration["Jwt:Issuer"]
                ?? throw new InvalidOperationException("JWT issuer is missing.");

            string jwtAudience = configuration["Jwt:Audience"]
                ?? throw new InvalidOperationException("JWT audience is missing.");

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role)
            };

            var securityKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtKey));

            var credentials = new SigningCredentials(
                securityKey,
                SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: jwtIssuer,
                audience: jwtAudience,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(2),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
