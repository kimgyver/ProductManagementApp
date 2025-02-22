using API.DTOs;

namespace API.Services;

public interface IJwtService
{
  string GenerateTokenForUser(string username, bool isAdmin);
  string GenerateTokenForClient(string clientId);
  string GetClientToken(UserLoginDto loginDto);
}
