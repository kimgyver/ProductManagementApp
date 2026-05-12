using API.Models;
using API.Infrastructure;
using API.DTOs;
using Microsoft.EntityFrameworkCore;

namespace API.Services;

public interface IAdminService
{
  Task<DashboardStatsDto> GetDashboardStatsAsync();
  Task<List<AdminUserDto>> GetAllUsersAsync();
  Task<List<AdminOrderDetailsDto>> GetAllOrdersAsync();
  Task<AdminOrderDetailsDto?> GetOrderDetailsAsync(int orderId);
}

public class AdminService : IAdminService
{
  private readonly ApplicationDbContext _context;
  private readonly IOrderRepository _orderRepository;
  private readonly ILogger<AdminService> _logger;

  public AdminService(
    ApplicationDbContext context,
    IOrderRepository orderRepository,
    ILogger<AdminService> logger)
  {
    _context = context;
    _orderRepository = orderRepository;
    _logger = logger;
  }

  public async Task<DashboardStatsDto> GetDashboardStatsAsync()
  {
    try
    {
      var allOrders = (await _orderRepository.GetAllAsync()).ToList();
      var allUsers = await _context.Users.ToListAsync();
      var allProducts = await _context.Products.ToListAsync();
      var allOrderItems = await _context.OrderItems.ToListAsync();

      var pendingOrders = allOrders.Where(o => o.Status == "pending" || o.Status == "pending_payment").ToList();
      var completedOrders = allOrders.Where(o => o.Status == "delivered").ToList();
      var cancelledOrders = allOrders.Where(o => o.Status == "cancelled").ToList();

      var stats = new DashboardStatsDto
      {
        TotalUsers = allUsers.Count,
        TotalOrders = allOrders.Count,
        TotalRevenue = allOrders
          .Where(o => o.Status == "paid" || o.Status == "processing" || o.Status == "shipped" || o.Status == "delivered")
          .Sum(o => o.TotalPrice),
        PendingOrders = pendingOrders.Count,
        CompletedOrders = completedOrders.Count,
        CancelledOrders = cancelledOrders.Count,
        RecentOrders = allOrders
          .OrderByDescending(o => o.CreatedAt)
          .Take(10)
          .Select(o => new OrderSummaryDto
          {
            Id = o.Id,
            UserEmail = o.User?.Email,
            TotalPrice = o.TotalPrice,
            Status = o.Status,
            CreatedAt = o.CreatedAt
          })
          .ToList(),
        TopProducts = allProducts
          .Select(p => new ProductSalesDto
          {
            ProductId = p.Id,
            ProductName = p.Name,
            SalesCount = allOrderItems.Where(oi => oi.ProductId == p.Id).Count(),
            Revenue = allOrderItems
              .Where(oi => oi.ProductId == p.Id)
              .Sum(oi => oi.Price * oi.Quantity)
          })
          .OrderByDescending(ps => ps.Revenue)
          .Take(10)
          .ToList()
      };

      _logger.LogInformation("[Admin] Dashboard stats generated");
      return stats;
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error getting dashboard stats");
      throw;
    }
  }

  public async Task<List<AdminUserDto>> GetAllUsersAsync()
  {
    try
    {
      var users = await _context.Users
        .OrderByDescending(u => u.CreatedAt)
        .ToListAsync();

      return users.Select(u => new AdminUserDto
      {
        Id = u.Id,
        Username = u.Username,
        Email = u.Email,
        Role = u.Role,
        Verified = u.Verified,
        CreatedAt = u.CreatedAt
      }).ToList();
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error getting all users");
      throw;
    }
  }

  public async Task<List<AdminOrderDetailsDto>> GetAllOrdersAsync()
  {
    try
    {
      var orders = await _orderRepository.GetAllAsync();

      return orders
        .OrderByDescending(o => o.CreatedAt)
        .Select(o => new AdminOrderDetailsDto
        {
          Id = o.Id,
          UserId = o.UserId,
          UserEmail = o.User?.Email ?? "",
          TotalPrice = o.TotalPrice,
          Status = o.Status,
          PaymentIntentId = o.PaymentIntentId,
          CreatedAt = o.CreatedAt,
          UpdatedAt = o.UpdatedAt,
          Items = o.Items.Select(oi => new OrderItemDto
          {
            ProductId = oi.ProductId,
            Quantity = oi.Quantity,
            SelectedOptions = oi.SelectedOptions
          }).ToList()
        })
        .ToList();
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error getting all orders");
      throw;
    }
  }

  public async Task<AdminOrderDetailsDto?> GetOrderDetailsAsync(int orderId)
  {
    try
    {
      var order = await _orderRepository.GetByIdAsync(orderId);
      if (order == null) return null;

      return new AdminOrderDetailsDto
      {
        Id = order.Id,
        UserId = order.UserId,
        UserEmail = order.User?.Email ?? "",
        TotalPrice = order.TotalPrice,
        Status = order.Status,
        PaymentIntentId = order.PaymentIntentId,
        CreatedAt = order.CreatedAt,
        UpdatedAt = order.UpdatedAt,
        Items = order.Items.Select(oi => new OrderItemDto
        {
          ProductId = oi.ProductId,
          Quantity = oi.Quantity,
          SelectedOptions = oi.SelectedOptions
        }).ToList()
      };
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error getting order details");
      throw;
    }
  }
}

