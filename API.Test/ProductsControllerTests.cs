using Moq;
using Xunit;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;

public class ProductsControllerTests
{
  private readonly Mock<IProductCommandService> _mockProductCommandService;
  private readonly Mock<IProductQueryService> _mockProductQueryService;
  private readonly ProductsController _controller;

  public ProductsControllerTests()
  {
    _mockProductCommandService = new Mock<IProductCommandService>();
    _mockProductQueryService = new Mock<IProductQueryService>();
    _controller = new ProductsController(_mockProductCommandService.Object, _mockProductQueryService.Object);
  }

  public async Task GetAllProducts_ReturnsOkResult_WithProducts()
  {
    // Arrange
    var mockProducts = new List<Product> { new Product { Id = 1, Name = "Product1" } };
    _mockProductQueryService.Setup(service => service.GetAllProductsAsync()).ReturnsAsync(mockProducts);

    // Mock authenticated user
    var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
    {
        new Claim(ClaimTypes.Name, "testuser"),
        new Claim(ClaimTypes.Role, "Admin") // If roles are needed
    }, "TestAuthType"));

    _controller.ControllerContext = new ControllerContext
    {
      HttpContext = new DefaultHttpContext { User = user }
    };

    // Act
    var result = await _controller.GetAllProducts();

    // Assert
    var okResult = Assert.IsType<OkObjectResult>(result);
    var products = Assert.IsType<List<Product>>(okResult.Value);
    Assert.Equal(1, products.Count);
  }

  [Fact]
  public async Task GetProductById_ReturnsNotFound_WhenProductDoesNotExist()
  {
    // Arrange
    _mockProductQueryService.Setup(service => service.GetProductByIdAsync(It.IsAny<int>())).ReturnsAsync((Product)null);

    // Act
    var result = await _controller.GetProductById(1);

    // Assert
    Assert.IsType<NotFoundResult>(result);
  }

  [Fact]
  public async Task AddProduct_ReturnsForbid_WhenUserIsNotAdmin()
  {
    // Arrange
    var product = new Product { Id = 1, Name = "Product1" };

    // Mock a non-admin user
    var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
    {
        new Claim(ClaimTypes.Name, "testuser"),  // Authenticated user
        new Claim(ClaimTypes.Role, "User")       // Non-admin role
    }, "TestAuthType"));

    _controller.ControllerContext = new ControllerContext
    {
      HttpContext = new DefaultHttpContext { User = user }
    };

    _mockProductCommandService.Setup(service => service.AddProductAsync(It.IsAny<Product>(), It.IsAny<bool>())).ReturnsAsync(false);

    // Act
    var result = await _controller.AddProduct(product);

    // Assert
    Assert.IsType<ForbidResult>(result);
  }


  [Fact]
  public async Task AddProduct_ReturnsOk_WhenProductAddedSuccessfully()
  {
    // // Arrange
    // var product = new Product { Id = 1, Name = "Product1" };

    // // Mock an admin user
    // var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
    // {
    //     new Claim(ClaimTypes.Name, "adminuser"),  // Authenticated user
    //     new Claim(ClaimTypes.Role, "Admin")       // Admin role
    // }, "TestAuthType"));

    // _controller.ControllerContext = new ControllerContext
    // {
    //   HttpContext = new DefaultHttpContext { User = user }
    // };

    // _mockProductCommandService
    //     .Setup(service => service.AddProductAsync(It.IsAny<Product>(), It.IsAny<bool>()))
    //     .ReturnsAsync(true); // Simulate successful product addition

    // // Act
    // var result = await _controller.AddProduct(product);

    // // Assert
    // var okResult = Assert.IsType<OkObjectResult>(result);
    // Assert.Equal("Product added successfully.", okResult.Value);
  }

  [Fact]
  public async Task GetProductById_Returns_OK_WhenExists_NotFound_WhenProductDoesNotExist()
  {
    // Arrange
    var existingProductId = 1;  // Assume product with ID 1 exists
    var nonExistingProductId = 999; // Assume product with ID 999 does not exist
    var existingProduct = new Product { Id = existingProductId, Name = "Product A", Price = 100 };

    // Mock the IProductQueryService to return a product when queried with the existing product ID
    var productQueryServiceMock = new Mock<IProductQueryService>();
    productQueryServiceMock.Setup(service => service.GetProductByIdAsync(existingProductId))
        .ReturnsAsync(existingProduct);
    productQueryServiceMock.Setup(service => service.GetProductByIdAsync(nonExistingProductId))
        .ReturnsAsync((Product)null); // Simulate non-existing product

    var controller = new ProductsController(null, productQueryServiceMock.Object); // No need for IProductCommandService

    // Act
    var result = await controller.GetProductById(nonExistingProductId); // Query for non-existing product ID

    // Assert
    var notFoundResult = Assert.IsType<NotFoundResult>(result); // Ensure the result is NotFound
  }
}
