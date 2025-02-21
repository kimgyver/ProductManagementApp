using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SQS;
using Amazon.SQS.Model;
using Amazon.SimpleEmail;
using Amazon.SimpleEmail.Model;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using BackgroundProcessor.Models;

namespace BackgroundProcessor.Services;

public class EmailBackgroundWorker : BackgroundService
{
    private readonly IAmazonSQS _sqsClient;
    private readonly IAmazonSimpleEmailService _sesClient;
    private readonly ILogger<EmailBackgroundWorker> _logger;
    private readonly IConfiguration _configuration;
    private readonly string _sendEmailQueueUrl;
    private readonly string _sourceEmailAddress;
    private readonly string _emailFailureQueueUrl;

    public EmailBackgroundWorker(IAmazonSQS sqsClient, IAmazonSimpleEmailService sesClient, ILogger<EmailBackgroundWorker> logger,
            IConfiguration configuration)
    {
        _sqsClient = sqsClient;
        _sesClient = sesClient;
        _logger = logger;
        _configuration = configuration;

        _sendEmailQueueUrl = _configuration["SQS:SendEmailQueueUrl"] ?? throw new ArgumentNullException("SQS SendEmailQueueUrl is missing in config.");
        _sourceEmailAddress = _configuration["SES:SourceEmailAddress"] ?? throw new ArgumentNullException("SES SourceEmailAddress is missing in config.");
        _emailFailureQueueUrl = _configuration["SQS:EmailFailureQueueUrl"] ?? throw new ArgumentNullException("SQS EmailFailureQueueUrl is missing in config.");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Email Background Worker started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var receiveMessageRequest = new ReceiveMessageRequest
                {
                    QueueUrl = _sendEmailQueueUrl,
                    MaxNumberOfMessages = 10,
                    WaitTimeSeconds = 10,
                    VisibilityTimeout = 30 // Allow retry if processing fails
                };

                var response = await _sqsClient.ReceiveMessageAsync(receiveMessageRequest, stoppingToken);

                foreach (var message in response.Messages)
                {
                    await ProcessMessageAsync(message, stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error polling SQS for messages.");
            }

            await Task.Delay(5000, stoppingToken); // Delay before next poll
        }

        _logger.LogInformation("Email Background Worker stopped.");
    }

    private async Task ProcessMessageAsync(Amazon.SQS.Model.Message message, CancellationToken stoppingToken)
    {
        try
        {
            var messageBody = JsonSerializer.Deserialize<EmailMessage>(message.Body);
            if (messageBody == null)
            {
                _logger.LogWarning("❌ Received invalid message format.");
                return;
            }

            var sendRequest = new SendEmailRequest
            {
                Source = _sourceEmailAddress,
                Destination = new Destination { ToAddresses = new List<string> { messageBody.Email } },
                Message = new Amazon.SimpleEmail.Model.Message
                {
                    Subject = new Content(messageBody.Subject),
                    Body = new Body { Text = new Content(messageBody.Body) }
                }
            };

            await _sesClient.SendEmailAsync(sendRequest, stoppingToken);
            _logger.LogInformation($"✅ Email sent to {messageBody.Email}");

            // Delete processed message from SQS
            await _sqsClient.DeleteMessageAsync(_sendEmailQueueUrl, message.ReceiptHandle, stoppingToken);
            _logger.LogInformation("🚀 Message deleted from SQS.");
        }
        catch (MessageRejectedException ex)
        {
            _logger.LogError($"❌ Error sending email to {message.Body}: {ex.Message}");

            var emailMessage = JsonSerializer.Deserialize<EmailMessage>(message.Body);

            // Send failure message to EmailFailureQueue
            await _sqsClient.SendMessageAsync(new SendMessageRequest
            {
                QueueUrl = _emailFailureQueueUrl,
                MessageBody = JsonSerializer.Serialize(new { Email = emailMessage?.Email, Status = "Failed" })
            }, stoppingToken);

            _logger.LogInformation($"✅ Message sent to EmailFailureQueue for {message.Body}.");

            // Delete the original failed message from SendEmailQueue
            await _sqsClient.DeleteMessageAsync(_sendEmailQueueUrl, message.ReceiptHandle, stoppingToken);
            _logger.LogInformation($"🗑 Deleted failed message from SendEmailQueue: {message.MessageId}");

        }
        catch (Exception ex)
        {
            _logger.LogError($"❌ Unexpected error: {ex.Message}");
        }
    }
}

