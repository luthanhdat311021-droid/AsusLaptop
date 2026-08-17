using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AsusLaptop.Data;
using AsusLaptop.Models;

namespace AsusLaptop.Controllers
{
    public class SupportController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SupportController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult TraCuuBaoHanh() => View();
        public IActionResult HuongDanMuaHang() => View();
        public IActionResult ChinhSachDoiTra() => View();
        public IActionResult PhuongThucThanhToan() => View();
        public IActionResult CauHoiThuongGap() => View();
        public IActionResult HoTroKyThuatTrucTuyen() => View();
        public IActionResult HuongDanCaiDatPhanMem() => View();
        public IActionResult ChinhSachBaoMat() => View();

        // ── Chương trình khách hàng thân thiết — tính hạng thành viên THẬT
        //    dựa trên tổng chi tiêu từ các đơn hàng đã hoàn tất ────────────
        public async Task<IActionResult> ChuongTrinhThanThiet()
        {
            decimal totalSpent = 0;
            int completedOrders = 0;

            if (User.Identity?.IsAuthenticated == true)
            {
                var userIdClaim = User.FindFirst("UserId")?.Value;
                if (int.TryParse(userIdClaim, out var userId))
                {
                    var completed = await _context.Orders
                        .Where(o => o.UserId == userId && o.Status == "Completed")
                        .ToListAsync();

                    totalSpent = completed.Sum(o => o.TotalAmount);
                    completedOrders = completed.Count;
                }
            }

            // Định nghĩa các hạng thành viên
            var tiers = new List<(string Name, decimal Threshold, string Color, string Icon, string[] Benefits)>
            {
                ("Đồng",      0,          "#b08d57", "fa-medal",
                    new[] { "Tích điểm 1% giá trị đơn hàng", "Ưu tiên hỗ trợ qua hotline" }),
                ("Bạc",       20_000_000, "#adb5bd", "fa-award",
                    new[] { "Tích điểm 2% giá trị đơn hàng", "Giảm 2% cho đơn hàng tiếp theo", "Ưu tiên hỗ trợ qua hotline" }),
                ("Vàng",      50_000_000, "#f5c400", "fa-crown",
                    new[] { "Tích điểm 3% giá trị đơn hàng", "Giảm 5% cho đơn hàng tiếp theo", "Miễn phí vận chuyển toàn quốc", "Ưu tiên bảo hành nhanh" }),
                ("Kim Cương", 100_000_000,"#7dd3fc", "fa-gem",
                    new[] { "Tích điểm 5% giá trị đơn hàng", "Giảm 8% cho đơn hàng tiếp theo", "Miễn phí vận chuyển toàn quốc", "Chăm sóc khách hàng riêng (1-1)", "Quà tặng sinh nhật hàng năm" }),
            };

            var currentTier = tiers.Last(t => totalSpent >= t.Threshold);
            var nextTier = tiers.FirstOrDefault(t => t.Threshold > totalSpent);

            ViewBag.TotalSpent = totalSpent;
            ViewBag.CompletedOrders = completedOrders;
            ViewBag.Tiers = tiers;
            ViewBag.CurrentTier = currentTier;
            ViewBag.NextTier = nextTier.Threshold == 0 && nextTier.Name == null ? ((string, decimal, string, string, string[])?)null : nextTier;
            ViewBag.IsLoggedIn = User.Identity?.IsAuthenticated == true;

            return View();
        }

        // ── Đăng ký sản phẩm (kích hoạt bảo hành cho serial chưa gắn đơn hàng) ──
        public IActionResult DangKySanPham() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DangKySanPham(string serialNo, string fullName, string phone, string? email, DateTime purchaseDate, string? purchasePlace)
        {
            if (string.IsNullOrWhiteSpace(serialNo) || string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(phone))
            {
                TempData["ErrorMessage"] = "Vui lòng nhập đầy đủ Số serial, Họ tên và Số điện thoại.";
                return RedirectToAction(nameof(DangKySanPham));
            }

            var serial = await _context.SerialNumbers.FirstOrDefaultAsync(s => s.SerialNo == serialNo.Trim());
            if (serial == null)
            {
                TempData["ErrorMessage"] = $"Không tìm thấy số serial \"{serialNo}\" trong hệ thống. Vui lòng kiểm tra lại hoặc liên hệ hotline 1800 1234 để được hỗ trợ.";
                return RedirectToAction(nameof(DangKySanPham));
            }

            var alreadyRegistered = await _context.ProductRegistrations.AnyAsync(r => r.SerialNo == serialNo.Trim());
            if (alreadyRegistered)
            {
                TempData["ErrorMessage"] = "Số serial này đã được đăng ký trước đó.";
                return RedirectToAction(nameof(DangKySanPham));
            }

            int? userId = null;
            if (User.Identity?.IsAuthenticated == true && int.TryParse(User.FindFirst("UserId")?.Value, out var uid))
                userId = uid;

            var registration = new ProductRegistration
            {
                UserId = userId,
                SerialNo = serial.SerialNo,
                ProductId = serial.ProductId,
                FullName = fullName.Trim(),
                Phone = phone.Trim(),
                Email = email?.Trim(),
                PurchaseDate = purchaseDate,
                PurchasePlace = purchasePlace?.Trim(),
                RegisteredAt = DateTime.Now,
                Status = "Approved"
            };
            _context.ProductRegistrations.Add(registration);

            // Nếu serial chưa có ngày hết hạn bảo hành, tự động kích hoạt bảo hành 24 tháng
            // tính từ ngày mua khách khai báo
            if (serial.WarrantyEnd == null)
            {
                serial.WarrantyEnd = purchaseDate.AddMonths(24);
                if (serial.Status == "Available" || serial.Status == "Sold")
                    serial.Status = "Warranty";
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Đăng ký sản phẩm thành công! Serial {serial.SerialNo} được bảo hành đến {serial.WarrantyEnd:dd/MM/yyyy}.";
            return RedirectToAction(nameof(DangKySanPham));
        }


        // ── Tra cứu bảo hành THẬT — query trực tiếp bảng SerialNumbers ──
        [HttpPost]
        public async Task<IActionResult> CheckWarranty(string? serial, string? orderCode)
        {
            serial = serial?.Trim().ToUpper();
            orderCode = orderCode?.Trim().TrimStart('#');

            if (string.IsNullOrEmpty(serial) && string.IsNullOrEmpty(orderCode))
                return Json(new { found = false, message = "Vui lòng nhập số serial hoặc mã đơn hàng." });

            IQueryable<AsusLaptop.Models.SerialNumber> query = _context.SerialNumbers
                .Include(s => s.Product)
                .Include(s => s.Variant)
                .Include(s => s.OrderDetail).ThenInclude(od => od!.Order);

            AsusLaptop.Models.SerialNumber? found = null;

            if (!string.IsNullOrEmpty(serial))
            {
                found = await query.FirstOrDefaultAsync(s => s.SerialNo == serial);
            }
            else if (int.TryParse(orderCode, out int orderId))
            {
                found = await query
                    .Where(s => s.OrderDetail != null && s.OrderDetail.OrderId == orderId)
                    .FirstOrDefaultAsync();
            }

            if (found == null)
                return Json(new { found = false, message = "Không tìm thấy thông tin bảo hành khớp với dữ liệu bạn nhập. Vui lòng kiểm tra lại số serial (in trên đáy máy) hoặc mã đơn hàng." });

            var order = found.OrderDetail?.Order;
            bool isActivated = found.WarrantyEnd.HasValue;
            bool isExpired = isActivated && found.WarrantyEnd!.Value < DateTime.Now;

            return Json(new
            {
                found = true,
                serialNo = found.SerialNo,
                productName = found.Product?.Name,
                variantInfo = found.Variant != null ? $"{found.Variant.RAM} / {found.Variant.Color}" : null,
                status = found.Status,
                statusVi = found.StatusVi,
                orderId = order?.Id,
                orderDate = order?.OrderDate,
                isActivated,
                isExpired,
                warrantyEnd = found.WarrantyEnd,
                daysLeft = isActivated && !isExpired ? (int?)(found.WarrantyEnd!.Value - DateTime.Now).TotalDays : null
            });
        }

        // ── Trung tâm Chăm Sóc Sau Mua & Bảo Hành (Warranty & Post-Purchase Care Hub) ──
        public async Task<IActionResult> ChamsocBaoHanh()
        {
            var vm = new ChamsocBaoHanhViewModel();
            try
            {
                vm.IsAuthenticated = User.Identity?.IsAuthenticated == true;

                int? userId = null;
                if (vm.IsAuthenticated && int.TryParse(User.FindFirst("UserId")?.Value, out var uid))
                {
                    userId = uid;
                    try
                    {
                        var user = await _context.Users.FindAsync(userId);
                        if (user != null)
                        {
                            vm.CustomerName = user.FullName;
                            vm.CustomerPhone = user.Phone;
                            vm.CustomerEmail = user.Email;
                        }
                    }
                    catch { }
                }

                var devicesDict = new Dictionary<string, UserDeviceViewModel>(StringComparer.OrdinalIgnoreCase);

                if (userId.HasValue)
                {
                    // 1. Lấy danh sách máy từ các Đơn hàng đã Hoàn tất (Completed)
                    try
                    {
                        var orderSerials = await _context.SerialNumbers
                            .Include(s => s.Product)
                            .Include(s => s.Variant)
                            .Include(s => s.OrderDetail).ThenInclude(od => od!.Order)
                            .Where(s => s.OrderDetail != null && s.OrderDetail.Order!.UserId == userId.Value && s.OrderDetail.Order.Status == "Completed")
                            .ToListAsync();

                        foreach (var sn in orderSerials)
                        {
                            var pDate = sn.OrderDetail?.Order?.OrderDate ?? sn.CreatedAt;
                            var d = BuildUserDeviceViewModel(sn.SerialNo, sn.Product?.Name ?? "ASUS Laptop", sn.Product?.ImageUrl, sn.Product?.Series,
                                sn.Variant != null ? $"{sn.Variant.RAM} / {sn.Variant.Color}" : null, pDate, sn.WarrantyEnd, "Đơn hàng Web");

                            devicesDict[sn.SerialNo] = d;
                        }
                    }
                    catch { }

                    // 2. Lấy danh sách máy từ Đăng ký sản phẩm (ProductRegistrations)
                    try
                    {
                        var regList = await _context.ProductRegistrations
                            .Include(r => r.Product)
                            .Where(r => r.UserId == userId.Value)
                            .ToListAsync();

                        foreach (var reg in regList)
                        {
                            if (!devicesDict.ContainsKey(reg.SerialNo))
                            {
                                var sn = await _context.SerialNumbers
                                    .Include(s => s.Variant)
                                    .FirstOrDefaultAsync(s => s.SerialNo == reg.SerialNo);

                                DateTime? wEnd = sn?.WarrantyEnd ?? reg.PurchaseDate.AddMonths(24);

                                var d = BuildUserDeviceViewModel(reg.SerialNo, reg.Product?.Name ?? "ASUS Laptop", reg.Product?.ImageUrl, reg.Product?.Series,
                                    sn?.Variant != null ? $"{sn.Variant.RAM} / {sn.Variant.Color}" : null, reg.PurchaseDate, wEnd, "Đã đăng ký Serial");

                                devicesDict[reg.SerialNo] = d;
                            }
                        }
                    }
                    catch { }

                    // 3. Lấy lịch sử đặt hẹn bảo trì của User
                    try
                    {
                        var phone = vm.CustomerPhone;
                        IQueryable<MaintenanceBooking> bookingQuery = _context.MaintenanceBookings;
                        if (!string.IsNullOrEmpty(phone))
                        {
                            bookingQuery = bookingQuery.Where(b => b.UserId == userId.Value || b.Phone == phone);
                        }
                        else
                        {
                            bookingQuery = bookingQuery.Where(b => b.UserId == userId.Value);
                        }

                        vm.Bookings = await bookingQuery.OrderByDescending(b => b.CreatedAt).ToListAsync();
                    }
                    catch
                    {
                        vm.Bookings = new List<MaintenanceBooking>();
                    }
                }

                vm.Devices = devicesDict.Values.OrderByDescending(d => d.PurchaseDate).ToList();
            }
            catch
            {
                // Soft fallback so HTTP 500 will never occur
            }

            return View(vm);
        }

        private static UserDeviceViewModel BuildUserDeviceViewModel(
            string serialNo, string productName, string? imageUrl, string? series, string? variantInfo,
            DateTime purchaseDate, DateTime? warrantyEnd, string source)
        {
            var now = DateTime.Now;
            bool isActivated = warrantyEnd.HasValue;
            bool isExpired = isActivated && warrantyEnd!.Value < now;
            int daysLeft = isActivated && !isExpired ? (int)(warrantyEnd!.Value - now).TotalDays : 0;

            int totalWarrantyDays = 730; // 24 tháng
            int elapsedDays = (int)(now - purchaseDate).TotalDays;
            int progress = Math.Clamp((int)((double)elapsedDays / totalWarrantyDays * 100), 0, 100);

            int monthsSincePurchase = (int)(elapsedDays / 30.4);

            int thermalScore;
            string thermalStatus;
            string thermalBadge;
            string recommendation;

            if (monthsSincePurchase < 6)
            {
                thermalScore = 100;
                thermalStatus = "Tản nhiệt hoạt động hoàn hảo";
                thermalBadge = "bg-success text-white";
                recommendation = "Máy của bạn hoạt động rất mát mẻ. Khuyến nghị kiểm tra lau quạt nhẹ sau 6 tháng.";
            }
            else if (monthsSincePurchase < 12)
            {
                thermalScore = 80;
                thermalStatus = "Khuyến nghị vệ sinh quạt định kỳ";
                thermalBadge = "bg-info text-dark";
                recommendation = "Bụi mịn có thể đã bám vào quạt tản nhiệt. Bạn nên đặt lịch vệ sinh tản nhiệt miễn phí tại Store.";
            }
            else if (monthsSincePurchase < 24)
            {
                thermalScore = 65;
                thermalStatus = "Cần bảo dưỡng & thay keo tản nhiệt";
                thermalBadge = "bg-warning text-dark";
                recommendation = "Keo tản nhiệt nguyên bản bắt đầu khô giảm hiệu năng. Khuyên dùng dịch vụ tra keo tản nhiệt Graphene / Liquid Metal.";
            }
            else
            {
                thermalScore = 45;
                thermalStatus = "Cần vệ sinh & thay keo tản nhiệt ngay";
                thermalBadge = "bg-danger text-white";
                recommendation = "Máy đã dùng trên 2 năm. Hãy bảo dưỡng tổng thể để tránh hiện tượng Thermal Throttling giật lag khi chơi game/đồ họa.";
            }

            return new UserDeviceViewModel
            {
                SerialNo = serialNo,
                ProductName = productName,
                ImageUrl = imageUrl,
                Series = series,
                VariantInfo = variantInfo,
                PurchaseDate = purchaseDate,
                WarrantyEnd = warrantyEnd,
                IsActivated = isActivated,
                IsExpired = isExpired,
                DaysLeft = daysLeft,
                WarrantyProgressPercent = progress,
                MonthsSincePurchase = monthsSincePurchase,
                ThermalHealthScore = thermalScore,
                ThermalHealthStatusText = thermalStatus,
                ThermalHealthBadgeClass = thermalBadge,
                MaintenanceRecommendation = recommendation,
                RegistrationSource = source
            };
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateMaintenanceBooking(
            string serialNo, string productName, string serviceType, string serviceMethod,
            DateTime preferredDate, string preferredTime, string customerName, string phone, string? address, string? note)
        {
            if (string.IsNullOrWhiteSpace(serialNo) || string.IsNullOrWhiteSpace(customerName) || string.IsNullOrWhiteSpace(phone))
            {
                TempData["ErrorMessage"] = "Vui lòng điền đầy đủ Họ tên, Số điện thoại và Số Serial thiết bị.";
                return RedirectToAction(nameof(ChamsocBaoHanh));
            }

            if (preferredDate.Date < DateTime.Today)
            {
                TempData["ErrorMessage"] = "Ngày hẹn bảo dưỡng không được chọn trong quá khứ.";
                return RedirectToAction(nameof(ChamsocBaoHanh));
            }

            int? userId = null;
            if (User.Identity?.IsAuthenticated == true && int.TryParse(User.FindFirst("UserId")?.Value, out var uid))
                userId = uid;

            var booking = new MaintenanceBooking
            {
                UserId = userId,
                SerialNo = serialNo.Trim().ToUpper(),
                ProductName = string.IsNullOrWhiteSpace(productName) ? "ASUS Laptop" : productName.Trim(),
                ServiceType = string.IsNullOrWhiteSpace(serviceType) ? "Vệ sinh tản nhiệt định kỳ (Miễn phí)" : serviceType,
                ServiceMethod = string.IsNullOrWhiteSpace(serviceMethod) ? "Mang tới Showroom" : serviceMethod,
                PreferredDate = preferredDate,
                PreferredTime = string.IsNullOrWhiteSpace(preferredTime) ? "09:00 - 11:30" : preferredTime,
                CustomerName = customerName.Trim(),
                Phone = phone.Trim(),
                Address = address?.Trim(),
                Note = note?.Trim(),
                Status = "Pending",
                CreatedAt = DateTime.Now
            };

            _context.MaintenanceBookings.Add(booking);

            if (userId.HasValue)
            {
                _context.Notifications.Add(new Notification
                {
                    UserId = userId.Value,
                    Title = "Đặt lịch bảo dưỡng thành công",
                    Message = $"Lịch hẹn {booking.ServiceType} cho máy {booking.SerialNo} vào ngày {preferredDate:dd/MM/yyyy} ({booking.PreferredTime}) đã được ghi nhận.",
                    Type = "System",
                    ActionUrl = "/Support/ChamsocBaoHanh",
                    IsRead = false,
                    CreatedAt = DateTime.Now
                });
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Đặt lịch bảo dưỡng cho Serial {booking.SerialNo} thành công! Kỹ thuật viên ASUS sẽ liên hệ xác nhận qua SĐT {booking.Phone}.";
            return RedirectToAction(nameof(ChamsocBaoHanh));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelMaintenanceBooking(int id)
        {
            var booking = await _context.MaintenanceBookings.FindAsync(id);
            if (booking == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy thông tin lịch hẹn.";
                return RedirectToAction(nameof(ChamsocBaoHanh));
            }

            int? userId = null;
            if (User.Identity?.IsAuthenticated == true && int.TryParse(User.FindFirst("UserId")?.Value, out var uid))
                userId = uid;

            if (booking.UserId != userId && !User.IsInRole("Admin"))
            {
                TempData["ErrorMessage"] = "Bạn không có quyền thực hiện thao tác này.";
                return RedirectToAction(nameof(ChamsocBaoHanh));
            }

            if (booking.Status != "Pending")
            {
                TempData["ErrorMessage"] = "Chỉ có thể hủy lịch hẹn ở trạng thái 'Chờ xác nhận'.";
                return RedirectToAction(nameof(ChamsocBaoHanh));
            }

            booking.Status = "Cancelled";
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Hủy lịch hẹn bảo dưỡng thành công.";
            return RedirectToAction(nameof(ChamsocBaoHanh));
        }
    }
}
