# 🚀 Hướng Dẫn K6 Load Test So Sánh Hiệu Năng EF Core vs Dapper

Dự án này tích hợp sẵn bộ API Endpoint và k6 script để đo đạc và so sánh hiệu năng giữa **Entity Framework Core** và **Dapper** dưới tải cao (50+ Virtual Users đồng thời).

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

Khi bạn mở **2 cửa sổ Terminal song song** để chạy 2 lệnh k6 cùng lúc, làm thế nào để biết Terminal nào đang test EF Core và Terminal nào đang test Dapper?

Bộ k6 script ([k6-benchmark.js](file:///e:/Compare%20EF%20and%20Dapper/k6-benchmark.js)) đã được gắn nhãn nhận biết rõ ràng trên giao diện Terminal:

| Vị trí hiển thị trên K6 Terminal | Test EF Core | Test Dapper |
| :--- | :--- | :--- |
| **Tiêu đề Scenario (Màn hình chính)** | `* TEST_EF_CORE_UPDATE` | `* TEST_DAPPER_UPDATE` |
| **Tên Metric đo thời gian (Custom)** | `latency_ef_update` | `latency_dapper_update` |
| **Dấu kiểm tra thành công (Check)** | `✓ [EF_CORE] status 200 OK` | `✓ [DAPPER] status 200 OK` |

---

## ⚡ 3. Các Câu Lệnh Chạy Test

Bạn truyền tham số `-e TARGET=ef` hoặc `-e TARGET=dapper` và `-e SCENARIO=...` vào lệnh k6:

### 1️⃣ Kịch bản Single Read (Đọc bản ghi theo ID)
```powershell
# Terminal 1 - EF Core
k6 run -e TARGET=ef -e SCENARIO=single-read k6-benchmark.js

# Terminal 2 - Dapper
k6 run -e TARGET=dapper -e SCENARIO=single-read k6-benchmark.js
```

### 2️⃣ Kịch bản Filter Query (Lọc theo CategoryId & Price)
```powershell
# Terminal 1 - EF Core
k6 run -e TARGET=ef -e SCENARIO=filter-query k6-benchmark.js

# Terminal 2 - Dapper
k6 run -e TARGET=dapper -e SCENARIO=filter-query k6-benchmark.js
```

### 3️⃣ Kịch bản Join Query (Multi-table Join Products + Categories)
```powershell
# Terminal 1 - EF Core
k6 run -e TARGET=ef -e SCENARIO=join-query k6-benchmark.js

# Terminal 2 - Dapper
k6 run -e TARGET=dapper -e SCENARIO=join-query k6-benchmark.js
```

### 4️⃣ Kịch bản Bulk Insert (Thêm mới hàng loạt bản ghi)
```powershell
# Terminal 1 - EF Core
k6 run -e TARGET=ef -e SCENARIO=bulk-insert k6-benchmark.js

# Terminal 2 - Dapper
k6 run -e TARGET=dapper -e SCENARIO=bulk-insert k6-benchmark.js
```

### 5️⃣ Kịch bản Update (Cập nhật dữ liệu)
```powershell
# Terminal 1 - EF Core
k6 run -e TARGET=ef -e SCENARIO=update k6-benchmark.js

# Terminal 2 - Dapper
k6 run -e TARGET=dapper -e SCENARIO=update k6-benchmark.js
```

> 💡 **Tùy chỉnh Port:** Nếu ứng dụng của bạn chạy cổng khác `5136`, thêm `-e BASE_URL=http://localhost:<PORT>` vào cuối câu lệnh.

---

## 📊 4. Cách Đọc Bảng Kết Quả K6

Sau 25 giây thực thi, k6 sẽ in ra bảng tổng kết. Hãy chú ý các chỉ số quan trọng:

1. **`http_reqs` (Throughput/RPS):** Tổng số request được xử lý mỗi giây.
   - *Ví dụ:* `768.64/s` -> **Càng cao càng tốt**.
2. **`http_req_duration` (`p(95)`):** Thời gian 95% request phản hồi đến tay client.
   - *Ví dụ:* `157.88ms` -> **Càng thấp càng tốt**.
3. **`http_req_failed`:** Tỷ lệ lỗi.
   - *Chuẩn:* `0.00%` (Không có request nào bị lỗi).
