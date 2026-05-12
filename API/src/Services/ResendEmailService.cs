using System.Text.Json;

namespace API.Services;

public class ResendEmailService : IEmailService
{
  private readonly ILogger<ResendEmailService> _logger;
  private readonly string _fromAddress;

  public ResendEmailService(IConfiguration configuration, ILogger<ResendEmailService> logger)
  {
    _logger = logger;
    _fromAddress = configuration["Email:FromAddress"] ?? "noreply@ecommerce-store.com";
  }

  public async Task<EmailSendResult> SendWelcomeEmailAsync(string email, string name)
  {
    try
    {
      _logger.LogInformation("[Email] Welcome email prepared for {Email} ({Name})", email, name);
      return await Task.FromResult(new EmailSendResult
      {
        Success = true,
        MessageId = Guid.NewGuid().ToString()
      });
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Failed to send welcome email to {Email}", email);
      return new EmailSendResult { Success = false, Error = ex.Message };
    }
  }

  public async Task<EmailSendResult> SendOrderConfirmationAsync(string email, string orderNumber, decimal totalAmount)
  {
    try
    {
      _logger.LogInformation("[Email] Order confirmation email prepared - Order: {OrderNumber}, Email: {Email}, Total: {Total}",
        orderNumber, email, totalAmount);
      return await Task.FromResult(new EmailSendResult
      {
        Success = true,
        MessageId = Guid.NewGuid().ToString()
      });
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Failed to send order confirmation to {Email}", email);
      return new EmailSendResult { Success = false, Error = ex.Message };
    }
  }

  public async Task<EmailSendResult> SendPaymentReminderAsync(string email, string poNumber, DateTime dueDate)
  {
    try
    {
      _logger.LogInformation("[Email] Payment reminder email prepared - PO: {PoNumber}, Email: {Email}, DueDate: {DueDate}",
        poNumber, email, dueDate.ToString("yyyy-MM-dd"));
      return await Task.FromResult(new EmailSendResult
      {
        Success = true,
        MessageId = Guid.NewGuid().ToString()
      });
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Failed to send payment reminder to {Email}", email);
      return new EmailSendResult { Success = false, Error = ex.Message };
    }
  }

  public async Task<EmailSendResult> SendCustomEmailAsync(string to, string subject, string htmlContent)
  {
    try
    {
      _logger.LogInformation("[Email] Custom email prepared - To: {To}, Subject: {Subject}", to, subject);
      return await Task.FromResult(new EmailSendResult
      {
        Success = true,
        MessageId = Guid.NewGuid().ToString()
      });
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Exception occurred while sending email to {Email}", to);
      return new EmailSendResult { Success = false, Error = ex.Message };
    }
  }
}
