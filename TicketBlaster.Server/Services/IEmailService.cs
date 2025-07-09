using TicketBlaster.Shared.Models;

namespace TicketBlaster.Server.Services
{
    public interface IEmailService
    {
        Task<bool> SendEmailAsync(string to, string subject, string body, bool isHtml = true);
        Task<bool> SendEmailAsync(List<string> to, string subject, string body, bool isHtml = true);
        Task<bool> SendOrderConfirmationAsync(Order order);
        Task<bool> SendTicketDeliveryAsync(Order order);
        Task<bool> SendEventReminderAsync(Event eventEntity, List<string> attendeeEmails);
        Task<bool> SendEventUpdateAsync(Event eventEntity, List<string> attendeeEmails, string updateMessage);
        Task<bool> SendEventCancellationAsync(Event eventEntity, List<string> attendeeEmails);
        Task<bool> SendRefundNotificationAsync(Order order, decimal refundAmount);
        Task<bool> SendPasswordResetAsync(string email, string resetToken);
        Task<bool> SendEmailVerificationAsync(string email, string verificationToken);
        Task<bool> SendWelcomeEmailAsync(User user);
        Task<bool> SendOrganizerApplicationAsync(User user);
        Task<EmailTemplate> GetEmailTemplateAsync(string templateName);
        Task<bool> UpdateEmailTemplateAsync(EmailTemplate template);
    }

    public class EmailTemplate
    {
        public string Name { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public bool IsHtml { get; set; } = true;
        public List<string> AvailableTokens { get; set; } = new();
    }
}