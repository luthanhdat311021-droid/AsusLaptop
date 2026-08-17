using System;
using System.Collections.Generic;

namespace AsusLaptop.Models
{
    public class UserDeviceViewModel
    {
        public string SerialNo { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public string? Series { get; set; }
        public string? VariantInfo { get; set; }
        public DateTime PurchaseDate { get; set; }
        public DateTime? WarrantyEnd { get; set; }
        public bool IsActivated { get; set; }
        public bool IsExpired { get; set; }
        public int DaysLeft { get; set; }
        public int WarrantyProgressPercent { get; set; }
        public int MonthsSincePurchase { get; set; }
        
        /// <summary>Điểm sức khỏe tản nhiệt/máy (0-100)</summary>
        public int ThermalHealthScore { get; set; }
        public string ThermalHealthStatusText { get; set; } = string.Empty;
        public string ThermalHealthBadgeClass { get; set; } = string.Empty;
        public string MaintenanceRecommendation { get; set; } = string.Empty;
        public string RegistrationSource { get; set; } = "Đơn hàng Web";
    }

    public class ChamsocBaoHanhViewModel
    {
        public List<UserDeviceViewModel> Devices { get; set; } = new();
        public List<MaintenanceBooking> Bookings { get; set; } = new();
        public int TotalDevicesCount => Devices.Count;
        public int ActiveWarrantyCount => Devices.Count(d => d.IsActivated && !d.IsExpired);
        public int ExpiringSoonCount => Devices.Count(d => d.IsActivated && !d.IsExpired && d.DaysLeft <= 60);
        public int PendingBookingCount => Bookings.Count(b => b.Status == "Pending" || b.Status == "Confirmed" || b.Status == "InService");
        
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerPhone { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public bool IsAuthenticated { get; set; }
    }
}
