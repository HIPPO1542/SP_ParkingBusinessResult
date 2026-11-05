# Giao diện kết quả kinh doanh

Giao diện nằm trong `index.html` và được API phục vụ ở trang gốc `/` trong môi trường Development. Không cần cài thêm Node.js hoặc chạy frontend riêng.

1. Khởi động API theo phần **Tài khoản cục bộ cho Development** trong [README dự án](../README.md#tài-khoản-cục-bộ-cho-development).
2. Mở <http://localhost:5069/> nếu dùng profile `http`. Nếu API chạy ở địa chỉ khác, mở địa chỉ đó.
3. Đăng nhập bằng tài khoản quản trị đã tạo. File mẫu nhập lượt xe nằm tại `outputs/parking-import/mau-luot-xe.xlsx`.
4. Nhập file, chọn kỳ báo cáo và nguồn dữ liệu, sau đó xem thống kê, danh sách hoặc xuất báo cáo.

Lượt xe nhập từ file và các bản kết quả kinh doanh đã lưu hiện là hai nguồn dữ liệu riêng. Token chỉ được giữ trong bộ nhớ trang và sẽ bị xóa khi tải lại trang hoặc đăng xuất. Đường dẫn tải báo cáo yêu cầu token của chính tài khoản đã xuất.
