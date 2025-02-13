using System.ComponentModel.DataAnnotations;

public class UserRegistrationDto
{

  [Required]
  [NoSpecialCharacters]
  [StringLength(50, MinimumLength = 3, ErrorMessage = "Username must be between 3 and 50 characters")]
  public string Username { get; set; }

  [Required]
  [EmailAddress]
  public string Email { get; set; }

  [Required]
  public string Password { get; set; }
}