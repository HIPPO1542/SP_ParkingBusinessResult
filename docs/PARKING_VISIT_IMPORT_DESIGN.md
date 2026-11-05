# Phương án nhập từng lượt xe từ Excel

Trạng thái: có API import và thống kê lượt xe **thử nghiệm Development**. File [mẫu](../outputs/parking-import/mau-luot-xe.xlsx) gồm mã bãi, biển số, loại xe, giờ vào, giờ ra, số tiền đã thu và phương thức thanh toán. Giờ vào/ra gồm cả ngày, tháng, năm, giờ và phút; giờ ra để trống khi xe còn trong bãi.

Đã xác nhận với người dùng:

- Một lượt xe hoàn tất khi đã vào rồi ra. Một dòng thể hiện một lượt.
- Lượt có cùng bãi, biển số và giờ vào được coi là trùng; không lưu hoặc cập nhật, trả lỗi cho dòng đó.
- Doanh thu báo cáo dùng số tiền thực thu trong file. Bảng giá dùng để đối chiếu, không tự thay thế số tiền thực thu.

Lưu vào bảng SQLite riêng `parking_visit`, có ID, khóa ngoại bãi, biển số, loại xe, thời điểm vào/ra, số tiền thực thu và phương thức thanh toán; ràng buộc chống trùng theo bãi, biển số và giờ vào. Bảng này được bổ sung theo yêu cầu import từng lượt và không có trong Excel thiết kế. Bảng `dev_parking_sessions` hiện chỉ là dữ liệu giả lập, không được coi là bảng lượt xe vận hành.

Migration `database/sqlite/apply_parking_visit_migration.py` tạo bản sao lưu trước khi cập nhật và từ chối chạy lại khi bảng đã tồn tại. API kiểm tra từng dòng, quyền đối với bãi, thời điểm ra sau thời điểm vào và báo rõ các dòng lỗi. Khi nhập trùng, dữ liệu lượt đã lưu không thay đổi. Các dòng hợp lệ được lưu, dòng lỗi được trả về riêng; lỗi hệ thống trong lúc ghi có thể dừng yêu cầu sau khi một số dòng đã được lưu.

Gửi multipart `POST /gdci/parking/parkingLot/businessOperation/import`, trường file tên `file`, với Bearer token. Hỗ trợ `.xlsx`, `.xls`, `.csv` tối đa 10 MB và tối đa 50.000 dòng dữ liệu; cột phải đúng file mẫu. Giờ nhập dạng `yyyy-MM-dd HH:mm` hoặc `dd/MM/yyyy HH:mm`, hoặc ô ngày giờ thực của Excel. Không nhập dấu phân cách hàng nghìn trong ô văn bản tiền. Kết quả trả số dòng thành công, ID và dữ liệu các lượt đã lưu, số dòng trùng và danh sách dòng lỗi. Dòng trùng bao gồm lượt đã có trong database hoặc lặp lại trong cùng file; dòng trùng được tính vào số lỗi, không tính vào số thành công.

`GET /gdci/parking/parkingLot/parkingVisit` liệt kê lượt đã nhập trong phạm vi quyền, kèm mã và tên bãi; hỗ trợ kết hợp `parkingLotId`, `vehicleType`, `status=PARKED` (chưa ra) hoặc `status=COMPLETED` (đã ra), `pageNumber` và `pageSize` (mặc định 1/10, tối đa 100 dòng mỗi trang). Kết quả có `items` và `totalCount` sau khi lọc quyền. `GET /gdci/parking/parkingLot/parkingVisit/filters` cung cấp các bãi được phép truy cập và loại xe đã có trong phạm vi quyền.

`GET /gdci/parking/parkingLot/parkingVisit/summary?reportingPeriod=2026-11` tính trực tiếp từ các lượt **đã ra trong tháng**, không tính xe chưa ra. Có thể thay kỳ tháng bằng `fromDate=2026-11-01&toDate=2026-11-30` để tính theo ngày ra trong khoảng, gồm cả hai ngày đầu/cuối. `collectedRevenue` cộng các số tiền đã có; `missingAmountCount` là số lượt hoàn tất chưa có tiền nên doanh thu có thể chưa đầy đủ. Có thể lọc thêm `parkingLotId`. Bình quân ngày và top 5 bãi chỉ được tính khi không thiếu tiền; tăng trưởng tháng chỉ được tính khi cả hai tháng đủ tiền, có dữ liệu kỳ trước và doanh thu kỳ trước lớn hơn 0; các trường không đủ điều kiện là null. Kết quả này độc lập với các bản tổng hợp cũ và không cộng lẫn với dữ liệu giả lập.

Thống kê chia số tiền đã biết thành `cashRevenue` (phương thức `CASH` hoặc `Tiền mặt`), `electronicRevenue` (`ELECTRONIC` hoặc `Điện tử`) và `unclassifiedRevenue` cho phương thức khác hoặc để trống. `unclassifiedPaymentCount` cho biết số lượt có tiền nhưng chưa phân loại được; các lượt chưa có tiền được đếm riêng bằng `missingAmountCount`.

Có thể lọc `district` theo chuỗi trong địa chỉ. `revenueByDistrict` thử lấy thành phần địa chỉ bắt đầu bằng `Quận ` hoặc `Huyện `, phân tách bằng dấu phẩy; địa chỉ thiếu hoặc có nhiều thành phần phù hợp được gom vào nhóm `district: null`. Đây là suy ra từ địa chỉ văn bản, không phải đối chiếu mã hành chính và có thể không nhận diện được cách ghi khác. Cơ cấu địa bàn chỉ hiển thị khi mọi lượt hoàn tất có số tiền.

`POST /gdci/parking/parkingLot/parkingVisit/export` nhận JSON `{"fileFormat":"EXCEL","reportingPeriod":"2026-11"}` (có thể thay kỳ tháng bằng `fromDate` và `toDate` định dạng `yyyy-MM-dd`, thay `EXCEL` bằng `PDF`, thêm `parkingLotId`, `district` hoặc `reportTitle`) và tải trực tiếp báo cáo theo các lượt đã ra. Chỉ admin hoặc district admin được xuất trong phạm vi quyền; nếu có lượt hoàn tất thiếu số tiền, API trả lỗi thay vì xuất một báo cáo doanh thu không đầy đủ. PDF hiện chỉ hỗ trợ Windows.

Đối chiếu một lượt bằng `GET /gdci/parking/parkingLot/parkingVisit/{id}/tariff-comparison?dayType=WEEKDAY`. Cần chọn rõ `WEEKDAY`, `WEEKEND` hoặc `HOLIDAY` vì chưa có lịch ngày lễ; `loại xe` trong file cần khớp mã trong bảng giá, ví dụ `MOTORBIKE` hoặc `CAR_9_SEATS`. Nếu tìm được duy nhất một mức giá đang áp dụng, thời gian gửi nằm trong cùng một ngày và tròn giờ, API so sánh giá tính được với tiền thực thu. Khi qua ngày, có giờ lẻ, không có giá hoặc có nhiều giá phù hợp, API trả `canCompare: false` và lý do; không đoán quy tắc. Giá mẫu hiện có chỉ là giả lập, không phải biểu giá thực tế. Tích hợp hợp đồng chính thức vẫn chưa hoàn thành.
