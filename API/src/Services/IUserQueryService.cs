using Microsoft.AspNetCore.Mvc;

public interface IUserQueryService
{
  public Task<IEnumerable<User>> GetAllUsersAsync();
  public Task<object?> AuthenticateUserAsync(UserLoginDto loginDto);
}