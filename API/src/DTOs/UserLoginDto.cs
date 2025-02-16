using System.ComponentModel.DataAnnotations;

namespace API.DTOs;

public class UserLoginDto
{
  [EmailAddress]
  public string Email { get; set; }
  public string Password { get; set; }
}
