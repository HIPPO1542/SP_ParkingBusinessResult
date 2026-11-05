# Phương án nhập từng lượt xe từ Excel

Trạng thái: đang thiết kế; API import và bảng lượt xe vận hành **chưa được tạo**. File [mẫu](../outputs/parking-import/mau-luot-xe.xlsx) gồm mã bãi, biển số, loại xe, giờ vào, giờ ra, số tiền đã thu và phương thức thanh toán. Giờ vào/ra gồm cả ngày, tháng, năm, giờ và phút; giờ ra để trống khi xe còn trong bãi.

Đã xác nhận với người dùng:

- Một lượt xe hoàn tất khi đã vào rồi ra. Một dòng thể hiện một lượt.
- Khi nhập lại lượt có cùng bãi, biển số và giờ vào, cập nhật lượt cũ, ví dụ bổ sung giờ ra và tiền thu.
- Doanh thu báo cáo dùng số tiền thực thu trong file. Bảng giá dùng để đối chiếu, không tự thay thế số tiền thực thu.

Đề xuất lưu vào bảng SQLite riêng `parking_visit`, có ID, khóa ngoại bãi, biển số, loại xe, thời điểm vào/ra, số tiền thực thu và phương thức thanh toán; ràng buộc chống trùng theo bãi, biển số và giờ vào. Bảng này chưa có trong Excel thiết kế. Bảng `dev_parking_sessions` hiện chỉ là dữ liệu giả lập, không được coi là bảng lượt xe vận hành.

Trước khi áp dụng lên database đang có phải kiểm thử migration, tạo bản sao lưu và bảo toàn các bản ghi hiện hữu. Import cần kiểm tra từng dòng, quyền đối với bãi, thời điểm ra sau thời điểm vào và báo rõ các dòng lỗi. Phải phân biệt cập nhật lượt đã có với tạo mới, không cộng dồn lại doanh thu khi nhập lại file. Chưa xác định nguồn dữ liệu và quy tắc kỳ báo cáo chính thức để tự động thay thế các bản tổng hợp đang có; không cộng lẫn số liệu nhập với bản tổng hợp sẵn có.
