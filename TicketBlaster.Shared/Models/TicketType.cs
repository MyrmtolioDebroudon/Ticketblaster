using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Oqtane.Shared;

namespace TicketBlaster.Shared.Models
{
    public class TicketType : ModelBase
    {
        [Key]
        public int TicketTypeId { get; set; }

        [Required]
        public int EventId { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string Description { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        [Required]
        public int Quantity { get; set; }

        public int SoldQuantity { get; set; }

        public int ReservedQuantity { get; set; }

        public int AvailableQuantity => Quantity - SoldQuantity - ReservedQuantity;

        public DateTime SaleStartDate { get; set; } = DateTime.UtcNow;

        public DateTime SaleEndDate { get; set; }

        public int? MaxPerOrder { get; set; }

        public int? MinPerOrder { get; set; } = 1;

        public TicketTypeStatus Status { get; set; } = TicketTypeStatus.Active;

        public TicketVisibility Visibility { get; set; } = TicketVisibility.Public;

        public bool RequiresApproval { get; set; }

        public bool IsActive { get; set; } = true;

        public int SortOrder { get; set; }

        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedOn { get; set; }

        public int CreatedBy { get; set; }

        public int? UpdatedBy { get; set; }

        // Navigation properties
        public virtual Event Event { get; set; } = null!;
        public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
        public virtual ICollection<TicketTypeDiscount> Discounts { get; set; } = new List<TicketTypeDiscount>();
    }

    public class TicketTypeDiscount : ModelBase
    {
        [Key]
        public int DiscountId { get; set; }

        [Required]
        public int TicketTypeId { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Code { get; set; } = string.Empty;

        public DiscountType Type { get; set; } = DiscountType.Percentage;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Value { get; set; }

        public int? MaxUses { get; set; }

        public int UsedCount { get; set; }

        public DateTime StartDate { get; set; } = DateTime.UtcNow;

        public DateTime EndDate { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;

        public int CreatedBy { get; set; }

        // Navigation properties
        public virtual TicketType TicketType { get; set; } = null!;
    }

    public enum TicketTypeStatus
    {
        Active = 0,
        Inactive = 1,
        SoldOut = 2,
        SaleEnded = 3
    }

    public enum TicketVisibility
    {
        Public = 0,
        Private = 1,
        Hidden = 2
    }

    public enum DiscountType
    {
        Percentage = 0,
        FixedAmount = 1
    }
}