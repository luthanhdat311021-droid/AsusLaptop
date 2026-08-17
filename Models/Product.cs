using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AsusLaptop.Models
{
    public class Product
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(150)]
        public string Name { get; set; } = string.Empty;

        // ── Quan hệ Category ───────────────────────────────
        public int? CategoryId { get; set; }
        [ForeignKey("CategoryId")]
        public virtual Category? Category { get; set; }

        // ── Quan hệ Brand (bảng riêng) ──────────────────────
        public int? BrandId { get; set; }
        [ForeignKey("BrandId")]
        public virtual Brand? BrandRef { get; set; }

        [Required]
        [Range(0, 1000000000)]
        public decimal Price { get; set; }

        [Range(0, 1000000000)]
        public decimal OriginalPrice { get; set; }

        [StringLength(500)]
        public string ImageUrl { get; set; } = string.Empty;

        [Range(0, 100000)]
        public int Quantity { get; set; }

        public string Description { get; set; } = string.Empty;

        [StringLength(50)]
        public string Brand { get; set; } = string.Empty; // ASUS, Acer, Dell, HP, Lenovo

        [StringLength(50)]
        public string Series { get; set; } = string.Empty; // ROG, VivoBook, ZenBook, TUF, ProArt

        // Laptop specs
        [StringLength(50)]
        public string ScreenSize { get; set; } = string.Empty; // 14 inch, 15.6 inch

        [StringLength(50)]
        public string ScreenResolution { get; set; } = string.Empty; // FHD, 2K, 4K

        [StringLength(80)]
        public string CPU { get; set; } = string.Empty; // Intel Core i7-13700H

        [StringLength(20)]
        public string RAM { get; set; } = string.Empty; // 16 GB

        [StringLength(30)]
        public string Storage { get; set; } = string.Empty; // 512 GB SSD

        [StringLength(80)]
        public string GPU { get; set; } = string.Empty; // NVIDIA RTX 4060

        [StringLength(30)]
        public string Battery { get; set; } = string.Empty; // 90 WHr

        [StringLength(30)]
        public string Weight { get; set; } = string.Empty; // 1.5 kg

        [StringLength(30)]
        public string OS { get; set; } = string.Empty; // Windows 11

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Lượt xem trang sản phẩm
        public int ViewCount { get; set; } = 0;

        // Link video đánh giá sản phẩm (YouTube, v.v.) do admin nhập tay
        [StringLength(500)]
        public string? VideoUrl { get; set; } = string.Empty;
    }
}
