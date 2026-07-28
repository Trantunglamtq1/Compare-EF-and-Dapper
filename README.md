# 🚀 Hướng Dẫn K6 Load Test So Sánh Hiệu Năng EF Core vs Dapper vs SQL Command

Dự án này tích hợp sẵn bộ API Endpoint và k6 script để đo đạc và so sánh hiệu năng giữa **Entity Framework Core**, **Dapper** và **Raw SQL Command (ADO.NET)** dưới tải cao (50 Virtual Users đồng thời trong 25 giây).

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

## 🏷️ 2. Dấu Hiệu Phân Biệt Test (3 Targets)

Khi mở **nhiều cửa sổ Terminal song song**, bạn có thể phân biệt từng ORM nhờ nhãn nhận diện tự động:

| Vị trí hiển thị trên K6 Terminal | EF Core | Dapper | SQL Command |
| :--- | :--- | :--- | :--- |
| **Tiêu đề Scenario** | `TEST_EF_CORE_UPDATE` | `TEST_DAPPER_UPDATE` | `TEST_SQL_CMD_UPDATE` |
| **Tên Metric (Custom)** | `latency_ef_update` | `latency_dapper_update` | `latency_sql_update` |
| **Check thành công** | `✓ [EF_CORE] status 200 OK` | `✓ [DAPPER] status 200 OK` | `✓ [SQL_CMD] status 200 OK` |

> 💡 **SQL Command** = ADO.NET thuần (`DbCommand` + `DbDataReader`) — không dùng ORM hay Micro-ORM. Đây là mức truy cập database thấp nhất và là **baseline hiệu năng tuyệt đối**.

---

## ⚡ 3. Các Câu Lệnh Chạy Test

Truyền tham số `-e TARGET=ef`, `-e TARGET=dapper` hoặc `-e TARGET=sql` và `-e SCENARIO=...` vào lệnh k6:

### 1️⃣ Kịch bản Single Read (Đọc bản ghi theo ID)
```powershell
# EF Core
k6 run -e TARGET=ef -e SCENARIO=single-read k6-benchmark.js

# Dapper
k6 run -e TARGET=dapper -e SCENARIO=single-read k6-benchmark.js

# Raw SQL Command
k6 run -e TARGET=sql -e SCENARIO=single-read k6-benchmark.js
```

### 2️⃣ Kịch bản Filter Query (Lọc theo CategoryId & Price)
```powershell
# EF Core
k6 run -e TARGET=ef -e SCENARIO=filter-query k6-benchmark.js

# Dapper
k6 run -e TARGET=dapper -e SCENARIO=filter-query k6-benchmark.js

# Raw SQL Command
k6 run -e TARGET=sql -e SCENARIO=filter-query k6-benchmark.js
```

### 3️⃣ Kịch bản Join Query (Multi-table Join Products + Categories)
```powershell
# EF Core
k6 run -e TARGET=ef -e SCENARIO=join-query k6-benchmark.js

# Dapper
k6 run -e TARGET=dapper -e SCENARIO=join-query k6-benchmark.js

# Raw SQL Command
k6 run -e TARGET=sql -e SCENARIO=join-query k6-benchmark.js
```

### 4️⃣ Kịch bản Bulk Insert (Thêm mới hàng loạt bản ghi)
```powershell
# EF Core
k6 run -e TARGET=ef -e SCENARIO=bulk-insert k6-benchmark.js

# Dapper
k6 run -e TARGET=dapper -e SCENARIO=bulk-insert k6-benchmark.js

# Raw SQL Command
k6 run -e TARGET=sql -e SCENARIO=bulk-insert k6-benchmark.js
```

### 5️⃣ Kịch bản Update (Cập nhật dữ liệu)
```powershell
# EF Core
k6 run -e TARGET=ef -e SCENARIO=update k6-benchmark.js

# Dapper
k6 run -e TARGET=dapper -e SCENARIO=update k6-benchmark.js

# Raw SQL Command
k6 run -e TARGET=sql -e SCENARIO=update k6-benchmark.js
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
