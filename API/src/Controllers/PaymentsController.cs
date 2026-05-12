using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using API.Services;
using API.DTOs;
using System.Security.Claims;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentsController : ControllerBase
{
  private readonly IPaymentService _paymentService;
  private readonly IOrderQueryService _orderQueryService;
  private readonly ILogger<PaymentsController> _logger;

  public PaymentsController(
    IPaymentService paymentService,
    IOrderQueryService orderQueryService,
    ILogger<PaymentsController> logger)
  {
    _paymentService = paymentService;
    _orderQueryService = orderQueryService;
    _logger = logger;
  }

  // POST: api/payments/process
  [HttpPost("process")]
  [Authorize]
  public async Task<ActionResult<PaymentResponseDto>> ProcessPayment([FromBody] ProcessPaymentDto dto)
  {
    if (!ModelState.IsValid)
      return BadRequest(ModelState);

    try
    {
      var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
      var order = await _orderQueryService.GetOrderByIdAsync(dto.OrderId);

      if (order == null)
        return NotFound(new { error = "Order not found" });

      // Check if user owns this order
      if (order.UserId != userId && !User.IsInRole("admin"))
        return Forbid();

      // Check if order is pending payment
      if (order.Status != "pending" && order.Status != "pending_payment")
        return BadRequest(new { error = "Order is not in a state where it can be paid" });

      // Process payment (mock)
      var (success, paymentIntentId) = await _paymentService.ProcessPaymentAsync(
        dto.OrderId,
        dto.Amount,
        dto.CardToken ?? "mock_token"
      );

      if (!success)
        return BadRequest(new PaymentResponseDto
        {
          Success = false,
          Message = "Payment processing failed. Please try again."
        });

      // Complete payment
      var completedOrder = await _paymentService.CompletePaymentAsync(dto.OrderId, paymentIntentId);
      if (completedOrder == null)
        return StatusCode(500, new { error = "Failed to complete payment" });

      _logger.LogInformation("Payment processed for order {OrderId} by user {UserId}", dto.OrderId, userId);

      return Ok(new PaymentResponseDto
      {
        Success = true,
        PaymentIntentId = paymentIntentId,
        Message = "Payment successful"
      });
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error processing payment");
      return StatusCode(500, new { error = "Error processing payment" });
    }
  }

  // GET: api/payments/:orderId/status
  [HttpGet("{orderId}/status")]
  [Authorize]
  public async Task<ActionResult<PaymentStatusDto>> GetPaymentStatus(int orderId)
  {
    try
    {
      var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
      var order = await _orderQueryService.GetOrderByIdAsync(orderId);

      if (order == null)
        return NotFound(new { error = "Order not found" });

      if (order.UserId != userId && !User.IsInRole("admin"))
        return Forbid();

      var status = await _paymentService.GetPaymentStatusAsync(orderId);
      if (status == null)
        return NotFound();

      return Ok(status);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error getting payment status");
      return StatusCode(500, new { error = "Error getting payment status" });
    }
  }

  // POST: api/payments/:orderId/refund
  [HttpPost("{orderId}/refund")]
  [Authorize]
  public async Task<ActionResult<object>> RequestRefund(int orderId, [FromBody] RefundRequestDto dto)
  {
    try
    {
      var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
      var order = await _orderQueryService.GetOrderByIdAsync(orderId);

      if (order == null)
        return NotFound(new { error = "Order not found" });

      if (order.UserId != userId && !User.IsInRole("admin"))
        return Forbid();

      if (order.Status != "paid" && order.Status != "processing" && order.Status != "shipped")
        return BadRequest(new { error = "Order cannot be refunded in its current state" });

      // TODO: Implement actual refund logic
      _logger.LogInformation("Refund requested for order {OrderId} by user {UserId}", orderId, userId);

      return Ok(new { message = "Refund request submitted" });
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error requesting refund");
      return StatusCode(500, new { error = "Error requesting refund" });
    }
  }
}
