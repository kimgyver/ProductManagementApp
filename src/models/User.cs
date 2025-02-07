using System.ComponentModel.DataAnnotations;

public class User
{
  public int Id { get; set; }
  public string Username { get; set; }
  [EmailAddress]
  public string Email { get; set; }
  public string HashedPassword { get; set; }
  public bool IsAdmin { get; set; }
}
