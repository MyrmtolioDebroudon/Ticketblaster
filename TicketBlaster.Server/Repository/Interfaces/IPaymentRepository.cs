using TicketBlaster.Shared.Models;

namespace TicketBlaster.Server.Repository.Interfaces
{
    public interface IPaymentRepository
    {
        Task<Payment?> GetPaymentAsync(int paymentId);
        Task<Payment?> GetPaymentByIntentIdAsync(string paymentIntentId);
        Task<IEnumerable<Payment>> GetOrderPaymentsAsync(int orderId);
        Task<Payment> CreatePaymentAsync(Payment payment);
        Task<Payment> UpdatePaymentAsync(Payment payment);
        Task<PaymentRefund?> GetRefundByTransactionIdAsync(string transactionId);
        Task<PaymentRefund> CreateRefundAsync(PaymentRefund refund);
        Task<PaymentRefund> UpdateRefundAsync(PaymentRefund refund);
        Task<PaymentStats> GetPaymentStatsAsync(int organizerId, DateTime? startDate = null, DateTime? endDate = null);
    }
}