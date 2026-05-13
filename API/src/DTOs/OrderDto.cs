using System.Text.Json.Serialization;

namespace API.DTOs;

public class CreateOrderDto
{
  [JsonPropertyName("items")]
  public List<OrderItemDto> Items { get; set; } = new();

  [JsonPropertyName("recipientName")]
  public string? RecipientName { get; set; }

  [JsonPropertyName("recipientPhone")]
  public string? RecipientPhone { get; set; }

  [JsonPropertyName("shippingAddress1")]
  public string? ShippingAddress1 { get; set; }

  [JsonPropertyName("shippingAddress2")]
  public string? ShippingAddress2 { get; set; }

  [JsonPropertyName("shippingPostalCode")]
  public string? ShippingPostalCode { get; set; }

  [JsonPropertyName("paymentMethod")]
  public string? PaymentMethod { get; set; } = "card"; // card, po
}

public class OrderItemDto
{
  [JsonPropertyName("productId")]
  public int ProductId { get; set; }

  [JsonPropertyName("quantity")]
  public int Quantity { get; set; }

  [JsonPropertyName("selectedOptions")]
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
