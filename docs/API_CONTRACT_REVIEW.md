# Đối chiếu API kết quả kinh doanh với tài liệu

Phạm vi: API thử nghiệm Development hiện có, đối chiếu ApiSpec/LLD của UC 1.5 và UC 3.4 trong thư mục `Giai_phap_bai_do_xe_thong_minh_theo_UC_final`, cùng sheet `SP_ParkingBusinessResult` trong Excel. Kết quả này không xác nhận API đã nghiệm thu.

| Hạng mục | Tài liệu yêu cầu | API hiện tại | Trạng thái |
| --- | --- | --- | --- |
| Route kết quả kinh doanh | `/gdci/{module}/parkingLot/businessOperation` | `/dev/business-results` | Chưa khớp; route hiện tại chỉ dùng Development. |
| Body thêm | `parkingLotId`, `reportDate`, `periodType`, `totalCheckIn`, `totalCheckOut`, `averageOccupancyRate`, `peakOccupancyRate`, `totalRevenue`, `cashRevenue`, `electronicRevenue`; có thêm `operatingExpenses`, `notes` tùy chọn | `parkingLotId`, `reportingPeriod`, `revenue`, `completedVehicleCount`, `note` | Chưa khớp. Body tài liệu bị từ chối với code 7 thay vì bị ghi sai. |
| Body sửa | `totalCheckIn`, `totalCheckOut`, các tỷ lệ lấp đầy, `totalRevenue`, `cashRevenue`, `electronicRevenue`; có `operatingExpenses`, `notes` tùy chọn | `revenue`, `completedVehicleCount`, `note` | Chưa khớp. Body thiếu trường hoặc chứa trường không hỗ trợ bị từ chối với code 7. |
| Dữ liệu trả về | ApiSpec đưa `parkingLot` dạng object, `periodType`, `periodValue`, doanh thu kỳ trước, tăng trưởng, nguồn dữ liệu và thông tin audit | Object phẳng gồm `id`, `parkingLotId`, `reportingPeriod`, `revenue`, `completedVehicleCount`, `note` | Chưa khớp. ApiSpec và LLD của chức năng xem theo thời gian cũng mô tả các cấu trúc khác nhau. |
| Xem theo thời gian | Lọc, phân trang, kỳ và sắp xếp theo ApiSpec | Danh sách thử nghiệm chưa hỗ trợ các tham số đó | Chưa hoàn thành. |
| Nhập file và báo cáo UC 3.4 | Import, danh sách báo cáo, xem tổng hợp và xuất Excel/PDF | Có danh sách, tổng hợp và xuất Excel/PDF thử nghiệm theo chuỗi kỳ; chưa có import | Chưa hoàn thành; route, bộ lọc và response xuất file chưa khớp ApiSpec. |
| Quyền | Các tài liệu liệt kê các vai trò khác nhau, trong đó API xóa đề cập `ROLE_BUSINESS_MANAGER` | Môi trường Development hỗ trợ `ROLE_ADMIN`, `ROLE_DISTRICT_ADMIN`, `ROLE_PARKING_OPERATOR` và phạm vi bãi theo token | Chưa thể xem là khớp chính sách quyền chính thức. |
| Xóa | LLD mô tả xóa vật lý | Theo yêu cầu mới của người dùng, API đánh dấu `daXoa = 1`, ẩn bản ghi và giữ lại dữ liệu | Đã thay đổi so với LLD theo yêu cầu trực tiếp; cần đồng bộ tài liệu khi chốt hợp đồng. |

## Chi tiết những chỗ chưa khớp

1. **Địa chỉ và môi trường:** API kết quả kinh doanh hiện ở `/dev/business-results`, còn ApiSpec yêu cầu `/gdci/{module}/parkingLot/businessOperation`; báo cáo hiện ở `/dev/business-reports`, còn tài liệu yêu cầu `/gdci/{module}/parkingLot/parkingBusinessReport`. Các route hiện chỉ được đăng ký trong Development.
2. **Thêm và sửa:** API thử nghiệm nhận `reportingPeriod`, `revenue`, `completedVehicleCount`, `note`; API thêm còn cần `parkingLotId`. ApiSpec yêu cầu `reportDate`, `periodType`, `totalCheckIn`, `totalCheckOut`, `averageOccupancyRate`, `peakOccupancyRate`, `totalRevenue`, `cashRevenue`, `electronicRevenue`, `operatingExpenses`, `notes` (trong đó chi phí và ghi chú tùy chọn). Sửa không được đổi bãi/ngày/loại kỳ. Các trường chưa hỗ trợ bị từ chối.
3. **Xem kết quả theo thời gian:** ApiSpec yêu cầu `parkingLotId`, `fromDate`, `toDate`, `periodType`, `pageNumber`, `pageSize`, `sortBy` và thứ tự sắp xếp. Danh sách thử nghiệm chưa thực hiện các bộ lọc và phân trang này. Dữ liệu trả về cũng thiếu thông tin bãi dạng object, kỳ trước/tăng trưởng và audit theo mẫu; ApiSpec và LLD mô tả response khác nhau.
4. **Import:** chưa có endpoint. Tài liệu yêu cầu multipart, `.xlsx`/`.xls`/`.csv` tối đa 10 MB, hai chế độ thêm hoặc thêm/cập nhật và kết quả thành công/lỗi từng dòng. File mẫu có 12 cột, gồm những chỉ số bảng Excel hiện chưa mô tả.
5. **Danh sách báo cáo:** endpoint Development yêu cầu chuỗi `reportingPeriod` chính xác, hỗ trợ `parkingLotId`, `pageNumber` và `pageSize`; chưa hỗ trợ `keyword`, `fromDate`, `toDate`, `district`. Response còn phẳng, thiếu object bãi, tên đơn vị, doanh thu bình quân và audit theo ApiSpec. Các tham số chưa hỗ trợ được báo lỗi thay vì bỏ qua.
6. **Tổng hợp báo cáo:** hiện trả tổng doanh thu, tổng lượt hoàn tất và bình quân/lượt theo đúng một kỳ; chưa hỗ trợ lọc ngày/quận, tăng trưởng, top bãi và cơ cấu theo địa bàn như tài liệu. Endpoint không tự tổng hợp lại từ lượt xe thực tế.
7. **Xuất báo cáo:** đã xuất trực tiếp Excel/PDF ở Development với kỳ và bãi; chưa lọc ngày/quận. Tài liệu yêu cầu trả JSON gồm `id`, `fileUrl`, tên file, kích thước, số bản ghi và audit để tải sau. File hiện có mã bãi và các tổng, chưa đầy đủ tên bãi, đơn vị, địa chỉ và chỉ số theo LLD; PDF thử nghiệm cần Windows.
8. **Vai trò:** danh sách/tổng hợp báo cáo hiện cho `ROLE_ADMIN`, `ROLE_DISTRICT_ADMIN`, `ROLE_PARKING_OPERATOR`; xuất chỉ cho `ROLE_ADMIN` và `ROLE_DISTRICT_ADMIN`. ApiSpec báo cáo yêu cầu `ROLE_DISTRICT_ADMIN`. API kết quả dùng chung ba vai trò thử nghiệm, trong khi đặc tả xóa yêu cầu `ROLE_DISTRICT_ADMIN` hoặc `ROLE_BUSINESS_MANAGER`; chính sách chưa trùng.
9. **Lỗi và xóa:** mẫu tài liệu có chỗ nêu HTTP 400/401/403, nhưng cũng quy định HTTP 200; ứng dụng thử nghiệm trả 200/code 7 cho lỗi nghiệp vụ và lỗi xác thực đã xử lý. Chưa xác minh mọi lỗi framework đều như vậy. Xóa mềm là yêu cầu mới của người dùng, khác LLD mô tả xóa vật lý.

Sheet Excel chỉ có một trường tổng số lượt và một trường tổng doanh thu. Nó không cung cấp riêng dữ liệu lượt vào/lượt ra, doanh thu tiền mặt/điện tử, chi phí vận hành hay tỷ lệ lấp đầy. Không thể suy ra chính xác các trường đó từ tổng doanh thu và tổng lượt; cần thống nhất nguồn dữ liệu hoặc thay đổi hợp đồng trước khi triển khai đầy đủ body/response theo tài liệu. `daXoa` là cột bổ sung theo yêu cầu xóa mềm, không có trong sheet gốc.

Để kiểm thử, SQLite cục bộ có thêm ba bảng giá theo Excel và `dev_parking_sessions` chứa các lượt giả lập. Có thể đối soát doanh thu tiền mặt/điện tử của **bộ mẫu** từ bảng thử này; nó không phải nguồn dữ liệu vận hành được phê duyệt và không tự bổ sung chi phí hoặc xác nhận quy tắc tỷ lệ lấp đầy cho production. Chi tiết tại `docs/DEVELOPMENT_PARKING_DATA.md`.

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
| Danh sách và tổng hợp báo cáo Development theo kỳ và bãi | `tests/seeded_parking_api_checks.ps1`: tổng, lọc, quyền bãi và xóa mềm trên SQLite tạm | `[Verified]` cho phạm vi thử nghiệm; chưa khớp đầy đủ UC 3.4 |
| Xuất Excel thử nghiệm theo kỳ và phạm vi bãi | `tests/seeded_parking_api_checks.ps1` kiểm tra file thực tế, quyền, số liệu và xóa mềm | `[Verified]` cho phạm vi Development; chưa khớp đầy đủ ApiSpec |
| Xuất PDF thử nghiệm theo kỳ và phạm vi bãi | `tests/seeded_parking_api_checks.ps1`: đọc nội dung file, số liệu và quyền theo bãi | `[Verified]` nội dung trên Windows; chưa kiểm tra trực quan hoặc khớp đầy đủ ApiSpec |
| Import UC 1.5 | Chưa được triển khai | `[Blocked / Not Verified]` |
