using System.ComponentModel.DataAnnotations;

namespace API.Models;

public enum ProductStatus
{
  draft,
  active,
  archived
}

public class Product
{
  public int Id { get; set; }

  [Required]
  [StringLength(100)]
  public string Sku { get; set; } = string.Empty; // Business identifier (e.g., "NIKE-001")

  [Required]
  [StringLength(100, MinimumLength = 3, ErrorMessage = "Name must be between 3 and 100 characters")]
  public string Name { get; set; } = string.Empty;

  public string Description { get; set; } = string.Empty;

  public ProductStatus Status { get; set; } = ProductStatus.active;

  public decimal Price { get; set; }

  [StringLength(50)]
  public string Category { get; set; } = string.Empty;

  public int Stock { get; set; }

  // Physical attributes (optional; used for shipping/warehouse planning)
  public int? WeightGrams { get; set; }
  public float? LengthCm { get; set; }
  public float? WidthCm { get; set; }
  public float? HeightCm { get; set; }

  public float? Rating { get; set; } // Average rating (0-5)
  public int? ReviewCount { get; set; }

  public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
  public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

  // Navigation properties
  public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
  public ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
}

