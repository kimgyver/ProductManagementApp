using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using API.Services;
using System.Security.Claims;
using System.Text.Json;
using System.Collections.Concurrent;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CartController : ControllerBase
{
  private readonly IProductQueryService _productQueryService;
  private readonly ILogger<CartController> _logger;
  private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
  private static readonly ConcurrentDictionary<string, List<SessionCartItem>> CartStore = new(StringComparer.OrdinalIgnoreCase);

  public CartController(IProductQueryService productQueryService, ILogger<CartController> logger)
  {
    _productQueryService = productQueryService;
    _logger = logger;
  }

  // GET: api/cart
  [HttpGet]
  [Authorize]
  public async Task<ActionResult<object>> GetCart()
  {
    var userKey = GetUserKey();
    if (string.IsNullOrWhiteSpace(userKey))
      return Unauthorized(new { error = "Invalid user context" });

    var sessionCart = GetCartFromStore(userKey);
    var products = (await _productQueryService.GetAllProductsAsync()).ToDictionary(p => p.Id);

    var items = sessionCart.Select(ci =>
    {
      products.TryGetValue(ci.ProductId, out var product);
      var price = product?.Price ?? 0m;
      return new
      {
        id = ci.Id,
        productId = ci.ProductId,
        productName = product?.Name,
        quantity = ci.Quantity,
        price,
        selectedOptions = ci.SelectedOptions,
        variantKey = ci.VariantKey
      };
    }).ToList();

    return Ok(new
    {
      id = userKey,
      items,
      totalPrice = items.Sum(ci => ci.price * ci.quantity)
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
      var userKey = GetUserKey();
      if (string.IsNullOrWhiteSpace(userKey))
        return Unauthorized(new { error = "Invalid user context" });

      if (dto.ProductId <= 0)
        return BadRequest(new { error = "Invalid product id" });

      var cart = GetCartFromStore(userKey);

      var variantKey = GenerateVariantKey(dto.SelectedOptions);
      var existingItem = cart.FirstOrDefault(ci =>
        ci.ProductId == dto.ProductId && ci.VariantKey == variantKey);

      if (existingItem != null)
      {
        existingItem.Quantity += dto.Quantity;
      }
      else
      {
        var cartItem = new SessionCartItem
        {
          Id = cart.Count == 0 ? 1 : cart.Max(c => c.Id) + 1,
          ProductId = dto.ProductId,
          Quantity = dto.Quantity <= 0 ? 1 : dto.Quantity,
          SelectedOptions = dto.SelectedOptions,
          VariantKey = variantKey
        };
        cart.Add(cartItem);
      }

      SaveCartToStore(userKey, cart);

      _logger.LogInformation("Product {ProductId} added to cart for user {UserKey}", dto.ProductId, userKey);

      return Ok(new { message = "Item added to cart" });
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error adding item to cart");
      return StatusCode(500, new { error = "Error adding item to cart", detail = ex.Message });
    }
  }

  // PUT: api/cart/items/:itemId
  [HttpPut("items/{itemId}")]
  [Authorize]
  public async Task<ActionResult<object>> UpdateCartItem(int itemId, [FromBody] UpdateCartItemDto dto)
  {
    try
    {
      var userKey = GetUserKey();
      if (string.IsNullOrWhiteSpace(userKey))
        return Unauthorized(new { error = "Invalid user context" });

      var cart = GetCartFromStore(userKey);
      var cartItem = cart.FirstOrDefault(ci => ci.Id == itemId);

      if (cartItem == null)
        return NotFound(new { error = "Cart item not found" });

      cartItem.Quantity = dto.Quantity <= 0 ? 1 : dto.Quantity;

      SaveCartToStore(userKey, cart);

      return Ok(new { message = "Cart item updated" });
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error updating cart item");
      return StatusCode(500, new { error = "Error updating cart item", detail = ex.Message });
    }
  }

  // DELETE: api/cart/items/:itemId
  [HttpDelete("items/{itemId}")]
  [Authorize]
  public async Task<ActionResult<object>> RemoveFromCart(int itemId)
  {
    try
    {
      var userKey = GetUserKey();
      if (string.IsNullOrWhiteSpace(userKey))
        return Unauthorized(new { error = "Invalid user context" });

      var cart = GetCartFromStore(userKey);
      var cartItem = cart.FirstOrDefault(ci => ci.Id == itemId);

      if (cartItem == null)
        return NotFound(new { error = "Cart item not found" });

      cart.Remove(cartItem);
      SaveCartToStore(userKey, cart);

      _logger.LogInformation("Item {ItemId} removed from cart for user {UserKey}", itemId, userKey);

      return Ok(new { message = "Item removed from cart" });
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error removing item from cart");
      return StatusCode(500, new { error = "Error removing item from cart", detail = ex.Message });
    }
  }

  // DELETE: api/cart
  [HttpDelete]
  [Authorize]
  public async Task<ActionResult<object>> ClearCart()
  {
    try
    {
      var userKey = GetUserKey();
      if (string.IsNullOrWhiteSpace(userKey))
        return Unauthorized(new { error = "Invalid user context" });

      SaveCartToStore(userKey, new List<SessionCartItem>());

      _logger.LogInformation("Cart cleared for user {UserKey}", userKey);

      return Ok(new { message = "Cart cleared" });
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error clearing cart");
      return StatusCode(500, new { error = "Error clearing cart", detail = ex.Message });
    }
  }

  private string? GetUserKey()
  {
    return User.FindFirst(ClaimTypes.Email)?.Value
      ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value
      ?? User.FindFirst(ClaimTypes.Name)?.Value;
  }

  private List<SessionCartItem> GetCartFromStore(string userKey)
  {
    if (!CartStore.TryGetValue(userKey, out var items))
    {
      return new List<SessionCartItem>();
    }

    // Return a copy so mutations are explicit via SaveCartToStore
    return JsonSerializer.Deserialize<List<SessionCartItem>>(JsonSerializer.Serialize(items, JsonOptions), JsonOptions)
      ?? new List<SessionCartItem>();
  }

  private void SaveCartToStore(string userKey, List<SessionCartItem> items)
  {
    CartStore[userKey] = items;
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

public class SessionCartItem
{
  public int Id { get; set; }
  public int ProductId { get; set; }
  public int Quantity { get; set; }
  public string VariantKey { get; set; } = string.Empty;
  public string? SelectedOptions { get; set; }
}
