using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using API.Infrastructure;
using API.Models;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CartController : ControllerBase
{
  private readonly ApplicationDbContext _context;
  private readonly ILogger<CartController> _logger;

  public CartController(ApplicationDbContext context, ILogger<CartController> logger)
  {
    _context = context;
    _logger = logger;
  }

  // GET: api/cart
  [HttpGet]
  [Authorize]
  public async Task<ActionResult<object>> GetCart()
  {
    var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
    
    var cart = await _context.Carts
      .Include(c => c.Items)
      .ThenInclude(ci => ci.Product)
      .FirstOrDefaultAsync(c => c.UserId == userId);

    if (cart == null)
    {
      // Create new cart if doesn't exist
      cart = new Cart { UserId = userId };
      _context.Carts.Add(cart);
      await _context.SaveChangesAsync();
    }

    return Ok(new
    {
      id = cart.Id,
      items = cart.Items.Select(ci => new
      {
        id = ci.Id,
        productId = ci.ProductId,
        productName = ci.Product?.Name,
        quantity = ci.Quantity,
        price = ci.Product?.Price,
        selectedOptions = ci.SelectedOptions,
        variantKey = ci.VariantKey
      }).ToList(),
      totalPrice = cart.Items.Sum(ci => (ci.Product?.Price ?? 0) * ci.Quantity)
    });
  }

  // POST: api/cart/items
  [HttpPost("items")]
  [Authorize]
  public async Task<ActionResult<object>> AddToCart([FromBody] AddToCartDto dto)
  {
    if (!ModelState.IsValid)
      return BadRequest(ModelState);

    try
    {
      var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
      
      var product = await _context.Products.FindAsync(dto.ProductId);
      if (product == null)
        return NotFound(new { error = "Product not found" });

      var cart = await _context.Carts
        .Include(c => c.Items)
        .FirstOrDefaultAsync(c => c.UserId == userId);

      if (cart == null)
      {
        cart = new Cart { UserId = userId };
        _context.Carts.Add(cart);
        await _context.SaveChangesAsync();
      }

      var variantKey = GenerateVariantKey(dto.SelectedOptions);
      var existingItem = cart.Items.FirstOrDefault(ci =>
        ci.ProductId == dto.ProductId && ci.VariantKey == variantKey);

      if (existingItem != null)
      {
        existingItem.Quantity += dto.Quantity;
        existingItem.UpdatedAt = DateTime.UtcNow;
      }
      else
      {
        var cartItem = new CartItem
        {
          CartId = cart.Id,
          ProductId = dto.ProductId,
          Quantity = dto.Quantity,
          SelectedOptions = dto.SelectedOptions,
          VariantKey = variantKey
        };
        cart.Items.Add(cartItem);
      }

      cart.UpdatedAt = DateTime.UtcNow;
      await _context.SaveChangesAsync();

      _logger.LogInformation("Product {ProductId} added to cart for user {UserId}", dto.ProductId, userId);

      return Ok(new { message = "Item added to cart" });
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error adding item to cart");
      return StatusCode(500, new { error = "Error adding item to cart" });
    }
  }

  // PUT: api/cart/items/:itemId
  [HttpPut("items/{itemId}")]
  [Authorize]
  public async Task<ActionResult<object>> UpdateCartItem(int itemId, [FromBody] UpdateCartItemDto dto)
  {
    try
    {
      var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
      
      var cartItem = await _context.CartItems
        .Include(ci => ci.Cart)
        .FirstOrDefaultAsync(ci => ci.Id == itemId);

      if (cartItem == null)
        return NotFound(new { error = "Cart item not found" });

      if (cartItem.Cart?.UserId != userId)
        return Forbid();

      cartItem.Quantity = dto.Quantity;
      cartItem.UpdatedAt = DateTime.UtcNow;

      await _context.SaveChangesAsync();

      return Ok(new { message = "Cart item updated" });
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error updating cart item");
      return StatusCode(500, new { error = "Error updating cart item" });
    }
  }

  // DELETE: api/cart/items/:itemId
  [HttpDelete("items/{itemId}")]
  [Authorize]
  public async Task<ActionResult<object>> RemoveFromCart(int itemId)
  {
    try
    {
      var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
      
      var cartItem = await _context.CartItems
        .Include(ci => ci.Cart)
        .FirstOrDefaultAsync(ci => ci.Id == itemId);

      if (cartItem == null)
        return NotFound(new { error = "Cart item not found" });

      if (cartItem.Cart?.UserId != userId)
        return Forbid();

      _context.CartItems.Remove(cartItem);
      await _context.SaveChangesAsync();

      _logger.LogInformation("Item {ItemId} removed from cart for user {UserId}", itemId, userId);

      return Ok(new { message = "Item removed from cart" });
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error removing item from cart");
      return StatusCode(500, new { error = "Error removing item from cart" });
    }
  }

  // DELETE: api/cart
  [HttpDelete]
  [Authorize]
  public async Task<ActionResult<object>> ClearCart()
  {
    try
    {
      var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
      
      var cart = await _context.Carts
        .Include(c => c.Items)
        .FirstOrDefaultAsync(c => c.UserId == userId);

      if (cart == null)
        return NotFound(new { error = "Cart not found" });

      _context.CartItems.RemoveRange(cart.Items);
      await _context.SaveChangesAsync();

      _logger.LogInformation("Cart cleared for user {UserId}", userId);

      return Ok(new { message = "Cart cleared" });
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error clearing cart");
      return StatusCode(500, new { error = "Error clearing cart" });
    }
  }

  private string GenerateVariantKey(string? selectedOptions)
  {
    if (string.IsNullOrEmpty(selectedOptions))
      return "";

    return selectedOptions;
  }
}

public class AddToCartDto
{
  public int ProductId { get; set; }
  public int Quantity { get; set; }
  public string? SelectedOptions { get; set; }
}

public class UpdateCartItemDto
{
  public int Quantity { get; set; }
}
