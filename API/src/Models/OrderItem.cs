namespace API.Models;

public class OrderItem
{
  public int Id { get; set; }

  public int OrderId { get; set; }
  public Order? Order { get; set; }

  public int ProductId { get; set; }
  public Product? Product { get; set; }

  public int Quantity { get; set; }
  public decimal Price { get; set; } // Effective price at the time of purchase (with B2B discounts applied)
  public decimal? BasePrice { get; set; } // Original price before B2B discounts (null for non-discounted items)
  
  // Phase-1 variants: capture selected option values (no per-variant SKU/price/stock yet)
  // Format: { "Color": "Black", "Size": "M" }
  public string? SelectedOptions { get; set; } // JSON string
}
