using System.ComponentModel.DataAnnotations;
using API.DTOs;

namespace API.Models;

public class User
{
  public int Id { get; set; }

  [Required]
  [NoSpecialCharacters]
  [StringLength(50, MinimumLength = 3, ErrorMessage = "Username must be between 3 and 50 characters")]
  public string Username { get; set; }

  [Required]
  [EmailAddress]
  public string Email { get; set; }

  [Required]
  public string HashedPassword { get; set; }

  public bool IsAdmin { get; set; }

  public bool Verified { get; set; }
}
