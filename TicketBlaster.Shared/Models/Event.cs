using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Oqtane.Shared;

namespace TicketBlaster.Shared.Models
{
    public class Event : ModelBase
    {
        [Key]
        public int EventId { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [StringLength(5000)]
        public string Description { get; set; } = string.Empty;

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }

        [StringLength(500)]
        public string Location { get; set; } = string.Empty;

        [StringLength(500)]
        public string VirtualUrl { get; set; } = string.Empty;

        [StringLength(500)]
        public string ImageUrl { get; set; } = string.Empty;

        [Required]
        public EventStatus Status { get; set; } = EventStatus.Draft;

        [Required]
        public int CategoryId { get; set; }

        [Required]
        public int MaxCapacity { get; set; }

        public int CurrentCapacity { get; set; }

        [Required]
        public int OrganizerId { get; set; }

        [StringLength(100)]
        public string CustomUrl { get; set; } = string.Empty;

        [StringLength(500)]
        public string Tags { get; set; } = string.Empty;

        public bool IsRecurring { get; set; }

        public bool IsMultiDay { get; set; }

        public bool IsVirtual { get; set; }

        public bool IsFeatured { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedOn { get; set; }

        public int CreatedBy { get; set; }

        public int? UpdatedBy { get; set; }

        // Navigation properties
        public virtual Category Category { get; set; } = null!;
        public virtual User Organizer { get; set; } = null!;
        public virtual ICollection<TicketType> TicketTypes { get; set; } = new List<TicketType>();
        public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
    }

    public enum EventStatus
    {
        Draft = 0,
        Published = 1,
        Cancelled = 2,
        Completed = 3,
        Suspended = 4
    }
}