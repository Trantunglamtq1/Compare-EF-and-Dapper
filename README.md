# 🚀 Hướng Dẫn K6 Load Test So Sánh Hiệu Năng EF Core vs Dapper

Dự án này tích hợp sẵn bộ API Endpoint và k6 script để đo đạc và so sánh hiệu năng giữa **Entity Framework Core** và **Dapper** dưới tải cao (50 Virtual Users đồng thời trong 25 giây).

---

## 🏆 BÁO CÁO KẾT QUẢ LOAD TEST TRỌN BỘ 5 KỊCH BẢN (50 Virtual Users)

Bảng tổng hợp kết quả đo đạc thực tế 5 kịch bản bằng Grafana k6 dưới tải 50 Virtual Users đồng thời:

| Kịch bản Test (Scenario) | Công nghệ | Tổng Request | Thông lượng (RPS) | Latency Trung bình (Avg) | Latency Trung vị (Med) | Latency 95% (p95) | Tỷ lệ Lỗi |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| **1. Single Read** *(Đọc 1 bản ghi)* | EF Core | 19,959 | 798.14 RPS | 14.21 ms | 2.20 ms | 52.68 ms | 0.00% |
| | **Dapper** | **32,115** | **1,284.37 RPS** | **1.07 ms** | **0.65 ms** | **2.69 ms** | **0.00%** |
| **2. Filter Query** *(Lọc & Sắp xếp)* | EF Core | 10,410 | 416.31 RPS | 46.12 ms | 12.40 ms | 141.23 ms | 0.00% |
| | **Dapper** | **29,961** | **1,197.79 RPS** | **2.58 ms** | **2.24 ms** | **4.91 ms** | **0.00%** |
| **3. Join Query** *(Multi-table Join)* | EF Core | 12,745 | 508.09 RPS | 33.89 ms | 5.50 ms | 119.65 ms | 0.00% |
| | **Dapper** | **31,124** | **1,244.77 RPS** | **1.78 ms** | **1.46 ms** | **3.82 ms** | **0.00%** |
| **4. Bulk Insert** *(Chèn 20 bản ghi)* | EF Core | 552 | 21.34 RPS | 1,321.93 ms | 544.53 ms | 2,682.76 ms | 0.00% |
| | **Dapper** | **1,823** | **71.96 RPS** | **383.26 ms** | **144.07 ms** | **1,882.53 ms** | **0.00%** |
| **5. Update** *(Cập nhật dữ liệu)* | EF Core | 8,357 | 334.16 RPS | 64.54 ms | 7.37 ms | 312.43 ms | 0.00% |
| | **Dapper** | **19,216** | **768.64 RPS** | **15.76 ms** | **0.53 ms** | **157.88 ms** | **0.00%** |

---

## 📌 1. Chuẩn bị Môi trường

### Bước 1: Khởi động Web Application
Mở Terminal tại thư mục dự án và chạy:
```powershell
dotnet run --project CompareEfAndDapper.Web
```
> 📍 **Lưu ý:** Ứng dụng mặc định chạy tại cổng `http://localhost:5136`. Hãy đảm bảo Terminal này luôn mở trong quá trình test.

### Bước 2: Cài đặt Grafana k6 (nếu chưa cài)
Mở một cửa sổ Terminal mới và chạy:
```powershell
winget install k6 --source winget
```

---

## 🏷️ 2. Dấu Hiệu Phân Biệt Test EF Core vs Dapper

Khi bạn mở **2 cửa sổ Terminal song song** để chạy 2 lệnh k6 cùng lúc, bạn có thể dễ dàng phân biệt nhờ các nhãn nhận diện trên giao diện K6 Terminal:

| Vị trí hiển thị trên K6 Terminal | Test EF Core | Test Dapper |
| :--- | :--- | :--- |
| **Tiêu đề Scenario (Màn hình chính)** | `* TEST_EF_CORE_UPDATE` | `* TEST_DAPPER_UPDATE` |
| **Tên Metric đo thời gian (Custom)** | `latency_ef_update` | `latency_dapper_update` |
| **Dấu kiểm tra thành công (Check)** | `✓ [EF_CORE] status 200 OK` | `✓ [DAPPER] status 200 OK` |

---

## ⚡ 3. Các Câu Lệnh Chạy Test

Truyền tham số `-e TARGET=ef` hoặc `-e TARGET=dapper` và `-e SCENARIO=...` vào lệnh k6:

### 1️⃣ Kịch bản Single Read (Đọc bản ghi theo ID)
```powershell
# EF Core
k6 run -e TARGET=ef -e SCENARIO=single-read k6-benchmark.js

# Dapper
k6 run -e TARGET=dapper -e SCENARIO=single-read k6-benchmark.js
```

### 2️⃣ Kịch bản Filter Query (Lọc theo CategoryId & Price)
```powershell
# EF Core
k6 run -e TARGET=ef -e SCENARIO=filter-query k6-benchmark.js

# Dapper
k6 run -e TARGET=dapper -e SCENARIO=filter-query k6-benchmark.js
```

### 3️⃣ Kịch bản Join Query (Multi-table Join Products + Categories)
```powershell
# EF Core
k6 run -e TARGET=ef -e SCENARIO=join-query k6-benchmark.js

# Dapper
k6 run -e TARGET=dapper -e SCENARIO=join-query k6-benchmark.js
```

### 4️⃣ Kịch bản Bulk Insert (Thêm mới hàng loạt bản ghi)
```powershell
# EF Core
k6 run -e TARGET=ef -e SCENARIO=bulk-insert k6-benchmark.js

# Dapper
k6 run -e TARGET=dapper -e SCENARIO=bulk-insert k6-benchmark.js
```

### 5️⃣ Kịch bản Update (Cập nhật dữ liệu)
```powershell
# EF Core
k6 run -e TARGET=ef -e SCENARIO=update k6-benchmark.js

# Dapper
k6 run -e TARGET=dapper -e SCENARIO=update k6-benchmark.js
```

---

## 📖 4. Hướng Dẫn Giải Thích Chi Tiết Các Thông Số Trong K6 Output

Khi k6 chạy xong, màn hình sẽ hiển thị bảng kết quả chia thành 4 khối chính: **CUSTOM**, **HTTP**, **EXECUTION**, và **NETWORK**. Dưới đây là ý nghĩa chi tiết từng thông số:

### 🎯 1. Khối `CUSTOM` (Chỉ số đo lường tùy chỉnh)
Khối này chứa các metric được định nghĩa riêng trong file `k6-benchmark.js` cho từng công nghệ:

* **`latency_<target>_<scenario>`**: Thời gian xử lý request riêng cho kịch bản và ORM đang test.
  * `avg`: Thời gian phản hồi trung bình (ms).
  * `min` / `med` / `max`: Thời gian nhanh nhất / trung vị (50%) / chậm nhất (ms).
  * `p(90)` / `p(95)`: **Percentile 90% và 95%** (Ví dụ: `p(95)=2.69ms` nghĩa là 95% số request phản hồi nhanh hơn 2.69ms).
* **`errors_<target>_<scenario>`**: Tổng số request bị lỗi trong suốt quá trình chạy test.

---

### 🌐 2. Khối `HTTP` (Chỉ số giao thức HTTP)
Khối này đo đạc các thông số về các HTTP Request được gửi đến Web Server:

* **`http_req_duration`**: Tổng thời gian từ khi client gửi request đến khi nhận xong response từ Server.
  * **`{ expected_response:true }`**: Chỉ số đo riêng cho các request thành công (Status 200 OK).
* **`http_req_failed`**: Tỷ lệ phần trăm request bị lỗi (`0.00%` là lý tưởng).
* **`http_reqs`**: Tổng số HTTP Request đã gửi và **Thông lượng (RPS - Request Per Second)**.
  * *Ví dụ:* `32115  1284.37/s` -> Tổng cộng 32,115 request, đạt tốc độ **1,284.37 request/giây**.

---

### ⚙️ 3. Khối `EXECUTION` (Chỉ số tiến trình thực thi của K6)
Khối này phản ánh cách các Virtual Users (VUs - Người dùng ảo) vận hành:

* **`iteration_duration`**: Thời gian hoàn thành 1 vòng lặp test (Bao gồm: Gửi HTTP Request + thời gian nghỉ `sleep(20ms)`).
* **`iterations`**: Tổng số vòng lặp k6 đã thực hiện và tốc độ vòng lặp/giây.
* **`vus`**: Số lượng Virtual Users đang chạy thực tế tại thời điểm kết thúc test (`min` / `max`).
* **`vus_max`**: Số lượng Virtual Users tối đa được cấu hình trong test (Ví dụ: `50 VUs`).

---

### 📡 4. Khối `NETWORK` (Chỉ số băng thông mạng)
Khối này thống kê lưu lượng dữ liệu truyền qua lại giữa K6 Client và Web Server:

* **`data_received`**: Tổng dung lượng dữ liệu K6 nhận về từ Web Server và tốc độ tải xuống (`kB/s` hoặc `MB/s`).
* **`data_sent`**: Tổng dung lượng dữ liệu K6 gửi lên Web Server và tốc độ tải lên (`kB/s` hoặc `MB/s`).
