using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TicketBlaster.Server.Services;
using TicketBlaster.Shared.Models;
using System.Security.Claims;

namespace TicketBlaster.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class OrderController : ControllerBase
    {
        private readonly IOrderService _orderService;
        private readonly ILogger<OrderController> _logger;

        public OrderController(IOrderService orderService, ILogger<OrderController> logger)
        {
            _orderService = orderService;
            _logger = logger;
        }

        [HttpGet("{orderId}")]
        public async Task<ActionResult<Order>> GetOrder(int orderId)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var order = await _orderService.GetOrderAsync(orderId);
                
                if (order == null)
                {
                    return NotFound();
                }

                // Check if user has access to this order
                if (order.UserId != userId && !User.IsInRole("Admin"))
                {
                    return Forbid();
                }

                return Ok(order);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving order {orderId}");
                return StatusCode(500, "An error occurred processing your request");
            }
        }

        [HttpGet("number/{orderNumber}")]
        public async Task<ActionResult<Order>> GetOrderByNumber(string orderNumber)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var order = await _orderService.GetOrderByNumberAsync(orderNumber);
                
                if (order == null)
                {
                    return NotFound();
                }

                // Check if user has access to this order
                if (order.UserId != userId && !User.IsInRole("Admin"))
                {
                    return Forbid();
                }

                return Ok(order);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving order by number {orderNumber}");
                return StatusCode(500, "An error occurred processing your request");
            }
        }

        [HttpGet("user/{userId}")]
        public async Task<ActionResult<IEnumerable<Order>>> GetUserOrders(int userId)
        {
            try
            {
                var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                
                // Users can only view their own orders unless they're admin
                if (userId != currentUserId && !User.IsInRole("Admin"))
                {
                    return Forbid();
                }

                var orders = await _orderService.GetUserOrdersAsync(userId);
                return Ok(orders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving orders for user {userId}");
                return StatusCode(500, "An error occurred processing your request");
            }
        }

        [HttpPost]
        public async Task<ActionResult<Order>> CreateOrder([FromBody] CreateOrderRequest request)
        {
            try
            {
                // Set the user ID from the authenticated user
                request.UserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                
                var order = await _orderService.CreateOrderAsync(request);
                return CreatedAtAction(nameof(GetOrder), new { orderId = order.OrderId }, order);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating order");
                return StatusCode(500, "An error occurred processing your request");
            }
        }

        [HttpPut("{orderId}")]
        public async Task<ActionResult<Order>> UpdateOrder(int orderId, [FromBody] Order order)
        {
            try
            {
                if (orderId != order.OrderId)
                {
                    return BadRequest("Order ID mismatch");
                }

                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var existingOrder = await _orderService.GetOrderAsync(orderId);
                
                if (existingOrder == null)
                {
                    return NotFound();
                }

                // Check if user has access to this order
                if (existingOrder.UserId != userId && !User.IsInRole("Admin"))
                {
                    return Forbid();
                }

                var updatedOrder = await _orderService.UpdateOrderAsync(order);
                return Ok(updatedOrder);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating order {orderId}");
                return StatusCode(500, "An error occurred processing your request");
            }
        }

        [HttpPost("{orderId}/cancel")]
        public async Task<ActionResult> CancelOrder(int orderId)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var order = await _orderService.GetOrderAsync(orderId);
                
                if (order == null)
                {
                    return NotFound();
                }

                // Check if user has access to this order
                if (order.UserId != userId && !User.IsInRole("Admin"))
                {
                    return Forbid();
                }

                var success = await _orderService.CancelOrderAsync(orderId);
                
                if (!success)
                {
                    return BadRequest("Order cannot be cancelled");
                }

                return Ok(new { message = "Order cancelled successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error cancelling order {orderId}");
                return StatusCode(500, "An error occurred processing your request");
            }
        }

        [HttpPost("calculate-summary")]
        public async Task<ActionResult<OrderSummary>> CalculateOrderSummary([FromBody] List<OrderItemRequest> items)
        {
            try
            {
                var summary = await _orderService.CalculateOrderSummaryAsync(items);
                return Ok(summary);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating order summary");
                return StatusCode(500, "An error occurred processing your request");
            }
        }
    }
}