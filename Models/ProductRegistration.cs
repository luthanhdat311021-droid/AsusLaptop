using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AsusLaptop.Models
{
    /// <summary>
    /// Lưu lại việc khách hàng đăng ký sản phẩm (kích hoạt/xác nhận bảo hành) —
    /// dùng cho các trường hợp mua ngoài web (đại lý khác, được tặng...) chưa gắn sẵn với 1 đơn hàng.
    /// </summary>
    public class ProductRegistration
    {
        [Key]
        public int Id { get; set; }

        public int? UserId { get; set; }
        [ForeignKey("UserId")]
        public virtual User? User { get; set; }

        [Required, StringLength(30)]
        public string SerialNo { get; set; } = string.Empty;

        public int ProductId { get; set; }
        [ForeignKey("ProductId")]
        public virtual Product? Product { get; set; }

        [Required, StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required, StringLength(20)]
        public string Phone { get; set; } = string.Empty;

        [EmailAddress, StringLength(100)]
        public string? Email { get; set; }

        [Required]
        public DateTime PurchaseDate { get; set; }

        [StringLength(200)]
        public string? PurchasePlace { get; set; }

        public DateTime RegisteredAt { get; set; } = DateTime.Now;

        /// <summary>Pending | Approved | Rejected</summary>
        [StringLength(20)]
        public string Status { get; set; } = "Approved";

        public string? Note { get; set; }
    }
}
