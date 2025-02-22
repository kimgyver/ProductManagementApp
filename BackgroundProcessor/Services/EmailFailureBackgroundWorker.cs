using System;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;
using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text;
using BackgroundProcessor.Models;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace BackgroundProcessor.Services;

public class EmailFailureBackgroundWorker : BackgroundService
{
    private readonly IAmazonSQS _sqsClient;
    private readonly ILogger<EmailFailureBackgroundWorker> _logger;
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly string _emailFailureQueueUrl;
    private readonly string _apiBaseUrl;
    private readonly int _longInterval;
    private readonly int _shortInterval;


    public EmailFailureBackgroundWorker(IAmazonSQS sqsClient, ILogger<EmailFailureBackgroundWorker> logger,
            IConfiguration configuration, HttpClient httpClient)
    {
        _sqsClient = sqsClient;
        _logger = logger;
        _configuration = configuration;
        _httpClient = httpClient;

        _emailFailureQueueUrl = _configuration["SQS:EmailFailureQueueUrl"] ?? throw new ArgumentNullException("SQS EmailFailureQueueUrl is missing in config.");
        _apiBaseUrl = _configuration["WebApi:BaseUrl"] ?? throw new ArgumentNullException("Web API base URL is missing in config.");
        _longInterval = _configuration.GetValue<int>("WorkerInterval:FailureProcessor:Long");   // Start with 30 seconds delay when no messages
        _shortInterval = _configuration.GetValue<int>("WorkerInterval:FailureProcessor:Short"); // Reduce delay if messages are being processed
        if (_longInterval == 0) throw new ArgumentNullException("WorkerInterval:FailureProcessor:Long is missing config");
        if (_shortInterval == 0) throw new ArgumentNullException("WorkerInterval:FailureProcessor:Short is missing config");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var receiveRequest = new ReceiveMessageRequest
                {
                    QueueUrl = _emailFailureQueueUrl,
                    MaxNumberOfMessages = 10,
                    WaitTimeSeconds = 20 // Long polling (reduces empty responses)
                };

                var response = await _sqsClient.ReceiveMessageAsync(receiveRequest, stoppingToken);

                if (response.Messages.Count > 0)
                {
                    foreach (var message in response.Messages)
                    {
                        await ProcessMessageAsync(message);
                    }

                    // If messages were found, set short delay (e.g., 5 seconds)
                    await Task.Delay(_shortInterval, stoppingToken);
                }
                else
                {
                    // If no messages, increase delay to 30 seconds to reduce polling cost
                    await Task.Delay(_longInterval, stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing SQS messages.");
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken); // Retry delay in case of error
            }
        }
    }

    private async Task ProcessMessageAsync(Message message)
    {
        try
        {
            // Get client JWT
            var jwtToken = await GetClientJwtTokenAsync();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwtToken);

            // Email verification API call
            EmailStatus emailStatus = JsonSerializer.Deserialize<EmailStatus>(message.Body);
            var requestPayload = new
            {
                email = emailStatus.Email,
                status = emailStatus.Status
            };
            var jsonContent = new StringContent(JsonSerializer.Serialize(requestPayload), Encoding.UTF8, "application/json");
            var endpoint = $"{_apiBaseUrl}/api/email/verification-failed";
            _logger.LogInformation($"Endpoint: {endpoint}");
            _logger.LogInformation($"Request payload (JSON): {await jsonContent.ReadAsStringAsync()}");
            var response = await _httpClient.PostAsync(endpoint, jsonContent);
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation($"Successfully updated user verification status for {emailStatus.Email}.");
            }
            else
            {
                _logger.LogError($"Failed to update user verification status for {emailStatus.Email}. Response: {response.StatusCode}");
            }

            // Delete message from queue after processing
            await _sqsClient.DeleteMessageAsync(_emailFailureQueueUrl, message.ReceiptHandle);
            _logger.LogInformation($"Processed and deleted message: {message.MessageId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to process message: {message.MessageId}");
        }
    }

    private async Task<string> GetClientJwtTokenAsync()
    {
        var clientSecret = _configuration["JWT:Secret"] ?? throw new ArgumentNullException("JWT:Secret is missing in config.");
        var clientAuthRequest = new
        {
            ClientId = "background-worker",
            ClientSecret = clientSecret
        };

        var jsonContent = new StringContent(JsonSerializer.Serialize(clientAuthRequest), Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync($"{_apiBaseUrl}/api/Users/login", jsonContent);

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception("Failed to get client JWT token");
        }

        var responseData = await response.Content.ReadFromJsonAsync<TokenResponse>();
        string jwtToken = responseData?.Token ?? throw new Exception("Invalid token response");
        return jwtToken;
    }
}
