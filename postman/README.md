# Gọi API bằng Postman

1. Chạy `.\start.ps1` từ thư mục gốc dự án.
2. Import duy nhất `ParkingBusinessResult.postman_collection.json` vào Postman.
3. Mở **Variables** của collection: đặt `baseUrl` (mặc định `http://localhost:5069`), `adminUsername`, `adminPassword` bằng tài khoản hiện có. Gửi **Đăng nhập quản trị viên**; token tự lưu. Chế độ Authority dùng token của IdP trong `accessToken`.
4. Gửi **Lấy bãi xe và loại xe**. Nếu `parkingLotId` trống, collection tự lấy ID bãi đầu tiên. Có thể thay bằng ID khác từ response.
5. Chọn từng request cần kiểm tra rồi bấm **Send**. Có 5 nhóm: xác thực, booking, CRUD kết quả tổng hợp, báo cáo tổng hợp và tải file.

**Import:** vào **Body → form-data → file**, chọn Excel/CSV. Dùng các mẫu tại `outputs/import-20261106/`. Dòng hợp lệ lưu vào database; trùng bãi + biển số + giờ vào trả lỗi, không cập nhật. `bookingId` tự lưu sau import hoặc lấy danh sách.

**Danh sách:** mặc định 10 dòng/trang; xem `totalCount`, tăng `pageNumber` hoặc `pageSize` (tối đa 100). Request lọc kết hợp có bãi, loại xe, trạng thái và ngày/tháng/năm; bỏ tham số không cần dùng.

**Thống kê:** đặt `reportingPeriod` theo `yyyy-MM` hoặc dùng `fromDate`/`toDate` theo `yyyy-MM-dd`, không gửi cả hai cách trong cùng request. Nhiều bãi: thêm các tham số `parkingLotIds` cùng tên, mỗi tham số một ID. Booking và kết quả tổng hợp là hai nguồn riêng, không tự cộng vào nhau.

Request **Thống kê theo loại xe** dùng thêm `vehicleType`. `revenueByVehicleType` trả doanh thu, số lượt và tỷ trọng từng loại; trong từng bãi, tỷ lệ luôn chia cho doanh thu mọi loại xe của bãi cùng kỳ, kể cả khi lọc một loại. Thiếu số tiền để xác định mẫu số thì tỷ lệ trả `null`. Khi xuất booking, có thể thêm `vehicleType` vào body để dùng cùng bộ lọc.

**Xuất:** gửi request Excel/PDF nhận JSON metadata; `fileUrl` tự lưu. Gửi **Tải báo cáo đã xuất** bằng **Send and Download**, dùng token của chính người xuất. Quyền admin/district admin; PDF cần Windows và Arial.

**Ghi dữ liệu:** tạo tài khoản và thêm/sửa/xóa mềm kết quả là các request tùy chọn, tác động vào database đang kết nối. Không chạy toàn bộ collection nếu chỉ muốn xem dữ liệu. Đặt `parkingLotId` thật và kỳ chưa tồn tại trước khi thêm; `resultId` tự lưu. Không có script seed đi kèm.

API JSON dùng HTTP 200; đọc `code` (`0`: thành công, `7`: lỗi) và `message`. Token Local hết hạn sau 30 phút. Body trong collection theo API hiện tại; khác biệt với tài liệu gốc nằm ở [API_CONTRACT_REVIEW.md](../docs/API_CONTRACT_REVIEW.md).
