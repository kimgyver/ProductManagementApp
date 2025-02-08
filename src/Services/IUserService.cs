using Microsoft.AspNetCore.Mvc;

public interface IUserService
{
  public Task<IEnumerable<User>> GetAllUsersAsync();
  public Task RegisterAsync(User user);
  public Task<object?> AuthenticateUserAsync(UserLoginDto loginDto);
  public Task RemoveUserAsync(int id);
  public Task UpdateUserAsync(User user);
}