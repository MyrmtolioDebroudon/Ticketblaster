using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Oqtane.Shared;

namespace TicketBlaster.Shared.Models
{
    public class Order : ModelBase
    {
        [Key]
        public int OrderId { get; set; }

        [Required]
        [StringLength(50)]
        public string OrderNumber { get; set; } = string.Empty;

        [Required]
        public int EventId { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal SubTotal { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TaxAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal ServiceFee { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal DiscountAmount { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        [Required]
        [StringLength(10)]
        public string Currency { get; set; } = "USD";

        [Required]
        public OrderStatus Status { get; set; } = OrderStatus.Pending;

        [StringLength(100)]
        public string CustomerFirstName { get; set; } = string.Empty;

        [StringLength(100)]
        public string CustomerLastName { get; set; } = string.Empty;

        [StringLength(256)]
        public string CustomerEmail { get; set; } = string.Empty;

        [StringLength(50)]
        public string CustomerPhone { get; set; } = string.Empty;

        [StringLength(500)]
        public string BillingAddress { get; set; } = string.Empty;

        [StringLength(100)]
        public string BillingCity { get; set; } = string.Empty;

        [StringLength(100)]
        public string BillingState { get; set; } = string.Empty;

        [StringLength(20)]
        public string BillingZipCode { get; set; } = string.Empty;

        [StringLength(100)]
        public string BillingCountry { get; set; } = string.Empty;

        [StringLength(100)]
        public string PaymentIntentId { get; set; } = string.Empty;

        [StringLength(100)]
        public string PaymentMethod { get; set; } = string.Empty;

        [StringLength(1000)]
        public string Notes { get; set; } = string.Empty;

        public DateTime OrderDate { get; set; } = DateTime.UtcNow;

        public DateTime? PaymentDate { get; set; }

        public DateTime? CancelledDate { get; set; }

        public DateTime? RefundedDate { get; set; }

        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedOn { get; set; }

        public int CreatedBy { get; set; }

        public int? UpdatedBy { get; set; }

        // Navigation properties
        public virtual Event Event { get; set; } = null!;
        public virtual User User { get; set; } = null!;
        public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
        public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
    }

    public class OrderItem : ModelBase
    {
        [Key]
        public int OrderItemId { get; set; }

        [Required]
        public int OrderId { get; set; }

        [Required]
        public int TicketTypeId { get; set; }

        [Required]
        public int Quantity { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal DiscountAmount { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalPrice { get; set; }

        [StringLength(100)]
        public string DiscountCode { get; set; } = string.Empty;

        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual Order Order { get; set; } = null!;
        public virtual TicketType TicketType { get; set; } = null!;
        public virtual ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
    }

    public class Ticket : ModelBase
    {
        [Key]
        public int TicketId { get; set; }

        [Required]
        public int OrderItemId { get; set; }

        [Required]
        [StringLength(100)]
        public string TicketNumber { get; set; } = string.Empty;

        [Required]
        [StringLength(500)]
        public string QRCode { get; set; } = string.Empty;

        [StringLength(100)]
        public string AttendeeFirstName { get; set; } = string.Empty;

        [StringLength(100)]
        public string AttendeeLastName { get; set; } = string.Empty;

        [StringLength(256)]
        public string AttendeeEmail { get; set; } = string.Empty;

        public TicketStatus Status { get; set; } = TicketStatus.Active;

        public DateTime? CheckedInDate { get; set; }

        public DateTime? TransferredDate { get; set; }

        public int? TransferredToUserId { get; set; }

        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedOn { get; set; }

        // Navigation properties
        public virtual OrderItem OrderItem { get; set; } = null!;
        public virtual User? TransferredToUser { get; set; }
    }

    public enum OrderStatus
    {
        Pending = 0,
        Processing = 1,
        Completed = 2,
        Cancelled = 3,
        Refunded = 4,
        Failed = 5
    }

    public enum TicketStatus
    {
        Active = 0,
        Used = 1,
        Cancelled = 2,
        Transferred = 3,
        Refunded = 4
    }
}