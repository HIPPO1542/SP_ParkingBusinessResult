# Dữ liệu bãi xe và giá dùng để phát triển

`database/sqlite/seed_development_parking_data.py` chỉ dành cho database phát triển được chỉ định rõ. Script tạo 5 bãi DEMO-01 đến DEMO-05, 5 đơn vị quản lý, 5 khung giá, 20 quy tắc giá, 5 bản áp dụng giá và các kết quả tổng hợp mẫu. Đây là dữ liệu giả lập, không phải giá hoặc số liệu vận hành thực tế.

Script không tạo bảng hoặc ghi dòng vào `dev_parking_sessions`, không tạo hay sửa booking trong `SP_datTruocChoDoXe`. Bảng giá đã tồn tại sẽ khiến script dừng, không ghi đè. Script backup trước khi ghi và rollback nếu gặp lỗi. Không chạy seed trên database production.

## Kết quả tổng hợp mẫu

Mỗi bãi có một kết quả mẫu với 30 lượt; tiền minh họa lấy từ các mức giá ngày thường/cuối tuần của ngày trước mốc `--as-of`. Với mốc 2026-11-05, mỗi bãi có 555.000 đồng, toàn bộ 5 bãi là 2.775.000 đồng. Đây là fixture độc lập cho báo cáo kết quả kinh doanh; không tổng hợp từ booking và không có dữ liệu lượt xe đi kèm.

## Các mức giá mẫu được giữ lại

| Loại xe | Loại ngày | Giờ đầu | Giờ tiếp theo |
|---|---|---:|---:|
| MOTORBIKE | WEEKDAY | 5.000 | 2.000 |
| MOTORBIKE | WEEKEND | 7.000 | 3.000 |
| CAR_9_SEATS | WEEKDAY | 20.000 | 10.000 |
| CAR_9_SEATS | WEEKEND | 25.000 | 12.000 |

Các bảng `SP_khungGiaThuPhi`, `SP_chiTietMucThuKhungGia`, `SP_apDungKhungGia`, đơn vị quản lý, bãi DEMO và kết quả tổng hợp mẫu không bị xóa trong lần đơn giản hóa lượt xe này. Tọa độ và mã quy hoạch của bãi mẫu vẫn là giá trị giả lập, không dùng cho GIS thực tế.

## Bảng lượt xe cũ

API và nghiệp vụ lượt xe dùng `SP_datTruocChoDoXe`. `DevParkingSessions`, DbSet, configuration và các navigation tương ứng đã được gỡ khỏi model hiện tại. SQL `005_dev_parking_sessions.sql` và các migration cũ được giữ nguyên làm lịch sử, không còn được seed gọi để tạo bảng.

Migration mới `RemoveLegacyParkingSessions` chỉ xóa `dev_parking_sessions`. Chưa áp dụng lên database hiện có: dữ liệu lượt cũ vẫn còn vật lý. Cần phê duyệt riêng và backup trước khi áp dụng vì các dòng trong bảng cũ sẽ mất; rollback chỉ tái tạo schema, không khôi phục dữ liệu. Không chuyển các lượt giả lập này vào booking.
