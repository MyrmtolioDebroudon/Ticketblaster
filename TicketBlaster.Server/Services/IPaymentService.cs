using TicketBlaster.Shared.Models;

namespace TicketBlaster.Server.Services
{
    public interface IPaymentService
    {
        Task<PaymentIntentResult> CreatePaymentIntentAsync(decimal amount, string currency, int orderId);
        Task<bool> ConfirmPaymentAsync(string paymentIntentId);
        Task<Payment?> GetPaymentByIntentIdAsync(string paymentIntentId);
        Task<IEnumerable<Payment>> GetOrderPaymentsAsync(int orderId);
        Task<RefundResult> ProcessRefundAsync(int paymentId, decimal amount, RefundReason reason);
        Task<bool> HandleWebhookAsync(string payload, string signature);
        Task<PaymentStats> GetPaymentStatsAsync(int organizerId, DateTime? startDate = null, DateTime? endDate = null);
    }

    public class PaymentIntentResult
    {
        public string PaymentIntentId { get; set; } = string.Empty;
        public string ClientSecret { get; set; } = string.Empty;
        public bool Success { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
    }

    public class RefundResult
    {
        public string RefundId { get; set; } = string.Empty;
        public RefundStatus Status { get; set; }
        public decimal Amount { get; set; }
        public bool Success { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
    }

    public class PaymentStats
    {
        public decimal TotalRevenue { get; set; }
        public decimal TotalProcessingFees { get; set; }
        public decimal NetRevenue { get; set; }
        public int TotalTransactions { get; set; }
        public int SuccessfulTransactions { get; set; }
        public int FailedTransactions { get; set; }
        public decimal SuccessRate { get; set; }
        public Dictionary<string, decimal> RevenueByPaymentMethod { get; set; } = new();
    }
}