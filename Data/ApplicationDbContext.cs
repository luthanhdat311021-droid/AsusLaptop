using Microsoft.EntityFrameworkCore;
using AsusLaptop.Models;

namespace AsusLaptop.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        // ── Bảng cũ ──
        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Product> Products { get; set; } = null!;
        public DbSet<ProductVariant> ProductVariants { get; set; } = null!;
        public DbSet<SerialNumber> SerialNumbers { get; set; } = null!;
        public DbSet<ProductRegistration> ProductRegistrations { get; set; } = null!;
        public DbSet<CartItem> CartItems { get; set; } = null!;
        public DbSet<Order> Orders { get; set; } = null!;
        public DbSet<OrderDetail> OrderDetails { get; set; } = null!;
        public DbSet<Review> Reviews { get; set; } = null!;
        public DbSet<WishlistItem> WishlistItems { get; set; } = null!;
        public DbSet<Voucher> Vouchers { get; set; } = null!;

        // ── Bảng mới ──
        public DbSet<Category> Categories { get; set; } = null!;
        public DbSet<Brand> Brands { get; set; } = null!;
        public DbSet<ProductImage> ProductImages { get; set; } = null!;
        public DbSet<ProductSpecification> ProductSpecifications { get; set; } = null!;
        public DbSet<Notification> Notifications { get; set; } = null!;
        public DbSet<ChatHistory> ChatHistories { get; set; } = null!;
        public DbSet<PaymentTransaction> PaymentTransactions { get; set; } = null!;
        public DbSet<UserAddress> UserAddresses { get; set; } = null!;
        public DbSet<InventoryLog> InventoryLogs { get; set; } = null!;
        public DbSet<ReturnRequest> ReturnRequests { get; set; } = null!;
        public DbSet<ReturnRequestItem> ReturnRequestItems { get; set; } = null!;
        public DbSet<MaintenanceBooking> MaintenanceBookings { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ── Bảng cũ ──────────────────────────────────────────────────

            modelBuilder.Entity<OrderDetail>()
                .HasOne(od => od.Product).WithMany()
                .HasForeignKey(od => od.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            // NoAction thay SetNull — tránh multiple cascade paths trên SQL Server
            modelBuilder.Entity<OrderDetail>()
                .HasOne(od => od.Variant).WithMany()
                .HasForeignKey(od => od.VariantId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<CartItem>()
                .HasOne(c => c.Variant).WithMany()
                .HasForeignKey(c => c.VariantId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<ProductVariant>()
                .HasOne(v => v.Product).WithMany()
                .HasForeignKey(v => v.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ProductVariant>()
                .HasIndex(v => new { v.ProductId, v.RAM, v.Color }).IsUnique();

            modelBuilder.Entity<SerialNumber>()
                .HasIndex(s => s.SerialNo).IsUnique();

            modelBuilder.Entity<SerialNumber>()
                .HasOne(s => s.Product).WithMany()
                .HasForeignKey(s => s.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<SerialNumber>()
                .HasOne(s => s.Variant).WithMany(v => v.SerialNumbers)
                .HasForeignKey(s => s.VariantId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<SerialNumber>()
                .HasOne(s => s.OrderDetail).WithMany()
                .HasForeignKey(s => s.OrderDetailId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Review>()
                .HasOne(r => r.Product).WithMany().HasForeignKey(r => r.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<Review>()
                .HasOne(r => r.User).WithMany().HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<Review>()
                .HasIndex(r => new { r.ProductId, r.UserId }).IsUnique();

            modelBuilder.Entity<WishlistItem>()
                .HasOne(w => w.Product).WithMany().HasForeignKey(w => w.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<WishlistItem>()
                .HasOne(w => w.User).WithMany().HasForeignKey(w => w.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<WishlistItem>()
                .HasIndex(w => new { w.UserId, w.ProductId }).IsUnique();

            modelBuilder.Entity<Voucher>()
                .HasIndex(v => v.Code).IsUnique();

            // ── Bảng mới ─────────────────────────────────────────────────

            modelBuilder.Entity<Category>()
                .HasOne(c => c.Parent).WithMany(c => c.Children)
                .HasForeignKey(c => c.ParentId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Product>()
                .HasOne(p => p.Category).WithMany(c => c.Products)
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Product>()
                .HasOne(p => p.BrandRef).WithMany(b => b.Products)
                .HasForeignKey(p => p.BrandId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<ProductImage>()
                .HasOne(pi => pi.Product).WithMany()
                .HasForeignKey(pi => pi.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ProductImage>()
                .HasOne(pi => pi.Variant).WithMany()
                .HasForeignKey(pi => pi.VariantId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<ProductSpecification>()
                .HasOne(ps => ps.Product).WithMany()
                .HasForeignKey(ps => ps.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Notification>()
                .HasOne(n => n.User).WithMany()
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ChatHistory>()
                .HasOne(ch => ch.User).WithMany()
                .HasForeignKey(ch => ch.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<ChatHistory>()
                .HasIndex(ch => ch.SessionId);

            modelBuilder.Entity<PaymentTransaction>()
                .HasOne(pt => pt.Order).WithMany()
                .HasForeignKey(pt => pt.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PaymentTransaction>()
                .HasIndex(pt => pt.TransactionCode).IsUnique()
                .HasFilter("[TransactionCode] IS NOT NULL");

            modelBuilder.Entity<UserAddress>()
                .HasOne(ua => ua.User).WithMany()
                .HasForeignKey(ua => ua.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<InventoryLog>()
                .HasOne(il => il.Product).WithMany()
                .HasForeignKey(il => il.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<InventoryLog>()
                .HasOne(il => il.Variant).WithMany()
                .HasForeignKey(il => il.VariantId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<InventoryLog>()
                .HasOne(il => il.CreatedByUser).WithMany()
                .HasForeignKey(il => il.CreatedByUserId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<InventoryLog>()
                .HasOne(il => il.Order).WithMany()
                .HasForeignKey(il => il.OrderId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<ReturnRequest>()
                .HasOne(r => r.Order).WithMany()
                .HasForeignKey(r => r.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ReturnRequest>()
                .HasOne(r => r.User).WithMany()
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<ReturnRequest>()
                .HasOne(r => r.ProcessedByUser).WithMany()
                .HasForeignKey(r => r.ProcessedByUserId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<ReturnRequestItem>()
                .HasOne(ri => ri.ReturnRequest).WithMany(r => r.Items)
                .HasForeignKey(ri => ri.ReturnRequestId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ReturnRequestItem>()
                .HasOne(ri => ri.OrderDetail).WithMany()
                .HasForeignKey(ri => ri.OrderDetailId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<MaintenanceBooking>()
                .HasOne(m => m.User).WithMany()
                .HasForeignKey(m => m.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            // ── STRATEGIC DATABASE INDEXES FOR PERFORMANCE ───────────────
            modelBuilder.Entity<Product>()
                .HasIndex(p => new { p.CategoryId, p.Price })
                .HasDatabaseName("IX_Products_Category_Price");

            modelBuilder.Entity<Product>()
                .HasIndex(p => p.Name)
                .HasDatabaseName("IX_Products_Name");

            modelBuilder.Entity<Product>()
                .HasIndex(p => new { p.BrandId, p.Price })
                .HasDatabaseName("IX_Products_Brand_Price");

            modelBuilder.Entity<Product>()
                .HasIndex(p => p.CreatedAt)
                .HasDatabaseName("IX_Products_CreatedAt");

            modelBuilder.Entity<Order>()
                .HasIndex(o => new { o.UserId, o.Status, o.OrderDate })
                .HasDatabaseName("IX_Orders_User_Status_OrderDate");

            modelBuilder.Entity<Order>()
                .HasIndex(o => o.OrderDate)
                .HasDatabaseName("IX_Orders_OrderDate");

            modelBuilder.Entity<OrderDetail>()
                .HasIndex(od => new { od.OrderId, od.ProductId })
                .HasDatabaseName("IX_OrderDetails_OrderId_ProductId");

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique()
                .HasDatabaseName("IX_Users_Email");

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Role)
                .HasDatabaseName("IX_Users_Role");

            modelBuilder.Entity<CartItem>()
                .HasIndex(c => new { c.SessionId, c.ProductId })
                .HasDatabaseName("IX_CartItems_Session_Product");

            modelBuilder.Entity<Notification>()
                .HasIndex(n => new { n.UserId, n.IsRead, n.CreatedAt })
                .HasDatabaseName("IX_Notifications_User_IsRead_CreatedAt");

            modelBuilder.Entity<InventoryLog>()
                .HasIndex(il => new { il.ProductId, il.CreatedAt })
                .HasDatabaseName("IX_InventoryLogs_Product_CreatedAt");
        }
    }
}
