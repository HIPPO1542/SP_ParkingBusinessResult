# Gọi API bằng Postman

1. Chạy `.\start.ps1` từ thư mục gốc dự án.
2. Import duy nhất `ParkingBusinessResult.postman_collection.json` vào Postman.
3. Mở **Variables** của collection: đặt `baseUrl` (mặc định `http://localhost:5069`), `adminUsername`, `adminPassword` bằng tài khoản hiện có. Gửi **Đăng nhập quản trị viên**; token tự lưu. Chế độ Authority dùng token của IdP trong `accessToken`.
4. Gửi **Lấy bãi xe và loại xe**. Nếu `parkingLotId` trống, collection tự lấy ID bãi đầu tiên. Có thể thay bằng ID khác từ response.
5. Chọn từng request cần kiểm tra rồi bấm **Send**. Có 6 nhóm: xác thực, booking, CRUD kết quả tổng hợp, báo cáo tổng hợp, tải file và luồng khách đặt chỗ/nhân viên nhận trả xe.

**Import:** vào **Body → form-data → file**, chọn Excel/CSV. Dùng các mẫu tại `outputs/import-20261106/`. Dòng hợp lệ lưu vào database; trùng bãi + biển số + giờ vào trả lỗi, không cập nhật. `bookingId` tự lưu sau import hoặc lấy danh sách.

**Danh sách:** mặc định 10 dòng/trang; xem `totalCount`, tăng `pageNumber` hoặc `pageSize` (tối đa 100). Request lọc kết hợp có bãi, loại xe, trạng thái và ngày/tháng/năm; bỏ tham số không cần dùng.

**Thống kê:** đặt `reportingPeriod` theo `yyyy-MM` hoặc dùng `fromDate`/`toDate` theo `yyyy-MM-dd`, không gửi cả hai cách trong cùng request. Nhiều bãi: thêm các tham số `parkingLotIds` cùng tên, mỗi tham số một ID. Booking và kết quả tổng hợp là hai nguồn riêng, không tự cộng vào nhau.

Request **Thống kê theo loại xe** dùng thêm `vehicleType`. `revenueByVehicleType` trả doanh thu, số lượt và tỷ trọng từng loại; trong từng bãi, tỷ lệ luôn chia cho doanh thu mọi loại xe của bãi cùng kỳ, kể cả khi lọc một loại. Thiếu số tiền để xác định mẫu số thì tỷ lệ trả `null`. Khi xuất booking, có thể thêm `vehicleType` vào body để dùng cùng bộ lọc.

**Xuất:** gửi request Excel/PDF nhận JSON metadata; `fileUrl` tự lưu. Gửi **Tải báo cáo đã xuất** bằng **Send and Download**, dùng token của chính người xuất. Quyền admin/district admin; PDF cần Windows và Arial.

**Ghi dữ liệu:** tạo tài khoản và thêm/sửa/xóa mềm kết quả là các request tùy chọn, tác động vào database đang kết nối. Không chạy toàn bộ collection nếu chỉ muốn xem dữ liệu. Đặt `parkingLotId` thật và kỳ chưa tồn tại trước khi thêm; `resultId` tự lưu. Không có script seed đi kèm.

API JSON dùng HTTP 200; đọc `code` (`0`: thành công, `7`: lỗi) và `message`. Token Local hết hạn sau 30 phút. Body trong collection theo API hiện tại; khác biệt với tài liệu gốc nằm ở [API_CONTRACT_REVIEW.md](../docs/API_CONTRACT_REVIEW.md).

## Khách đặt chỗ và nhân viên nhận/trả xe

Điền `customerUsername`, `customerPassword` rồi chạy nhóm 6: đăng ký khách, đăng nhập khách, lấy bãi/loại xe, đặt chỗ, xem booking của mình. `customerAccessToken` được lưu riêng với `accessToken` nhân viên; `reservationId` tự lưu sau khi đặt. Nếu `expectedArrivalAt` trống, request đặt chỗ tự chọn giờ dự kiến sau một giờ.

Đăng nhập nhân viên/admin ở nhóm 1, dùng `accessToken` đó để check-in và check-out trong nhóm 6. Check-in/out tự lấy giờ máy chủ; check-out gửi `dayType` (`WEEKDAY`, `WEEKEND`, `HOLIDAY`), không nhận số tiền từ client. Thiếu hoặc trùng giá phù hợp thì trả lỗi và chưa hoàn tất booking. Không có chức năng tự tạo giá ngày lễ.

Trên giao diện web, đăng ký/đăng nhập khách để đặt chỗ; đăng xuất rồi đăng nhập nhân viên, bấm Xem danh sách (có thể lọc Đã đặt), Check-in khi xe đến, chọn loại ngày và Check-out khi xe ra. Khách chỉ thấy booking của mình; nhân viên chỉ thao tác tại bãi được phân quyền.

Khách chọn `expectedArrivalAt` và `expectedDepartureAt`; dùng **Tính tiền dự kiến** để xem giá theo ngày thường/cuối tuần trước khi đặt. Hai mốc dự kiến được lưu khi `RESERVED`; check-in thay bằng giờ vào thực tế và xóa giờ ra dự kiến. `estimatedAmount` tách khỏi `amount` (tiền thực tế); chỉ booking hoàn tất mới vào doanh thu.

File xuất booking có lượt hoàn tất (theo giờ ra), lượt chưa hoàn tất (đã check-in/chưa check-out, theo giờ vào trong kỳ), và mỗi loại xe một cột đếm riêng lượt hoàn tất. Booking chưa check-in không được đếm. Bỏ cột bình quân mỗi lượt; hàng Tổng cộng riêng từng cột. Bãi chỉ có xe đang gửi vẫn xuất với doanh thu/lượt hoàn tất bằng 0.

Nhóm 7 dùng để hủy booking chưa check-in: đặt booking mới ở nhóm 6, rồi chọn hủy bằng token khách hoặc token nhân viên phù hợp. Không gửi cả hai request hủy liên tiếp cho cùng booking và mong cả hai thành công. `POST /parkingReservation/{id}/cancel` nhận `{}` hoặc `{"note":"..."}`; ghi chú tùy chọn được nối vào `ghiChu` hiện có, giữ ghi chú đặt chỗ. Khách chỉ hủy booking của mình; nhân viên/quản lý chỉ hủy tại bãi được phân quyền, kể cả booking khách không đến. Trạng thái `CANCELLED` giữ lịch sử, giờ dự kiến, tiền dự kiến và audit cập nhật; không tính vào doanh thu/lượt hoàn tất/xe đang gửi/booking đang chờ. Danh sách nhân viên hỗ trợ `status=CANCELLED`. Không hủy lại, check-in hoặc check-out booking đã hủy.
