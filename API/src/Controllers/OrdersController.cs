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
  private readonly IUserQueryService _userQueryService;
  private readonly ILogger<OrdersController> _logger;

  public OrdersController(
    IOrderCommandService commandService,
    IOrderQueryService queryService,
    IUserQueryService userQueryService,
    ILogger<OrdersController> logger)
  {
    _commandService = commandService;
    _queryService = queryService;
    _userQueryService = userQueryService;
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

    var userId = await ResolveUserIdAsync();
    if (userId == null)
      return Unauthorized(new { error = "Invalid user token. Please login again." });

    // Check if user owns this order or is admin
    if (order.UserId != userId.Value && !User.IsInRole("admin"))
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
      var userId = await ResolveUserIdAsync();
      if (userId == null)
        return Unauthorized(new { error = "Invalid user token. Please login again." });

      var orders = await _queryService.GetUserOrdersAsync(userId.Value);

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
      _logger.LogWarning("ModelState validation failed for CreateOrder. Errors: {Errors}", string.Join("; ", errors));
      return BadRequest(new { error = "Invalid order data", details = errors });
    }

    try
    {
      _logger.LogInformation("CreateOrder request received with {ItemCount} items", dto.Items?.Count ?? 0);

      var userId = await ResolveUserIdAsync();
      if (userId == null)
      {
        _logger.LogWarning("ResolveUserIdAsync returned null. User claims: NameIdentifier={NameId}, Email={Email}, Sub={Sub}",
          User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
          User.FindFirst(ClaimTypes.Email)?.Value,
          User.FindFirst("sub")?.Value);
        return Unauthorized(new { error = "Invalid user token. Please login again." });
      }

      _logger.LogInformation("Creating order for userId {UserId} with items: {@Items}", userId, dto.Items);

      var order = await _commandService.CreateOrderAsync(userId.Value, dto);

      _logger.LogInformation("Order created successfully: {OrderId} for user {UserId}", order.Id, userId.Value);

      return CreatedAtAction(nameof(GetOrder), new { id = order.Id }, MapOrderToDto(order));
    }
    catch (InvalidOperationException ex)
    {
      _logger.LogWarning(ex, "Invalid order creation attempt: {Message}", ex.Message);
      return BadRequest(new { error = ex.Message, details = new { exception = "InvalidOperationException" } });
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error creating order. Exception type: {ExceptionType}, Message: {Message}, StackTrace: {StackTrace}",
        ex.GetType().Name, ex.Message, ex.StackTrace);
      return StatusCode(500, new { 
        error = "Error creating order", 
        message = ex.Message,
        exceptionType = ex.GetType().Name,
        details = ex.InnerException?.Message
      });
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

      var userId = await ResolveUserIdAsync();
      if (userId == null)
        return Unauthorized(new { error = "Invalid user token. Please login again." });

      // Only order owner or admin can cancel
      if (order.UserId != userId.Value && !User.IsInRole("admin"))
        return Forbid();

      var cancelledOrder = await _commandService.CancelOrderAsync(id, userId.Value, dto.Reason);

      _logger.LogInformation("Order {OrderId} cancelled by user {UserId}", id, userId.Value);
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

      var userId = await ResolveUserIdAsync();
      if (userId == null)
        return Unauthorized(new { error = "Invalid user token. Please login again." });

      // Only order owner or admin can request refund
      if (order.UserId != userId.Value && !User.IsInRole("admin"))
        return Forbid();

      var refundOrder = await _commandService.RequestRefundAsync(id, dto.Reason);

      _logger.LogInformation("Refund requested for order {OrderId} by user {UserId}", id, userId.Value);
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

  private async Task<int?> ResolveUserIdAsync()
  {
    var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
      ?? User.FindFirst("sub")?.Value;

    if (int.TryParse(idClaim, out var parsedUserId) && parsedUserId > 0)
    {
      return parsedUserId;
    }

    var email = User.FindFirst(ClaimTypes.Email)?.Value;
    if (string.IsNullOrWhiteSpace(email))
    {
      return null;
    }

    return await _userQueryService.GetUserIdByEmailAsync(email);
  }
}
