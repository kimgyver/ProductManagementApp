using API.DTOs;
using API.Models;

namespace API.Services;

public interface IJwtService
{
  string GenerateTokenForUser(User user);
  string GenerateTokenForClient(string clientId);
  string GetClientToken(UserLoginDto loginDto);
}
