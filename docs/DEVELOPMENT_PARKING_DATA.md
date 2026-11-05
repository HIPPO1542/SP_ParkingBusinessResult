# Dữ liệu bãi xe và giá dùng để phát triển

Tệp `database/sqlite/seed_development_parking_data.py` tạo dữ liệu **giả lập**, không phải dữ liệu vận hành, biển số hay giá thu phí thực tế. Database cục bộ đã được sao lưu trước khi thêm dữ liệu. Các trường hợp được tạo một lần; script sẽ dừng nếu một trong các bảng giá hoặc bảng lượt thử đã tồn tại, không ghi đè dữ liệu có sẵn.

## Các bảng

- `SP_khungGiaThuPhi`: khung giá gắn với bãi, theo sheet `SP_ParkingPriceFrame`.
- `SP_chiTietMucThuKhungGia`: loại xe, loại ngày, số giờ đầu, giá giờ đầu và giá giờ tiếp theo, theo sheet `SP_ParkingPriceRule`.
- `SP_apDungKhungGia`: thông tin áp dụng khung giá tại bãi, theo sheet `SP260211_ParkingPriceApplicatio`.
- `dev_parking_sessions`: lượt xe giả lập dành riêng cho Development. Bảng này **không có trong Excel** và không phải hợp đồng CSDL production. `exitedAt` bằng `NULL` nghĩa là lượt xe chưa ghi nhận rời bãi.

Ba bảng giá được tạo theo tên bảng/cột trong Excel; các giá trị ObjType và ngày tháng dùng dạng văn bản trong SQLite. Cột `quyHoachId` và tọa độ của bãi là giá trị giả lập để thỏa cấu trúc thử nghiệm; chưa có khóa ngoại quy hoạch và không dùng dữ liệu này cho GIS thực tế.

## Quy mô dữ liệu

Có 5 bãi `DEMO-01` đến `DEMO-05`; mỗi bãi có 50 lượt: 30 lượt đã rời bãi và 20 lượt chưa ghi nhận rời bãi. 150 lượt hoàn tất có thông tin thanh toán tiền mặt hoặc điện tử; 100 lượt đang đỗ không ghi tiền đã thanh toán. Trạng thái này là dữ liệu giả lập tại thời điểm tạo, không phản ánh xe thực tế và không tự thay đổi theo thời gian.

Số liệu trong `SP_ketQuaKinhDoanhBaiDoXe` được tổng hợp theo bãi và tháng từ **các lượt đã hoàn tất**. Mỗi bãi có 30 lượt hoàn tất; số lượt đang đỗ không được tính vào tổng lượt hoàn tất hay doanh thu đã thu.

## Mức giá giả lập

Mỗi bãi có một khung giá mẫu với số giờ đầu là 1. Đây là số tiền tự tạo để kiểm thử, không phải quyết định giá hoặc dữ liệu giá thực tế.

| Loại xe | Loại ngày | Giá 1 giờ đầu | Giá mỗi giờ tiếp theo |
| --- | --- | ---: | ---: |
| Xe máy | Ngày thường | 5.000 đồng | 2.000 đồng |
| Xe máy | Cuối tuần | 7.000 đồng | 3.000 đồng |
| Ô tô đến 9 chỗ | Ngày thường | 20.000 đồng | 10.000 đồng |
| Ô tô đến 9 chỗ | Cuối tuần | 25.000 đồng | 12.000 đồng |

Các lượt đã hoàn tất được dựng với thời gian đỗ tròn 1, 2 hoặc 3 giờ trong cùng một ngày. Tiền mẫu được tính bằng giá giờ đầu cộng giá các giờ tiếp theo, sử dụng đúng quy tắc giá có hiệu lực được gắn với bãi và loại xe. Bộ mẫu chỉ phân biệt ngày thường/cuối tuần; không mô phỏng ngày lễ, làm tròn giờ lẻ, thay đổi giá giữa lượt hoặc các ngoại lệ. Các quy tắc đó cần được xác nhận trước khi tính phí cho nghiệp vụ thật.

## Xem dữ liệu trong SQLite

```sql
-- Mã và ID các bãi giả lập, có thể sao chép ID vào Postman.
SELECT Id, ma, ten FROM DL_KTHT_baiDoXe WHERE ma LIKE 'DEMO-%' ORDER BY ma;

-- Số lượt hoàn tất và số lượt còn trong bãi.
SELECT p.ma, COUNT(*) AS tongLuot,
       SUM(s.exitedAt IS NOT NULL) AS daRoiBai,
       SUM(s.exitedAt IS NULL) AS dangDo
FROM dev_parking_sessions s
JOIN DL_KTHT_baiDoXe p ON p.Id = s.parkingLotId
GROUP BY p.Id, p.ma ORDER BY p.ma;

-- Doanh thu đã thu và số lượt hoàn tất theo bảng kết quả kinh doanh.
SELECT p.ma, r.kyBaoCao, r.soLuotXe, r.doanhThu
FROM SP_ketQuaKinhDoanhBaiDoXe r
JOIN DL_KTHT_baiDoXe p ON p.Id = r.baiDoXeId
WHERE r.daXoa = 0 AND p.ma LIKE 'DEMO-%'
ORDER BY p.ma, r.kyBaoCao;

-- Đối chiếu từng khoản đã thu với quy tắc giá được dùng.
SELECT p.ma, s.testPlate, s.enteredAt, s.exitedAt, s.chargedAmount,
       r.soGioDau, r.giaGioDau, r.giaGioTiepTheo
FROM dev_parking_sessions s
JOIN DL_KTHT_baiDoXe p ON p.Id = s.parkingLotId
JOIN SP_chiTietMucThuKhungGia r ON r.id = s.priceRuleId
WHERE s.exitedAt IS NOT NULL
ORDER BY p.ma, s.testPlate;
```

Endpoint kết quả kinh doanh hiện có có thể đọc các bản tổng hợp sau khi đăng nhập bằng tài khoản có quyền. **Chưa có API đọc từng lượt giả lập, API tính phí, API import file hay API báo cáo UC 3.4.** Các dữ liệu giả lập không tự động làm các API đó hoàn thành.
