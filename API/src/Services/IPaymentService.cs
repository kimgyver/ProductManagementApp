using API.Models;
using API.Infrastructure;
using API.DTOs;

namespace API.Services;

public interface IPaymentService
{
  Task<(bool success, string paymentIntentId)> ProcessPaymentAsync(int orderId, decimal amount, string cardToken);
  Task<Order?> CompletePaymentAsync(int orderId, string paymentIntentId);
  Task<Order?> FailPaymentAsync(int orderId, string reason);
  Task<PaymentStatusDto?> GetPaymentStatusAsync(int orderId);
}

public class MockPaymentService : IPaymentService
{
  private readonly IOrderRepository _orderRepository;
  private readonly ILogger<MockPaymentService> _logger;

  public MockPaymentService(IOrderRepository orderRepository, ILogger<MockPaymentService> logger)
  {
    _orderRepository = orderRepository;
    _logger = logger;
  }

  public async Task<(bool success, string paymentIntentId)> ProcessPaymentAsync(int orderId, decimal amount, string cardToken)
  {
    try
    {
      var order = await _orderRepository.GetByIdAsync(orderId);
      if (order == null)
        return (false, "");

      if (Math.Abs(order.TotalPrice - amount) > 0.01m)
      {
        _logger.LogWarning("Payment amount mismatch for order {OrderId}: expected {Expected}, got {Actual}",
          orderId, order.TotalPrice, amount);
        return (false, "");
      }

      // Mock payment processing
      // 90% success rate for demo
      var isSuccess = new Random().Next(100) < 90;

      if (isSuccess)
      {
        var paymentIntentId = $"pi_{Guid.NewGuid().ToString()[..12]}";
        _logger.LogInformation("[Payment] Mock payment processed successfully - OrderId: {OrderId}, Amount: {Amount}, PaymentIntentId: {PaymentIntentId}",
          orderId, amount, paymentIntentId);
        return (true, paymentIntentId);
      }

      _logger.LogWarning("[Payment] Mock payment failed for order {OrderId}", orderId);
      return (false, "");
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error processing payment for order {OrderId}", orderId);
      return (false, "");
    }
  }

  public async Task<Order?> CompletePaymentAsync(int orderId, string paymentIntentId)
  {
    try
    {
      var order = await _orderRepository.GetByIdAsync(orderId);
      if (order == null) return null;

      order.PaymentIntentId = paymentIntentId;
      order.Status = "paid";
      order.PaidAt = DateTime.UtcNow;
      order.UpdatedAt = DateTime.UtcNow;

      var result = await _orderRepository.UpdateAsync(order);
      _logger.LogInformation("[Payment] Payment completed for order {OrderId}, status set to paid", orderId);
      return result;
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error completing payment for order {OrderId}", orderId);
      return null;
    }
  }

  public async Task<Order?> FailPaymentAsync(int orderId, string reason)
  {
    try
    {
      var order = await _orderRepository.GetByIdAsync(orderId);
      if (order == null) return null;

      order.Status = "payment_failed";
      order.UpdatedAt = DateTime.UtcNow;

      var result = await _orderRepository.UpdateAsync(order);
      _logger.LogInformation("[Payment] Payment failed for order {OrderId}, reason: {Reason}", orderId, reason);
      return result;
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error failing payment for order {OrderId}", orderId);
      return null;
    }
  }

  public async Task<PaymentStatusDto?> GetPaymentStatusAsync(int orderId)
  {
    var order = await _orderRepository.GetByIdAsync(orderId);
    if (order == null) return null;

    return new PaymentStatusDto
    {
      Status = order.Status,
      Amount = order.TotalPrice,
      PaymentIntentId = order.PaymentIntentId,
      PaidAt = order.PaidAt
    };
  }
}

