# Đối chiếu API kết quả kinh doanh với tài liệu

Phạm vi: API thử nghiệm Development hiện có, đối chiếu ApiSpec/LLD của UC 1.5 và UC 3.4 trong thư mục `Giai_phap_bai_do_xe_thong_minh_theo_UC_final`, cùng sheet `SP_ParkingBusinessResult` trong Excel. Kết quả này không xác nhận API đã nghiệm thu.

| Hạng mục | Tài liệu yêu cầu | API hiện tại | Trạng thái |
| --- | --- | --- | --- |
| Route kết quả kinh doanh | `/gdci/{module}/parkingLot/businessOperation` | `/dev/business-results` | Chưa khớp; route hiện tại chỉ dùng Development. |
| Body thêm | `parkingLotId`, `reportDate`, `periodType`, `totalCheckIn`, `totalCheckOut`, `averageOccupancyRate`, `peakOccupancyRate`, `totalRevenue`, `cashRevenue`, `electronicRevenue`; có thêm `operatingExpenses`, `notes` tùy chọn | `parkingLotId`, `reportingPeriod`, `revenue`, `completedVehicleCount`, `note` | Chưa khớp. Body tài liệu bị từ chối với code 7 thay vì bị ghi sai. |
| Body sửa | `totalCheckIn`, `totalCheckOut`, các tỷ lệ lấp đầy, `totalRevenue`, `cashRevenue`, `electronicRevenue`; có `operatingExpenses`, `notes` tùy chọn | `revenue`, `completedVehicleCount`, `note` | Chưa khớp. Body thiếu trường hoặc chứa trường không hỗ trợ bị từ chối với code 7. |
| Dữ liệu trả về | ApiSpec đưa `parkingLot` dạng object, `periodType`, `periodValue`, doanh thu kỳ trước, tăng trưởng, nguồn dữ liệu và thông tin audit | Object phẳng gồm `id`, `parkingLotId`, `reportingPeriod`, `revenue`, `completedVehicleCount`, `note` | Chưa khớp. ApiSpec và LLD của chức năng xem theo thời gian cũng mô tả các cấu trúc khác nhau. |
| Xem theo thời gian | Lọc, phân trang, kỳ và sắp xếp theo ApiSpec | Danh sách thử nghiệm chưa hỗ trợ các tham số đó | Chưa hoàn thành. |
| Nhập file và báo cáo UC 3.4 | Import, danh sách báo cáo, xem tổng hợp và xuất báo cáo | Chưa có các API tương ứng | Chưa hoàn thành. |
| Quyền | Các tài liệu liệt kê các vai trò khác nhau, trong đó API xóa đề cập `ROLE_BUSINESS_MANAGER` | Môi trường Development hỗ trợ `ROLE_ADMIN`, `ROLE_DISTRICT_ADMIN`, `ROLE_PARKING_OPERATOR` và phạm vi bãi theo token | Chưa thể xem là khớp chính sách quyền chính thức. |
| Xóa | LLD mô tả xóa vật lý | Theo yêu cầu mới của người dùng, API đánh dấu `daXoa = 1`, ẩn bản ghi và giữ lại dữ liệu | Đã thay đổi so với LLD theo yêu cầu trực tiếp; cần đồng bộ tài liệu khi chốt hợp đồng. |

Sheet Excel chỉ có một trường tổng số lượt và một trường tổng doanh thu. Nó không cung cấp riêng dữ liệu lượt vào/lượt ra, doanh thu tiền mặt/điện tử, chi phí vận hành hay tỷ lệ lấp đầy. Không thể suy ra chính xác các trường đó từ tổng doanh thu và tổng lượt; cần thống nhất nguồn dữ liệu hoặc thay đổi hợp đồng trước khi triển khai đầy đủ body/response theo tài liệu. `daXoa` là cột bổ sung theo yêu cầu xóa mềm, không có trong sheet gốc.

## Bằng chứng kiểm thử

- Chạy Postman Collection bằng Newman với database SQLite tạm: 11 request và 11 assertion thành công. Trong đó, route mẫu theo tài liệu **kỳ vọng trả 404** và hai request body theo tài liệu **kỳ vọng bị từ chối** vì API thử nghiệm chưa hỗ trợ; kết quả này không chứng minh API khớp tài liệu.
- Kiểm tra body sửa theo tài liệu bị từ chối và không làm doanh thu/số lượt về 0. Trước khi sửa, cùng body đã bị chấp nhận và ghi cả hai giá trị thành 0.
- Kiểm tra xóa mềm: bản ghi vẫn tồn tại trong SQLite với `daXoa = 1`, không hiện qua API, không sửa/xóa lại được; có thể tạo bản ghi mới cho cùng bãi và kỳ.
- Kiểm tra migration trên database tạm: giữ dữ liệu cũ, sao lưu trước khi áp dụng và từ chối khi bảng khác có khóa ngoại trỏ tới bảng kết quả. Database cục bộ đã được sao lưu và áp dụng migration.

Các bằng chứng trên chỉ áp dụng cho môi trường Development và các trường hợp đã chạy; chưa xác nhận tất cả trường hợp lỗi framework trả HTTP 200 hay toàn bộ yêu cầu ApiSpec/LLD.

## Ma trận truy vết

| Yêu cầu | Bằng chứng | Trạng thái |
| --- | --- | --- |
| Xóa mềm và ẩn bản ghi đã xóa | Kiểm tra repository và Newman trên SQLite tạm; đọc trực tiếp xác nhận bản ghi vẫn còn | `[Verified]` |
| Bảo toàn dữ liệu khi cập nhật schema | `tests/test_soft_delete_migration.py`: dữ liệu cũ, sao lưu, chặn khóa ngoại tham chiếu và chạy lại | `[Verified]` |
| Không ghi 0 khi body thiếu trường hoặc dùng body tài liệu chưa hỗ trợ | Newman gửi PUT theo ApiSpec và body thiếu trường; xác nhận code 7 và dữ liệu gốc không đổi | `[Verified]` |
| Đúng route, body và response ApiSpec/LLD | Route và các trường còn khác; thiếu nguồn dữ liệu cho nhiều trường | `[Blocked / Not Verified]` |
| Import và báo cáo theo UC 3.4 | Endpoint chưa được triển khai | `[Blocked / Not Verified]` |
