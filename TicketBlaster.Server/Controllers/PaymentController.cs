using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TicketBlaster.Server.Services;
using TicketBlaster.Shared.Models;
using System.Security.Claims;
using Microsoft.Extensions.Options;

namespace TicketBlaster.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class PaymentController : ControllerBase
    {
        private readonly IPaymentService _paymentService;
        private readonly IOrderService _orderService;
        private readonly StripeSettings _stripeSettings;
        private readonly ILogger<PaymentController> _logger;

        public PaymentController(
            IPaymentService paymentService,
            IOrderService orderService,
            IOptions<StripeSettings> stripeSettings,
            ILogger<PaymentController> logger)
        {
            _paymentService = paymentService;
            _orderService = orderService;
            _stripeSettings = stripeSettings.Value;
            _logger = logger;
        }

        [HttpPost("create-payment-intent")]
        public async Task<ActionResult<PaymentIntentResponse>> CreatePaymentIntent([FromBody] CreatePaymentIntentRequest request)
        {
            try
            {
                // Validate order belongs to current user
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var order = await _orderService.GetOrderAsync(request.OrderId);
                
                if (order == null)
                {
                    return NotFound("Order not found");
                }

                if (order.UserId != userId && !User.IsInRole("Admin"))
                {
                    return Forbid("Access denied");
                }

                if (order.Status != OrderStatus.Pending)
                {
                    return BadRequest("Order is not in pending status");
                }

                var result = await _paymentService.CreatePaymentIntentAsync(
                    order.TotalAmount,
                    order.Currency,
                    order.OrderId
                );

                if (!result.Success)
                {
                    return BadRequest(result.ErrorMessage);
                }

                return Ok(new PaymentIntentResponse
                {
                    ClientSecret = result.ClientSecret,
                    PaymentIntentId = result.PaymentIntentId,
                    PublishableKey = _stripeSettings.PublishableKey,
                    Amount = order.TotalAmount,
                    Currency = order.Currency
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating payment intent");
                return StatusCode(500, "An error occurred processing your request");
            }
        }

        [HttpPost("confirm-payment")]
        public async Task<ActionResult> ConfirmPayment([FromBody] ConfirmPaymentRequest request)
        {
            try
            {
                var success = await _paymentService.ConfirmPaymentAsync(request.PaymentIntentId);
                
                if (!success)
                {
                    return BadRequest("Payment confirmation failed");
                }

                return Ok(new { success = true, message = "Payment confirmed successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error confirming payment");
                return StatusCode(500, "An error occurred processing your request");
            }
        }

        [HttpGet("order/{orderId}/payments")]
        public async Task<ActionResult<IEnumerable<Payment>>> GetOrderPayments(int orderId)
        {
            try
            {
                // Validate order belongs to current user
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var order = await _orderService.GetOrderAsync(orderId);
                
                if (order == null)
                {
                    return NotFound("Order not found");
                }

                if (order.UserId != userId && !User.IsInRole("Admin"))
                {
                    return Forbid("Access denied");
                }

                var payments = await _paymentService.GetOrderPaymentsAsync(orderId);
                return Ok(payments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving order payments");
                return StatusCode(500, "An error occurred processing your request");
            }
        }

        [HttpPost("refund")]
        [Authorize(Roles = "Admin,Organizer")]
        public async Task<ActionResult<RefundResult>> ProcessRefund([FromBody] RefundRequest request)
        {
            try
            {
                var result = await _paymentService.ProcessRefundAsync(
                    request.PaymentId,
                    request.Amount,
                    request.Reason
                );

                if (!result.Success)
                {
                    return BadRequest(result.ErrorMessage);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing refund");
                return StatusCode(500, "An error occurred processing your request");
            }
        }

        [HttpPost("webhook")]
        [AllowAnonymous]
        public async Task<ActionResult> HandleStripeWebhook()
        {
            try
            {
                var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
                var stripeSignature = Request.Headers["Stripe-Signature"];

                if (string.IsNullOrEmpty(stripeSignature))
                {
                    return BadRequest("Missing Stripe signature");
                }

                var success = await _paymentService.HandleWebhookAsync(json, stripeSignature);
                
                if (!success)
                {
                    return BadRequest("Invalid webhook signature");
                }

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling Stripe webhook");
                return StatusCode(500);
            }
        }

        [HttpGet("stats")]
        [Authorize(Roles = "Admin,Organizer")]
        public async Task<ActionResult<PaymentStats>> GetPaymentStats(
            [FromQuery] int? organizerId,
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate)
        {
            try
            {
                // If not admin, use current user's organizer ID
                if (!User.IsInRole("Admin"))
                {
                    organizerId = int.Parse(User.FindFirst("OrganizerId")?.Value ?? "0");
                    if (organizerId == 0)
                    {
                        return Forbid("User is not an organizer");
                    }
                }

                if (!organizerId.HasValue)
                {
                    return BadRequest("Organizer ID is required");
                }

                var stats = await _paymentService.GetPaymentStatsAsync(
                    organizerId.Value,
                    startDate,
                    endDate
                );

                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving payment stats");
                return StatusCode(500, "An error occurred processing your request");
            }
        }
    }

    public class CreatePaymentIntentRequest
    {
        public int OrderId { get; set; }
    }

    public class PaymentIntentResponse
    {
        public string ClientSecret { get; set; } = string.Empty;
        public string PaymentIntentId { get; set; } = string.Empty;
        public string PublishableKey { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = string.Empty;
    }

    public class ConfirmPaymentRequest
    {
        public string PaymentIntentId { get; set; } = string.Empty;
    }

    public class RefundRequest
    {
        public int PaymentId { get; set; }
        public decimal Amount { get; set; }
        public RefundReason Reason { get; set; }
    }
}