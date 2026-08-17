using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AsusLaptop.Models
{
    // ───────────── CATEGORIES (DANH MỤC SẢN PHẨM) ─────────────
    public class Category
    {
        [Key] public int Id { get; set; }

        [Required, StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        public int? ParentId { get; set; }
        [ForeignKey("ParentId")]
        public virtual Category? Parent { get; set; }
        public virtual ICollection<Category> Children { get; set; } = new List<Category>();

        [StringLength(300)]
        public string? ImageUrl { get; set; }

        public int SortOrder { get; set; } = 0;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public virtual ICollection<Product> Products { get; set; } = new List<Product>();
    }

    // ───────────── BRANDS (THƯƠNG HIỆU) ─────────────
    public class Brand
    {
        [Key] public int Id { get; set; }

        [Required, StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        [StringLength(300)]
        public string? LogoUrl { get; set; }

        [StringLength(300)]
        public string? WebsiteUrl { get; set; }

        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public virtual ICollection<Product> Products { get; set; } = new List<Product>();
    }

    // ───────────── PRODUCT IMAGES (ẢNH SẢN PHẨM) ─────────────
    public class ProductImage
    {
        [Key] public int Id { get; set; }

        [Required]
        public int ProductId { get; set; }
        [ForeignKey("ProductId")]
        public virtual Product? Product { get; set; }

        [Required, StringLength(500)]
        public string ImageUrl { get; set; } = string.Empty;

        [StringLength(200)]
        public string? AltText { get; set; }

        public int SortOrder { get; set; } = 0;
        public bool IsPrimary { get; set; } = false;

        public int? VariantId { get; set; }
        [ForeignKey("VariantId")]
        public virtual ProductVariant? Variant { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }

    // ───────────── PRODUCT SPECIFICATIONS (THÔNG SỐ KỸ THUẬT MỞ RỘNG) ─────────────
    public class ProductSpecification
    {
        [Key] public int Id { get; set; }

        [Required]
        public int ProductId { get; set; }
        [ForeignKey("ProductId")]
        public virtual Product? Product { get; set; }

        [Required, StringLength(100)]
        public string SpecName { get; set; } = string.Empty;   // VD: "Cổng kết nối"

        [Required, StringLength(500)]
        public string SpecValue { get; set; } = string.Empty;  // VD: "2x USB-A, 1x USB-C"

        [StringLength(100)]
        public string? GroupName { get; set; }                 // VD: "Kết nối"

        public int SortOrder { get; set; } = 0;
    }

    // ───────────── NOTIFICATIONS (THÔNG BÁO) ─────────────
    public class Notification
    {
        [Key] public int Id { get; set; }

        public int? UserId { get; set; }   // null = broadcast toàn hệ thống
        [ForeignKey("UserId")]
        public virtual User? User { get; set; }

        [Required, StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Message { get; set; } = string.Empty;

        [StringLength(30)]
        public string Type { get; set; } = "System";  // Order|Promotion|System|Review|Stock

        public bool IsRead { get; set; } = false;

        [StringLength(300)]
        public string? ActionUrl { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? ReadAt { get; set; }
    }

    // ───────────── CHAT HISTORY (LỊCH SỬ CHAT) ─────────────
    public class ChatHistory
    {
        [Key] public int Id { get; set; }

        public int? UserId { get; set; }   // null = khách vãng lai
        [ForeignKey("UserId")]
        public virtual User? User { get; set; }

        [Required, StringLength(100)]
        public string SessionId { get; set; } = string.Empty;

        [Required, StringLength(20)]
        public string SenderRole { get; set; } = "User";  // User|Bot|Admin

        [Required]
        public string Message { get; set; } = string.Empty;

        [StringLength(30)]
        public string MessageType { get; set; } = "Text";  // Text|Image|ProductCard|Order

        public DateTime SentAt { get; set; } = DateTime.Now;
    }

    // ───────────── PAYMENT TRANSACTIONS (GIAO DỊCH THANH TOÁN) ─────────────
    public class PaymentTransaction
    {
        [Key] public int Id { get; set; }

        [Required]
        public int OrderId { get; set; }
        [ForeignKey("OrderId")]
        public virtual Order? Order { get; set; }

        [Required, StringLength(30)]
        public string Gateway { get; set; } = string.Empty;  // VNPay|Momo|ZaloPay|COD|BankTransfer

        [StringLength(100)]
        public string? TransactionCode { get; set; }

        [Required, Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [StringLength(10)]
        public string Currency { get; set; } = "VND";

        [Required, StringLength(20)]
        public string Status { get; set; } = "Pending";  // Pending|Success|Failed|Refunded

        public string? RawResponse { get; set; }  // JSON thô từ cổng thanh toán

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? CompletedAt { get; set; }
    }

    // ───────────── USER ADDRESSES (ĐỊA CHỈ GIAO HÀNG) ─────────────
    public class UserAddress
    {
        [Key] public int Id { get; set; }

        [Required]
        public int UserId { get; set; }
        [ForeignKey("UserId")]
        public virtual User? User { get; set; }

        [Required, StringLength(100)]
        public string RecipientName { get; set; } = string.Empty;

        [Required, StringLength(20)]
        public string Phone { get; set; } = string.Empty;

        [Required, StringLength(300)]
        public string AddressLine { get; set; } = string.Empty;

        [StringLength(100)] public string? Ward { get; set; }      // Phường/Xã
        [StringLength(100)] public string? District { get; set; }  // Quận/Huyện

        [Required, StringLength(100)]
        public string City { get; set; } = string.Empty;

        public bool IsDefault { get; set; } = false;

        [StringLength(50)]
        public string? Label { get; set; }  // "Nhà riêng", "Văn phòng"

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }

    // ───────────── INVENTORY LOGS (NHẬT KÝ KHO HÀNG) ─────────────
    public class InventoryLog
    {
        [Key] public int Id { get; set; }

        [Required]
        public int ProductId { get; set; }
        [ForeignKey("ProductId")]
        public virtual Product? Product { get; set; }

        public int? VariantId { get; set; }
        [ForeignKey("VariantId")]
        public virtual ProductVariant? Variant { get; set; }

        [Required]
        public int QuantityChange { get; set; }  // Dương = nhập, Âm = xuất

        public int StockAfter { get; set; }      // Tồn kho sau thay đổi

        [Required, StringLength(30)]
        public string Reason { get; set; } = "Adjustment";  // Import|Sale|Return|Adjustment|Damage

        [StringLength(500)]
        public string? Note { get; set; }

        public int? CreatedByUserId { get; set; }  // null = hệ thống tự động
        [ForeignKey("CreatedByUserId")]
        public virtual User? CreatedByUser { get; set; }

        public int? OrderId { get; set; }
        [ForeignKey("OrderId")]
        public virtual Order? Order { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
