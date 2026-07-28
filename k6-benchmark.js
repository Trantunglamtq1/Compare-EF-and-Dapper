import http from 'k6/http';
import { check, sleep } from 'k6';
import { Trend, Counter } from 'k6/metrics';

// Custom Parameters (truyền qua -e TARGET=... -e SCENARIO=... -e BASE_URL=...)
const target = (__ENV.TARGET || 'ef').toLowerCase(); // 'ef' hoặc 'dapper'
const scenario = (__ENV.SCENARIO || 'single-read').toLowerCase();
const baseUrl = __ENV.BASE_URL || 'http://localhost:5136';

const targetLabel = target === 'dapper' ? 'DAPPER' : 'EF_CORE';
const metricNameLatency = `latency_${target}_${scenario}`;
const metricNameErrors = `errors_${target}_${scenario}`;

const responseTrend = new Trend(metricNameLatency);
const errorCounter = new Counter(metricNameErrors);

// Phân biệt tên scenario trực tiếp trên giao diện K6 Terminal Header
export const options = {
    scenarios: {
        [`TEST_${targetLabel}_${scenario.toUpperCase()}`]: {
            executor: 'ramping-vus',
            startVUs: 0,
            stages: [
                { duration: '5s', target: 20 },  // Ramp-up 20 VUs
                { duration: '15s', target: 50 }, // Maintain 50 VUs
                { duration: '5s', target: 0 },   // Ramp-down
            ],
            gracefulRampDown: '30s',
        },
    },
    thresholds: {
        http_req_duration: ['p(95)<1000'],
        http_req_failed: ['rate<0.01'],
    },
};

export default function () {
    let res;
    
    if (scenario === 'single-read') {
        const id = Math.floor(Math.random() * 100) + 1;
        const url = `${baseUrl}/api/k6/${target}/single-read/${id}`;
        res = http.get(url, { headers: { 'Accept': 'application/json' } });
    } 
    else if (scenario === 'filter-query') {
        const url = `${baseUrl}/api/k6/${target}/filter-query?categoryId=1&minPrice=50&limit=50`;
        res = http.get(url, { headers: { 'Accept': 'application/json' } });
    } 
    else if (scenario === 'join-query') {
        const url = `${baseUrl}/api/k6/${target}/join-query?limit=50`;
        res = http.get(url, { headers: { 'Accept': 'application/json' } });
    } 
    else if (scenario === 'bulk-insert') {
        const url = `${baseUrl}/api/k6/${target}/bulk-insert?count=20`;
        res = http.post(url, null, { headers: { 'Accept': 'application/json' } });
    } 
    else if (scenario === 'update') {
        const id = Math.floor(Math.random() * 10) + 1;
        const url = `${baseUrl}/api/k6/${target}/update/${id}`;
        res = http.put(url, null, { headers: { 'Accept': 'application/json' } });
    } 
    else {
        const url = `${baseUrl}/api/k6/${target}/single-read/42`;
        res = http.get(url, { headers: { 'Accept': 'application/json' } });
    }

    // Thống kê kết quả
    const success = check(res, {
        [`[${targetLabel}] status 200 OK`]: (r) => r.status === 200,
    });

    responseTrend.add(res.timings.duration);

    if (!success) {
        errorCounter.add(1);
    }

    // Nghỉ ngắn 20ms
    sleep(0.02);
}
