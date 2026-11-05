# Báo cáo kết quả kinh doanh thử nghiệm

Sau khi chạy API Development và đăng nhập như trong README, gửi token Bearer tới:

```http
GET /dev/business-reports?reportingPeriod=2026-11
GET /dev/business-reports/summary?reportingPeriod=2026-11
```

Thêm `&parkingLotId=<UUID>` để chỉ xem một bãi. `reportingPeriod` bắt buộc và được so khớp chính xác với `kyBaoCao` trong database. Kết quả danh sách chỉ gồm các bản ghi chưa xóa mà tài khoản được phép xem. Tổng hợp tính `totalRevenue`, `totalVehicleCount` (lượt đã hoàn tất) và `averageRevenuePerVehicle`; kỳ không có dữ liệu cho kết quả bằng 0. Chỉ tổng hợp cùng một chuỗi kỳ để tránh cộng trùng các kỳ tháng, quý và năm chồng nhau.

Với dữ liệu giả lập ban đầu kỳ `2026-11`, tổng là 2.775.000 đồng và 150 lượt, bình quân 18.500 đồng/lượt. Kết quả thay đổi khi dữ liệu kết quả kinh doanh được sửa hoặc xóa mềm. Những bản ghi này là số liệu tổng hợp đã lưu, không tự cập nhật theo các lượt xe thử nghiệm hay thay đổi bảng giá.

Hai endpoint này chỉ chạy Development và chưa phải hợp đồng chính thức UC 3.4: chưa có lọc ngày/quận, tìm kiếm, phân trang, tăng trưởng, xuất Excel/PDF và các chỉ số khác theo tài liệu. Chưa có import file. Không dùng các lượt thử nghiệm để coi là dữ liệu vận hành thực tế.
