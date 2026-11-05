# Kiểm thử API bằng Postman

1. Import `ParkingBusinessResult.postman_collection.json` vào Postman.
2. Khởi động API theo phần **Tài khoản cục bộ cho Development** trong [README dự án](../README.md). Dùng `http://localhost:5069` với profile `http`; nếu chạy ở địa chỉ khác, sửa biến collection `baseUrl`.
3. Trong tab **Variables** của collection, nhập `adminUsername` và `adminPassword` là tài khoản quản trị đã khởi tạo. Các giá trị này không được cung cấp sẵn trong collection. Chạy **Đăng nhập quản trị viên**; script tự lưu `accessToken` khi thành công. Nếu đăng nhập thất bại, token cũ được xóa.
4. Đặt `parkingLotId` là ID bãi xe đã có trong database. Database mới có thể chưa có bãi xe; để thêm dữ liệu thử vào **database phát triển cục bộ**, có thể chạy một lần:

   ```powershell
   python postman/seed_demo.py
   ```

   Nếu lệnh `python` trên máy Windows không hoạt động nhưng Codex runtime có sẵn, dùng:

   ```powershell
   & "$env:USERPROFILE/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe" postman/seed_demo.py
   ```

   Script thêm một đơn vị và một bãi xe mẫu với ID mặc định trong collection. Script không xóa hoặc ghi đè dữ liệu; chạy lại sẽ báo lỗi nếu dữ liệu mẫu đã tồn tại. `quyHoachId` trong dữ liệu mẫu chỉ là giá trị thử, chưa có khóa ngoại quy hoạch. Không dùng dữ liệu này cho production.
5. Chạy lần lượt **Thêm kết quả**, **Xem chi tiết**, **Cập nhật kết quả**. ID được lưu tự động sau khi thêm. Có thể xem danh sách bất kỳ lúc nào. Nếu đã có kết quả cùng bãi và kỳ, đổi `reportingPeriod` trước khi thêm.
6. Chỉ chạy **Xóa kết quả** nếu bạn muốn xóa bản ghi đang được chọn trong `resultId`; request này xóa dữ liệu trong database đang kết nối. Request tạo tài khoản vận hành là tùy chọn và cần điền `operatorUsername`, `operatorPassword`.

Nếu dùng **Collection Runner**, bỏ chọn request tạo tài khoản tùy chọn khi chưa cấu hình tài khoản và bỏ chọn request xóa nếu muốn giữ dữ liệu thử.

API và tài khoản trong collection hiện chỉ dành cho môi trường Development. Các lỗi nghiệp vụ vẫn trả HTTP 200; kiểm tra trường `code` (`0`: thành công, `7`: lỗi). Token hết hạn sau 30 phút; đăng nhập lại khi cần.
