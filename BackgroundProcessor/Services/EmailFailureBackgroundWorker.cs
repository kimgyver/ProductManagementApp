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

namespace BackgroundProcessor.Services;

public class EmailFailureBackgroundWorker : BackgroundService
{
    private readonly IAmazonSQS _sqsClient;
    private readonly ILogger<EmailFailureBackgroundWorker> _logger;
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly string _emailFailureQueueUrl;
    private readonly string _apiBaseUrl;

    public EmailFailureBackgroundWorker(IAmazonSQS sqsClient, ILogger<EmailFailureBackgroundWorker> logger,
            IConfiguration configuration, HttpClient httpClient)
    {
        _sqsClient = sqsClient;
        _logger = logger;
        _configuration = configuration;
        _httpClient = httpClient;

        _emailFailureQueueUrl = _configuration["SQS:EmailFailureQueueUrl"] ?? throw new ArgumentNullException("SQS EmailFailureQueueUrl is missing in config.");
        _apiBaseUrl = _configuration["WebApi:BaseUrl"] ?? throw new ArgumentNullException("Web API base URL is missing in config.");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        int noMessageDelay = 30; // Start with 30 seconds delay when no messages
        int shortDelay = 5; // Reduce delay if messages are being processed

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
                    await Task.Delay(TimeSpan.FromSeconds(shortDelay), stoppingToken);
                }
                else
                {
                    // If no messages, increase delay to 30 seconds to reduce polling cost
                    await Task.Delay(TimeSpan.FromSeconds(noMessageDelay), stoppingToken);
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
            // Email verification controller
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
}
