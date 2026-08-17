using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using AsusLaptop.Models;

namespace AsusLaptop.Services
{
    public class EmailService
    {
        private readonly IConfiguration _config;

        public EmailService(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendOtpEmailAsync(string toEmail, string toName, string otp)
        {
            var smtpHost = _config["Email:SmtpHost"] ?? "smtp.gmail.com";
            var smtpPort = int.Parse(_config["Email:SmtpPort"] ?? "587");
            var senderEmail = _config["Email:SenderEmail"] ?? "";
            var senderName = _config["Email:SenderName"] ?? "ASUS Laptop Store";
            var appPassword = _config["Email:AppPassword"] ?? "";

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(senderName, senderEmail));
            message.To.Add(new MailboxAddress(toName, toEmail));
            message.Subject = "Mã OTP đặt lại mật khẩu - ASUS Laptop Store";

            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = $@"
<!DOCTYPE html>
<html>
<head><meta charset='utf-8'></head>
<body style='margin:0;padding:0;background:#0f0f0f;font-family:Arial,sans-serif'>
  <div style='max-width:480px;margin:40px auto;background:#1a1a2e;border-radius:16px;overflow:hidden;border:1px solid rgba(0,212,255,0.2)'>
    
    <!-- Header -->
    <div style='background:linear-gradient(135deg,#0f3460,#16213e);padding:32px 36px;text-align:center'>
      <div style='font-size:28px;font-weight:900;letter-spacing:3px;color:#00d4ff'>ASUS</div>
      <div style='color:rgba(255,255,255,0.6);font-size:13px;margin-top:4px'>Laptop Store</div>
    </div>

    <!-- Body -->
    <div style='padding:36px'>
      <h2 style='color:#ffffff;font-size:20px;margin:0 0 8px'>Đặt lại mật khẩu</h2>
      <p style='color:rgba(255,255,255,0.6);font-size:14px;margin:0 0 28px'>
        Xin chào <strong style='color:#ffffff'>{toName}</strong>, chúng tôi nhận được yêu cầu đặt lại mật khẩu từ tài khoản của bạn.
      </p>

      <p style='color:rgba(255,255,255,0.6);font-size:13px;margin:0 0 12px'>Mã OTP của bạn là:</p>

      <!-- OTP Box -->
      <div style='background:#0f3460;border:2px solid #00d4ff;border-radius:12px;padding:20px;text-align:center;margin-bottom:24px'>
        <div style='font-size:40px;font-weight:900;letter-spacing:12px;color:#00d4ff;font-family:monospace'>{otp}</div>
      </div>

      <div style='background:rgba(255,193,7,0.1);border:1px solid rgba(255,193,7,0.3);border-radius:8px;padding:14px;margin-bottom:24px'>
        <p style='color:#ffc107;font-size:13px;margin:0'>
          ⏱ Mã OTP có hiệu lực trong <strong>10 phút</strong>. Không chia sẻ mã này với bất kỳ ai.
        </p>
      </div>

      <p style='color:rgba(255,255,255,0.4);font-size:12px;margin:0'>
        Nếu bạn không yêu cầu đặt lại mật khẩu, vui lòng bỏ qua email này. Tài khoản của bạn vẫn an toàn.
      </p>
    </div>

    <!-- Footer -->
    <div style='background:#0f0f1a;padding:20px 36px;text-align:center;border-top:1px solid rgba(255,255,255,0.05)'>
      <p style='color:rgba(255,255,255,0.3);font-size:11px;margin:0'>© {DateTime.Now.Year} ASUS Laptop Store. All rights reserved.</p>
    </div>
  </div>
</body>
</html>"
            };

            message.Body = bodyBuilder.ToMessageBody();

            using var smtp = new SmtpClient();
            await smtp.ConnectAsync(smtpHost, smtpPort, SecureSocketOptions.StartTls);
            await smtp.AuthenticateAsync(senderEmail, appPassword);
            await smtp.SendAsync(message);
            await smtp.DisconnectAsync(true);
        }
        public async Task SendOrderConfirmationEmailAsync(Order order, List<OrderDetail> details)
        {
            if (string.IsNullOrWhiteSpace(order.Email)) return;

            var smtpHost    = _config["Email:SmtpHost"]    ?? "smtp.gmail.com";
            var smtpPort    = int.Parse(_config["Email:SmtpPort"] ?? "587");
            var senderEmail = _config["Email:SenderEmail"] ?? "";
            var senderName  = _config["Email:SenderName"]  ?? "ASUS Laptop Store";
            var appPassword = _config["Email:AppPassword"] ?? "";

            // Build product rows
            var rows = new System.Text.StringBuilder();
            foreach (var d in details)
            {
                var name    = d.Product?.Name ?? "Sản phẩm";
                var variant = d.Variant != null ? $" ({d.Variant.RAM} · {d.Variant.Color})" : "";
                var price   = d.Price.ToString("N0");
                var qty     = d.Quantity;
                var sub     = (d.Price * d.Quantity).ToString("N0");
                rows.Append($@"
                <tr>
                  <td style='padding:10px 8px;border-bottom:1px solid #1e2a3a;color:#cbd5e1;font-size:13px'>{name}{variant}</td>
                  <td style='padding:10px 8px;border-bottom:1px solid #1e2a3a;color:#94a3b8;font-size:13px;text-align:center'>{qty}</td>
                  <td style='padding:10px 8px;border-bottom:1px solid #1e2a3a;color:#94a3b8;font-size:13px;text-align:right'>{price}₫</td>
                  <td style='padding:10px 8px;border-bottom:1px solid #1e2a3a;color:#00d4ff;font-size:13px;font-weight:700;text-align:right'>{sub}₫</td>
                </tr>");
            }

            var paymentLabel = order.PaymentMethod switch
            {
                "VNPay"        => "VNPay (ATM/Visa/QR)",
                "BankTransfer" => "Chuyển khoản ngân hàng",
                _              => "Thanh toán khi nhận hàng (COD)"
            };

            var paymentIcon = order.PaymentMethod switch
            {
                "VNPay"        => "💳",
                "BankTransfer" => "🏦",
                _              => "💵"
            };

            var statusLabel = order.PaymentMethod == "VNPay" ? "Đã thanh toán qua VNPay" : "Chờ xác nhận";
            var statusColor = order.PaymentMethod == "VNPay" ? "#22c55e" : "#f59e0b";

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(senderName, senderEmail));
            message.To.Add(new MailboxAddress(order.CustomerName, order.Email));
            message.Subject = $"✅ Xác nhận đơn hàng #{order.Id} - ASUS Laptop Store";

            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = $@"
<!DOCTYPE html>
<html>
<head><meta charset='utf-8'><meta name='viewport' content='width=device-width,initial-scale=1'></head>
<body style='margin:0;padding:0;background:#0a0f1e;font-family:Arial,sans-serif'>
  <div style='max-width:560px;margin:32px auto;background:#111827;border-radius:16px;overflow:hidden;border:1px solid rgba(0,212,255,0.15)'>

    <!-- Header -->
    <div style='background:linear-gradient(135deg,#0f3460 0%,#16213e 100%);padding:28px 36px;text-align:center;border-bottom:2px solid rgba(0,212,255,0.2)'>
      <div style='font-size:26px;font-weight:900;letter-spacing:4px;color:#00d4ff'>ASUS</div>
      <div style='color:rgba(255,255,255,0.5);font-size:12px;letter-spacing:1px;margin-top:2px'>LAPTOP STORE</div>
    </div>

    <!-- Success banner -->
    <div style='background:rgba(34,197,94,0.08);border-bottom:1px solid rgba(34,197,94,0.2);padding:20px 36px;text-align:center'>
      <div style='font-size:36px;margin-bottom:6px'>🎉</div>
      <div style='color:#22c55e;font-size:18px;font-weight:700'>Đặt hàng thành công!</div>
      <div style='color:rgba(255,255,255,0.5);font-size:13px;margin-top:4px'>Cảm ơn bạn đã mua hàng tại ASUS Laptop Store</div>
    </div>

    <!-- Body -->
    <div style='padding:28px 36px'>

      <!-- Order ID -->
      <div style='background:#0f172a;border:1px solid rgba(0,212,255,0.2);border-radius:10px;padding:16px 20px;margin-bottom:20px;display:flex;justify-content:space-between;align-items:center'>
        <div>
          <div style='color:rgba(255,255,255,0.4);font-size:11px;text-transform:uppercase;letter-spacing:1px'>Mã đơn hàng</div>
          <div style='color:#00d4ff;font-size:22px;font-weight:900;margin-top:2px'>#{order.Id}</div>
        </div>
        <div style='text-align:right'>
          <div style='color:rgba(255,255,255,0.4);font-size:11px;text-transform:uppercase;letter-spacing:1px'>Ngày đặt</div>
          <div style='color:#cbd5e1;font-size:13px;margin-top:2px'>{order.OrderDate:dd/MM/yyyy HH:mm}</div>
        </div>
      </div>

      <!-- Customer info -->
      <div style='margin-bottom:20px'>
        <div style='color:rgba(255,255,255,0.4);font-size:11px;text-transform:uppercase;letter-spacing:1px;margin-bottom:10px'>Thông tin giao hàng</div>
        <table style='width:100%;border-collapse:collapse'>
          <tr>
            <td style='padding:5px 0;color:rgba(255,255,255,0.4);font-size:12px;width:40%'>👤 Họ tên</td>
            <td style='padding:5px 0;color:#cbd5e1;font-size:13px;font-weight:600'>{order.CustomerName}</td>
          </tr>
          <tr>
            <td style='padding:5px 0;color:rgba(255,255,255,0.4);font-size:12px'>📞 Điện thoại</td>
            <td style='padding:5px 0;color:#cbd5e1;font-size:13px'>{order.Phone}</td>
          </tr>
          <tr>
            <td style='padding:5px 0;color:rgba(255,255,255,0.4);font-size:12px'>📍 Địa chỉ</td>
            <td style='padding:5px 0;color:#cbd5e1;font-size:13px'>{order.Address}</td>
          </tr>
          {(string.IsNullOrEmpty(order.Note) ? "" : $@"
          <tr>
            <td style='padding:5px 0;color:rgba(255,255,255,0.4);font-size:12px'>📝 Ghi chú</td>
            <td style='padding:5px 0;color:#94a3b8;font-size:13px;font-style:italic'>{order.Note}</td>
          </tr>")}
        </table>
      </div>

      <!-- Products table -->
      <div style='margin-bottom:20px'>
        <div style='color:rgba(255,255,255,0.4);font-size:11px;text-transform:uppercase;letter-spacing:1px;margin-bottom:10px'>Sản phẩm đặt mua</div>
        <table style='width:100%;border-collapse:collapse;background:#0f172a;border-radius:10px;overflow:hidden'>
          <thead>
            <tr style='background:rgba(0,212,255,0.08)'>
              <th style='padding:10px 8px;color:rgba(255,255,255,0.4);font-size:11px;text-align:left;font-weight:600'>SẢN PHẨM</th>
              <th style='padding:10px 8px;color:rgba(255,255,255,0.4);font-size:11px;text-align:center;font-weight:600'>SL</th>
              <th style='padding:10px 8px;color:rgba(255,255,255,0.4);font-size:11px;text-align:right;font-weight:600'>ĐƠN GIÁ</th>
              <th style='padding:10px 8px;color:rgba(255,255,255,0.4);font-size:11px;text-align:right;font-weight:600'>THÀNH TIỀN</th>
            </tr>
          </thead>
          <tbody>{rows}</tbody>
        </table>
      </div>

      <!-- Total -->
      <div style='background:#0f172a;border:1px solid rgba(0,212,255,0.15);border-radius:10px;padding:16px 20px;margin-bottom:20px'>
        <div style='display:flex;justify-content:space-between;align-items:center;margin-bottom:8px'>
          <span style='color:rgba(255,255,255,0.5);font-size:13px'>Phí vận chuyển</span>
          <span style='color:#22c55e;font-size:13px;font-weight:600'>Miễn phí</span>
        </div>
        <div style='border-top:1px solid rgba(255,255,255,0.06);padding-top:10px;display:flex;justify-content:space-between;align-items:center'>
          <span style='color:#ffffff;font-size:15px;font-weight:700'>Tổng cộng</span>
          <span style='color:#00d4ff;font-size:22px;font-weight:900'>{order.TotalAmount.ToString("N0")}₫</span>
        </div>
      </div>

      <!-- Payment -->
      <div style='background:rgba(0,212,255,0.04);border:1px solid rgba(0,212,255,0.1);border-radius:10px;padding:14px 20px;margin-bottom:20px'>
        <div style='display:flex;justify-content:space-between;align-items:center'>
          <div>
            <div style='color:rgba(255,255,255,0.4);font-size:11px;text-transform:uppercase;letter-spacing:1px'>Phương thức thanh toán</div>
            <div style='color:#cbd5e1;font-size:13px;margin-top:4px'>{paymentIcon} {paymentLabel}</div>
          </div>
          <div style='background:rgba(0,0,0,0.3);border-radius:6px;padding:4px 10px'>
            <span style='color:{statusColor};font-size:12px;font-weight:700'>{statusLabel}</span>
          </div>
        </div>
      </div>

      <!-- Note -->
      <div style='background:rgba(245,158,11,0.06);border:1px solid rgba(245,158,11,0.2);border-radius:8px;padding:12px 16px;font-size:12px;color:#fbbf24'>
        <strong>📦 Lưu ý:</strong> Đơn hàng sẽ được xử lý trong vòng 24 giờ làm việc. Nhân viên sẽ liên hệ xác nhận qua số điện thoại {order.Phone}.
      </div>
    </div>

    <!-- Footer -->
    <div style='background:#0a0f1e;padding:20px 36px;text-align:center;border-top:1px solid rgba(255,255,255,0.05)'>
      <div style='color:rgba(255,255,255,0.3);font-size:11px'>© {DateTime.Now.Year} ASUS Laptop Store · Hotline: 1800-xxxx</div>
      <div style='color:rgba(255,255,255,0.2);font-size:10px;margin-top:4px'>Email này được gửi tự động, vui lòng không trả lời.</div>
    </div>
  </div>
</body>
</html>"
            };

            message.Body = bodyBuilder.ToMessageBody();

            using var smtp = new SmtpClient();
            await smtp.ConnectAsync(smtpHost, smtpPort, SecureSocketOptions.StartTls);
            await smtp.AuthenticateAsync(senderEmail, appPassword);
            await smtp.SendAsync(message);
            await smtp.DisconnectAsync(true);
        }
        public async Task SendShippingEmailAsync(Order order, List<OrderDetail> details)
        {
            if (string.IsNullOrWhiteSpace(order.Email)) return;

            var smtpHost    = _config["Email:SmtpHost"]    ?? "smtp.gmail.com";
            var smtpPort    = int.Parse(_config["Email:SmtpPort"] ?? "587");
            var senderEmail = _config["Email:SenderEmail"] ?? "";
            var senderName  = _config["Email:SenderName"]  ?? "ASUS Laptop Store";
            var appPassword = _config["Email:AppPassword"] ?? "";

            // Build item list (compact)
            var items = new System.Text.StringBuilder();
            foreach (var d in details)
            {
                var name    = d.Product?.Name ?? "Sản phẩm";
                var variant = d.Variant != null ? $" ({d.Variant.RAM} · {d.Variant.Color})" : "";
                items.Append($@"
                <tr>
                  <td style='padding:8px 8px;border-bottom:1px solid #1e2a3a;color:#cbd5e1;font-size:13px'>{name}{variant}</td>
                  <td style='padding:8px 8px;border-bottom:1px solid #1e2a3a;color:#94a3b8;font-size:13px;text-align:center'>x{d.Quantity}</td>
                </tr>");
            }

            var estArrival = DateTime.Now.AddDays(3).ToString("dd/MM/yyyy");

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(senderName, senderEmail));
            message.To.Add(new MailboxAddress(order.CustomerName, order.Email));
            message.Subject = $"🚚 Đơn hàng #{order.Id} đang được giao - ASUS Laptop Store";

            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = $@"
<!DOCTYPE html>
<html>
<head><meta charset='utf-8'><meta name='viewport' content='width=device-width,initial-scale=1'></head>
<body style='margin:0;padding:0;background:#0a0f1e;font-family:Arial,sans-serif'>
  <div style='max-width:560px;margin:32px auto;background:#111827;border-radius:16px;overflow:hidden;border:1px solid rgba(255,145,0,0.2)'>

    <!-- Header -->
    <div style='background:linear-gradient(135deg,#0f3460 0%,#16213e 100%);padding:28px 36px;text-align:center;border-bottom:2px solid rgba(255,145,0,0.25)'>
      <div style='font-size:26px;font-weight:900;letter-spacing:4px;color:#00d4ff'>ASUS</div>
      <div style='color:rgba(255,255,255,0.5);font-size:12px;letter-spacing:1px;margin-top:2px'>LAPTOP STORE</div>
    </div>

    <!-- Shipping banner -->
    <div style='background:rgba(255,145,0,0.08);border-bottom:1px solid rgba(255,145,0,0.25);padding:20px 36px;text-align:center'>
      <div style='font-size:36px;margin-bottom:6px'>🚚</div>
      <div style='color:#ff9100;font-size:18px;font-weight:700'>Đơn hàng của bạn đang được giao!</div>
      <div style='color:rgba(255,255,255,0.5);font-size:13px;margin-top:4px'>Đơn vị vận chuyển đã tiếp nhận và đang trên đường giao đến bạn</div>
    </div>

    <!-- Body -->
    <div style='padding:28px 36px'>

      <!-- Order ID + ETA -->
      <div style='background:#0f172a;border:1px solid rgba(255,145,0,0.2);border-radius:10px;padding:16px 20px;margin-bottom:20px;display:flex;justify-content:space-between;align-items:center'>
        <div>
          <div style='color:rgba(255,255,255,0.4);font-size:11px;text-transform:uppercase;letter-spacing:1px'>Mã đơn hàng</div>
          <div style='color:#00d4ff;font-size:22px;font-weight:900;margin-top:2px'>#{order.Id}</div>
        </div>
        <div style='text-align:right'>
          <div style='color:rgba(255,255,255,0.4);font-size:11px;text-transform:uppercase;letter-spacing:1px'>Dự kiến giao</div>
          <div style='color:#ff9100;font-size:14px;font-weight:700;margin-top:2px'>{estArrival}</div>
        </div>
      </div>

      <!-- Progress -->
      <div style='margin-bottom:24px'>
        <table style='width:100%;border-collapse:collapse'>
          <tr>
            <td style='width:33%;text-align:center'>
              <div style='width:28px;height:28px;border-radius:50%;background:#22c55e;color:#fff;font-size:14px;line-height:28px;margin:0 auto 6px'>✓</div>
              <div style='color:#22c55e;font-size:11px;font-weight:700'>Đã xác nhận</div>
            </td>
            <td style='width:33%;text-align:center'>
              <div style='width:28px;height:28px;border-radius:50%;background:#ff9100;color:#fff;font-size:14px;line-height:28px;margin:0 auto 6px'>🚚</div>
              <div style='color:#ff9100;font-size:11px;font-weight:700'>Đang giao</div>
            </td>
            <td style='width:33%;text-align:center'>
              <div style='width:28px;height:28px;border-radius:50%;background:#1e2a3a;color:#64748b;font-size:14px;line-height:28px;margin:0 auto 6px'>●</div>
              <div style='color:#64748b;font-size:11px;font-weight:700'>Đã nhận hàng</div>
            </td>
          </tr>
        </table>
      </div>

      <!-- Delivery info -->
      <div style='margin-bottom:20px'>
        <div style='color:rgba(255,255,255,0.4);font-size:11px;text-transform:uppercase;letter-spacing:1px;margin-bottom:10px'>Giao đến</div>
        <table style='width:100%;border-collapse:collapse'>
          <tr>
            <td style='padding:5px 0;color:rgba(255,255,255,0.4);font-size:12px;width:40%'>👤 Người nhận</td>
            <td style='padding:5px 0;color:#cbd5e1;font-size:13px;font-weight:600'>{order.CustomerName}</td>
          </tr>
          <tr>
            <td style='padding:5px 0;color:rgba(255,255,255,0.4);font-size:12px'>📞 Điện thoại</td>
            <td style='padding:5px 0;color:#cbd5e1;font-size:13px'>{order.Phone}</td>
          </tr>
          <tr>
            <td style='padding:5px 0;color:rgba(255,255,255,0.4);font-size:12px'>📍 Địa chỉ</td>
            <td style='padding:5px 0;color:#cbd5e1;font-size:13px'>{order.Address}</td>
          </tr>
        </table>
      </div>

      <!-- Items -->
      <div style='margin-bottom:20px'>
        <div style='color:rgba(255,255,255,0.4);font-size:11px;text-transform:uppercase;letter-spacing:1px;margin-bottom:10px'>Sản phẩm trong đơn</div>
        <table style='width:100%;border-collapse:collapse;background:#0f172a;border-radius:10px;overflow:hidden'>
          <tbody>{items}</tbody>
        </table>
      </div>

      <!-- Note -->
      <div style='background:rgba(255,145,0,0.06);border:1px solid rgba(255,145,0,0.2);border-radius:8px;padding:12px 16px;font-size:12px;color:#ffb84d'>
        <strong>📦 Lưu ý:</strong> Vui lòng giữ điện thoại {order.Phone} luôn sẵn sàng để shipper liên hệ. Đơn hàng dự kiến đến trong 1–3 ngày tới.
      </div>
    </div>

    <!-- Footer -->
    <div style='background:#0a0f1e;padding:20px 36px;text-align:center;border-top:1px solid rgba(255,255,255,0.05)'>
      <div style='color:rgba(255,255,255,0.3);font-size:11px'>© {DateTime.Now.Year} ASUS Laptop Store · Hotline: 1800-xxxx</div>
      <div style='color:rgba(255,255,255,0.2);font-size:10px;margin-top:4px'>Email này được gửi tự động, vui lòng không trả lời.</div>
    </div>
  </div>
</body>
</html>"
            };

            message.Body = bodyBuilder.ToMessageBody();

            using var smtp = new SmtpClient();
            await smtp.ConnectAsync(smtpHost, smtpPort, SecureSocketOptions.StartTls);
            await smtp.AuthenticateAsync(senderEmail, appPassword);
            await smtp.SendAsync(message);
            await smtp.DisconnectAsync(true);
        }
    }
}
