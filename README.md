# SP_ParkingBusinessResult

Khung ASP.NET Core theo DDD, CQRS và Clean Architecture. Bốn project nằm trong `Backend/`; dự án dùng .NET 10 SDK hiện có trên máy, phiên bản dùng chung của nhóm cần được xác nhận trước khi tích hợp.

- Domain: mô hình miền và hợp đồng repository.
- Application: command, query và handler theo từng tác vụ.
- Infrastructure: ánh xạ và truy cập dữ liệu.
- Api: Minimal API endpoints, cấu hình xác thực và hợp đồng response.

Các thao tác thử nghiệm trả HTTP 200 với code 0 khi thành công hoặc code 7 khi có lỗi nghiệp vụ; không có metadata. Các lỗi do framework xử lý chưa được chuẩn hóa đầy đủ theo hợp đồng này.

Theo sheet SP_ParkingBusinessResult, tên bảng là SP_ketQuaKinhDoanhBaiDoXe; các cột gồm id, baiDoXeId, kyBaoCao, doanhThu, soLuotXe, idNguoiTao, idNguoiCapNhat, nguoiCapNhat, ngayTao, nguoiTao, ngayCapNhat, ghiChu. SQLite cục bộ là môi trường phát triển, chưa xác minh schema triển khai chung của nhóm.

Cần chốt nguồn dữ liệu cho các chỉ số, quy tắc kỳ báo cáo, cơ chế xác thực, phân quyền và xử lý trùng trong cùng file. Skill đề xuất cột phiên bản để kiểm soát đồng thời, nhưng sheet không có cột này; không tự thay đổi schema.

Chạy `dotnet build SP_ParkingBusinessResult.sln` và `dotnet run --project Backend/ParkingBusinessResult.Api`. API thử nghiệm dưới đây chỉ hoạt động trong môi trường Development; endpoint nghiệp vụ theo ApiSpec, xác thực, MediatR và Swagger chưa được hoàn thiện.

Các Minimal API endpoint tiếp nhận HTTP và gọi trực tiếp handler tương ứng; command xử lý tác vụ ghi, query xử lý tác vụ đọc. Mỗi route có endpoint riêng. Các handler được đăng ký qua dependency injection, chưa dùng MediatR. Không có tầng service nghiệp vụ.

Các kiểu chỉ phục vụ kết quả kinh doanh hoặc tài khoản Development được đặt trong `Common` của từng feature; `Application/Common` dành cho thành phần thực sự dùng chung. API tham chiếu Application cho các handler/DTO và Infrastructure để cấu hình truy cập dữ liệu.

```text
Backend/
├── ParkingBusinessResult.Api/
│   ├── Endpoint/           # Minimal API: mỗi route một endpoint
│   ├── Contracts/          # Envelope phản hồi
│   ├── Extensions/         # Cấu hình DI và xác thực
│   ├── Middlewares/
│   └── Properties/
├── ParkingBusinessResult.Application/
│   ├── Common/
│   ├── DTOs/
│   └── Features/           # Commands, Queries và handler theo chức năng
├── ParkingBusinessResult.Domain/
│   ├── Common/
│   ├── Entities/
│   ├── Enums/
│   ├── Exceptions/
│   ├── Repositories/
│   └── ValueObjects/
└── ParkingBusinessResult.Infrastructure/
    ├── Data/
    ├── EntityConfigs/
    └── Repositories/
```

Các thư mục chưa có thành phần được giữ bằng `.gitkeep`; repository SQLite hiện dùng SQL trực tiếp, chưa dùng EF Core entity configuration. Tên project giữ theo module `ParkingBusinessResult`, chưa đổi thành tên solution chung của nhóm.

Mô hình kết quả kinh doanh sử dụng tổng số lượt xe hoàn tất (một xe vào rồi ra là một lượt). Doanh thu và số lượt không được âm. Phép tổng hợp tính doanh thu bình quân từ tổng doanh thu chia tổng số lượt, làm tròn hai chữ số; khi không có lượt xe, giá trị bằng 0 theo LLD UC 3.4. Kiểm tra các quy tắc này bằng `dotnet run --project tests/RevenueMetrics.Checks`.

Kho dữ liệu tạm `InMemoryBusinessResultRepository` hỗ trợ thêm, lấy chi tiết, liệt kê, cập nhật và xóa, đồng thời từ chối hai bản ghi có cùng bãi xe và cùng chuỗi kỳ báo cáo. Đây là thành phần phục vụ phát triển: dữ liệu mất khi tiến trình dừng. Quy tắc chuẩn hóa chuỗi kỳ và tích hợp CSDL sẽ cần được xác định trước khi dùng thật.

Database SQLite cục bộ nằm tại `database/sqlite/parking_business_result.db` và không được đưa vào Git. Script `database/sqlite/001_create_business_result.sql` tạo bảng kết quả kinh doanh; `002_business_reference_tables.sql` bổ sung các bảng đơn vị quản lý, bãi xe và khóa ngoại từ kết quả kinh doanh đến bãi xe, từ bãi xe đến đơn vị quản lý. Các cột được đối chiếu với ba sheet `SP_ParkingBusinessResult`, `DL_KTHT_ParkingLot` và `SP_ManagementUnit`. Các trường ngày lưu văn bản; SQLite lưu `geo` và ObjType dưới dạng văn bản mà không cung cấp chức năng GIS. Khóa ngoại `quyHoachId` chưa được tạo vì Excel có hai sheet quy hoạch và chưa xác định được đích. Chạy `python tests/sqlite_schema_checks.py` để kiểm tra database; dữ liệu kiểm thử được hoàn tác sau khi chạy.

Database Development còn có ba bảng giá theo Excel và bảng lượt xe giả lập `dev_parking_sessions`. Xem [nguồn gốc, giới hạn và cách truy vấn dữ liệu giả lập](docs/DEVELOPMENT_PARKING_DATA.md); bảng lượt xe này không phải thiết kế production được xác nhận. Các mức giá và biển số mẫu không phải dữ liệu thật.

API Development có thể nhập lượt xe từng dòng từ Excel và thống kê trực tiếp số lượt hoàn tất, tiền thực thu theo tháng. Chi tiết cấu trúc, cách gọi và giới hạn tại [hướng dẫn import lượt xe](docs/PARKING_VISIT_IMPORT_DESIGN.md). Bảng `parking_visit` được bổ sung theo yêu cầu nhập từng lượt, độc lập với dữ liệu giả lập. Trên database đã có, script `database/sqlite/apply_parking_visit_migration.py` sao lưu trước khi tạo bảng; database cục bộ đã được áp dụng.

Migration `003_soft_delete_business_result.sql` bổ sung `daXoa` theo yêu cầu xóa mềm; đây là cột mở rộng không có trong Excel gốc. API đánh dấu bản ghi đã xóa và ẩn khỏi danh sách/chi tiết; bản ghi vẫn còn trong SQLite và kỳ báo cáo có thể được tạo lại. Để cập nhật database đã áp dụng `002`, chạy `python database/sqlite/apply_soft_delete_migration.py`; script sao lưu trước và dừng khi phát hiện schema hoặc quan hệ cần rà soát. Với database mới, áp dụng lần lượt `001`, `002`, `003`. Đối chiếu API với tài liệu tại [`docs/API_CONTRACT_REVIEW.md`](docs/API_CONTRACT_REVIEW.md).

Để cập nhật database chỉ có schema `001`, chạy `python database/sqlite/apply_business_reference_migration.py`. Script tạo bản sao lưu trước khi cập nhật và dừng nếu đã có dữ liệu kết quả kinh doanh, bảng tham chiếu, hoặc chỉ mục/trigger tự tạo; cần bổ sung dữ liệu bãi xe và đối soát thủ công trước khi di chuyển dữ liệu cũ. Chạy `python tests/test_sqlite_migration.py` để kiểm thử migration trên database tạm. Sau khi áp dụng, API từ chối tạo kết quả cho bãi xe chưa tồn tại. Hiện chưa có chức năng nhập dữ liệu bãi xe hoặc đơn vị quản lý trong module này.

Các endpoint `GET/POST /gdci/parking/parkingLot/businessOperation` và `GET/PUT/DELETE /gdci/parking/parkingLot/businessOperation/{id}` dùng SQLite và chỉ đăng ký trong Development. API thử nghiệm kiểm tra JWT HS256 có issuer, audience, thời hạn, chữ ký và `sub`; `ROLE_ADMIN` có thể truy cập mọi bãi, còn `ROLE_DISTRICT_ADMIN` và `ROLE_PARKING_OPERATOR` chỉ truy cập các bãi có ID trong claim `parking_lot_id`. Response lỗi quyền dùng HTTP 200 và code 7. Những vai trò và phạm vi này hiện là cấu hình thử nghiệm, chưa thay thế chính sách quyền được phê duyệt. API chưa tương thích đầy đủ với ApiSpec và không được sử dụng cho production.

Để chạy API thử nghiệm, cấu hình `Authentication:Issuer`, `Authentication:Audience` và `Authentication:SigningKey` (ít nhất 32 byte) qua biến môi trường hoặc nguồn cấu hình bí mật. Không đưa khóa ký thật vào Git. Ứng dụng sẽ không khởi động ở Development nếu thiếu cấu hình. Có thể thay đổi đường dẫn database bằng cấu hình `ParkingBusinessResult:DatabasePath`. Trường `doanhThu` dùng kiểu REAL theo kiểu double của sheet; cần thống nhất lại cách lưu số tiền chính xác trước khi dùng nghiệp vụ thật. Chạy `dotnet run --project tests/RevenueMetrics.Checks` và `powershell -NoProfile -ExecutionPolicy Bypass -File tests/api_sqlite_checks.ps1` để kiểm tra; bài kiểm tra HTTP sử dụng database tạm riêng.

Collection và hướng dẫn kiểm thử bằng Postman nằm trong thư mục [`postman/`](postman/README.md).

Giao diện nằm trong [`Frontend/`](Frontend/README.md). Sau khi chạy API Development theo hướng dẫn bên dưới, mở `http://localhost:5069/` để đăng nhập, nhập file, xem số liệu và xuất báo cáo.

### Tài khoản cục bộ cho Development

Khi chạy lần đầu, đặt `Authentication:BootstrapUsername` và `Authentication:BootstrapPassword` trong cấu hình bí mật hoặc biến môi trường; mật khẩu có từ 12 đến 256 ký tự. Nếu bảng `dev_auth_users` chưa có tài khoản, ứng dụng tạo một `ROLE_ADMIN`; cấu hình bootstrap không thay đổi tài khoản đã tồn tại. Mật khẩu được lưu dưới dạng băm bằng ASP.NET Core Identity PasswordHasher. Bảng tài khoản cục bộ được tạo trong SQLite khi ứng dụng khởi động ở Development.

Ví dụ khởi động cục bộ trong PowerShell (khóa được tạo mới khi chạy lại, do đó token cũ sẽ hết hiệu lực):

```powershell
$env:Authentication__Issuer = 'parking-local'
$env:Authentication__Audience = 'parking-api'
$keyBytes = New-Object byte[] 32
[System.Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($keyBytes)
$env:Authentication__SigningKey = [Convert]::ToBase64String($keyBytes)
$env:Authentication__BootstrapUsername = Read-Host 'Tên quản trị viên'
$bootstrapPassword = Read-Host 'Mật khẩu quản trị viên (ít nhất 12 ký tự)' -AsSecureString
$env:Authentication__BootstrapPassword = [System.Net.NetworkCredential]::new('', $bootstrapPassword).Password
dotnet run --project Backend/ParkingBusinessResult.Api
```

- `POST /dev/auth/login` nhận JSON `username`, `password`; khi thành công trả `data.accessToken` có hiệu lực 30 phút. Giới hạn đăng nhập là 5 lần mỗi phút theo địa chỉ IP, trong bộ nhớ của tiến trình.
- `POST /dev/auth/users` yêu cầu token `ROLE_ADMIN`; JSON gồm `username`, `password`, `role` và `parkingLotId`. Vai trò `ROLE_DISTRICT_ADMIN` và `ROLE_PARKING_OPERATOR` bắt buộc có ID bãi xe. Mỗi tài khoản cục bộ hiện chỉ được gán tối đa một bãi.
- Gửi token bằng header `Authorization: Bearer <accessToken>` khi gọi API nghiệp vụ.

Cơ chế này chỉ dùng phát triển cục bộ: chưa có đổi/khôi phục mật khẩu, khóa tài khoản, thu hồi token hay quản lý nhiều bãi cho một tài khoản. Trước production cần tích hợp hệ thống định danh, kết nối HTTPS và chính sách phân quyền chính thức.
