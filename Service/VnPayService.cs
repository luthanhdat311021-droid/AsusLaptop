using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace AsusLaptop.Services
{
    public class VnPayService
    {
        private readonly IConfiguration _config;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public VnPayService(IConfiguration config, IHttpContextAccessor httpContextAccessor)
        {
            _config = config;
            _httpContextAccessor = httpContextAccessor;
        }

        // Tạo URL thanh toán VNPay
        public string CreatePaymentUrl(int orderId, decimal amount, string orderInfo)
        {
            var vnpUrl      = _config["VNPay:Url"]!;
            var tmnCode     = _config["VNPay:TmnCode"]!;
            var hashSecret  = _config["VNPay:HashSecret"]!;
            var returnUrl   = _config["VNPay:ReturnUrl"]!;

            // ── Luôn tính giờ theo múi giờ Việt Nam, bất kể server đặt giờ hệ thống gì ──
            // (Server hosting có thể chạy giờ Mỹ/UTC khác, nhưng VNPay xử lý theo giờ VN,
            //  lệch múi giờ sẽ khiến giao dịch bị coi là hết hạn ngay lập tức)
            var vietnamTz = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
            var now = TimeZoneInfo.ConvertTime(DateTime.UtcNow, vietnamTz);
            var txnRef  = $"{orderId}_{now:yyyyMMddHHmmss}";
            var amountVnd = (long)(amount * 100); // VNPay nhận đơn vị VND * 100

            var request = _httpContextAccessor.HttpContext!.Request;
            var ipAddr  = request.Headers["X-Forwarded-For"].FirstOrDefault()
                          ?? request.HttpContext.Connection.RemoteIpAddress?.ToString()
                          ?? "127.0.0.1";

            // Tập hợp tham số (phải sắp xếp theo alphabet)
            var data = new SortedDictionary<string, string>
            {
                { "vnp_Version",    "2.1.0" },
                { "vnp_Command",    "pay" },
                { "vnp_TmnCode",    tmnCode },
                { "vnp_Amount",     amountVnd.ToString() },
                { "vnp_CurrCode",   "VND" },
                { "vnp_TxnRef",     txnRef },
                { "vnp_OrderInfo",  orderInfo },
                { "vnp_OrderType",  "other" },
                { "vnp_Locale",     "vn" },
                { "vnp_ReturnUrl",  returnUrl },
                { "vnp_IpAddr",     ipAddr },
                { "vnp_CreateDate", now.ToString("yyyyMMddHHmmss") },
                { "vnp_ExpireDate", now.AddMinutes(15).ToString("yyyyMMddHHmmss") },
            };

            // Build query string và tạo chữ ký
            var queryBuilder = new StringBuilder();
            foreach (var kv in data)
            {
                queryBuilder.Append(WebUtility.UrlEncode(kv.Key));
                queryBuilder.Append('=');
                queryBuilder.Append(WebUtility.UrlEncode(kv.Value));
                queryBuilder.Append('&');
            }

            // Chuỗi cần ký (bỏ dấu & cuối)
            var rawHash = queryBuilder.ToString().TrimEnd('&');
            var secureHash = HmacSha512(hashSecret, rawHash);

            return $"{vnpUrl}?{rawHash}&vnp_SecureHash={secureHash}";
        }

        // Kiểm tra chữ ký VNPay callback
        public bool ValidateSignature(IQueryCollection query, out string txnRef, out string responseCode)
        {
            txnRef       = query["vnp_TxnRef"].ToString();
            responseCode = query["vnp_ResponseCode"].ToString();

            var hashSecret   = _config["VNPay:HashSecret"]!;
            var vnpSecure    = query["vnp_SecureHash"].ToString();

            // Lấy tất cả param trừ vnp_SecureHash, sort và build lại
            var data = new SortedDictionary<string, string>();
            foreach (var key in query.Keys)
            {
                if (!key.Equals("vnp_SecureHash", StringComparison.OrdinalIgnoreCase) &&
                    !key.Equals("vnp_SecureHashType", StringComparison.OrdinalIgnoreCase))
                {
                    data[key] = query[key].ToString();
                }
            }

            var rawHash = string.Join("&", data.Select(kv =>
                $"{WebUtility.UrlEncode(kv.Key)}={WebUtility.UrlEncode(kv.Value)}"));

            var checkHash = HmacSha512(hashSecret, rawHash);
            return checkHash.Equals(vnpSecure, StringComparison.OrdinalIgnoreCase);
        }

        private static string HmacSha512(string key, string data)
        {
            var keyBytes  = Encoding.UTF8.GetBytes(key);
            var dataBytes = Encoding.UTF8.GetBytes(data);
            using var hmac = new HMACSHA512(keyBytes);
            var hash = hmac.ComputeHash(dataBytes);
            return Convert.ToHexString(hash).ToLower();
        }
    }
}
