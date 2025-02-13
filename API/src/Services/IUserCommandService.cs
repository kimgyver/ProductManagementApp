using Microsoft.AspNetCore.Mvc;

public interface IUserCommandService
{
  public Task AddUserAsync(User user);
  public Task RemoveUserAsync(int id);
  public Task<User?> UpdateUserAsync(User user);
  Task RegisterUserAsync(UserRegistrationDto userDto);
  public Task MarkUserUnverified(string email);
}