import http from 'k6/http';
import { check, sleep } from 'k6';

// ── KỊCH BẢN KIỂM THỬ TẢI HỆ THỐNG ASUSLAPTOP ──────────────────────────────
export const options = {
    stages: [
        { duration: '30s', target: 50 },  // Ramp-up: Tăng từ 0 lên 50 Virtual Users (VUs) trong 30s
        { duration: '1m', target: 200 },  // Load Test: Giữ 200 VUs trong 1 phút
        { duration: '30s', target: 500 }, // Stress Test Peak: Tăng vọt lên 500 VUs trong 30s
        { duration: '30s', target: 0 },   // Ramp-down: Hạ xuống 0 VUs trong 30s
    ],
    thresholds: {
        http_req_failed: ['rate<0.01'],   // Tỷ lệ lỗi request phải < 1%
        http_req_duration: ['p(95)<200'], // 95% request phải hoàn tất dưới 200ms
    },
};

const BASE_URL = __ENV.TARGET_URL || 'http://localhost:5000';

export default function () {
    // 1. Kiểm thử Trang Chủ (Home Index) - Đã cấu hình Output Cache
    const homeRes = http.get(`${BASE_URL}/`);
    check(homeRes, {
        'Home status is 200': (r) => r.status === 200,
        'Home response time < 100ms': (r) => r.timings.duration < 100,
    });

    sleep(1);

    // 2. Kiểm thử Danh Mục Sản Phẩm (Filtering & Searching)
    const catalogRes = http.get(`${BASE_URL}/Home/Index?series=ROG&sort=price_asc&page=1`);
    check(catalogRes, {
        'Catalog status is 200': (r) => r.status === 200,
        'Catalog response time < 150ms': (r) => r.timings.duration < 150,
    });

    sleep(1);

    // 3. Kiểm thử Price Tracker Prediction API
    const priceRes = http.get(`${BASE_URL}/PriceTracker/GetPrediction?id=1`);
    check(priceRes, {
        'Price Tracker status is 200': (r) => r.status === 200,
        'Price Tracker response time < 80ms': (r) => r.timings.duration < 80,
    });

    sleep(1);

    // 4. Kiểm thử Deal Radar API
    const radarRes = http.get(`${BASE_URL}/PriceTracker/DealRadar`);
    check(radarRes, {
        'Deal Radar status is 200': (r) => r.status === 200,
    });

    sleep(1);
}
