using System.ComponentModel.DataAnnotations;
using API.DTOs;

namespace API.Models;

public class User
{
  public int Id { get; set; }

  [Required]
  [NoSpecialCharacters]
  [StringLength(50, MinimumLength = 3, ErrorMessage = "Username must be between 3 and 50 characters")]
  public string Username { get; set; }

  [Required]
  [EmailAddress]
  public string Email { get; set; }

  [Required]
  public string HashedPassword { get; set; }

  public bool IsAdmin { get; set; }

  public bool Verified { get; set; }

  // Role: customer, admin, distributor
  public string Role { get; set; } = "customer";

  // B2C customer info
  public string? Phone { get; set; }
  public string? Address { get; set; }

  // Shipping info
  public bool AlwaysUseProfileShipping { get; set; } = false;
  public string? DefaultRecipientName { get; set; }
  public string? DefaultRecipientPhone { get; set; }
  public string? DefaultShippingAddress1 { get; set; }
  public string? DefaultShippingAddress2 { get; set; }
  public string? DefaultShippingPostalCode { get; set; }

  public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
  public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

  // Navigation properties
  public ICollection<Order> Orders { get; set; } = new List<Order>();
  public ICollection<Order> CancelledOrders { get; set; } = new List<Order>();
  public Cart? Cart { get; set; }
}

