using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AsusLaptop.Models
{
    // ───────────── ĐÁNH GIÁ 5 SAO ─────────────
    public class Review
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ProductId { get; set; }
        [ForeignKey("ProductId")]
        public virtual Product? Product { get; set; }

        [Required]
        public int UserId { get; set; }
        [ForeignKey("UserId")]
        public virtual User? User { get; set; }

        [Required, Range(1, 5)]
        public int Rating { get; set; }

        [StringLength(1000)]
        public string? Comment { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }

    // ───────────── WISHLIST (SẢN PHẨM YÊU THÍCH) ─────────────
    public class WishlistItem
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }
        [ForeignKey("UserId")]
        public virtual User? User { get; set; }

        [Required]
        public int ProductId { get; set; }
        [ForeignKey("ProductId")]
        public virtual Product? Product { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }

    // ───────────── VOUCHER / MÃ GIẢM GIÁ ─────────────
    public class Voucher
    {
        [Key]
        public int Id { get; set; }

        [Required, StringLength(30)]
        public string Code { get; set; } = string.Empty;

        [StringLength(200)]
        public string? Description { get; set; }

        // "Percent" = giảm theo %, "Amount" = giảm số tiền cố định
        [Required, StringLength(10)]
        public string DiscountType { get; set; } = "Percent";

        [Required]
        public decimal DiscountValue { get; set; }

        // Giảm tối đa bao nhiêu tiền (áp dụng khi DiscountType = Percent), null = không giới hạn
        public decimal? MaxDiscountAmount { get; set; }

        // Đơn hàng phải đạt giá trị tối thiểu này mới áp dụng được
        public decimal MinOrderAmount { get; set; } = 0;

        public DateTime StartDate { get; set; } = DateTime.Now;
        public DateTime ExpiryDate { get; set; }

        // Tổng số lượt được dùng (null = không giới hạn)
        public int? UsageLimit { get; set; }
        public int UsedCount { get; set; } = 0;

        public bool IsActive { get; set; } = true;
    }
}
