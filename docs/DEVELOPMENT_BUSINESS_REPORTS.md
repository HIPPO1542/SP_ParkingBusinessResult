# Báo cáo kết quả kinh doanh thử nghiệm

Sau khi chạy API Development và đăng nhập như trong README, gửi token Bearer tới:

```http
GET /dev/business-reports?reportingPeriod=2026-11
GET /dev/business-reports/summary?reportingPeriod=2026-11
```

Thêm `&parkingLotId=<UUID>` để chỉ xem một bãi. `reportingPeriod` bắt buộc và được so khớp chính xác với `kyBaoCao` trong database. Danh sách hỗ trợ `pageNumber` (từ 1, mặc định 1) và `pageSize` (1–100, mặc định 10), ví dụ `&pageNumber=2&pageSize=10`. Bộ lọc chưa hỗ trợ bị từ chối. Kết quả danh sách chỉ gồm các bản ghi chưa xóa mà tài khoản được phép xem. Tổng hợp tính `totalRevenue`, `totalVehicleCount` (lượt đã hoàn tất) và `averageRevenuePerVehicle`; kỳ không có dữ liệu cho kết quả bằng 0. Chỉ tổng hợp cùng một chuỗi kỳ để tránh cộng trùng các kỳ tháng, quý và năm chồng nhau.

Với dữ liệu giả lập ban đầu kỳ `2026-11`, tổng là 2.775.000 đồng và 150 lượt, bình quân 18.500 đồng/lượt. Kết quả thay đổi khi dữ liệu kết quả kinh doanh được sửa hoặc xóa mềm. Những bản ghi này là số liệu tổng hợp đã lưu, không tự cập nhật theo các lượt xe thử nghiệm hay thay đổi bảng giá.

## Xuất Excel và PDF

Gửi `POST /dev/business-reports/export` với Bearer token của `ROLE_ADMIN` hoặc `ROLE_DISTRICT_ADMIN` và body:

```json
{
  "fileFormat": "EXCEL",
  "reportingPeriod": "2026-11",
  "parkingLotId": null,
  "reportTitle": "Báo cáo kết quả kinh doanh"
}
```

`parkingLotId` và `reportTitle` không bắt buộc; `reportingPeriod` phải khớp chính xác kỳ đã lưu. Đổi `fileFormat` thành `PDF` để xuất PDF. Thành công trả trực tiếp file (trong Postman chọn **Send and Download**); lỗi nghiệp vụ trả JSON `code: 7`. File chứa mã bãi, kỳ, doanh thu, số lượt hoàn tất và doanh thu bình quân; các giá trị tổng ở dòng cuối. File chỉ chứa bãi trong phạm vi của tài khoản, bỏ bản ghi đã xóa mềm. Tiêu đề Excel được ghi thành văn bản, không chạy như công thức.

Các endpoint này chỉ chạy Development và chưa phải hợp đồng chính thức UC 3.4. PDF thử nghiệm hiện cần Windows và font Arial. Đặc tả yêu cầu tạo file, trả thông tin cùng URL tải; endpoint thử nghiệm trả file trực tiếp. Chưa hỗ trợ lọc ngày/quận, tìm kiếm, tăng trưởng và các chỉ số khác theo tài liệu; các tham số xuất chưa hỗ trợ bị từ chối. Chưa có import file. Không dùng các lượt thử nghiệm để coi là dữ liệu vận hành thực tế.
