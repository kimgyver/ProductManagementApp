using Microsoft.AspNetCore.Mvc;
using API.Services;
using API.DTOs;

namespace API.Controller;

[ApiController]
[Route("api/email")]
public class EmailController : ControllerBase
{
  private readonly IUserCommandService _userService;

  public EmailController(IUserCommandService userService)
  {
    _userService = userService;
  }

  [HttpPost("verification-failed")]
  public async Task<IActionResult> MarkEmailVerificationFailed([FromBody] EmailVerificationFailedRequestDto request)
  {
    if (request.Status != "Failed")
    {
      return BadRequest("Invalid UserId");
    }

    await _userService.MarkUserUnverified(request.Email);
    return Ok(new { message = "User verification status updated to 0" });
  }
}
