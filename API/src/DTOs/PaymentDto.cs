using System.Text.Json.Serialization;

namespace API.DTOs;

public class ProcessPaymentDto
{
  [JsonPropertyName("orderId")]
  public int OrderId { get; set; }

  [JsonPropertyName("amount")]
  public decimal Amount { get; set; }

  [JsonPropertyName("cardToken")]
  public string? CardToken { get; set; } // Mock token

  [JsonPropertyName("paymentMethod")]
  public string PaymentMethod { get; set; } = "card"; // card, po
}

public class PaymentResponseDto
{
  public bool Success { get; set; }
  public string? PaymentIntentId { get; set; }
  public string? Message { get; set; }
}

public class PaymentStatusDto
{
  public string Status { get; set; } = string.Empty;
  public decimal Amount { get; set; }
  public string? PaymentIntentId { get; set; }
  public DateTime? PaidAt { get; set; }
}
