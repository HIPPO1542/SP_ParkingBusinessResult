# SP_ParkingBusinessResult

Ứng dụng quản lý kết quả kinh doanh bãi đỗ xe, nhập lượt xe từ Excel, thống kê doanh thu và xuất báo cáo. Backend dùng .NET 10, Minimal API, Clean Architecture, DDD và CQRS; dữ liệu lưu trong SQLite. Giao diện HTML được phục vụ cùng API.

## Chạy chương trình

Trên máy đã có database, tài khoản và cấu hình khóa ký trong User Secrets:

```powershell
.\start.ps1
```

Mở <http://localhost:5069/> và đăng nhập bằng tài khoản hiện có. Script chạy **Release / Production**, đọc database hiện có, không seed hay reset dữ liệu. Có thể dùng `.\start.ps1 -Port 5070` khi cần đổi cổng. Trong VS Code, chọn **Debug Parking API** rồi nhấn **F5**; cấu hình debug cũng chạy môi trường Production.

Lệnh trực tiếp với profile đã cấu hình:

```powershell
dotnet run --project Backend/ParkingBusinessResult.Api --launch-profile http
```

Profile `http`/`https` cũng dùng Production và cho phép đọc User Secrets trên máy. Debug/Release là cấu hình biên dịch, khác với tên môi trường Production/Development.

Trên máy mới, cần database đã áp dụng migration và cấu hình `Authentication:Issuer`, `Authentication:Audience`, `Authentication:SigningKey` tối thiểu 32 byte. Khóa không nằm trong repository. `Authentication:BootstrapUsername` và `Authentication:BootstrapPassword` chỉ tạo admin khi chưa có tài khoản; không đổi mật khẩu tài khoản đã tồn tại. Mật khẩu bootstrap dài 12–256 ký tự. Cấu hình bằng biến môi trường hoặc `dotnet user-secrets set --project Backend/ParkingBusinessResult.Api <key> <value>`; không ghi khóa/mật khẩu thật vào tài liệu hay Git.

## Kiến trúc

```text
Frontend/                               # Giao diện đăng nhập, import, thống kê và danh sách
Backend/
├── ParkingBusinessResult.Api/
│   ├── Endpoint/                       # Một endpoint cho mỗi route nghiệp vụ
│   ├── Contracts/                      # ApiResponse: code, message, data
│   ├── Extensions/                     # DI, xác thực, cấu hình ứng dụng
│   └── Middlewares/                    # Lỗi tập trung, request ID, log
├── ParkingBusinessResult.Application/
│   ├── Common/                         # Thành phần dùng chung toàn Application
│   ├── DTOs/
│   └── Features/
│       ├── BusinessResults/            # CRUD kết quả tổng hợp
│       ├── BusinessReports/            # Query báo cáo; Command tạo file lưu
│       ├── ParkingBookings/            # Command import; Query danh sách/thống kê
│       └── LocalAccounts/              # Đăng nhập và quản lý tài khoản cục bộ
├── ParkingBusinessResult.Domain/       # Entity, value object, luật miền, hợp đồng repository
└── ParkingBusinessResult.Infrastructure/
    ├── Repositories/                   # Hiện thực đọc/ghi SQLite và đọc Excel
    ├── Reports/                        # Tạo và lưu file Excel/PDF
    └── Data/                           # Kiểm tra storage sẵn sàng
database/sqlite/                        # Schema, migration và script dữ liệu mẫu
tests/                                  # Kiểm tra bằng database tạm
```

Luồng: `Endpoint → Command/Query → Handler → interface → Infrastructure → SQLite/file`. Domain không phụ thuộc framework; Application không tham chiếu Infrastructure. Handler được gọi qua DI, không có service nghiệp vụ, MediatR hay EF Core. Quy tắc không âm nằm trong entity; tính bình quân và kỳ báo cáo nằm trong Domain.

## Các chức năng

- Thêm, sửa, xem, liệt kê và xóa mềm kết quả tổng hợp trong `SP_ketQuaKinhDoanhBaiDoXe`; ghi thông tin người tạo/cập nhật từ token.
- Import `.xlsx`, `.xls`, `.csv` tối đa 10 MB / 50.000 dòng vào `SP_datTruocChoDoXe` theo sheet `SP_ParkingBooking`. Trùng bãi + biển số + giờ vào là lỗi, không cập nhật. Dòng hợp lệ ghi chung một giao dịch; lỗi lưu trữ hoặc hủy yêu cầu trước commit sẽ rollback batch.
- Danh sách lượt xe kèm mã/tên bãi, kết hợp lọc bãi, loại xe, đã ra/chưa ra và ngày/tháng/năm theo giờ vào hoặc giờ ra. Lọc quyền và phân trang thực hiện ở SQLite.
- Thống kê booking theo khoảng ngày và một/nhiều bãi: tiền đã thu, lượt hoàn tất, bình quân, tăng trưởng khi đủ dữ liệu, top bãi và cơ cấu địa bàn từ địa chỉ.
- Danh sách/tổng hợp báo cáo theo kỳ từ bảng kết quả kinh doanh; xuất Excel/PDF, trả metadata và URL tải file riêng của người xuất. Xuất từ booking trả file trực tiếp. PDF cần Windows và Arial.
- Đối chiếu tiền thu với bảng giá khi có đủ điều kiện; không tự đoán quy tắc qua ngày, giờ lẻ hay ngày lễ.
- Đăng nhập qua `/auth/login`, tạo tài khoản qua `/auth/users` bằng quyền admin. Mật khẩu băm, JWT có hạn 30 phút, giới hạn đăng nhập 5 lần/phút/IP trong tiến trình. Chế độ Authority/SSO là lựa chọn khi tích hợp, không bắt buộc chạy ứng dụng.

Một xe đã vào rồi ra là một lượt hoàn tất. `thoiGianBatDau`/`thoiGianKetThuc` là giờ vào/ra; `tongTien` là tiền đã thu. Không quản lý phương thức thanh toán. Booking và kết quả tổng hợp là hai nguồn độc lập, không tự đồng bộ hay cộng lẫn. Dữ liệu `dev_parking_sessions` và bảng giá mẫu chỉ dùng minh họa, không phải dữ liệu thật.

API JSON dùng `{ "code": 0, "message": "Thành công", "data": ... }`; lỗi dùng HTTP 200, code 7, data null theo hợp đồng GDCI. `X-Request-Id` liên kết log. `/health/live` và `/health/ready` là route vận hành riêng, readiness trả 503 khi storage không sẵn sàng.

## Tài liệu và kiểm tra

- [Kiến trúc, cấu hình triển khai và giới hạn vận hành](docs/ENTERPRISE_DEPLOYMENT.md).
- [Giải thích từng file](GIAI_THICH_TUNG_FILE.md), [tổng quan dự án](TONG_QUAN_DU_AN.md).
- [File import và nghiệp vụ booking](docs/PARKING_BOOKING_IMPORT_DESIGN.md), [báo cáo tổng hợp](docs/DEVELOPMENT_BUSINESS_REPORTS.md).
- [Các khác biệt còn lại với ApiSpec/LLD](docs/API_CONTRACT_REVIEW.md), [Postman](postman/README.md).

```powershell
dotnet build SP_ParkingBusinessResult.sln -c Release
dotnet run --project tests/RevenueMetrics.Checks -c Release
dotnet run --project tests/Enterprise.Checks -c Release -- .
```

Workflow `.github/workflows/ci.yml` chạy kiểm tra kiến trúc, migration, quyền, CRUD, import, báo cáo và Production trên database tạm. Không đưa database vận hành, khóa ký hay file xuất vào Git. Chưa có chứng nhận nghiệm thu toàn bộ hợp đồng, kiểm thử tải hoặc SSO thực; các giới hạn được ghi rõ trong tài liệu triển khai.
