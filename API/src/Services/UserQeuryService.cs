
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
    var user = (await GetAllUsersAsync()).FirstOrDefault(u => u.Email == loginDto.Email);
    if (user == null || !_passwordHasherService.VerifyPassword(loginDto.Password, user.HashedPassword))
    {
      return null; // Invalid credentials
    }
    var token = _jwtService.GenerateToken(user.Username, user.IsAdmin);

    _sessionService.GenerateSessionAsync(user.Username, user.IsAdmin);

    return new { Token = token, Message = "Login is successful", User = user };
  }
}