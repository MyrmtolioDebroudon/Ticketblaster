using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Oqtane.Shared;

namespace TicketBlaster.Shared.Models
{
    public class Payment : ModelBase
    {
        [Key]
        public int PaymentId { get; set; }

        [Required]
        public int OrderId { get; set; }

        [Required]
        [StringLength(100)]
        public string PaymentIntentId { get; set; } = string.Empty;

        [StringLength(100)]
        public string TransactionId { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Required]
        [StringLength(10)]
        public string Currency { get; set; } = "USD";

        [Required]
        public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

        [Required]
        public PaymentMethod Method { get; set; } = PaymentMethod.Card;

        [StringLength(100)]
        public string Provider { get; set; } = "stripe";

        [StringLength(50)]
        public string CardLast4 { get; set; } = string.Empty;

        [StringLength(50)]
        public string CardBrand { get; set; } = string.Empty;

        [StringLength(100)]
        public string PaymentMethodId { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        public decimal ProcessingFee { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal NetAmount { get; set; }

        [StringLength(500)]
        public string FailureReason { get; set; } = string.Empty;

        [StringLength(100)]
        public string FailureCode { get; set; } = string.Empty;

        public DateTime? ProcessedDate { get; set; }

        public DateTime? RefundedDate { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal RefundedAmount { get; set; }

        [StringLength(1000)]
        public string Metadata { get; set; } = string.Empty;

        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedOn { get; set; }

        // Navigation properties
        public virtual Order Order { get; set; } = null!;
        public virtual ICollection<PaymentRefund> Refunds { get; set; } = new List<PaymentRefund>();
    }

    public class PaymentRefund : ModelBase
    {
        [Key]
        public int RefundId { get; set; }

        [Required]
        public int PaymentId { get; set; }

        [Required]
        [StringLength(100)]
        public string RefundTransactionId { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Required]
        public RefundStatus Status { get; set; } = RefundStatus.Pending;

        [Required]
        public RefundReason Reason { get; set; } = RefundReason.RequestedByCustomer;

        [StringLength(500)]
        public string Notes { get; set; } = string.Empty;

        public DateTime? ProcessedDate { get; set; }

        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedOn { get; set; }

        public int CreatedBy { get; set; }

        public int? UpdatedBy { get; set; }

        // Navigation properties
        public virtual Payment Payment { get; set; } = null!;
    }

    public enum PaymentStatus
    {
        Pending = 0,
        Processing = 1,
        Succeeded = 2,
        Failed = 3,
        Cancelled = 4,
        Refunded = 5,
        PartiallyRefunded = 6
    }

    public enum PaymentMethod
    {
        Card = 0,
        BankTransfer = 1,
        PayPal = 2,
        ApplePay = 3,
        GooglePay = 4,
        Other = 5
    }

    public enum RefundStatus
    {
        Pending = 0,
        Processing = 1,
        Succeeded = 2,
        Failed = 3,
        Cancelled = 4
    }

    public enum RefundReason
    {
        RequestedByCustomer = 0,
        Duplicate = 1,
        Fraudulent = 2,
        EventCancelled = 3,
        Other = 4
    }
}