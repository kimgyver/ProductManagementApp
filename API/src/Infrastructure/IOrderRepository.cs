using API.Models;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace API.Infrastructure;

public interface IOrderRepository
{
  Task<Order?> GetByIdAsync(int id);
  Task<IEnumerable<Order>> GetByUserIdAsync(int userId);
  Task<IEnumerable<Order>> GetAllAsync();
  Task<Order> AddAsync(Order order);
  Task<Order?> UpdateAsync(Order order);
  Task<bool> DeleteAsync(int id);
  Task<Order?> GetByPoNumberAsync(string poNumber);
  Task SaveChangesAsync();
}

public class OrderRepository : IOrderRepository
{
  private readonly ApplicationDbContext _context;

  public OrderRepository(ApplicationDbContext context)
  {
    _context = context;
  }

  public async Task<Order?> GetByIdAsync(int id)
  {
    try
    {
      return await _context.Orders
        .Include(o => o.Items)
        .ThenInclude(oi => oi.Product)
        .Include(o => o.User)
        .FirstOrDefaultAsync(o => o.Id == id);
    }
    catch (Exception ex) when (IsOrderSchemaMismatch(ex))
    {
      return null;
    }
  }

  public async Task<IEnumerable<Order>> GetByUserIdAsync(int userId)
  {
    try
    {
      return await _context.Orders
        .Where(o => o.UserId == userId)
        .Include(o => o.Items)
        .ThenInclude(oi => oi.Product)
        .OrderByDescending(o => o.CreatedAt)
        .ToListAsync();
    }
    catch (Exception ex) when (IsOrderSchemaMismatch(ex))
    {
      return Enumerable.Empty<Order>();
    }
  }

  public async Task<IEnumerable<Order>> GetAllAsync()
  {
    try
    {
      return await _context.Orders
        .Include(o => o.Items)
        .ThenInclude(oi => oi.Product)
        .Include(o => o.User)
        .OrderByDescending(o => o.CreatedAt)
        .ToListAsync();
    }
    catch (Exception ex) when (IsOrderSchemaMismatch(ex))
    {
      return Enumerable.Empty<Order>();
    }
  }

  public async Task<Order> AddAsync(Order order)
  {
    _context.Orders.Add(order);
    await _context.SaveChangesAsync();
    return order;
  }

  public async Task<Order?> UpdateAsync(Order order)
  {
    _context.Orders.Update(order);
    await _context.SaveChangesAsync();
    return order;
  }

  public async Task<bool> DeleteAsync(int id)
  {
    var order = await GetByIdAsync(id);
    if (order == null) return false;

    _context.Orders.Remove(order);
    await _context.SaveChangesAsync();
    return true;
  }

  public async Task<Order?> GetByPoNumberAsync(string poNumber)
  {
    try
    {
      return await _context.Orders
        .Include(o => o.Items)
        .ThenInclude(oi => oi.Product)
        .FirstOrDefaultAsync(o => o.PoNumber == poNumber);
    }
    catch (Exception ex) when (IsOrderSchemaMismatch(ex))
    {
      return null;
    }
  }

  public async Task SaveChangesAsync()
  {
    await _context.SaveChangesAsync();
  }

  private static bool IsOrderSchemaMismatch(Exception ex)
  {
    if (ex is PostgresException pg && (pg.SqlState == "42P01" || pg.SqlState == "42703" || pg.SqlState == "22P02"))
    {
      return true;
    }

    return ex.InnerException != null && IsOrderSchemaMismatch(ex.InnerException);
  }
}
