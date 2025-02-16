namespace API.Services;

public interface IPasswordHasherService
{
  string HashPassword(string password);
  bool VerifyPassword(string hashedPassword, string password);
}