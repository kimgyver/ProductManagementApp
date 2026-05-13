using API.DTOs;
using API.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace API.Services;

public class JwtService : IJwtService
{
  private readonly IConfiguration _configuration;

  public JwtService(IConfiguration configuration)
  {
    _configuration = configuration;
  }

  public string GenerateTokenForUser(User user)
  {
    var nameIdentifier = user.Id > 0 ? user.Id.ToString() : user.Email;
    var claims = new[]
    {
      new Claim(ClaimTypes.NameIdentifier, nameIdentifier),
      new Claim(ClaimTypes.Email, user.Email),
      new Claim(ClaimTypes.Name, user.Username),
      new Claim(ClaimTypes.Role, user.IsAdmin ? "Admin" : "User")
    };

    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(GetJwtSecret()));
    var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

    var token = new JwtSecurityToken(
        issuer: _configuration["Jwt:Issuer"],
        audience: _configuration["Jwt:Audience"],
        claims: claims,
        expires: DateTime.Now.AddHours(1), // token expiration time
        signingCredentials: creds
    );

    return new JwtSecurityTokenHandler().WriteToken(token);
  }

  public string GenerateTokenForClient(string clientId)
  {
    var claims = new List<Claim>
    {
        new Claim(JwtRegisteredClaimNames.Sub, clientId),
        new Claim("role", "Client") // Custom claim to indicate client role
    };

    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(GetJwtSecret()));
    var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

    var token = new JwtSecurityToken(
        issuer: _configuration["Jwt:Issuer"],
        audience: _configuration["Jwt:Audience"],
        claims: claims,
        expires: DateTime.UtcNow.AddMinutes(5),
        signingCredentials: credentials
    );

    return new JwtSecurityTokenHandler().WriteToken(token);
  }

  public string GetClientToken(UserLoginDto loginDto)
  {
    if (loginDto.ClientId == "background-worker" && loginDto.ClientSecret == GetJwtSecret())
    {
      var token = GenerateTokenForClient(loginDto.ClientId);
      return token;
    }
    return string.Empty;
  }

  private string GetJwtSecret()
  {
    return _configuration["Jwt:Secret"]
      ?? throw new InvalidOperationException("Jwt:Secret is missing in configuration.");
  }
}