using Microsoft.AspNetCore.Mvc;

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
  public async Task<IActionResult> MarkEmailVerificationFailed([FromBody] EmailVerificationFailedRequest request)
  {
    if (request.Status != "Failed")
    {
      return BadRequest("Invalid UserId");
    }

    await _userService.MarkUserUnverified(request.Email);
    return Ok(new { message = "User verification status updated to 0" });
  }
}

public class EmailVerificationFailedRequest
{
  public string Email { get; set; }
  public string Status { get; set; }
}
