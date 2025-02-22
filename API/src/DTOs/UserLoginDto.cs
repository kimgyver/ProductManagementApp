using System.ComponentModel.DataAnnotations;

namespace API.DTOs;

public class UserLoginDto
{
  [EmailAddress]
  public string? Email { get; set; }
  public string? Password { get; set; }
  public string? ClientId { get; set; }
  public string? ClientSecret { get; set; }
  public bool IsClient => !string.IsNullOrEmpty(ClientId) && !string.IsNullOrEmpty(ClientSecret);
}
