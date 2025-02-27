using Microsoft.AspNetCore.Mvc;
using API.Services;
using API.Models;
using API.DTOs;

namespace API.Controller;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
  private readonly IUserCommandService _userCommandService;
  private readonly IUserQueryService _userQueryService;
  private readonly ISessionService _sessionService;
  private readonly IJwtService _jwtService;
  private readonly IConfiguration _configuration;

  public UsersController(IUserCommandService userCommandService, IUserQueryService userQueryService, ISessionService sessionService,
      IJwtService jwtService, IConfiguration configuration)
  {
    _userCommandService = userCommandService;
    _userQueryService = userQueryService;
    _sessionService = sessionService;
    _jwtService = jwtService;
    _configuration = configuration;
  }

  [HttpGet]
  public async Task<IActionResult> Index()
  {
    return Ok(_userQueryService.GetAllUsersAsync());
  }

  [HttpPost]
  public async Task<IActionResult> Add([FromBody] User user)
  {
    try
    {
      await _userCommandService.AddUserAsync(user);
    }
    catch (ApplicationException ex)
    {
      return StatusCode(400, new { error = ex.Message });
    }
    return Ok("User registered successfully.");
  }

  [HttpPut]
  public async Task<IActionResult> Update([FromBody] User user)
  {
    var userUpdated = await _userCommandService.UpdateUserAsync(user);
    if (userUpdated == null) return NotFound("Cannot find user");
    return Ok("Updated successfully");
  }

  [HttpDelete]
  public async Task<IActionResult> Delete(int id)
  {
    await _userCommandService.RemoveUserAsync(id);
    return Ok("Removed successfully");
  }

  [HttpPost("login")]
  public async Task<IActionResult> Login([FromBody] UserLoginDto loginDto)
  {
    // For clients (e.g. background worker)
    if (loginDto.IsClient)
    {
      var clientJwt = _jwtService.GetClientToken(loginDto);
      return !string.IsNullOrWhiteSpace(clientJwt) ? Ok(new { Token = clientJwt }) : Unauthorized();
    }

    // For users
    if (string.IsNullOrEmpty(loginDto.Email) || string.IsNullOrEmpty(loginDto.Password))
    {
      return BadRequest("Email and Password are required for user login.");
    }

    var result = await _userQueryService.AuthenticateUserAsync(loginDto);
    if (result == null)
    {
      return Unauthorized("Invalid credentials.");
    }
    return Ok(result);
  }

  [HttpPost("logout")]
  public async Task<IActionResult> Logout()
  {
    await _sessionService.RemoveSessionAsync();
    return Ok("Successfully logged out.");
  }

  [HttpPost("register")]
  public async Task<IActionResult> Register([FromBody] UserRegistrationDto userDto)
  {
    if (!ModelState.IsValid)
    {
      return BadRequest(ModelState);
    }

    await _userCommandService.RegisterUserAsync(userDto);
    return Ok(new { message = "User registered successfully. Email will be sent shortly." });
  }
}
