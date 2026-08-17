using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AsusLaptop.Models
{
    /// <summary>
    /// Đơn đặt lịch bảo dưỡng / vệ sinh máy / bảo hành từ khách hàng.
    /// </summary>
    public class MaintenanceBooking
    {
        [Key]
        public int Id { get; set; }

        public int? UserId { get; set; }
        [ForeignKey("UserId")]
        public virtual User? User { get; set; }

        [Required, StringLength(50)]
        public string SerialNo { get; set; } = string.Empty;

        [Required, StringLength(150)]
        public string ProductName { get; set; } = string.Empty;

        /// <summary>
        /// Loại dịch vụ: "Vệ sinh tản nhiệt định kỳ (Miễn phí)", "Thay keo tản nhiệt", "Kiểm tra & Chẩn đoán phần cứng", "Cài đặt Driver/BIOS"
        /// </summary>
        [Required, StringLength(100)]
        public string ServiceType { get; set; } = "Vệ sinh tản nhiệt định kỳ (Miễn phí)";

        /// <summary>
        /// Phương thức: "Mang tới Showroom" | "Gửi nhận tận nhà miễn phí"
        /// </summary>
        [Required, StringLength(50)]
        public string ServiceMethod { get; set; } = "Mang tới Showroom";

        [Required]
        public DateTime PreferredDate { get; set; }

        [Required, StringLength(50)]
        public string PreferredTime { get; set; } = "09:00 - 11:30";

        [Required, StringLength(100)]
        public string CustomerName { get; set; } = string.Empty;

        [Required, StringLength(20)]
        public string Phone { get; set; } = string.Empty;

        [StringLength(250)]
        public string? Address { get; set; }

        [StringLength(500)]
        public string? Note { get; set; }

        /// <summary>
        /// Trạng thái: Pending (Chờ xác nhận) | Confirmed (Đã xác nhận) | InService (Đang bảo dưỡng) | Completed (Hoàn thành) | Cancelled (Đã hủy)
        /// </summary>
        [Required, StringLength(30)]
        public string Status { get; set; } = "Pending";

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [NotMapped]
        public string StatusBadgeClass => Status switch
        {
            "Pending"   => "bg-warning text-dark",
            "Confirmed" => "bg-info text-dark",
            "InService" => "bg-primary",
            "Completed" => "bg-success",
            "Cancelled" => "bg-secondary",
            _           => "bg-light text-dark"
        };

        [NotMapped]
        public string StatusVi => Status switch
        {
            "Pending"   => "Chờ xác nhận",
            "Confirmed" => "Đã xác nhận lịch",
            "InService" => "Đang xử lý / Bảo dưỡng",
            "Completed" => "Hoàn thành",
            "Cancelled" => "Đã hủy",
            _           => Status
        };
    }
}
