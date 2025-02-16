namespace API.Services;

public class PasswordHasherService : IPasswordHasherService
{
  public PasswordHasherService()
  {
  }

  public string HashPassword(string password)
  {
    return PasswordHasher.HashPassword(password);
  }

  public bool VerifyPassword(string hashedPassword, string password)
  {
    return PasswordHasher.VerifyPassword(hashedPassword, password);
  }
}
