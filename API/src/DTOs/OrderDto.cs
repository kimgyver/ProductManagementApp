namespace API.DTOs;

public class CreateOrderDto
{
  public List<OrderItemDto> Items { get; set; } = new();
  public string? RecipientName { get; set; }
  public string? RecipientPhone { get; set; }
  public string? ShippingAddress1 { get; set; }
  public string? ShippingAddress2 { get; set; }
  public string? ShippingPostalCode { get; set; }
  public string? PaymentMethod { get; set; } = "card"; // card, po
}

public class OrderItemDto
{
  public int ProductId { get; set; }
  public int Quantity { get; set; }
  public string? SelectedOptions { get; set; } // JSON string
}

public class OrderDto
{
  public int Id { get; set; }
  public int UserId { get; set; }
  public List<OrderItemDto> Items { get; set; } = new();
  public decimal TotalPrice { get; set; }
  public string Status { get; set; }
  public string? PoNumber { get; set; }
  public DateTime? PaymentDueDate { get; set; }
  public string PaymentMethod { get; set; }
  public DateTime CreatedAt { get; set; }
  public DateTime UpdatedAt { get; set; }
}

public class UpdateOrderStatusDto
{
  public string Status { get; set; }
}

public class CancelOrderDto
{
  public string Reason { get; set; }
}

public class RefundRequestDto
{
  public string Reason { get; set; }
}
