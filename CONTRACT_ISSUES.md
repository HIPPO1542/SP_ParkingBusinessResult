# Các điểm cần thống nhất trước khi triển khai

Phạm vi rà soát: kết quả kinh doanh bãi đỗ xe, UC 1.5 và UC 3.4. Nguồn gồm sheet `SP_ParkingBusinessResult` trong `[LDG] CSDL XD LDG.xlsx` và các ApiSpec/LLD tương ứng trong thư mục giải pháp.

1. Theo xác nhận của người dùng, một lượt hoàn tất là một xe vào rồi ra; `soLuotXe` là tổng số lượt hoàn tất. Có thể dùng giá trị này để tính doanh thu bình quân mỗi lượt. Sheet chỉ có một trường `doanhThu`; các chỉ số riêng theo phương thức thanh toán, tỷ lệ lấp đầy và chi phí vận hành vẫn cần dữ liệu đầu vào mà sheet này không mô tả. Không suy ra số xe đang ở trong bãi hoặc số lượt vào và ra theo từng thời điểm từ số lượt hoàn tất.
2. ApiSpec xem dữ liệu UC 1.5 trả `parkingLot`, `periodValue`, `currentRevenue`, `previousRevenue` và `growthRate`; LLD lại trả các trường phẳng và các chỉ số vận hành khác. Cần thống nhất cấu trúc response và cách tính kỳ trước, tăng trưởng.
3. Sheet đặt tên bảng là `SP_ketQuaKinhDoanhBaiDoXe`, với khóa ngoại `baiDoXeId`; LLD dùng `ket_qua_kinh_doanh_bai_do_xe` và SQL ở UC 3.4 dùng `id_bai_do_xe`. Cần xác nhận tên vật lý và schema CSDL đang chạy trước khi kết nối.
4. Sheet mô tả `kyBaoCao` bằng chuỗi với ví dụ tháng, quý, năm; API UC 1.5 yêu cầu `reportDate`, `periodType` và lọc theo ngày. Cần quy tắc chuyển đổi và xác định kỳ ngày có được hỗ trợ hay không.
5. ApiSpec và LLD nêu các tập vai trò khác nhau và phản hồi HTTP 401/403/404; README dự án quy định HTTP 200 cho lỗi. Cần chốt hợp đồng lỗi, xác thực, phạm vi bãi xe được phép truy cập và nguồn thông tin người dùng.
6. UC 3.4 cần kết nối dữ liệu bãi xe, đơn vị quản lý và nơi lưu tệp xuất; cần xác nhận nguồn tích hợp và cấu hình. Sheet thiết kế không thay thế schema và môi trường CSDL thực tế.

Không suy diễn giá trị thiếu, tự bổ sung cột, hoặc coi tên bảng trong ví dụ SQL là schema vật lý đã xác minh.

7. Người dùng làm rõ nhu cầu import Excel là **các lượt xe vào/ra**, trong khi ApiSpec import hiện mô tả các dòng số liệu đã tổng hợp theo kỳ. `dev_parking_sessions` chỉ là bảng dữ liệu giả lập, không phải bảng vận hành trong Excel thiết kế. Cần xác nhận mẫu file, định danh lượt, cách xử lý lượt chưa ra và nguồn phí trước khi lưu nhập hoặc tự tổng hợp doanh thu.
