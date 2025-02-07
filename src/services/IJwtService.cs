
public interface IJwtService
{
  string GenerateToken(string username, bool isAdmin);
}
