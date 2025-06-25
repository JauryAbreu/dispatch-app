using dispatch_app.Models.Transactions;
using dispatch_app.Models.User;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Reflection.Emit;

namespace dispatch_app.Models
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
        // Transactions
        public DbSet<Customer> Customers { get; set; }
        public DbSet<Delivery> Deliveries { get; set; }
        public DbSet<Header> Headers { get; set; }
        public DbSet<Lines> Lines { get; set; }
        public DbSet<Store> Stores { get; set; }
        public DbSet<Fiscal> Fiscal { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);


            // Customer ↔ Delivery
            builder.Entity<Delivery>()
                .HasOne(d => d.Customer)
                .WithMany(c => c.Deliveries)
                .HasForeignKey(d => d.CustomerId)
                .OnDelete(DeleteBehavior.SetNull);


            // Store ↔ Delivery
            builder.Entity<Delivery>()
                .HasOne(d => d.Store)
                .WithMany(s => s.Deliveries)
                .HasForeignKey(d => d.StoreId)
                .OnDelete(DeleteBehavior.SetNull);

            // Header ↔ Delivery
            builder.Entity<Delivery>()
                .HasOne(d => d.Header)
                .WithMany(h => h.Deliveries)
                .HasForeignKey(d => d.HeaderId)
                .OnDelete(DeleteBehavior.SetNull);

            // Header ↔ Lines
            builder.Entity<Lines>()
                .HasOne(l => l.Header)
                .WithMany(h => h.Lines)
                .HasForeignKey(l => l.HeaderId)
                .OnDelete(DeleteBehavior.Cascade);


            builder.Entity<Lines>()
                .Property(l => l.CreatedDate)
                .HasDefaultValueSql("GETDATE()");

            builder.Entity<Lines>()
                .Property(l => l.UpdatedDate)
                .HasDefaultValueSql("GETDATE()");

            builder.Entity<Lines>()
                .Property(l => l.UserCode)
                .HasDefaultValueSql("''");

            builder.Entity<Lines>()
                .Property(l => l.Status)
                .HasDefaultValueSql("4");

            builder.Entity<Lines>()
                .Property(l => l.Bin)
                .HasDefaultValueSql("''");

            builder.Entity<Header>()
                .HasIndex(h => h.ReceiptId)
                .HasDatabaseName("IX_Header_ReceiptId");

            builder.Entity<Header>()
                .Property(l => l.Quantity)
                .HasDefaultValueSql("0");

            builder.Entity<Header>()
                .Property(l => l.UserCode)
                .HasDefaultValueSql("''");

            builder.Entity<Header>()
                .Property(l => l.QuantityPending)
                .HasDefaultValueSql("0");

            builder.Entity<Header>()
                .Property(l => l.QuantityDispatched)
                .HasDefaultValueSql("0");

            builder.Entity<Header>()
                .Property(l => l.QuantityDelivery)
                .HasDefaultValueSql("0");

            builder.Entity<Header>()
                .Property(l => l.Status)
                .HasDefaultValueSql("5");


            builder.Entity<Header>()
                .Property(l => l.CreatedDate)
                .HasDefaultValueSql("GETDATE()");

            builder.Entity<Header>()
                .Property(l => l.UpdatedDate)
                .HasDefaultValueSql("GETDATE()");

            builder.Entity<Header>()
                .Property(l => l.IsRecalculate)
                .HasDefaultValueSql("0");

            builder.Entity<Header>()
                .Property(l => l.IsDelivery)
                .HasDefaultValueSql("0");

            builder.Entity<Header>()
                .Property(l => l.IsAssigned)
                .HasDefaultValueSql("0");



            builder.Entity<Customer>()
                .HasIndex(h => h.CustomerId)
                .HasDatabaseName("IX_Customer_CustomerId");

            builder.Entity<Customer>()
                .Property(l => l.Address)
                .HasDefaultValueSql("'Aut. Duarte'");

            builder.Entity<Customer>()
                .Property(l => l.City)
                .HasDefaultValueSql("'Santo Domingo'");

            builder.Entity<Customer>()
                .Property(l => l.State)
                .HasDefaultValueSql("'Rep. Dom.'");

            builder.Entity<Store>()
                .HasIndex(h => h.StoreId)
                .HasDatabaseName("IX_Store_StoreId");

            builder.Entity<Fiscal>()
                .HasIndex(h => h.ReceiptId)
                .HasDatabaseName("IX_Fiscal_ReceiptId");


        }
    }
}
