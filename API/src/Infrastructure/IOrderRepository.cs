using API.Models;
using Microsoft.EntityFrameworkCore;

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
    return await _context.Orders
      .Include(o => o.Items)
      .ThenInclude(oi => oi.Product)
      .Include(o => o.User)
      .FirstOrDefaultAsync(o => o.Id == id);
  }

  public async Task<IEnumerable<Order>> GetByUserIdAsync(int userId)
  {
    return await _context.Orders
      .Where(o => o.UserId == userId)
      .Include(o => o.Items)
      .ThenInclude(oi => oi.Product)
      .OrderByDescending(o => o.CreatedAt)
      .ToListAsync();
  }

  public async Task<IEnumerable<Order>> GetAllAsync()
  {
    return await _context.Orders
      .Include(o => o.Items)
      .ThenInclude(oi => oi.Product)
      .Include(o => o.User)
      .OrderByDescending(o => o.CreatedAt)
      .ToListAsync();
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
    return await _context.Orders
      .Include(o => o.Items)
      .ThenInclude(oi => oi.Product)
      .FirstOrDefaultAsync(o => o.PoNumber == poNumber);
  }

  public async Task SaveChangesAsync()
  {
    await _context.SaveChangesAsync();
  }
}
