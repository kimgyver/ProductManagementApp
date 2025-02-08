
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;

public class UserService : IUserService
{
  private readonly UserRepository _userRepository;
  private readonly IPasswordHasherService _passwordHasherService;
  private readonly JwtService _jwtService;
  private readonly SessionService _sessionService;

  public UserService(UserRepository userRepository, IPasswordHasherService passwordHasherService, JwtService jwtService, SessionService sessionService)
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

  public async Task RegisterAsync(User user)
  {
    // Hash the password before storing it
    user.HashedPassword = _passwordHasherService.HashPassword(user.HashedPassword);
    await _userRepository.AddAsync(user);
  }

  public async Task<object?> AuthenticateUserAsync(UserLoginDto loginDto)
  {
    var user = (await GetAllUsersAsync()).FirstOrDefault(u => u.Email == loginDto.Email);
    if (user == null || !_passwordHasherService.VerifyPassword(loginDto.Password, user.HashedPassword))
    {
      return null; // Invalid credentials
    }
    var token = _jwtService.GenerateToken(user.Username, user.IsAdmin);

    _sessionService.GenerateSessionAsync(user.Username, user.IsAdmin);

    return new { Token = token, Message = "Login is successful", User = user };
  }

  public async Task RemoveUserAsync(int id)
  {
    await _userRepository.RemoveUserAsync(id);
  }

  public async Task UpdateUserAsync(User user)
  {
    await _userRepository.UpdateUserAsync(user);
  }
}