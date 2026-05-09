using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BCrypt.Net;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace PlanAI.Tests
{
    public class AuthTests
    {
        [Fact]
        public void BCryptHash_VerifiesCorrectly()
        {
            var password = "SuperSecretPassword123!";
            var hash = BCrypt.Net.BCrypt.HashPassword(password);
            Assert.True(BCrypt.Net.BCrypt.Verify(password, hash));
        }

        [Fact]
        public void BCryptHash_FailsWithDifferentPassword()
        {
            var password = "SuperSecretPassword123!";
            var hash = BCrypt.Net.BCrypt.HashPassword(password);
            Assert.False(BCrypt.Net.BCrypt.Verify("WrongPassword", hash));
        }

        private string GenerateToken(Guid userId, string role, int expiryHours)
        {
            var secret = "planai-super-secret-key-32-chars-minimum";
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Role, role)
            };

            var token = new JwtSecurityToken(
                claims: claims,
                expires: DateTime.UtcNow.AddHours(expiryHours),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        [Fact]
        public void JwtToken_ContainsUserIdAndRoleClaims()
        {
            var userId = Guid.NewGuid();
            var role = "Admin";
            var tokenStr = GenerateToken(userId, role, 1);
            
            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(tokenStr);

            Assert.Contains(jwt.Claims, c => c.Type == ClaimTypes.NameIdentifier && c.Value == userId.ToString());
            Assert.Contains(jwt.Claims, c => c.Type == ClaimTypes.Role && c.Value == role);
        }

        [Fact]
        public void JwtToken_Expired_FailsValidation()
        {
            var tokenStr = GenerateToken(Guid.NewGuid(), "Member", -1);
            
            var handler = new JwtSecurityTokenHandler();
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("planai-super-secret-key-32-chars-minimum")),
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

            Assert.Throws<SecurityTokenExpiredException>(() =>
            {
                handler.ValidateToken(tokenStr, validationParameters, out var validatedToken);
            });
        }
    }
}
