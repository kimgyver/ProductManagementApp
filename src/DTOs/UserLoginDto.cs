using System.ComponentModel.DataAnnotations;

public class UserLoginDto
{
  [EmailAddress]
  public string Email { get; set; }
  public string Password { get; set; }
}
