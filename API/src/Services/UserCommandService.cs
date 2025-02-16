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
  private readonly IConfiguration _configuration;
  private readonly IAmazonSQS _sqsClient;

  public UserCommandService(IUserRepository userRepository, IPasswordHasherService passwordHasherService,
    IConfiguration configuration, IAmazonSQS sqsClient)
  {
    _userRepository = userRepository;
    _passwordHasherService = passwordHasherService;
    _configuration = configuration;
    _sqsClient = sqsClient;
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

    // Send message to SQS for email notification
    var message = new
    {
      Email = userDto.Email,
      Subject = "Welcome!",
      Body = "Thanks for signing up!"
    };

    string messageBody = JsonSerializer.Serialize(message);
    var queueUrl = _configuration["AWS:SQSQueueUrl"];

    var sendMessageRequest = new SendMessageRequest
    {
      QueueUrl = queueUrl,
      MessageBody = messageBody
    };

    var response = await _sqsClient.SendMessageAsync(sendMessageRequest);
    Console.WriteLine($"Message sent! ID: {response.MessageId}, MD5: {response.MD5OfMessageBody}");
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