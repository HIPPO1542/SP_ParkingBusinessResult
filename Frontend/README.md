# Giao diện kết quả kinh doanh

Giao diện nằm trong `index.html` và được API phục vụ ở trang gốc `/` trong chế độ xác thực Local, bao gồm môi trường **Production**. Không cần cài thêm Node.js hoặc chạy frontend riêng.

1. Chạy `.\start.ps1` từ thư mục gốc theo [README dự án](../README.md). Script chạy Release / Production bằng database và tài khoản đã có.
2. Mở <http://localhost:5069/> nếu dùng profile `http`. Nếu API chạy ở địa chỉ khác, mở địa chỉ đó.
3. Đăng nhập bằng tài khoản quản trị đã tạo. File mẫu nhập lượt xe nằm tại `outputs/parking-import/mau-luot-xe.xlsx`.
4. Nhập file, chọn khoảng ngày và một hoặc nhiều bãi, sau đó xem thống kê, danh sách hoặc xuất báo cáo. Nếu không chọn bãi nào, thống kê sử dụng tất cả bãi được phép xem.

Chọn bãi thống kê bằng nút xổ xuống và tích vào từng bãi; tích “Tất cả bãi” để bỏ giới hạn theo bãi. Danh sách lượt xe hỗ trợ kết hợp bãi, loại xe, trạng thái và ngày/tháng/năm của ngày vào bãi; để trống thành phần nào thì không giới hạn theo thành phần đó.

Giao diện thống kê và xuất trực tiếp từ các lượt xe trong bảng booking; dữ liệu kết quả kinh doanh theo kỳ là nguồn riêng. Token chỉ được giữ trong bộ nhớ trang và sẽ bị xóa khi tải lại trang hoặc đăng xuất.

## Chạy debug trong VS Code

Mở thư mục gốc `SP_ParkingBusinessResult` bằng VS Code và cài extension C# nếu chưa có. Vào **Run and Debug**, chọn **Debug Parking API**, sau đó nhấn **F5**. Khi backend chạy, mở <http://localhost:5069/>. Có thể đặt breakpoint trong handler, rồi thao tác trên giao diện để dừng tại dòng đó. Nhấn **F10** để chạy từng bước và **Shift+F5** để dừng debug.

Project sử dụng User Secrets để đọc cấu hình xác thực khi debug. Các giá trị bí mật chỉ nằm trên máy đã cấu hình; khi chạy trên máy khác vẫn cần thiết lập issuer, audience và signing key theo README dự án.
