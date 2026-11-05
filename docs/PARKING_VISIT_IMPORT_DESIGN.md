# Phương án nhập từng lượt xe từ Excel

Trạng thái: có API import và thống kê lượt xe **thử nghiệm Development**. File [mẫu](../outputs/parking-import/mau-luot-xe.xlsx) gồm mã bãi, biển số, loại xe, giờ vào, giờ ra, số tiền đã thu và phương thức thanh toán. Giờ vào/ra gồm cả ngày, tháng, năm, giờ và phút; giờ ra để trống khi xe còn trong bãi.

Đã xác nhận với người dùng:

- Một lượt xe hoàn tất khi đã vào rồi ra. Một dòng thể hiện một lượt.
- Khi nhập lại lượt có cùng bãi, biển số và giờ vào, cập nhật lượt cũ, ví dụ bổ sung giờ ra và tiền thu.
- Doanh thu báo cáo dùng số tiền thực thu trong file. Bảng giá dùng để đối chiếu, không tự thay thế số tiền thực thu.

Lưu vào bảng SQLite riêng `parking_visit`, có ID, khóa ngoại bãi, biển số, loại xe, thời điểm vào/ra, số tiền thực thu và phương thức thanh toán; ràng buộc chống trùng theo bãi, biển số và giờ vào. Bảng này được bổ sung theo yêu cầu import từng lượt và không có trong Excel thiết kế. Bảng `dev_parking_sessions` hiện chỉ là dữ liệu giả lập, không được coi là bảng lượt xe vận hành.

Migration `database/sqlite/apply_parking_visit_migration.py` tạo bản sao lưu trước khi cập nhật và từ chối chạy lại khi bảng đã tồn tại. API kiểm tra từng dòng, quyền đối với bãi, thời điểm ra sau thời điểm vào và báo rõ các dòng lỗi. Nhập lại cập nhật lượt có cùng bãi, biển số và giờ vào; ô giờ ra, số tiền và phương thức thanh toán để trống không xóa dữ liệu đã lưu. Các dòng hợp lệ được lưu, dòng lỗi được trả về riêng; lỗi hệ thống trong lúc ghi có thể dừng yêu cầu sau khi một số dòng đã được lưu.

Gửi multipart `POST /dev/parking-visits/import`, trường file tên `file`, với Bearer token. Hỗ trợ `.xlsx`, `.xls`, `.csv` tối đa 10 MB và tối đa 50.000 dòng dữ liệu; cột phải đúng file mẫu. Đã kiểm thử cả ba định dạng trên file mẫu tạo riêng cho kiểm thử. Giờ nhập dạng `yyyy-MM-dd HH:mm` hoặc `dd/MM/yyyy HH:mm`, hoặc ô ngày giờ thực của Excel. Không nhập dấu phân cách hàng nghìn trong ô văn bản tiền. Kết quả trả số dòng thành công, ID các lượt đã lưu và danh sách dòng lỗi.

`GET /dev/parking-visits` liệt kê lượt đã nhập trong phạm vi quyền, hỗ trợ `parkingLotId`, `status=PARKED` (chưa ra) hoặc `status=COMPLETED` (đã ra), `pageNumber` và `pageSize` (mặc định 1/10, tối đa 100 dòng mỗi trang). Kết quả có `items` và `totalCount` sau khi lọc quyền.

`GET /dev/parking-visits/summary?reportingPeriod=2026-11` tính trực tiếp từ các lượt **đã ra trong tháng**, không tính xe chưa ra. Có thể thay kỳ tháng bằng `fromDate=2026-11-01&toDate=2026-11-30` để tính theo ngày ra trong khoảng, gồm cả hai ngày đầu/cuối. `collectedRevenue` cộng các số tiền đã có; `missingAmountCount` là số lượt hoàn tất chưa có tiền nên doanh thu có thể chưa đầy đủ. Có thể lọc thêm `parkingLotId`. Bình quân ngày và top 5 bãi chỉ được tính khi không thiếu tiền; tăng trưởng tháng chỉ được tính khi cả hai tháng đủ tiền, có dữ liệu kỳ trước và doanh thu kỳ trước lớn hơn 0; các trường không đủ điều kiện là null. Kết quả này độc lập với các bản tổng hợp cũ và không cộng lẫn với dữ liệu giả lập.

`POST /dev/parking-visits/export` nhận JSON `{"fileFormat":"EXCEL","reportingPeriod":"2026-11"}` (có thể thay `EXCEL` bằng `PDF`, thêm `parkingLotId` hoặc `reportTitle`) và tải trực tiếp báo cáo theo các lượt đã ra. Chỉ admin hoặc district admin được xuất trong phạm vi quyền; nếu có lượt hoàn tất thiếu số tiền, API trả lỗi thay vì xuất một báo cáo doanh thu không đầy đủ. PDF hiện chỉ hỗ trợ Windows.

Đối chiếu một lượt bằng `GET /dev/parking-visits/{id}/tariff-comparison?dayType=WEEKDAY`. Cần chọn rõ `WEEKDAY`, `WEEKEND` hoặc `HOLIDAY` vì chưa có lịch ngày lễ; `loại xe` trong file cần khớp mã trong bảng giá, ví dụ `MOTORBIKE` hoặc `CAR_9_SEATS`. Nếu tìm được duy nhất một mức giá đang áp dụng, thời gian gửi nằm trong cùng một ngày và tròn giờ, API so sánh giá tính được với tiền thực thu. Khi qua ngày, có giờ lẻ, không có giá hoặc có nhiều giá phù hợp, API trả `canCompare: false` và lý do; không đoán quy tắc. Giá mẫu hiện có chỉ là giả lập, không phải biểu giá thực tế. Tích hợp hợp đồng chính thức vẫn chưa hoàn thành.
