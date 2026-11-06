# Kiểm thử API bằng Postman

1. Import `ParkingBusinessResult.postman_collection.json` vào Postman.
2. Khởi động API theo phần **Tài khoản cục bộ cho Development** trong [README dự án](../README.md). Dùng `http://localhost:5069` với profile `http`; nếu chạy ở địa chỉ khác, sửa biến collection `baseUrl`.
3. Trong tab **Variables** của collection, nhập `adminUsername` và `adminPassword` là tài khoản quản trị đã khởi tạo. Các giá trị này không được cung cấp sẵn trong collection. Chạy **Đăng nhập quản trị viên**; script tự lưu `accessToken` khi thành công. Nếu đăng nhập thất bại, token cũ được xóa.
4. Đặt `parkingLotId` là ID bãi xe đã có trong database. Nếu đã tạo 5 bãi `DEMO-01` đến `DEMO-05`, lấy ID bằng câu truy vấn trong [hướng dẫn dữ liệu giả lập](../docs/DEVELOPMENT_PARKING_DATA.md) và không cần chạy script bên dưới. Database mới có thể chưa có bãi xe; để thêm một bãi xe thử khác vào **database phát triển cục bộ**, có thể chạy một lần:

   ```powershell
   python postman/seed_demo.py
   ```

   Nếu lệnh `python` trên máy Windows không hoạt động nhưng Codex runtime có sẵn, dùng:

   ```powershell
   & "$env:USERPROFILE/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe" postman/seed_demo.py
   ```

   Script thêm một đơn vị và một bãi xe mẫu với ID mặc định trong collection. Script không xóa hoặc ghi đè dữ liệu; chạy lại sẽ báo lỗi nếu dữ liệu mẫu đã tồn tại. `quyHoachId` trong dữ liệu mẫu chỉ là giá trị thử, chưa có khóa ngoại quy hoạch. Không dùng dữ liệu này cho production.
5. Chạy lần lượt **Thêm kết quả**, **Xem chi tiết**, **Cập nhật kết quả**. ID được lưu tự động sau khi thêm. Có thể xem danh sách bất kỳ lúc nào. Nếu đã có kết quả cùng bãi và kỳ, đổi `reportingPeriod` trước khi thêm.
6. Chỉ chạy **Xóa mềm kết quả** nếu bạn muốn ẩn bản ghi đang được chọn trong `resultId`; bản ghi vẫn được giữ trong database với trạng thái đã xóa. Request tạo tài khoản vận hành là tùy chọn và cần điền `operatorUsername`, `operatorPassword`.

Nếu dùng **Collection Runner**, bỏ chọn request tạo tài khoản tùy chọn khi chưa cấu hình tài khoản. Nếu muốn giữ dữ liệu thử, bỏ chọn cả request xóa mềm và request kiểm tra bản ghi đã xóa.

Request **Kiểm tra route theo tài liệu** xác nhận route có thể gọi; các request đối chiếu body xác nhận API từ chối body chưa hỗ trợ thay vì âm thầm ghi sai dữ liệu. Các kiểm tra đó **không có nghĩa API đã khớp hoàn toàn ApiSpec**. Chi tiết khác biệt được ghi trong `docs/API_CONTRACT_REVIEW.md`.

API và tài khoản trong collection hiện chỉ dành cho môi trường Development. Các lỗi nghiệp vụ vẫn trả HTTP 200; kiểm tra trường `code` (`0`: thành công, `7`: lỗi). Token hết hạn sau 30 phút; đăng nhập lại khi cần.

## Import và báo cáo theo từng lượt xe

Import thêm collection `ParkingVisits.postman_collection.json`. Điền tài khoản quản trị ở tab Variables và chạy **Đăng nhập**. Tại request **Import file lượt xe**, chọn file trong ô `file` (mẫu tại `outputs/parking-import/mau-luot-xe.xlsx`); các dòng hợp lệ sẽ được lưu thật vào SQLite. Sau import, collection tự lưu ID lượt đầu tiên để thử đối chiếu giá. Chỉnh `reportingPeriod` theo tháng xe ra, chạy request thống kê hoặc chọn **Send and Download** ở request xuất file. Đây là collection riêng cho dữ liệu lượt xe import; dữ liệu tổng hợp mẫu cũ không tự cộng vào kết quả.

Trước khi import, database cần bảng `SP_datTruocChoDoXe`. Với database cũ đã có `parking_visit`, chạy `python database/sqlite/migrate_visits_to_booking.py` để sao lưu và chuyển dữ liệu. Xem đầy đủ giới hạn file, quy tắc nhập và cách dùng tại [hướng dẫn import](../docs/PARKING_VISIT_IMPORT_DESIGN.md).
