namespace API.DTOs;

public class ProcessPaymentDto
{
  public int OrderId { get; set; }
  public decimal Amount { get; set; }
  public string? CardToken { get; set; } // Mock token
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
  public string Status { get; set; }
  public decimal Amount { get; set; }
  public string? PaymentIntentId { get; set; }
  public DateTime? PaidAt { get; set; }
}
