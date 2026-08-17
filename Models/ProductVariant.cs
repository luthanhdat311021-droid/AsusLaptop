using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AsusLaptop.Models
{
    public class ProductVariant
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ProductId { get; set; }

        [ForeignKey("ProductId")]
        public virtual Product? Product { get; set; }

        [Required, StringLength(50)]
        public string RAM { get; set; } = string.Empty;        // "16 GB DDR5"

        [Required, StringLength(50)]
        public string Color { get; set; } = string.Empty;      // "Eclipse Gray"

        [Required, StringLength(10)]
        public string ColorHex { get; set; } = "#333333";      // "#3D3D3D"

        /// <summary>Chênh lệch giá so với giá gốc của Product (có thể âm)</summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal PriceAdjust { get; set; } = 0;

        [Required]
        public int Stock { get; set; } = 0;

        /// <summary>Biến thể mặc định hiển thị khi vào trang sản phẩm</summary>
        public bool IsDefault { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public virtual ICollection<SerialNumber> SerialNumbers { get; set; } = new List<SerialNumber>();

        [NotMapped]
        public string DisplayLabel => $"{RAM} · {Color}";
    }
}
