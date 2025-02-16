using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;

namespace API.Services;

public class SessionService : ISessionService
{
  private readonly IHttpContextAccessor _httpContextAccessor;

  public SessionService(IHttpContextAccessor httpContextAccessor)
  {
    _httpContextAccessor = httpContextAccessor;
  }

  public async Task GenerateSessionAsync(string username, bool isAdmin)
  {
    // Store session data
    _httpContextAccessor.HttpContext.Session.SetString("Username", username.ToString());
    _httpContextAccessor.HttpContext.Session.SetString("IsAdmin", isAdmin.ToString());

    // Sign in with Cookie Authentication
    var claims = new List<Claim>
    {
      new Claim(ClaimTypes.NameIdentifier, username.ToString()),
      new Claim(ClaimTypes.Role, isAdmin ? "Admin" : "User")
    };
    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
    await _httpContextAccessor.HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));
  }

  public async Task RemoveSessionAsync()
  {
    var httpContext = _httpContextAccessor.HttpContext;

    // Sign out from cookie authentication
    await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

    // Clear session if using session data
    httpContext.Session.Clear();
  }
}