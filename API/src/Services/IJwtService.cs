namespace API.Services;

public interface IJwtService
{
  string GenerateToken(string username, bool isAdmin);
}
