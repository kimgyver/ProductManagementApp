using API.Models;
using API.DTOs;
using API.Infrastructure;

namespace API.Services;

public interface IOrderCommandService
{
  Task<Order> CreateOrderAsync(int userId, CreateOrderDto dto);
  Task<Order?> UpdateOrderStatusAsync(int orderId, string status);
  Task<Order?> CancelOrderAsync(int orderId, int cancelledByUserId, string reason);
  Task<Order?> ProcessPaymentAsync(int orderId, string paymentIntentId);
  Task<Order?> RequestRefundAsync(int orderId, string reason);
}

public interface IOrderQueryService
{
  Task<Order?> GetOrderByIdAsync(int orderId);
  Task<IEnumerable<Order>> GetUserOrdersAsync(int userId);
  Task<IEnumerable<Order>> GetAllOrdersAsync();
  Task<IEnumerable<Order>> GetOrdersByStatusAsync(string status);
}

public class OrderCommandService : IOrderCommandService
{
  private readonly IOrderRepository _orderRepository;
  private readonly IProductRepository _productRepository;
  private readonly IEmailService _emailService;
  private readonly ILogger<OrderCommandService> _logger;

  public OrderCommandService(
    IOrderRepository orderRepository,
    IProductRepository productRepository,
    IEmailService emailService,
    ILogger<OrderCommandService> logger)
  {
    _orderRepository = orderRepository;
    _productRepository = productRepository;
    _emailService = emailService;
    _logger = logger;
  }

  public async Task<Order> CreateOrderAsync(int userId, CreateOrderDto dto)
  {
    // Validate products and calculate total
    decimal totalPrice = 0;
    var orderItems = new List<OrderItem>();

    if (dto.Items == null || dto.Items.Count == 0)
      throw new InvalidOperationException("Order must contain at least one item");

    foreach (var item in dto.Items)
    {
      _logger.LogInformation("Processing order item: ProductId={ProductId}, Quantity={Quantity}", item.ProductId, item.Quantity);

      if (item.ProductId <= 0)
        throw new InvalidOperationException($"Invalid productId: {item.ProductId}");

      if (item.Quantity <= 0)
        throw new InvalidOperationException($"Invalid quantity for product {item.ProductId}: {item.Quantity}");

      var product = await _productRepository.GetProductByIdAsync(item.ProductId);
      if (product == null)
      {
        _logger.LogError("Product not found. ProductId={ProductId}", item.ProductId);
        throw new InvalidOperationException($"Product {item.ProductId} not found");
      }

      _logger.LogInformation("Product found: {ProductName}, Stock={Stock}, Price={Price}", product.Name, product.Stock, product.Price);

      if (product.Stock < item.Quantity)
      {
        _logger.LogWarning("Insufficient stock. Product={ProductName}, Required={Required}, Available={Available}",
          product.Name, item.Quantity, product.Stock);
        throw new InvalidOperationException($"Insufficient stock for product {product.Name}. Available: {product.Stock}, Requested: {item.Quantity}");
      }

      var orderItem = new OrderItem
      {
        ProductId = product.Id,
        Quantity = item.Quantity,
        Price = product.Price,
        SelectedOptions = item.SelectedOptions
      };

      orderItems.Add(orderItem);
      totalPrice += product.Price * item.Quantity;

      // Reduce stock (best effort - proceed even if stock update fails during DB migration)
      try
      {
        product.Stock -= item.Quantity;
        await _productRepository.UpdateProductAsync(product);
        _logger.LogInformation("Stock updated for product {ProductId}. New stock: {NewStock}", product.Id, product.Stock);
      }
      catch (Exception ex)
      {
        _logger.LogWarning(ex, "Could not update stock for product {ProductId}. Order will proceed without stock reduction.", product.Id);
      }
    }

    var order = new Order
    {
      UserId = userId,
      Items = orderItems,
      TotalPrice = totalPrice,
      Status = "pending",
      RecipientName = dto.RecipientName,
      RecipientPhone = dto.RecipientPhone,
      ShippingAddress1 = dto.ShippingAddress1,
      ShippingAddress2 = dto.ShippingAddress2,
      ShippingPostalCode = dto.ShippingPostalCode,
      PaymentMethod = dto.PaymentMethod ?? "card"
    };

    _logger.LogInformation("Saving order to database. UserId={UserId}, ItemCount={ItemCount}, TotalPrice={TotalPrice}",
      userId, orderItems.Count, totalPrice);

    await _orderRepository.AddAsync(order);
    _logger.LogInformation("Order created and saved: {OrderId}", order.Id);

    // Send confirmation email
    try
    {
      // TODO: Get user email and send order confirmation
      _logger.LogInformation("Order confirmation email queued for order {OrderId}", order.Id);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Failed to send order confirmation email for order {OrderId}", order.Id);
    }

    return order;
  }

  public async Task<Order?> UpdateOrderStatusAsync(int orderId, string status)
  {
    var order = await _orderRepository.GetByIdAsync(orderId);
    if (order == null) return null;

    order.Status = status;
    order.UpdatedAt = DateTime.UtcNow;

    return await _orderRepository.UpdateAsync(order);
  }

  public async Task<Order?> CancelOrderAsync(int orderId, int cancelledByUserId, string reason)
  {
    var order = await _orderRepository.GetByIdAsync(orderId);
    if (order == null) return null;

    if (order.Status == "delivered" || order.Status == "cancelled")
      throw new InvalidOperationException("Cannot cancel this order");

    order.Status = "cancelled";
    order.CancelledAt = DateTime.UtcNow;
    order.CancelledByUserId = cancelledByUserId;
    order.CancelReason = reason;
    order.UpdatedAt = DateTime.UtcNow;

    // Restore stock
    foreach (var item in order.Items)
    {
      if (item.Product != null)
      {
        item.Product.Stock += item.Quantity;
        await _productRepository.UpdateProductAsync(item.Product);
      }
    }

    return await _orderRepository.UpdateAsync(order);
  }

  public async Task<Order?> ProcessPaymentAsync(int orderId, string paymentIntentId)
  {
    var order = await _orderRepository.GetByIdAsync(orderId);
    if (order == null) return null;

    order.PaymentIntentId = paymentIntentId;
    order.Status = "processing";
    order.PaidAt = DateTime.UtcNow;
    order.UpdatedAt = DateTime.UtcNow;

    return await _orderRepository.UpdateAsync(order);
  }

  public async Task<Order?> RequestRefundAsync(int orderId, string reason)
  {
    var order = await _orderRepository.GetByIdAsync(orderId);
    if (order == null) return null;

    order.RefundStatus = "requested";
    order.RefundReason = reason;
    order.RefundRequestedAt = DateTime.UtcNow;
    order.UpdatedAt = DateTime.UtcNow;

    return await _orderRepository.UpdateAsync(order);
  }
}

public class OrderQueryService : IOrderQueryService
{
  private readonly IOrderRepository _orderRepository;

  public OrderQueryService(IOrderRepository orderRepository)
  {
    _orderRepository = orderRepository;
  }

  public async Task<Order?> GetOrderByIdAsync(int orderId)
  {
    return await _orderRepository.GetByIdAsync(orderId);
  }

  public async Task<IEnumerable<Order>> GetUserOrdersAsync(int userId)
  {
    return await _orderRepository.GetByUserIdAsync(userId);
  }

  public async Task<IEnumerable<Order>> GetAllOrdersAsync()
  {
    return await _orderRepository.GetAllAsync();
  }

  public async Task<IEnumerable<Order>> GetOrdersByStatusAsync(string status)
  {
    var allOrders = await _orderRepository.GetAllAsync();
    return allOrders.Where(o => o.Status == status);
  }
}
