using API.Controller;
using API.DTOs;
using API.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Moq;

namespace API.Test;

public class UsersControllerTests
{
  private readonly Mock<IUserCommandService> _mockUserCommandService;
  private readonly Mock<IUserQueryService> _mockUserQueryService;
  private readonly Mock<ISessionService> _mockSessionService;
  private readonly Mock<IJwtService> _mockJwtService;
  private readonly IConfiguration _configuration;
  private readonly UsersController _controller;

  public UsersControllerTests()
  {
    _mockUserCommandService = new Mock<IUserCommandService>();
    _mockUserQueryService = new Mock<IUserQueryService>();
    _mockSessionService = new Mock<ISessionService>();
    _mockJwtService = new Mock<IJwtService>();
    _configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>()).Build();

    _controller = new UsersController(
        _mockUserCommandService.Object,
        _mockUserQueryService.Object,
        _mockSessionService.Object,
        _mockJwtService.Object,
        _configuration);
  }

  [Fact]
  public async Task Login_ReturnsBadRequest_WhenUserCredentialsAreMissing()
  {
    // Arrange
    var loginDto = new UserLoginDto { Email = null, Password = null };

    // Act
    var result = await _controller.Login(loginDto);

    // Assert
    var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
    Assert.Equal("Email and Password are required for user login.", badRequestResult.Value);
  }

  [Fact]
  public async Task Login_ReturnsUnauthorized_WhenUserCredentialsAreInvalid()
  {
    // Arrange
    var loginDto = new UserLoginDto { Email = "user@test.com", Password = "wrong-password" };
    _mockUserQueryService
        .Setup(service => service.AuthenticateUserAsync(It.IsAny<UserLoginDto>()))
        .ReturnsAsync((object?)null);

    // Act
    var result = await _controller.Login(loginDto);

    // Assert
    var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
    Assert.Equal("Invalid credentials.", unauthorizedResult.Value);
  }

  [Fact]
  public async Task Login_ReturnsOk_WhenUserCredentialsAreValid()
  {
    // Arrange
    var loginDto = new UserLoginDto { Email = "user@test.com", Password = "valid-password" };
    var authResult = new { Token = "user-token", Message = "Login is successful" };

    _mockUserQueryService
        .Setup(service => service.AuthenticateUserAsync(It.IsAny<UserLoginDto>()))
        .ReturnsAsync(authResult);

    // Act
    var result = await _controller.Login(loginDto);

    // Assert
    var okResult = Assert.IsType<OkObjectResult>(result);
    Assert.Equal(authResult, okResult.Value);
  }

  [Fact]
  public async Task Login_ReturnsOk_WhenClientCredentialsAreValid()
  {
    // Arrange
    var loginDto = new UserLoginDto { ClientId = "worker-client", ClientSecret = "secret" };
    _mockJwtService
        .Setup(service => service.GetClientToken(It.IsAny<UserLoginDto>()))
        .Returns("client-token");

    // Act
    var result = await _controller.Login(loginDto);

    // Assert
    var okResult = Assert.IsType<OkObjectResult>(result);
    Assert.NotNull(okResult.Value);
  }

  [Fact]
  public async Task Login_ReturnsUnauthorized_WhenClientCredentialsAreInvalid()
  {
    // Arrange
    var loginDto = new UserLoginDto { ClientId = "worker-client", ClientSecret = "invalid" };
    _mockJwtService
        .Setup(service => service.GetClientToken(It.IsAny<UserLoginDto>()))
        .Returns(string.Empty);

    // Act
    var result = await _controller.Login(loginDto);

    // Assert
    Assert.IsType<UnauthorizedResult>(result);
  }
}
