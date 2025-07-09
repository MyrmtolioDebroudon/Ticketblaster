using TicketBlaster.Shared.Models;

namespace TicketBlaster.Server.Services
{
    public class EmailService : IEmailService
    {
        public Task SendOrderConfirmationAsync(Order order)
        {
            throw new NotImplementedException();
        }

        public Task SendTicketsAsync(Order order)
        {
            throw new NotImplementedException();
        }

        public Task SendPasswordResetAsync(string email, string resetToken)
        {
            throw new NotImplementedException();
        }

        public Task SendEventReminderAsync(int userId, Event eventItem)
        {
            throw new NotImplementedException();
        }

        public Task SendRefundNotificationAsync(Order order, decimal refundAmount)
        {
            throw new NotImplementedException();
        }
    }
}