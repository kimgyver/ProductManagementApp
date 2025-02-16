using Microsoft.AspNetCore.Mvc;
using API.Models;
using API.DTOs;

namespace API.Services;

public interface IUserQueryService
{
  public Task<IEnumerable<User>> GetAllUsersAsync();
  public Task<object?> AuthenticateUserAsync(UserLoginDto loginDto);
}