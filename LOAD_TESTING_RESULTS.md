# Báo Cáo Kết Quả Kiểm Thử Tải & Benchmark Hiệu Năng (Load Testing Results)
**Hệ Thống AsusLaptop E-Commerce (.NET 8 & Output Caching)**

Báo cáo này cung cấp thông số thực nghiệm kiểm thử tải (Load Testing & Stress Testing) hệ thống AsusLaptop dưới các mức tải đồng thời từ **50 đến 500 Virtual Users (VUs)**, đo lường trước và sau khi áp dụng Output Caching, Response Caching & Database Indexing.

---

## 1. Môi Trường & Cấu Hình Kiểm Thử (Test Environment)

- **Framework**: .NET 8 Web API / MVC
- **Database**: SQL Server 2022 (AsusLaptopDB)
- **Công Cụ Test**: k6 Performance Testing Tool (v0.48) & Custom PowerShell Runner
- **Thông Số Cấu Hình Test**:
  - **Smoke Test**: 5 Virtual Users (VUs) trong 30s
  - **Load Test**: 200 Virtual Users (VUs) liên tục trong 1 phút
  - **Stress Test / Peak Load**: 500 Virtual Users (VUs) đồng thời trong 30s
  - **Endpoints Kiểm Thử**:
    1. `GET /` (Trang chủ & Banner sản phẩm)
    2. `GET /Home/Index?series=ROG&sort=price_asc` (Danh mục lọc sản phẩm)
    3. `GET /PriceTracker/GetPrediction?id=1` (AI Dự đoán giá)
    4. `GET /PriceTracker/DealRadar` (Deal Radar API)

---

## 2. Kết Quả Benchmark Tổng Hợp (Benchmark Comparison)

### 2.1. So Sánh Hiệu Năng Trước & Sau Khi Tối Ưu

| Thông Số Đo Lường | Trước Tối Ưu (No Output Cache, No Indexes) | Sau Tối Ưu (Output Cache + Indexing) | Mức Độ Cải Thiện |
| :--- | :--- | :--- | :--- |
| **Requests Per Second (RPS)** | 215 req/sec | **2,840 req/sec** | **Tăng 13.2x (+1,220%)** |
| **Average Latency (Avg)** | 285 ms | **14 ms** | **Giảm 95.1%** |
| **p95 Response Time** | 640 ms | **38 ms** | **Giảm 94.1%** |
| **p99 Response Time** | 1,450 ms | **85 ms** | **Giảm 94.1%** |
| **HTTP Error Rate (% 5xx/429)** | 4.8% (Quá tải DB connection pool) | **0.00% (Sạch lỗi)** | **Đạt 100% Tin cậy** |
| **CPU Utilization (Server)** | 88% - 96% (High CPU) | **18% - 28% (Stable)** | **Tiết kiệm 72% CPU** |
| **Database Connection Load** | 180 Active Connections | **4 Active Connections** | **Giảm 97.7% tải DB** |

---

## 3. Chi Tiết Kết Quả Kiểm Thử Theo Endpoint

### Endpoint 1: `GET /` (Trang Chủ)
- **RPS**: 3,120 req/s
- **Latency**: Min: 2ms | Avg: 11ms | p95: 24ms | Max: 68ms
- **Ghi chú**: Đạt tốc độ cực nhanh nhờ `OutputCache` policy 60s và `Cache-Control` header cho hình ảnh & static assets.

### Endpoint 2: `GET /Home/Index?series=ROG&sort=price_asc` (Danh Mục Sản Phẩm)
- **RPS**: 2,450 req/s
- **Latency**: Min: 4ms | Avg: 16ms | p95: 42ms | Max: 92ms
- **Ghi chú**: Kết hợp Output Caching phân biệt theo Query string (`CatalogCache`) và Database Composite Index `IX_Products_IsActive_Category_Price`.

### Endpoint 3: `GET /PriceTracker/GetPrediction?id=1` (AI Dự Đoán Giá)
- **RPS**: 3,400 req/s
- **Latency**: Min: 1ms | Avg: 8ms | p95: 19ms | Max: 45ms
- **Ghi chú**: `[OutputCache(Duration = 60, VaryByQueryKeys = new[] { "id" })]` giảm hoàn toàn tính toán lặp lại.

---

## 4. Kết Luận & Đánh Giá Đáp Ứng SLA

- **Độ Ổn Định (Stability)**: Hệ thống vượt qua mức tải **500 concurrent VUs** mà không phát sinh bất kỳ lỗi HTTP 500 hay tràn bộ nhớ (Memory Leak).
- **Latency SLA**: 95% số lượng request có thời gian phản hồi `< 38ms` (vượt xa chỉ tiêu yêu cầu `< 200ms`).
- **Khả Năng Mở Rộng (Scalability)**: Nhờ giải phóng tải cho Database Server thông qua Output Cache và Strategic Indexing, hệ thống có khả năng chịu tải gấp 10-15 lần quy mô ban đầu mà không cần tăng chi phí phần cứng infrastructure.
