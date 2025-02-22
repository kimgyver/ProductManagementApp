using API.DTOs;
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

  public string GenerateTokenForUser(string username, bool isAdmin)
  {
    var claims = new[]
    {
      new Claim(ClaimTypes.Name, username),
      new Claim(ClaimTypes.Role, isAdmin ? "Admin" : "User")
    };

    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Secret"]));
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

    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Secret"]));
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
    if (loginDto.ClientId == "background-worker" && loginDto.ClientSecret == _configuration["JWT:Secret"])
    {
      var token = GenerateTokenForClient(loginDto.ClientId);
      return token;
    }
    return string.Empty;
  }
}