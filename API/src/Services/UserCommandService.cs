using System.Text.Json;
using Amazon.SQS;
using Amazon.SQS.Model;
using API.Infrastructure;
using API.Models;
using API.DTOs;

namespace API.Services;

public class UserCommandService : IUserCommandService
{
  private readonly IUserRepository _userRepository;
  private readonly IPasswordHasherService _passwordHasherService;
  private readonly IEmailService _emailService;
  private readonly ILogger<UserCommandService> _logger;

  public UserCommandService(
    IUserRepository userRepository, 
    IPasswordHasherService passwordHasherService,
    IEmailService emailService,
    ILogger<UserCommandService> logger)
  {
    _userRepository = userRepository;
    _passwordHasherService = passwordHasherService;
    _emailService = emailService;
    _logger = logger;
  }

  public async Task AddUserAsync(User user)
  {
    // Hash the password before storing it
    user.HashedPassword = _passwordHasherService.HashPassword(user.HashedPassword);
    await _userRepository.AddAsync(user);
  }

  public async Task RegisterUserAsync(UserRegistrationDto userDto)
  {
    // Hash the password
    var hashedPassword = _passwordHasherService.HashPassword(userDto.Password);

    // Create user entity
    var user = new User
    {
      Email = userDto.Email,
      Username = userDto.Username,
      HashedPassword = hashedPassword,
      Verified = true
    };

    // Save user to database
    await _userRepository.AddAsync(user);

    // Send welcome email using Resend (replaces SQS+Lambda+SES)
    try
    {
      var result = await _emailService.SendWelcomeEmailAsync(userDto.Email, userDto.Username);
      if (result.Success)
      {
        _logger.LogInformation("Welcome email sent to {Email}", userDto.Email);
      }
      else
      {
        _logger.LogWarning("Failed to send welcome email to {Email}: {Error}", userDto.Email, result.Error);
      }
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Exception occurred while sending welcome email to {Email}", userDto.Email);
    }
  }

  public async Task RemoveUserAsync(int id)
  {
    await _userRepository.RemoveUserAsync(id);
  }

  public async Task<User?> UpdateUserAsync(User user)
  {
    return await _userRepository.UpdateUserAsync(user);
  }

  public async Task MarkUserUnverified(string email)
  {
    await _userRepository.MarkUserUnverifiedAsync(email);
  }
}