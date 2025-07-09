using TicketBlaster.Shared.Models;

namespace TicketBlaster.Server.Services
{
    public class EmailService : IEmailService
    {
        private readonly ILogger<EmailService> _logger;

        public EmailService(ILogger<EmailService> logger)
        {
            _logger = logger;
        }

        public Task<bool> SendEmailAsync(string to, string subject, string body, bool isHtml = true)
        {
            throw new NotImplementedException();
        }

        public Task<bool> SendEmailAsync(List<string> to, string subject, string body, bool isHtml = true)
        {
            throw new NotImplementedException();
        }

        public Task<bool> SendOrderConfirmationAsync(Order order)
        {
            throw new NotImplementedException();
        }

        public Task<bool> SendTicketDeliveryAsync(Order order)
        {
            throw new NotImplementedException();
        }

        public Task<bool> SendEventReminderAsync(Event eventEntity, List<string> attendeeEmails)
        {
            throw new NotImplementedException();
        }

        public Task<bool> SendEventUpdateAsync(Event eventEntity, List<string> attendeeEmails, string updateMessage)
        {
            throw new NotImplementedException();
        }

        public Task<bool> SendEventCancellationAsync(Event eventEntity, List<string> attendeeEmails)
        {
            throw new NotImplementedException();
        }

        public Task<bool> SendRefundNotificationAsync(Order order, decimal refundAmount)
        {
            throw new NotImplementedException();
        }

        public Task<bool> SendPasswordResetAsync(string email, string resetToken)
        {
            throw new NotImplementedException();
        }

        public Task<bool> SendEmailVerificationAsync(string email, string verificationToken)
        {
            throw new NotImplementedException();
        }

        public Task<bool> SendWelcomeEmailAsync(User user)
        {
            throw new NotImplementedException();
        }

        public Task<bool> SendOrganizerApplicationAsync(User user)
        {
            throw new NotImplementedException();
        }

        public Task<EmailTemplate> GetEmailTemplateAsync(string templateName)
        {
            throw new NotImplementedException();
        }

        public Task<bool> UpdateEmailTemplateAsync(EmailTemplate template)
        {
            throw new NotImplementedException();
        }
    }
}