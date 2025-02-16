using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.Cookies;
using API.Services;
using API.Models;

namespace API.Controller;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
  private readonly IProductCommandService _productCommandService;
  private readonly IProductQueryService _productQueryService;

  public ProductsController(IProductCommandService productCommandService, IProductQueryService productQueryService)
  {
    _productCommandService = productCommandService;
    _productQueryService = productQueryService;
  }

  [HttpGet]
  [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme + "," + CookieAuthenticationDefaults.AuthenticationScheme)]
  public async Task<IActionResult> GetAllProducts()
  {
    var Username = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    var isAdmin = HttpContext.User.IsInRole("Admin");

    var products = await _productQueryService.GetAllProductsAsync();
    return Ok(products);
  }

  [HttpGet("{id}")]
  public async Task<IActionResult> GetProductById(int id)
  {
    var product = await _productQueryService.GetProductByIdAsync(id);
    if (product == null)
      return NotFound();

    return Ok(product);
  }

  [Authorize]
  [HttpPost]
  public async Task<IActionResult> AddProduct([FromBody] Product product)
  {
    bool isAdmin = User.Claims.Any(c => c.Type == ClaimTypes.Role && c.Value == "Admin");

    var success = await _productCommandService.AddProductAsync(product, isAdmin);
    if (!success)
      return Forbid("Only admins can add products.");

    return Ok($"Product added successfully. {User.Identity?.Name}");
  }

  [Authorize]
  [HttpPut("{id}")]
  public async Task<IActionResult> UpdateProduct(int id, [FromBody] Product product)
  {
    bool isAdmin = User.Claims.Any(c => c.Type == ClaimTypes.Role && c.Value == "Admin");

    var success = await _productCommandService.UpdateProductAsync(product, isAdmin);
    if (!success)
      return Forbid("Only admins can update products.");

    return Ok("Product updated successfully.");
  }

  [Authorize]
  [HttpDelete("{id}")]
  public async Task<IActionResult> DeleteProduct(int id)
  {
    bool isAdmin = User.Claims.Any(c => c.Type == ClaimTypes.Role && c.Value == "Admin");

    var success = await _productCommandService.DeleteProductAsync(id, isAdmin);
    if (!success)
      return Forbid("Only admins can delete products.");

    return Ok("Product deleted successfully.");
  }
}
