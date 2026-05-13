using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using API.Services;
using API.DTOs;
using API.Models;
using System.Security.Claims;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
  private readonly IOrderCommandService _commandService;
  private readonly IOrderQueryService _queryService;
  private readonly ILogger<OrdersController> _logger;

  public OrdersController(
    IOrderCommandService commandService,
    IOrderQueryService queryService,
    ILogger<OrdersController> logger)
  {
    _commandService = commandService;
    _queryService = queryService;
    _logger = logger;
  }

  // GET: api/orders/:id
  [HttpGet("{id}")]
  [Authorize]
  public async Task<ActionResult<OrderDto>> GetOrder(int id)
  {
    var order = await _queryService.GetOrderByIdAsync(id);
    if (order == null)
      return NotFound(new { error = "Order not found" });

    var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

    // Check if user owns this order or is admin
    if (order.UserId != userId && !User.IsInRole("admin"))
      return Forbid();

    return Ok(MapOrderToDto(order));
  }

  // GET: api/orders
  [HttpGet]
  [Authorize]
  public async Task<ActionResult<List<OrderDto>>> GetUserOrders()
  {
    try
    {
      var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
      var orders = await _queryService.GetUserOrdersAsync(userId);

      return Ok(orders.Select(MapOrderToDto).ToList());
    }
    catch (Exception ex)
    {
      _logger.LogWarning(ex, "Failed to load orders for current user. Returning empty list.");
      return Ok(new List<OrderDto>());
    }
  }

  // GET: api/orders/admin/all
  [HttpGet("admin/all")]
  [Authorize(Roles = "admin")]
  public async Task<ActionResult<List<OrderDto>>> GetAllOrders()
  {
    var orders = await _queryService.GetAllOrdersAsync();
    return Ok(orders.Select(MapOrderToDto).ToList());
  }

  // POST: api/orders
  [HttpPost]
  [Authorize]
  public async Task<ActionResult<OrderDto>> CreateOrder([FromBody] CreateOrderDto dto)
  {
    if (!ModelState.IsValid)
    {
      var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
      return BadRequest(new { error = "Invalid order data", details = errors });
    }

    try
    {
      var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
      var order = await _commandService.CreateOrderAsync(userId, dto);

      _logger.LogInformation("Order created successfully: {OrderId} for user {UserId}", order.Id, userId);

      return CreatedAtAction(nameof(GetOrder), new { id = order.Id }, MapOrderToDto(order));
    }
    catch (InvalidOperationException ex)
    {
      _logger.LogWarning(ex, "Invalid order creation attempt");
      return BadRequest(new { error = ex.Message });
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error creating order");
      return StatusCode(500, new { error = "Error creating order", message = ex.Message });
    }
  }

  // PUT: api/orders/:id/status
  [HttpPut("{id}/status")]
  [Authorize(Roles = "admin")]
  public async Task<ActionResult<OrderDto>> UpdateOrderStatus(int id, [FromBody] UpdateOrderStatusDto dto)
  {
    try
    {
      var order = await _commandService.UpdateOrderStatusAsync(id, dto.Status);
      if (order == null)
        return NotFound(new { error = "Order not found" });

      _logger.LogInformation("Order {OrderId} status updated to {Status}", id, dto.Status);
      return Ok(MapOrderToDto(order));
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error updating order status");
      return StatusCode(500, new { error = "Error updating order status" });
    }
  }

  // POST: api/orders/:id/cancel
  [HttpPost("{id}/cancel")]
  [Authorize]
  public async Task<ActionResult<OrderDto>> CancelOrder(int id, [FromBody] CancelOrderDto dto)
  {
    try
    {
      var order = await _queryService.GetOrderByIdAsync(id);
      if (order == null)
        return NotFound(new { error = "Order not found" });

      var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

      // Only order owner or admin can cancel
      if (order.UserId != userId && !User.IsInRole("admin"))
        return Forbid();

      var cancelledOrder = await _commandService.CancelOrderAsync(id, userId, dto.Reason);

      _logger.LogInformation("Order {OrderId} cancelled by user {UserId}", id, userId);
      return Ok(MapOrderToDto(cancelledOrder!));
    }
    catch (InvalidOperationException ex)
    {
      _logger.LogWarning(ex, "Cannot cancel order");
      return BadRequest(new { error = ex.Message });
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error cancelling order");
      return StatusCode(500, new { error = "Error cancelling order" });
    }
  }

  // POST: api/orders/:id/refund
  [HttpPost("{id}/refund")]
  [Authorize]
  public async Task<ActionResult<OrderDto>> RequestRefund(int id, [FromBody] RefundRequestDto dto)
  {
    try
    {
      var order = await _queryService.GetOrderByIdAsync(id);
      if (order == null)
        return NotFound(new { error = "Order not found" });

      var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

      // Only order owner or admin can request refund
      if (order.UserId != userId && !User.IsInRole("admin"))
        return Forbid();

      var refundOrder = await _commandService.RequestRefundAsync(id, dto.Reason);

      _logger.LogInformation("Refund requested for order {OrderId} by user {UserId}", id, userId);
      return Ok(MapOrderToDto(refundOrder!));
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error requesting refund");
      return StatusCode(500, new { error = "Error requesting refund" });
    }
  }

  private OrderDto MapOrderToDto(Order order)
  {
    return new OrderDto
    {
      Id = order.Id,
      UserId = order.UserId,
      Items = order.Items.Select(oi => new OrderItemDto
      {
        ProductId = oi.ProductId,
        Quantity = oi.Quantity,
        SelectedOptions = oi.SelectedOptions
      }).ToList(),
      TotalPrice = order.TotalPrice,
      Status = order.Status,
      PoNumber = order.PoNumber,
      PaymentDueDate = order.PaymentDueDate,
      PaymentMethod = order.PaymentMethod,
      CreatedAt = order.CreatedAt,
      UpdatedAt = order.UpdatedAt
    };
  }
}
