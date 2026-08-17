using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AsusLaptop.Models
{
    public class Order
    {
        [Key]
        public int Id { get; set; }

        public int? UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual User? User { get; set; }

        [Required, StringLength(100)]
        public string CustomerName { get; set; } = string.Empty;

        [Required, StringLength(20)]
        public string Phone { get; set; } = string.Empty;

        [Required, StringLength(250)]
        public string Address { get; set; } = string.Empty;

        [EmailAddress, StringLength(100)]
        public string Email { get; set; } = string.Empty;

        [Required]
        public decimal TotalAmount { get; set; }

        public DateTime OrderDate { get; set; } = DateTime.Now;

        [Required, StringLength(50)]
        public string Status { get; set; } = "Pending";

        public string? Note { get; set; }

        [StringLength(30)]
        public string PaymentMethod { get; set; } = "COD";

        [StringLength(30)]
        public string PaymentStatus { get; set; } = "Unpaid";

        [StringLength(30)]
        public string? VoucherCode { get; set; }

        public decimal DiscountAmount { get; set; } = 0;

        // ── Theo dõi đơn hàng bằng bản đồ thời gian thực ────
        [StringLength(100)]
        public string? ShipperName { get; set; }

        [StringLength(20)]
        public string? ShipperPhone { get; set; }

        public double? ShipperLat { get; set; }
        public double? ShipperLng { get; set; }

        // Toạ độ điểm giao hàng (khách hàng) - admin ghim trên bản đồ
        public double? DestinationLat { get; set; }
        public double? DestinationLng { get; set; }

        public DateTime? LastLocationUpdate { get; set; }

        public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
    }

    public class OrderDetail
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int OrderId { get; set; }

        [ForeignKey("OrderId")]
        public virtual Order? Order { get; set; }

        [Required]
        public int ProductId { get; set; }

        [ForeignKey("ProductId")]
        public virtual Product? Product { get; set; }

        // Biến thể được chọn (RAM + Màu)
        public int? VariantId { get; set; }

        [ForeignKey("VariantId")]
        public virtual ProductVariant? Variant { get; set; }

        [Required]
        public int Quantity { get; set; }

        [Required]
        public decimal Price { get; set; }
    }

    public class CartItem
    {
        [Key]
        public int Id { get; set; }

        [Required, StringLength(100)]
        public string SessionId { get; set; } = string.Empty;

        [Required]
        public int ProductId { get; set; }

        [ForeignKey("ProductId")]
        public virtual Product? Product { get; set; }

        // Biến thể được chọn (RAM + Màu)
        public int? VariantId { get; set; }

        [ForeignKey("VariantId")]
        public virtual ProductVariant? Variant { get; set; }

        [Range(1, 1000)]
        public int Quantity { get; set; }
    }
}
