# Đối chiếu hợp đồng API và các yêu cầu đã chốt

Tài liệu này phân biệt nâng cấp chất lượng source với việc khớp toàn bộ ApiSpec/LLD gốc. Nâng cấp kiến trúc không tự thay đổi các quyết định nghiệp vụ người dùng đã xác nhận. API, giao diện và đăng nhập cục bộ hiện chạy được ở **Production**. Xem [cấu hình triển khai](ENTERPRISE_DEPLOYMENT.md).

| Hạng mục | Hiện tại | Phạm vi / khác biệt |
|---|---|---|
| Route kết quả tổng hợp | `/gdci/parking/parkingLot/businessOperation` và `/{id}` | Theo ví dụ module `parking`; CRUD chạy mọi môi trường |
| Body thêm/sửa kết quả | `parkingLotId`, `reportingPeriod`, `revenue`, `completedVehicleCount`, `note`; sửa không đổi bãi/kỳ | Chưa phải body có `reportDate`, `periodType`, các chỉ số lấp đầy, tiền mặt/điện tử và chi phí trong tài liệu gốc. Trường lạ bị từ chối, không ghi thành 0 |
| Response CRUD kết quả | Entity phẳng với id, bãi, kỳ, doanh thu, lượt và ghi chú | Khác mẫu CRUD có nhiều chỉ số bổ sung. Không nhầm với response danh sách báo cáo ở hàng dưới |
| Danh sách báo cáo UC 3.4 | `/parkingBusinessReport`: bãi dạng object, đơn vị, doanh thu, lượt, bình quân và audit | Đã có cấu trúc người dùng cung cấp. Lọc kỳ chính xác, bãi, từ khóa/địa chỉ, district và phân trang; chưa lọc khoảng ngày trên nguồn tổng hợp |
| Tổng hợp báo cáo UC 3.4 | `/parkingBusinessReport/summary`: tổng, bình quân/lượt, tăng trưởng, bình quân/ngày, top bãi, cơ cấu địa bàn | Tăng trưởng chỉ có khi đủ dữ liệu đối chiếu cùng tập bãi; district suy từ địa chỉ, không phải mã địa bàn chính thức |
| Xuất báo cáo UC 3.4 | `/parkingBusinessReport/export`: JSON metadata gồm id, URL, tên/định dạng/kích thước, số bản ghi và audit | Đã trả mẫu người dùng yêu cầu. File có mã/tên bãi. Chủ sở hữu tải bằng token qua URL riêng; PDF cần Windows |
| Import | `/businessOperation/import`: nhận file từng lượt xe; trả totalRows, successRows, errorRows, importedIds, errors có parkingLotCode; thêm duplicateRows và importedRows để giao diện hiển thị | Theo yêu cầu mới của người dùng, khác import tổng hợp 12 cột và hai chế độ trong tài liệu gốc |
| Booking | `/parkingBooking`: danh sách, filters, summary, export và đối chiếu bảng giá | Bảng `SP_datTruocChoDoXe` theo sheet `SP_ParkingBooking`; giờ bắt đầu/kết thúc là vào/ra theo xác nhận |
| Xóa | Xóa mềm kết quả tổng hợp, giữ bản ghi và audit | Yêu cầu trực tiếp của người dùng, khác LLD mô tả xóa vật lý |
| Quyền | Admin mọi bãi; district admin/operator theo claim parking_lot_id; xuất chỉ admin/district admin | Chính sách hiện có. Các tài liệu gốc không thống nhất vai trò, có chỗ yêu cầu ROLE_BUSINESS_MANAGER; cần đồng bộ hợp đồng khi tích hợp nhóm |
| Xác thực | Local JWT cho ứng dụng độc lập hoặc Authority JWT khi chọn SSO | Local chạy Production, dùng lại tài khoản trong database. Authority chưa xác minh với IdP thực |
| Lỗi | HTTP 200, code 7, data null cho lỗi nghiệp vụ, xác thực, bind request và lỗi trong pipeline đã kiểm tra | Không bao gồm lỗi kết nối/proxy/Kestrel trước pipeline, hoặc response đã gửi dở; health readiness dùng 503 riêng |

## Quyết định nghiệp vụ được giữ lại

- Một dòng Excel là một lượt xe. Cùng bãi + biển số chuẩn hóa + giờ vào là trùng: báo lỗi và không cập nhật, kể cả trùng trong cùng file.
- Lượt hoàn tất cần có giờ ra. `tongTien` là số tiền đã thu; không tự tính lại doanh thu theo bảng giá và không quản lý phương thức thanh toán.
- Giờ ra được phép trống để thể hiện xe còn trong bãi, khác đánh dấu bắt buộc trong Excel gốc. Bảng booking vẫn giữ 16 cột đã chốt.
- Booking và bảng kết quả tổng hợp không tự đồng bộ. Không cộng chồng hai nguồn khi báo cáo.
- Dòng sai nghiệp vụ trả lỗi riêng. Lỗi lưu trữ/hủy yêu cầu trước commit làm rollback batch; không giữ một phần batch rồi báo thất bại toàn bộ.

Chi phí vận hành, doanh thu tiền mặt/điện tử và tỷ lệ lấp đầy không có nguồn đầy đủ trong các bảng đã chốt. Không thể suy ra các giá trị đó chỉ từ tổng doanh thu/số lượt. Nâng cấp source không tự tạo cột hoặc số liệu giả cho các trường này. `daXoa` là phần mở rộng đã được người dùng yêu cầu trước đó.

## Ma trận bằng chứng

| Yêu cầu | Bằng chứng | Trạng thái |
|---|---|---|
| Quy tắc tính, CRUD, audit, xóa mềm | `tests/RevenueMetrics.Checks`, `tests/api_sqlite_checks.ps1` | Verified cục bộ |
| Migration bảo toàn dữ liệu và sao lưu | `test_sqlite_migration.py`, `test_soft_delete_migration.py`, `test_parking_visit_migration.py`, `test_booking_migration.py` | Kiểm tra trên database tạm |
| Danh sách, tổng hợp, Excel/PDF, chủ file và quyền bãi | `tests/seeded_parking_api_checks.ps1` | Verified cục bộ trên Windows |
| Import, trùng, lọc kết hợp, thống kê, xuất booking | `tests/parking_booking_import_checks.ps1` | Verified cục bộ; XLS chỉ chạy khi có xlwt |
| Giao dịch rollback và hủy import | `tests/Enterprise.Checks` | Verified cục bộ |
| Giao diện/đăng nhập/API Production, lỗi framework và health | `tests/runtime_checks.ps1` | Verified cục bộ bằng database tạm |
| Ranh giới Clean Architecture | `tests/test_architecture.py` | Verified cục bộ |
| Toàn bộ hợp đồng gốc, quyền theo tổ chức, SSO và tải thực | Cần nguồn dữ liệu và môi trường tích hợp phù hợp | Chưa xác minh |
