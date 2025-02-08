using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
  private readonly UserService _userService;
  private readonly SessionService _sessionService;

  public UsersController(UserService userService, SessionService sessionService)
  {
    _userService = userService;
    _sessionService = sessionService;
  }

  [HttpGet]
  public async Task<IActionResult> Index()
  {
    return Ok(_userService.GetAllUsersAsync());
  }

  [HttpPost]
  public async Task<IActionResult> Register([FromBody] User user)
  {
    await _userService.RegisterAsync(user);
    return Ok("User registered successfully.");
  }

  [HttpPut]
  public async Task<IActionResult> Update([FromBody] User user)
  {
    await _userService.UpdateUserAsync(user);
    return Ok("Updated successfully");
  }

  [HttpDelete]
  public async Task<IActionResult> Delete(int id)
  {
    await _userService.RemoveUserAsync(id);
    return Ok("Removed successfully");
  }

  [HttpPost("login")]
  public async Task<IActionResult> Login([FromBody] UserLoginDto loginDto)
  {
    var result = await _userService.AuthenticateUserAsync(loginDto);
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
}
