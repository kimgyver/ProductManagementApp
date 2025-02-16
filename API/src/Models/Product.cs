using System.ComponentModel.DataAnnotations;

namespace API.Models;

public class Product
{
  public int Id { get; set; }

  [Required]
  [StringLength(50, MinimumLength = 3, ErrorMessage = "Name must be between 3 and 50 characters")]
  public string Name { get; set; } = string.Empty;

  public string Description { get; set; } = string.Empty;

  public decimal Price { get; set; }

  public int Stock { get; set; }

  public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
