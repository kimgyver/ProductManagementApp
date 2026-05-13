using API.Infrastructure;
using API.Models;
using API.DTOs;

namespace API.Services;

public class UserQueryService : IUserQueryService
{
  private readonly IUserRepository _userRepository;
  private readonly IPasswordHasherService _passwordHasherService;
  private readonly IJwtService _jwtService;
  private readonly ISessionService _sessionService;

  public UserQueryService(IUserRepository userRepository, IPasswordHasherService passwordHasherService, IJwtService jwtService, ISessionService sessionService)
  {
    _userRepository = userRepository;
    _passwordHasherService = passwordHasherService;
    _jwtService = jwtService;
    _sessionService = sessionService;
  }

  public async Task<IEnumerable<User>> GetAllUsersAsync()
  {
    return await _userRepository.GetAllUsersAsync();
  }

  public async Task<object?> AuthenticateUserAsync(UserLoginDto loginDto)
  {
    if (string.IsNullOrWhiteSpace(loginDto.Email))
    {
      return null;
    }

    var user = await _userRepository.GetUserForLoginByEmailAsync(loginDto.Email);
    if (user == null || string.IsNullOrWhiteSpace(user.HashedPassword) || string.IsNullOrWhiteSpace(loginDto.Password))
    {
      return null; // Invalid credentials
    }

    if (!_passwordHasherService.VerifyPassword(user.HashedPassword, loginDto.Password))
    {
      return null; // Invalid credentials
    }
    var token = _jwtService.GenerateTokenForUser(user);

    await _sessionService.GenerateSessionAsync(user.Username, user.IsAdmin);

    return new { Token = token, Message = "Login is successful", User = user };
  }

  public async Task<int?> GetUserIdByEmailAsync(string email)
  {
    if (string.IsNullOrWhiteSpace(email))
    {
      return null;
    }

    var user = await _userRepository.GetUserForLoginByEmailAsync(email);
    return user?.Id;
  }
}