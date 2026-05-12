namespace API.DTOs;

public class DashboardStatsDto
{
  public int TotalUsers { get; set; }
  public int TotalOrders { get; set; }
  public decimal TotalRevenue { get; set; }
  public int PendingOrders { get; set; }
  public int CompletedOrders { get; set; }
  public int CancelledOrders { get; set; }
  public List<OrderSummaryDto> RecentOrders { get; set; } = new();
  public List<ProductSalesDto> TopProducts { get; set; } = new();
}

public class OrderSummaryDto
{
  public int Id { get; set; }
  public string? UserEmail { get; set; }
  public decimal TotalPrice { get; set; }
  public string Status { get; set; }
  public DateTime CreatedAt { get; set; }
}

public class ProductSalesDto
{
  public int ProductId { get; set; }
  public string ProductName { get; set; }
  public int SalesCount { get; set; }
  public decimal Revenue { get; set; }
}

public class AdminUserDto
{
  public int Id { get; set; }
  public string Username { get; set; }
  public string Email { get; set; }
  public string Role { get; set; }
  public bool Verified { get; set; }
  public DateTime CreatedAt { get; set; }
}

public class AdminOrderDetailsDto
{
  public int Id { get; set; }
  public int UserId { get; set; }
  public string UserEmail { get; set; }
  public decimal TotalPrice { get; set; }
  public string Status { get; set; }
  public string? PaymentIntentId { get; set; }
  public DateTime CreatedAt { get; set; }
  public DateTime UpdatedAt { get; set; }
  public List<OrderItemDto> Items { get; set; } = new();
}
