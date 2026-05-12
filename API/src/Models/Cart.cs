namespace API.Models;

public class Cart
{
  public int Id { get; set; }

  public int UserId { get; set; }
  public User? User { get; set; }

  public ICollection<CartItem> Items { get; set; } = new List<CartItem>();

  public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
  public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
