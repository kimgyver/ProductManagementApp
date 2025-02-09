public class UserCommandService : IUserCommandService
{
  private readonly IUserRepository _userRepository;
  private readonly IPasswordHasherService _passwordHasherService;

  public UserCommandService(IUserRepository userRepository, IPasswordHasherService passwordHasherService)
  {
    _userRepository = userRepository;
    _passwordHasherService = passwordHasherService;
  }

  public async Task RegisterAsync(User user)
  {
    // Hash the password before storing it
    user.HashedPassword = _passwordHasherService.HashPassword(user.HashedPassword);
    await _userRepository.AddAsync(user);
  }

  public async Task RemoveUserAsync(int id)
  {
    await _userRepository.RemoveUserAsync(id);
  }

  public async Task<User?> UpdateUserAsync(User user)
  {
    return await _userRepository.UpdateUserAsync(user);
  }
}