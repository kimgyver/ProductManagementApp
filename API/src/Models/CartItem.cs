namespace API.Models;

public class CartItem
{
  public int Id { get; set; }

  public int CartId { get; set; }
  public Cart? Cart { get; set; }

  public int ProductId { get; set; }
  public Product? Product { get; set; }

  public int Quantity { get; set; } = 1;

  // Phase-1 variants: a stable key for selectedOptions so cart can have multiple lines per product
  // Example: "Color=Black|Size=M"
  public string VariantKey { get; set; } = "";

  // Format: { "Color": "Black", "Size": "M" }
  public string? SelectedOptions { get; set; } // JSON string

  public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
  public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
