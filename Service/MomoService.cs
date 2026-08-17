using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace AsusLaptop.Services
{
    /// <summary>
    /// Tích hợp thanh toán ví MoMo (payment gateway API v2 - captureWallet).
    ///
    /// LƯU Ý QUAN TRỌNG: MoMo không cung cấp một API "Mua trước trả sau" tách biệt
    /// cho merchant. "Ví trả sau" là MỘT NGUỒN TIỀN mà khách tự chọn ngay trong app
    /// MoMo ở bước xác nhận thanh toán (nếu tài khoản của khách đã được MoMo duyệt
    /// hạn mức trả sau). Việc tích hợp phía merchant (website này) hoàn toàn giống
    /// thanh toán ví MoMo thông thường — chỉ khác ở chỗ khách hàng nhìn thấy thêm
    /// tuỳ chọn "Ví trả sau" trong màn hình chọn nguồn tiền của MoMo app.
    /// Vì vậy nút thanh toán trên web được đặt tên "Ví MoMo (hỗ trợ Trả sau)"
    /// để phản ánh đúng bản chất, thay vì hứa hẹn một luồng trả góp riêng không có thật.
    /// </summary>
    public class MomoService
    {
        private readonly IConfiguration _config;
        private readonly IHttpClientFactory _httpClientFactory;

        public MomoService(IConfiguration config, IHttpClientFactory httpClientFactory)
        {
            _config = config;
            _httpClientFactory = httpClientFactory;
        }

        /// <summary>Gọi MoMo tạo giao dịch, trả về payUrl để redirect khách sang MoMo.</summary>
        public async Task<(bool success, string payUrl, string message)> CreatePaymentAsync(int orderId, decimal amount, string orderInfo)
        {
            var endpoint    = _config["Momo:Endpoint"]!;
            var partnerCode = _config["Momo:PartnerCode"]!;
            var accessKey   = _config["Momo:AccessKey"]!;
            var secretKey   = _config["Momo:SecretKey"]!;
            var returnUrl   = _config["Momo:ReturnUrl"]!;
            var notifyUrl   = _config["Momo:NotifyUrl"]!;

            string requestId  = $"{orderId}_{DateTimeOffset.Now.ToUnixTimeMilliseconds()}";
            string momoOrderId = requestId;
            long amountLong   = (long)amount;
            string extraData  = Convert.ToBase64String(Encoding.UTF8.GetBytes($"orderId={orderId}"));

            // Chuỗi ký phải đúng thứ tự alphabet theo tài liệu MoMo
            string rawSignature =
                $"accessKey={accessKey}" +
                $"&amount={amountLong}" +
                $"&extraData={extraData}" +
                $"&ipnUrl={notifyUrl}" +
                $"&orderId={momoOrderId}" +
                $"&orderInfo={orderInfo}" +
                $"&partnerCode={partnerCode}" +
                $"&redirectUrl={returnUrl}" +
                $"&requestId={requestId}" +
                $"&requestType=captureWallet";

            string signature = HmacSha256(secretKey, rawSignature);

            var payload = new
            {
                partnerCode,
                partnerName = "ASUS Laptop Store",
                storeId     = "AsusLaptopStore",
                requestId,
                amount      = amountLong,
                orderId     = momoOrderId,
                orderInfo,
                redirectUrl = returnUrl,
                ipnUrl      = notifyUrl,
                lang        = "vi",
                extraData,
                requestType = "captureWallet",
                signature,
                autoCapture = true
            };

            try
            {
                var client = _httpClientFactory.CreateClient();
                var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
                var response = await client.PostAsync(endpoint, content);
                var body = await response.Content.ReadAsStringAsync();

                using var doc = JsonDocument.Parse(body);
                var root = doc.RootElement;

                int resultCode = root.TryGetProperty("resultCode", out var rc) ? rc.GetInt32() : -1;
                if (resultCode == 0 && root.TryGetProperty("payUrl", out var payUrlEl))
                {
                    return (true, payUrlEl.GetString() ?? "", "OK");
                }

                string msg = root.TryGetProperty("message", out var m) ? m.GetString() ?? "Lỗi không xác định" : "Lỗi không xác định";
                return (false, "", $"MoMo từ chối giao dịch: {msg} (resultCode={resultCode})");
            }
            catch (Exception ex)
            {
                return (false, "", $"Không thể kết nối tới MoMo: {ex.Message}");
            }
        }

        /// <summary>Xác thực chữ ký MoMo gửi về từ query string (dùng cho redirectUrl - GET).</summary>
        public bool ValidateSignature(IQueryCollection query, out int orderId, out int resultCode)
        {
            var data = query.Keys.ToDictionary(k => k, k => query[k].ToString());
            return ValidateSignatureCore(data, out orderId, out resultCode);
        }

        /// <summary>Xác thực chữ ký MoMo gửi về từ JSON body (dùng cho ipnUrl - POST server-to-server).</summary>
        public bool ValidateSignature(Dictionary<string, string> data, out int orderId, out int resultCode)
        {
            return ValidateSignatureCore(data, out orderId, out resultCode);
        }

        private bool ValidateSignatureCore(Dictionary<string, string> data, out int orderId, out int resultCode)
        {
            data.TryGetValue("resultCode", out var resultCodeStr);
            resultCode = int.TryParse(resultCodeStr, out var rc) ? rc : -1;
            orderId = 0;

            data.TryGetValue("extraData", out var extraData);
            extraData ??= "";
            if (!string.IsNullOrEmpty(extraData))
            {
                try
                {
                    var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(extraData));
                    var part = decoded.Split('=');
                    if (part.Length == 2) orderId = int.Parse(part[1]);
                }
                catch { /* ignore parse errors, orderId stays 0 */ }
            }

            var accessKey = _config["Momo:AccessKey"]!;
            var secretKey = _config["Momo:SecretKey"]!;

            string Get(string key) => data.TryGetValue(key, out var v) ? v : "";

            string rawSignature =
                $"accessKey={accessKey}" +
                $"&amount={Get("amount")}" +
                $"&extraData={extraData}" +
                $"&message={Get("message")}" +
                $"&orderId={Get("orderId")}" +
                $"&orderInfo={Get("orderInfo")}" +
                $"&orderType={Get("orderType")}" +
                $"&partnerCode={Get("partnerCode")}" +
                $"&payType={Get("payType")}" +
                $"&requestId={Get("requestId")}" +
                $"&responseTime={Get("responseTime")}" +
                $"&resultCode={resultCode}" +
                $"&transId={Get("transId")}";

            string checkSignature = HmacSha256(secretKey, rawSignature);
            string receivedSignature = Get("signature");
            return checkSignature.Equals(receivedSignature, StringComparison.OrdinalIgnoreCase);
        }

        private static string HmacSha256(string key, string data)
        {
            var keyBytes  = Encoding.UTF8.GetBytes(key);
            var dataBytes = Encoding.UTF8.GetBytes(data);
            using var hmac = new HMACSHA256(keyBytes);
            var hashBytes = hmac.ComputeHash(dataBytes);
            var sb = new StringBuilder();
            foreach (var b in hashBytes) sb.Append(b.ToString("x2"));
            return sb.ToString();
        }
    }
}
