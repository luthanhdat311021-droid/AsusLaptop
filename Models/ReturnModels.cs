using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AsusLaptop.Models
{
    /// <summary>
    /// Yêu cầu đổi trả / hoàn tiền cho một đơn hàng đã giao thành công.
    /// </summary>
    public class ReturnRequest
    {
        [Key] public int Id { get; set; }

        [Required]
        public int OrderId { get; set; }
        [ForeignKey("OrderId")]
        public virtual Order? Order { get; set; }

        [Required]
        public int UserId { get; set; }
        [ForeignKey("UserId")]
        public virtual User? User { get; set; }

        /// <summary>Return | Refund | Exchange</summary>
        [Required, StringLength(20)]
        public string RequestType { get; set; } = "Return";

        [Required, StringLength(100)]
        public string Reason { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Description { get; set; }

        /// <summary>Danh sách URL ảnh minh chứng, cách nhau bởi dấu phẩy</summary>
        [StringLength(2000)]
        public string? ImageUrls { get; set; }

        /// <summary>Pending | Approved | Rejected | Refunded | Completed | Cancelled</summary>
        [Required, StringLength(20)]
        public string Status { get; set; } = "Pending";

        public decimal? RefundAmount { get; set; }

        /// <summary>BankTransfer | OriginalPayment | StoreCredit</summary>
        [StringLength(30)]
        public string? RefundMethod { get; set; }

        [StringLength(1000)]
        public string? AdminNote { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? ProcessedAt { get; set; }

        public int? ProcessedByUserId { get; set; }
        [ForeignKey("ProcessedByUserId")]
        public virtual User? ProcessedByUser { get; set; }

        public virtual ICollection<ReturnRequestItem> Items { get; set; } = new List<ReturnRequestItem>();

        [NotMapped]
        public string StatusVi => Status switch
        {
            "Pending"   => "Chờ duyệt",
            "Approved"  => "Đã duyệt — chờ nhận hàng hoàn",
            "Rejected"  => "Đã từ chối",
            "Refunded"  => "Đã hoàn tiền",
            "Completed" => "Hoàn tất",
            "Cancelled" => "Đã hủy yêu cầu",
            _           => Status
        };

        [NotMapped]
        public string RequestTypeVi => RequestType switch
        {
            "Return"   => "Trả hàng",
            "Refund"   => "Hoàn tiền",
            "Exchange" => "Đổi hàng",
            _          => RequestType
        };
    }

    /// <summary>Chi tiết từng sản phẩm được yêu cầu trả trong 1 ReturnRequest (hỗ trợ trả 1 phần đơn).</summary>
    public class ReturnRequestItem
    {
        [Key] public int Id { get; set; }

        [Required]
        public int ReturnRequestId { get; set; }
        [ForeignKey("ReturnRequestId")]
        public virtual ReturnRequest? ReturnRequest { get; set; }

        [Required]
        public int OrderDetailId { get; set; }
        [ForeignKey("OrderDetailId")]
        public virtual OrderDetail? OrderDetail { get; set; }

        [Required]
        public int Quantity { get; set; } = 1;
    }
}
