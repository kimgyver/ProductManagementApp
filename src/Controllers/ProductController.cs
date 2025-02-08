using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.Cookies;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
  private readonly IProductService _productService;

  public ProductsController(IProductService productService)
  {
    _productService = productService;
  }

  [HttpGet]
  [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme + "," + CookieAuthenticationDefaults.AuthenticationScheme)]
  public async Task<IActionResult> GetAllProducts()
  {
    var Username = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    var isAdmin = HttpContext.User.IsInRole("Admin");

    var products = await _productService.GetAllProductsAsync();
    return Ok(products);
  }

  [HttpGet("{id}")]
  public async Task<IActionResult> GetProductById(int id)
  {
    var product = await _productService.GetProductByIdAsync(id);
    if (product == null)
      return NotFound();

    return Ok(product);
  }

  [Authorize]
  [HttpPost]
  public async Task<IActionResult> AddProduct([FromBody] Product product)
  {
    bool isAdmin = User.Claims.Any(c => c.Type == ClaimTypes.Role && c.Value == "Admin");

    var success = await _productService.AddProductAsync(product, isAdmin);
    if (!success)
      return Forbid("Only admins can add products.");

    return Ok($"Product added successfully. {User.Identity?.Name}");
  }

  [Authorize]
  [HttpPut("{id}")]
  public async Task<IActionResult> UpdateProduct(int id, [FromBody] Product product)
  {
    bool isAdmin = User.Claims.Any(c => c.Type == ClaimTypes.Role && c.Value == "Admin");

    var success = await _productService.UpdateProductAsync(product, isAdmin);
    if (!success)
      return Forbid("Only admins can update products.");

    return Ok("Product updated successfully.");
  }

  [Authorize]
  [HttpDelete("{id}")]
  public async Task<IActionResult> DeleteProduct(int id)
  {
    bool isAdmin = User.Claims.Any(c => c.Type == ClaimTypes.Role && c.Value == "Admin");

    var success = await _productService.DeleteProductAsync(id, isAdmin);
    if (!success)
      return Forbid("Only admins can delete products.");

    return Ok("Product deleted successfully.");
  }
}
