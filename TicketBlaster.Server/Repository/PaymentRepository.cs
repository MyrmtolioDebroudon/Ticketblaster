using Microsoft.EntityFrameworkCore;
using TicketBlaster.Database;
using TicketBlaster.Server.Repository.Interfaces;
using TicketBlaster.Shared.Models;

namespace TicketBlaster.Server.Repository
{
    public class PaymentRepository : IPaymentRepository
    {
        private readonly TicketBlasterDbContext _dbContext;

        public PaymentRepository(TicketBlasterDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<Payment?> GetPaymentAsync(int paymentId)
        {
            return await _dbContext.Payments
                .Include(p => p.Order)
                .Include(p => p.Refunds)
                .FirstOrDefaultAsync(p => p.PaymentId == paymentId);
        }

        public async Task<Payment?> GetPaymentByIntentIdAsync(string paymentIntentId)
        {
            return await _dbContext.Payments
                .Include(p => p.Order)
                .Include(p => p.Refunds)
                .FirstOrDefaultAsync(p => p.PaymentIntentId == paymentIntentId);
        }

        public async Task<IEnumerable<Payment>> GetOrderPaymentsAsync(int orderId)
        {
            return await _dbContext.Payments
                .Include(p => p.Refunds)
                .Where(p => p.OrderId == orderId)
                .OrderByDescending(p => p.CreatedOn)
                .ToListAsync();
        }

        public async Task<Payment> CreatePaymentAsync(Payment payment)
        {
            _dbContext.Payments.Add(payment);
            await _dbContext.SaveChangesAsync();
            return payment;
        }

        public async Task<Payment> UpdatePaymentAsync(Payment payment)
        {
            _dbContext.Payments.Update(payment);
            await _dbContext.SaveChangesAsync();
            return payment;
        }

        public async Task<PaymentRefund?> GetRefundByTransactionIdAsync(string transactionId)
        {
            return await _dbContext.PaymentRefunds
                .Include(r => r.Payment)
                .FirstOrDefaultAsync(r => r.RefundTransactionId == transactionId);
        }

        public async Task<PaymentRefund> CreateRefundAsync(PaymentRefund refund)
        {
            _dbContext.PaymentRefunds.Add(refund);
            await _dbContext.SaveChangesAsync();
            return refund;
        }

        public async Task<PaymentRefund> UpdateRefundAsync(PaymentRefund refund)
        {
            _dbContext.PaymentRefunds.Update(refund);
            await _dbContext.SaveChangesAsync();
            return refund;
        }

        public async Task<PaymentStats> GetPaymentStatsAsync(int organizerId, DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = _dbContext.Payments
                .Include(p => p.Order)
                    .ThenInclude(o => o.Event)
                .Where(p => p.Order.Event.OrganizerId == organizerId);

            if (startDate.HasValue)
                query = query.Where(p => p.CreatedOn >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(p => p.CreatedOn <= endDate.Value);

            var payments = await query.ToListAsync();

            var stats = new PaymentStats
            {
                TotalTransactions = payments.Count,
                SuccessfulTransactions = payments.Count(p => p.Status == PaymentStatus.Succeeded),
                FailedTransactions = payments.Count(p => p.Status == PaymentStatus.Failed),
                TotalRevenue = payments.Where(p => p.Status == PaymentStatus.Succeeded).Sum(p => p.Amount),
                TotalProcessingFees = payments.Where(p => p.Status == PaymentStatus.Succeeded).Sum(p => p.ProcessingFee),
                NetRevenue = payments.Where(p => p.Status == PaymentStatus.Succeeded).Sum(p => p.NetAmount)
            };

            stats.SuccessRate = stats.TotalTransactions > 0 
                ? (decimal)stats.SuccessfulTransactions / stats.TotalTransactions * 100 
                : 0;

            // Revenue by payment method
            stats.RevenueByPaymentMethod = payments
                .Where(p => p.Status == PaymentStatus.Succeeded)
                .GroupBy(p => p.Method.ToString())
                .ToDictionary(g => g.Key, g => g.Sum(p => p.Amount));

            return stats;
        }
    }
}