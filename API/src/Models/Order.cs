using System.ComponentModel.DataAnnotations;

namespace API.Models;

public class Order
{
  public int Id { get; set; }

  public int UserId { get; set; }
  public User? User { get; set; }

  // Shipping address info
  public string? RecipientName { get; set; }
  public string? RecipientPhone { get; set; }
  public string? ShippingPostalCode { get; set; }
  public string? ShippingAddress1 { get; set; }
  public string? ShippingAddress2 { get; set; }

  // Shipping fulfillment info
  public string? Carrier { get; set; }
  public string? TrackingNumber { get; set; }
  public DateTime? ShippedAt { get; set; }
  public DateTime? DeliveredAt { get; set; }

  // Payment info
  public string? PaymentIntentId { get; set; }

  // B2B PO (Purchase Order) fields
  public string? PoNumber { get; set; } // PO-2026-0001 format (auto-generated for B2B orders)
  public DateTime? PaymentDueDate { get; set; } // Payment due date for Net 30/60 terms
  public string PaymentMethod { get; set; } = "card"; // card, po (purchase order)
  public string? InvoiceNumber { get; set; } // INV-2026-0001 format

  public DateTime? PaidAt { get; set; } // When the PO order was marked as paid

  // Payment reminder tracking (JSON array of sent reminder types)
  public string? PaymentRemindersSent { get; set; } = "[]";

  public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
  public decimal TotalPrice { get; set; }

  // Status: pending, pending_payment, processing, shipped, delivered, cancelled, paid
  public string Status { get; set; } = "pending";

  // Cancellation metadata
  public DateTime? CancelledAt { get; set; }
  public int? CancelledByUserId { get; set; }
  public User? CancelledByUser { get; set; }
  public string? CancelReason { get; set; }

  // Refund metadata
  public string RefundStatus { get; set; } = "none"; // none | requested | processing | succeeded | failed
  public decimal? RefundAmount { get; set; }
  public string? RefundId { get; set; }
  public string? RefundReason { get; set; }
  public DateTime? RefundRequestedAt { get; set; }
  public DateTime? RefundedAt { get; set; }
  public string? RefundFailureReason { get; set; }

  public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
  public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
