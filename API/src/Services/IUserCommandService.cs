using Microsoft.AspNetCore.Mvc;
using API.Models;
using API.DTOs;

namespace API.Services;

public interface IUserCommandService
{
  public Task AddUserAsync(User user);
  public Task RemoveUserAsync(int id);
  public Task<User?> UpdateUserAsync(User user);
  Task RegisterUserAsync(UserRegistrationDto userDto);
  public Task MarkUserUnverified(string email);
}