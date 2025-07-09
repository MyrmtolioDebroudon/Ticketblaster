using Microsoft.EntityFrameworkCore;
using TicketBlaster.Shared.Models;

namespace TicketBlaster.Database
{
    public class TicketBlasterDbContext : DbContext
    {
        public TicketBlasterDbContext(DbContextOptions<TicketBlasterDbContext> options) : base(options)
        {
        }

        // DbSets for our entities
        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<UserRole> UserRoles { get; set; }
        public DbSet<Event> Events { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<TicketType> TicketTypes { get; set; }
        public DbSet<TicketTypeDiscount> TicketTypeDiscounts { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<Ticket> Tickets { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<PaymentRefund> PaymentRefunds { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure User entity
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.UserId);
                entity.Property(e => e.Email).IsRequired().HasMaxLength(256);
                entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.KeycloakId).HasMaxLength(100);
                entity.HasIndex(e => e.Email).IsUnique();
                entity.HasIndex(e => e.KeycloakId).IsUnique();
            });

            // Configure Role entity
            modelBuilder.Entity<Role>(entity =>
            {
                entity.HasKey(e => e.RoleId);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(50);
                entity.HasIndex(e => e.Name).IsUnique();
            });

            // Configure UserRole entity
            modelBuilder.Entity<UserRole>(entity =>
            {
                entity.HasKey(e => e.UserRoleId);
                entity.HasOne(e => e.User)
                    .WithMany(u => u.UserRoles)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.Role)
                    .WithMany(r => r.UserRoles)
                    .HasForeignKey(e => e.RoleId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure Event entity
            modelBuilder.Entity<Event>(entity =>
            {
                entity.HasKey(e => e.EventId);
                entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Description).IsRequired().HasMaxLength(5000);
                entity.Property(e => e.CustomUrl).HasMaxLength(100);
                entity.HasIndex(e => e.CustomUrl).IsUnique();
                entity.HasIndex(e => e.StartDate);
                entity.HasIndex(e => e.Status);
                entity.HasIndex(e => e.CategoryId);
                entity.HasIndex(e => e.OrganizerId);
                
                entity.HasOne(e => e.Category)
                    .WithMany(c => c.Events)
                    .HasForeignKey(e => e.CategoryId)
                    .OnDelete(DeleteBehavior.Restrict);
                
                entity.HasOne(e => e.Organizer)
                    .WithMany(u => u.OrganizedEvents)
                    .HasForeignKey(e => e.OrganizerId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure Category entity
            modelBuilder.Entity<Category>(entity =>
            {
                entity.HasKey(e => e.CategoryId);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.HasIndex(e => e.Name).IsUnique();
            });

            // Configure TicketType entity
            modelBuilder.Entity<TicketType>(entity =>
            {
                entity.HasKey(e => e.TicketTypeId);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Price).HasPrecision(18, 2);
                entity.HasIndex(e => e.EventId);
                entity.HasIndex(e => e.Status);
                
                entity.HasOne(e => e.Event)
                    .WithMany(ev => ev.TicketTypes)
                    .HasForeignKey(e => e.EventId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure TicketTypeDiscount entity
            modelBuilder.Entity<TicketTypeDiscount>(entity =>
            {
                entity.HasKey(e => e.DiscountId);
                entity.Property(e => e.Code).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Value).HasPrecision(18, 2);
                entity.HasIndex(e => e.Code).IsUnique();
                
                entity.HasOne(e => e.TicketType)
                    .WithMany(tt => tt.Discounts)
                    .HasForeignKey(e => e.TicketTypeId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure Order entity
            modelBuilder.Entity<Order>(entity =>
            {
                entity.HasKey(e => e.OrderId);
                entity.Property(e => e.OrderNumber).IsRequired().HasMaxLength(50);
                entity.Property(e => e.SubTotal).HasPrecision(18, 2);
                entity.Property(e => e.TaxAmount).HasPrecision(18, 2);
                entity.Property(e => e.ServiceFee).HasPrecision(18, 2);
                entity.Property(e => e.DiscountAmount).HasPrecision(18, 2);
                entity.Property(e => e.TotalAmount).HasPrecision(18, 2);
                entity.HasIndex(e => e.OrderNumber).IsUnique();
                entity.HasIndex(e => e.EventId);
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.Status);
                entity.HasIndex(e => e.OrderDate);
                
                entity.HasOne(e => e.Event)
                    .WithMany(ev => ev.Orders)
                    .HasForeignKey(e => e.EventId)
                    .OnDelete(DeleteBehavior.Restrict);
                
                entity.HasOne(e => e.User)
                    .WithMany(u => u.Orders)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure OrderItem entity
            modelBuilder.Entity<OrderItem>(entity =>
            {
                entity.HasKey(e => e.OrderItemId);
                entity.Property(e => e.UnitPrice).HasPrecision(18, 2);
                entity.Property(e => e.DiscountAmount).HasPrecision(18, 2);
                entity.Property(e => e.TotalPrice).HasPrecision(18, 2);
                
                entity.HasOne(e => e.Order)
                    .WithMany(o => o.OrderItems)
                    .HasForeignKey(e => e.OrderId)
                    .OnDelete(DeleteBehavior.Cascade);
                
                entity.HasOne(e => e.TicketType)
                    .WithMany(tt => tt.OrderItems)
                    .HasForeignKey(e => e.TicketTypeId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure Ticket entity
            modelBuilder.Entity<Ticket>(entity =>
            {
                entity.HasKey(e => e.TicketId);
                entity.Property(e => e.TicketNumber).IsRequired().HasMaxLength(100);
                entity.Property(e => e.QRCode).IsRequired().HasMaxLength(500);
                entity.HasIndex(e => e.TicketNumber).IsUnique();
                entity.HasIndex(e => e.Status);
                
                entity.HasOne(e => e.OrderItem)
                    .WithMany(oi => oi.Tickets)
                    .HasForeignKey(e => e.OrderItemId)
                    .OnDelete(DeleteBehavior.Cascade);
                
                entity.HasOne(e => e.TransferredToUser)
                    .WithMany()
                    .HasForeignKey(e => e.TransferredToUserId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // Configure Payment entity
            modelBuilder.Entity<Payment>(entity =>
            {
                entity.HasKey(e => e.PaymentId);
                entity.Property(e => e.PaymentIntentId).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Amount).HasPrecision(18, 2);
                entity.Property(e => e.ProcessingFee).HasPrecision(18, 2);
                entity.Property(e => e.NetAmount).HasPrecision(18, 2);
                entity.Property(e => e.RefundedAmount).HasPrecision(18, 2);
                entity.HasIndex(e => e.PaymentIntentId).IsUnique();
                entity.HasIndex(e => e.Status);
                
                entity.HasOne(e => e.Order)
                    .WithMany(o => o.Payments)
                    .HasForeignKey(e => e.OrderId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure PaymentRefund entity
            modelBuilder.Entity<PaymentRefund>(entity =>
            {
                entity.HasKey(e => e.RefundId);
                entity.Property(e => e.RefundTransactionId).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Amount).HasPrecision(18, 2);
                entity.HasIndex(e => e.RefundTransactionId).IsUnique();
                
                entity.HasOne(e => e.Payment)
                    .WithMany(p => p.Refunds)
                    .HasForeignKey(e => e.PaymentId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Seed data
            SeedData(modelBuilder);
        }

        private void SeedData(ModelBuilder modelBuilder)
        {
            // Seed Roles
            modelBuilder.Entity<Role>().HasData(
                new Role { RoleId = 1, Name = "Admin", Description = "System Administrator", IsActive = true, CreatedOn = DateTime.UtcNow },
                new Role { RoleId = 2, Name = "Organizer", Description = "Event Organizer", IsActive = true, CreatedOn = DateTime.UtcNow },
                new Role { RoleId = 3, Name = "Customer", Description = "Event Attendee", IsActive = true, CreatedOn = DateTime.UtcNow }
            );

            // Seed Categories
            modelBuilder.Entity<Category>().HasData(
                new Category { CategoryId = 1, Name = "Music", Description = "Music events and concerts", Color = "#FF6B6B", SortOrder = 1, IsActive = true, CreatedOn = DateTime.UtcNow, CreatedBy = 1 },
                new Category { CategoryId = 2, Name = "Sports", Description = "Sports events and games", Color = "#4ECDC4", SortOrder = 2, IsActive = true, CreatedOn = DateTime.UtcNow, CreatedBy = 1 },
                new Category { CategoryId = 3, Name = "Business", Description = "Business and networking events", Color = "#45B7D1", SortOrder = 3, IsActive = true, CreatedOn = DateTime.UtcNow, CreatedBy = 1 },
                new Category { CategoryId = 4, Name = "Arts", Description = "Arts and cultural events", Color = "#96CEB4", SortOrder = 4, IsActive = true, CreatedOn = DateTime.UtcNow, CreatedBy = 1 },
                new Category { CategoryId = 5, Name = "Food", Description = "Food and beverage events", Color = "#FFEAA7", SortOrder = 5, IsActive = true, CreatedOn = DateTime.UtcNow, CreatedBy = 1 },
                new Category { CategoryId = 6, Name = "Technology", Description = "Technology and innovation events", Color = "#DDA0DD", SortOrder = 6, IsActive = true, CreatedOn = DateTime.UtcNow, CreatedBy = 1 }
            );
        }
    }
}