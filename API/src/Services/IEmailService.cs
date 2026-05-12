namespace API.Services;

public interface IEmailService
{
  Task<EmailSendResult> SendWelcomeEmailAsync(string email, string name);
  Task<EmailSendResult> SendOrderConfirmationAsync(string email, string orderNumber, decimal totalAmount);
  Task<EmailSendResult> SendPaymentReminderAsync(string email, string poNumber, DateTime dueDate);
  Task<EmailSendResult> SendCustomEmailAsync(string to, string subject, string htmlContent);
}

public class EmailSendResult
{
  public bool Success { get; set; }
  public string? MessageId { get; set; }
  public string? Error { get; set; }
}
