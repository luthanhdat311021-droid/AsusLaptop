using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AsusLaptop.Models
{
    /// <summary>
    /// Số serial riêng cho từng đơn vị sản phẩm vật lý.
    /// Format: ASU-[SERIES3]-[YY]-[000001]
    /// </summary>
    public class SerialNumber
    {
        [Key]
        public int Id { get; set; }

        [Required, StringLength(30)]
        public string SerialNo { get; set; } = string.Empty;

        [Required]
        public int ProductId { get; set; }

        [ForeignKey("ProductId")]
        public virtual Product? Product { get; set; }

        public int? VariantId { get; set; }

        [ForeignKey("VariantId")]
        public virtual ProductVariant? Variant { get; set; }

        /// <summary>Available | Reserved | Sold | Warranty</summary>
        [Required, StringLength(20)]
        public string Status { get; set; } = "Available";

        public int? OrderDetailId { get; set; }

        [ForeignKey("OrderDetailId")]
        public virtual OrderDetail? OrderDetail { get; set; }

        public DateTime? WarrantyEnd { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        [NotMapped]
        public string StatusBadge => Status switch
        {
            "Available" => "success",
            "Reserved"  => "warning",
            "Sold"      => "secondary",
            "Warranty"  => "info",
            _           => "light"
        };

        [NotMapped]
        public string StatusVi => Status switch
        {
            "Available" => "Có sẵn",
            "Reserved"  => "Đang giữ",
            "Sold"      => "Đã bán",
            "Warranty"  => "Bảo hành",
            _           => Status
        };
    }

    public static class SerialNumberGenerator
    {
        /// <summary>
        /// Sinh 1 chuỗi serial. startSeq phải được tính từ DB (số lớn nhất hiện có + 1)
        /// để tránh trùng lặp khi ứng dụng khởi động lại — KHÔNG dùng biến đếm trong RAM.
        /// </summary>
        public static string Generate(string seriesPrefix, int seq)
        {
            var clean = new string(seriesPrefix.Where(char.IsLetterOrDigit).ToArray())
                            .ToUpper();
            string prefix = clean.Length >= 3 ? clean[..3] : clean.PadRight(3, 'X');
            string year   = DateTime.Now.ToString("yy");
            return $"ASU-{prefix}-{year}-{seq:D6}";
        }

        public static List<string> GenerateBatch(string seriesPrefix, int count, int startSeq = 1)
            => Enumerable.Range(startSeq, count).Select(seq => Generate(seriesPrefix, seq)).ToList();

        /// <summary>Tiền tố + năm dùng để tra cứu serial hiện có, đảm bảo tính đúng dải số tiếp theo.</summary>
        public static string BuildPrefixPattern(string seriesPrefix)
        {
            var clean = new string(seriesPrefix.Where(char.IsLetterOrDigit).ToArray()).ToUpper();
            string prefix = clean.Length >= 3 ? clean[..3] : clean.PadRight(3, 'X');
            string year   = DateTime.Now.ToString("yy");
            return $"ASU-{prefix}-{year}-";
        }
    }
}
