using Stripe;
using TicketBlaster.Shared.Models;
using TicketBlaster.Server.Repository.Interfaces;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace TicketBlaster.Server.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly IPaymentRepository _paymentRepository;
        private readonly IOrderRepository _orderRepository;
        private readonly StripeSettings _stripeSettings;
        private readonly ILogger<PaymentService> _logger;

        public PaymentService(
            IPaymentRepository paymentRepository,
            IOrderRepository orderRepository,
            IOptions<StripeSettings> stripeSettings,
            ILogger<PaymentService> logger)
        {
            _paymentRepository = paymentRepository;
            _orderRepository = orderRepository;
            _stripeSettings = stripeSettings.Value;
            _logger = logger;
            
            // Configure Stripe
            StripeConfiguration.ApiKey = _stripeSettings.SecretKey;
        }

        public async Task<PaymentIntentResult> CreatePaymentIntentAsync(decimal amount, string currency, int orderId)
        {
            try
            {
                var order = await _orderRepository.GetOrderAsync(orderId);
                if (order == null)
                {
                    return new PaymentIntentResult
                    {
                        Success = false,
                        ErrorMessage = "Order not found"
                    };
                }

                var options = new PaymentIntentCreateOptions
                {
                    Amount = (long)(amount * 100), // Convert to cents
                    Currency = currency.ToLower(),
                    PaymentMethodTypes = new List<string> { "card" },
                    Metadata = new Dictionary<string, string>
                    {
                        { "order_id", orderId.ToString() },
                        { "order_number", order.OrderNumber }
                    }
                };

                var service = new PaymentIntentService();
                var paymentIntent = await service.CreateAsync(options);

                // Create payment record
                var payment = new Payment
                {
                    OrderId = orderId,
                    PaymentIntentId = paymentIntent.Id,
                    Amount = amount,
                    Currency = currency,
                    Status = PaymentStatus.Pending,
                    Method = PaymentMethod.Card,
                    Provider = "stripe",
                    CreatedOn = DateTime.UtcNow
                };

                await _paymentRepository.CreatePaymentAsync(payment);

                return new PaymentIntentResult
                {
                    PaymentIntentId = paymentIntent.Id,
                    ClientSecret = paymentIntent.ClientSecret,
                    Success = true
                };
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex, "Stripe error creating payment intent");
                return new PaymentIntentResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating payment intent");
                return new PaymentIntentResult
                {
                    Success = false,
                    ErrorMessage = "An error occurred processing your payment"
                };
            }
        }

        public async Task<bool> ConfirmPaymentAsync(string paymentIntentId)
        {
            try
            {
                var service = new PaymentIntentService();
                var paymentIntent = await service.GetAsync(paymentIntentId);

                var payment = await _paymentRepository.GetPaymentByIntentIdAsync(paymentIntentId);
                if (payment == null)
                {
                    _logger.LogWarning($"Payment record not found for intent: {paymentIntentId}");
                    return false;
                }

                // Update payment status based on Stripe status
                payment.Status = paymentIntent.Status switch
                {
                    "succeeded" => PaymentStatus.Succeeded,
                    "processing" => PaymentStatus.Processing,
                    "canceled" => PaymentStatus.Cancelled,
                    _ => PaymentStatus.Failed
                };

                if (paymentIntent.Status == "succeeded")
                {
                    payment.ProcessedDate = DateTime.UtcNow;
                    payment.TransactionId = paymentIntent.LatestCharge?.Id ?? string.Empty;
                    
                    // Get charge details for fees
                    if (!string.IsNullOrEmpty(payment.TransactionId))
                    {
                        var chargeService = new ChargeService();
                        var charge = await chargeService.GetAsync(payment.TransactionId);
                        
                        payment.ProcessingFee = charge.BalanceTransaction?.Fee / 100m ?? 0;
                        payment.NetAmount = payment.Amount - payment.ProcessingFee;
                        
                        if (charge.PaymentMethodDetails?.Card != null)
                        {
                            payment.CardLast4 = charge.PaymentMethodDetails.Card.Last4;
                            payment.CardBrand = charge.PaymentMethodDetails.Card.Brand;
                        }
                    }

                    // Update order status
                    var order = await _orderRepository.GetOrderAsync(payment.OrderId);
                    if (order != null)
                    {
                        order.Status = OrderStatus.Completed;
                        order.PaymentDate = DateTime.UtcNow;
                        order.PaymentIntentId = paymentIntentId;
                        await _orderRepository.UpdateOrderAsync(order);
                    }
                }

                payment.UpdatedOn = DateTime.UtcNow;
                await _paymentRepository.UpdatePaymentAsync(payment);

                return payment.Status == PaymentStatus.Succeeded;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error confirming payment: {paymentIntentId}");
                return false;
            }
        }

        public async Task<Payment?> GetPaymentByIntentIdAsync(string paymentIntentId)
        {
            return await _paymentRepository.GetPaymentByIntentIdAsync(paymentIntentId);
        }

        public async Task<IEnumerable<Payment>> GetOrderPaymentsAsync(int orderId)
        {
            return await _paymentRepository.GetOrderPaymentsAsync(orderId);
        }

        public async Task<RefundResult> ProcessRefundAsync(int paymentId, decimal amount, RefundReason reason)
        {
            try
            {
                var payment = await _paymentRepository.GetPaymentAsync(paymentId);
                if (payment == null || payment.Status != PaymentStatus.Succeeded)
                {
                    return new RefundResult
                    {
                        Success = false,
                        ErrorMessage = "Payment not found or not eligible for refund"
                    };
                }

                var refundAmount = amount > 0 ? amount : payment.Amount;
                if (refundAmount > payment.Amount - payment.RefundedAmount)
                {
                    return new RefundResult
                    {
                        Success = false,
                        ErrorMessage = "Refund amount exceeds available amount"
                    };
                }

                var options = new RefundCreateOptions
                {
                    PaymentIntent = payment.PaymentIntentId,
                    Amount = (long)(refundAmount * 100),
                    Reason = reason switch
                    {
                        RefundReason.Duplicate => RefundReasons.Duplicate,
                        RefundReason.Fraudulent => RefundReasons.Fraudulent,
                        _ => RefundReasons.RequestedByCustomer
                    },
                    Metadata = new Dictionary<string, string>
                    {
                        { "payment_id", paymentId.ToString() },
                        { "order_id", payment.OrderId.ToString() }
                    }
                };

                var service = new RefundService();
                var refund = await service.CreateAsync(options);

                // Create refund record
                var paymentRefund = new PaymentRefund
                {
                    PaymentId = paymentId,
                    RefundTransactionId = refund.Id,
                    Amount = refundAmount,
                    Status = refund.Status == "succeeded" ? RefundStatus.Succeeded : RefundStatus.Processing,
                    Reason = reason,
                    ProcessedDate = refund.Status == "succeeded" ? DateTime.UtcNow : null,
                    CreatedOn = DateTime.UtcNow
                };

                await _paymentRepository.CreateRefundAsync(paymentRefund);

                // Update payment refunded amount
                payment.RefundedAmount += refundAmount;
                if (payment.RefundedAmount >= payment.Amount)
                {
                    payment.Status = PaymentStatus.Refunded;
                    payment.RefundedDate = DateTime.UtcNow;
                }
                else
                {
                    payment.Status = PaymentStatus.PartiallyRefunded;
                }
                
                await _paymentRepository.UpdatePaymentAsync(payment);

                return new RefundResult
                {
                    RefundId = refund.Id,
                    Amount = refundAmount,
                    Status = paymentRefund.Status,
                    Success = true
                };
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex, "Stripe error processing refund");
                return new RefundResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing refund");
                return new RefundResult
                {
                    Success = false,
                    ErrorMessage = "An error occurred processing the refund"
                };
            }
        }

        public async Task<bool> HandleWebhookAsync(string payload, string signature)
        {
            try
            {
                var stripeEvent = EventUtility.ConstructEvent(
                    payload,
                    signature,
                    _stripeSettings.WebhookSecret
                );

                switch (stripeEvent.Type)
                {
                    case Events.PaymentIntentSucceeded:
                        var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
                        if (paymentIntent != null)
                        {
                            await ConfirmPaymentAsync(paymentIntent.Id);
                        }
                        break;

                    case Events.PaymentIntentPaymentFailed:
                        var failedIntent = stripeEvent.Data.Object as PaymentIntent;
                        if (failedIntent != null)
                        {
                            var payment = await _paymentRepository.GetPaymentByIntentIdAsync(failedIntent.Id);
                            if (payment != null)
                            {
                                payment.Status = PaymentStatus.Failed;
                                payment.FailureReason = failedIntent.LastPaymentError?.Message ?? "Payment failed";
                                payment.FailureCode = failedIntent.LastPaymentError?.Code ?? string.Empty;
                                payment.UpdatedOn = DateTime.UtcNow;
                                await _paymentRepository.UpdatePaymentAsync(payment);
                            }
                        }
                        break;

                    case Events.RefundUpdated:
                        var refund = stripeEvent.Data.Object as Refund;
                        if (refund != null)
                        {
                            var paymentRefund = await _paymentRepository.GetRefundByTransactionIdAsync(refund.Id);
                            if (paymentRefund != null)
                            {
                                paymentRefund.Status = refund.Status switch
                                {
                                    "succeeded" => RefundStatus.Succeeded,
                                    "failed" => RefundStatus.Failed,
                                    "canceled" => RefundStatus.Cancelled,
                                    _ => RefundStatus.Processing
                                };
                                
                                if (paymentRefund.Status == RefundStatus.Succeeded)
                                {
                                    paymentRefund.ProcessedDate = DateTime.UtcNow;
                                }
                                
                                paymentRefund.UpdatedOn = DateTime.UtcNow;
                                await _paymentRepository.UpdateRefundAsync(paymentRefund);
                            }
                        }
                        break;
                }

                return true;
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex, "Invalid webhook signature");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing webhook");
                return false;
            }
        }

        public async Task<PaymentStats> GetPaymentStatsAsync(int organizerId, DateTime? startDate = null, DateTime? endDate = null)
        {
            return await _paymentRepository.GetPaymentStatsAsync(organizerId, startDate, endDate);
        }
    }

    public class StripeSettings
    {
        public string SecretKey { get; set; } = string.Empty;
        public string PublishableKey { get; set; } = string.Empty;
        public string WebhookSecret { get; set; } = string.Empty;
    }
}