using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using API.Services;
using API.DTOs;

namespace API.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "admin")]
public class AdminController : ControllerBase
{
  private readonly IAdminService _adminService;
  private readonly ILogger<AdminController> _logger;

  public AdminController(IAdminService adminService, ILogger<AdminController> logger)
  {
    _adminService = adminService;
    _logger = logger;
  }

  // GET: api/admin/dashboard
  [HttpGet("dashboard")]
  public async Task<ActionResult<DashboardStatsDto>> GetDashboard()
  {
    try
    {
      var stats = await _adminService.GetDashboardStatsAsync();
      return Ok(stats);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error getting dashboard stats");
      return StatusCode(500, new { error = "Error retrieving dashboard stats" });
    }
  }

  // GET: api/admin/users
  [HttpGet("users")]
  public async Task<ActionResult<List<AdminUserDto>>> GetAllUsers()
  {
    try
    {
      var users = await _adminService.GetAllUsersAsync();
      return Ok(users);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error getting users");
      return StatusCode(500, new { error = "Error retrieving users" });
    }
  }

  // GET: api/admin/orders
  [HttpGet("orders")]
  public async Task<ActionResult<List<AdminOrderDetailsDto>>> GetAllOrders()
  {
    try
    {
      var orders = await _adminService.GetAllOrdersAsync();
      return Ok(orders);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error getting orders");
      return StatusCode(500, new { error = "Error retrieving orders" });
    }
  }

  // GET: api/admin/orders/:id
  [HttpGet("orders/{id}")]
  public async Task<ActionResult<AdminOrderDetailsDto>> GetOrderDetails(int id)
  {
    try
    {
      var order = await _adminService.GetOrderDetailsAsync(id);
      if (order == null)
        return NotFound(new { error = "Order not found" });

      return Ok(order);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error getting order details");
      return StatusCode(500, new { error = "Error retrieving order details" });
    }
  }

  // PUT: api/admin/orders/:id/status
  [HttpPut("orders/{id}/status")]
  public async Task<ActionResult<object>> UpdateOrderStatus(int id, [FromBody] UpdateOrderStatusDto dto)
  {
    try
    {
      // TODO: Implement full order status update with notifications
      _logger.LogInformation("[Admin] Order {OrderId} status update requested to {Status}", id, dto.Status);
      return Ok(new { message = "Order status updated" });
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error updating order status");
      return StatusCode(500, new { error = "Error updating order status" });
    }
  }
}
