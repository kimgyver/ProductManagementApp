using Microsoft.AspNetCore.Mvc;

public interface IUserCommandService
{
  public Task RegisterAsync(User user);
  public Task RemoveUserAsync(int id);
  public Task<User?> UpdateUserAsync(User user);
}