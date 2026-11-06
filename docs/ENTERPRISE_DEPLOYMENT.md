# Kiến trúc và triển khai

Bản nâng cấp giữ Clean Architecture, DDD và CQRS của module. Không đổi tên bảng, thêm cột phiên bản hay thay đổi quy tắc tính tiền để phục vụ việc tổ chức source.

## Chiều phụ thuộc và luồng xử lý

```text
Api ──────────> Application ────────> Domain
 │                  ▲
 └─ cấu hình DI ─> Infrastructure
```

Infrastructure tham chiếu Application và dùng các kiểu Domain qua tham chiếu bắc cầu. Domain không phụ thuộc framework. Endpoint chỉ gọi handler, không gọi bộ tạo Excel/PDF hoặc repository SQLite trực tiếp. Kiểm tra kiến trúc tự động trước đây được lưu trong lịch sử Git tại tag `vesion-3`.

Luồng đọc danh sách:

```text
ListBookingsEndpoint
 → ListBookingsQuery → ListBookingsHandler
 → IBookingListRepository → BookingQueryRepository
 → SELECT có điều kiện quyền, bộ lọc, COUNT và LIMIT/OFFSET
 → BookingPage → ApiResponse → giao diện
```

Luồng ghi import:

```text
ImportBookingsEndpoint
 → ImportBookingsCommand → ImportBookingsHandler
 → IBookingSpreadsheetReader → kiểm tra từng dòng
 → IBookingImportSession → INSERT trong một giao dịch
 → Commit → kết quả từng dòng → ApiResponse → giao diện
```

Xuất báo cáo tổng hợp là `Commands/Export` vì tạo file lưu trên máy chủ. Tải file là `Queries/Download`. Xuất thống kê booking vẫn là Query vì chỉ tạo nội dung trả về, không lưu thêm dữ liệu. Handler dùng `IReportDocumentWriter` và `IReportExportStore`; implementation nằm trong Infrastructure.

Không cần MediatR để thực hiện CQRS. Không thêm event bus, Outbox hoặc Unit of Work tổng quát khi module chưa có nghiệp vụ cần chúng.

## Giao dịch và dữ liệu

- Một dòng import tương ứng một lượt xe trong `SP_datTruocChoDoXe`.
- Cùng bãi, biển số đã chuẩn hóa và giờ vào là trùng: trả lỗi dòng, không ghi đè. Unique constraint bảo vệ cả khi có hai yêu cầu import cùng lúc.
- Một file là một batch import. Đây là ngoại lệ có chủ đích so với giao dịch ghi một aggregate: dòng sai nghiệp vụ bị bỏ qua; toàn bộ dòng hợp lệ chỉ commit cùng nhau. Lỗi lưu trữ hoặc hủy yêu cầu trước commit làm rollback các dòng đang ghi. Không trả danh sách ID thành công cho batch đã rollback.
- Đọc file xong mới mở giao dịch, tái sử dụng một connection và tra cứu bãi có cache trong phạm vi file. Không mở connection mới cho từng dòng.
- Thông tin người tạo/cập nhật lấy từ token, không lấy từ body. Import và CRUD kết quả kinh doanh ghi audit vào các cột đã có; sửa/xóa mềm giữ nguyên thông tin người tạo.
- `tongTien` là tiền đã thu. Chỉ lượt có giờ ra được tính hoàn tất. Không quản lý phương thức thanh toán. Bảng booking và bảng kết quả tổng hợp không tự đồng bộ với nhau.
- Danh sách booking lọc quyền và phân trang ở SQLite qua EF Core. COUNT và dữ liệu trang chạy trong một giao dịch đọc ngắn để cùng thấy một snapshot. Thống kê chỉ tải các lượt trong khoảng thời gian và phạm vi bãi được phép. Mapping và migrations nằm trong Infrastructure/Persistence; xem [EF_CORE.md](EF_CORE.md).
- Không thêm `rowVersion` vì schema chưa có trường này. CRUD hiện chưa phát hiện mọi trường hợp người dùng sửa trên dữ liệu cũ; nếu nhóm yêu cầu optimistic concurrency cần chốt thêm hợp đồng request/schema.

## Xác thực theo môi trường

| Chế độ xác thực | Cấu hình | Route nghiệp vụ | Đăng nhập cục bộ và giao diện `/` |
|---|---|---|---|
| Local (mặc định, mọi môi trường kể cả Production) | JWT HS256: Issuer, Audience, SigningKey tối thiểu 32 byte | Có | Có: `/auth/login`, `/auth/users`, `/` |
| Authority (tùy chọn khi tích hợp SSO) | JWT qua HTTPS Authority, xác minh issuer, audience, chữ ký và hạn token | Có | Không; frontend SSO tích hợp riêng |

Ứng dụng độc lập chạy Production bằng `.\start.ps1`; dùng lại tài khoản trong SQLite, không reset dữ liệu. Profile `http`/`https` và cấu hình F5 cũng đã chọn Production. Chế độ Local cần Issuer, Audience và khóa ký; không có khóa mặc định trong source. `Authentication:UseUserSecrets=true` cho phép chủ động đọc kho bí mật của tài khoản Windows trên máy này. Biến môi trường và tham số dòng lệnh được ưu tiên hơn User Secrets. Máy triển khai khác có thể cấp khóa bằng secret provider hoặc biến môi trường.

SSO không phải điều kiện bắt buộc để chạy. Khi nhóm chọn SSO, cấu hình:

```text
ASPNETCORE_ENVIRONMENT=Production
Authentication__Mode=Authority
Authentication__Authority=https://<identity-provider-cua-nhom>
Authentication__Audience=<audience-api-da-dang-ky>
Authentication__RoleClaimType=role
ParkingBusinessResult__DatabasePath=H:\ParkingData\parking_business_result.db
ParkingBusinessResult__ExportDirectory=H:\ParkingData\exports
```

Authority phải cung cấp metadata OpenID Connect và khóa ký. Token cần `sub` không rỗng; claim vai trò mặc định là `role`. Các vai trò hiện có là `ROLE_ADMIN`, `ROLE_DISTRICT_ADMIN`, `ROLE_PARKING_OPERATOR`. Tài khoản không phải admin chỉ thấy UUID bãi trong các claim `parking_lot_id`. Hệ thống định danh cần ánh xạ các claim này; không mặc định đọc được vai trò nằm trong object lồng nhau. Tên hiển thị lấy từ `preferred_username`, `unique_name` hoặc `name`.

Không đặt khóa thật, tài khoản bootstrap hoặc token vào Git. Chế độ Authority không mở API tạo tài khoản hay phát token cục bộ; Local có đăng nhập và quản lý tài khoản với quyền admin. Bảng tài khoản giữ tên lịch sử `dev_auth_users` để bảo toàn tài khoản đã có, nhưng được dùng bởi chế độ Local ở Production. Mật khẩu băm, token hết hạn sau 30 phút; chưa có đổi/khôi phục mật khẩu, thu hồi token hoặc MFA. Những chức năng quản trị định danh đó có thể giao cho IdP khi tích hợp SSO. URL Authority, audience và chính sách quyền thực của nhóm cần được cấp khi chọn SSO; kiểm tra Authority giả không chứng minh SSO thành công.

## Chạy bản Release

1. Dùng Windows và .NET 10 cho bản có xuất PDF; bộ tạo PDF hiện dùng Arial trên Windows.
2. Sao lưu SQLite bằng công cụ backup SQLite hoặc script migration đã có. Áp dụng migration đã được duyệt trước khi chạy; ứng dụng không tự nâng schema Production. Không chạy script seed dữ liệu giả lập trên database vận hành.
3. Đặt database và thư mục exports ở vùng dữ liệu bền vững ngoài thư mục publish. Tài khoản chạy ứng dụng cần quyền đọc/ghi ở đó; hạn chế quyền truy cập từ tài khoản khác.
4. Publish và đặt các biến môi trường trên, sau đó chạy:

```powershell
dotnet publish Backend/ParkingBusinessResult.Api -c Release -o artifacts/api
dotnet artifacts/api/ParkingBusinessResult.Api.dll --urls http://127.0.0.1:5069
```

Đặt dịch vụ sau reverse proxy có HTTPS, chỉ chuyển tiếp vào cổng nội bộ này khi mở ra mạng. Với Local, giao diện và đăng nhập được phục vụ trực tiếp bởi API ở Production. Với Authority, frontend triển khai riêng cần tích hợp đăng nhập của IdP. Đường dẫn database/export tương đối được giải quyết từ content root; khi publish nên cấp đường dẫn tuyệt đối ngoài thư mục ứng dụng để không mất dữ liệu khi thay bản phát hành.

Đây là mô hình một instance với SQLite và thư mục file cục bộ. Không triển khai nhiều replica bằng cách chia sẻ file SQLite qua ổ mạng. Nhu cầu nhiều instance cần chọn database/storage dùng chung và triển khai repository tương ứng. Cột doanh thu tổng hợp giữ REAL theo Excel; độ chính xác tiền của schema đó chưa được thay đổi.

## Lỗi, log, health và file xuất

- JSON nghiệp vụ dùng ba trường `code`, `message`, `data`; thành công code 0, lỗi code 7. Lỗi có `data: null`. Middleware chuẩn hóa lỗi bind JSON, lỗi chưa xử lý và trạng thái lỗi route trong `/gdci` về HTTP 200 theo hợp đồng GDCI.
- Lỗi nội bộ không trả SQL, đường dẫn database hay stack trace. Header `X-Request-Id` liên kết response với log; log có method, path, thời gian và HTTP status, không ghi body, token hoặc query string. Vì lỗi nghiệp vụ vẫn HTTP 200, hệ thống giám sát cần đọc code nghiệp vụ khi kiểm tra API.
- `GET /health/live` kiểm tra tiến trình; `GET /health/ready` kiểm tra đọc được database và tồn tại ba bảng lõi. Readiness trả 503 khi storage không sẵn sàng. Hai route vận hành này nằm ngoài envelope GDCI, không xác nhận IdP hay toàn bộ schema/tham chiếu dữ liệu.
- Giới hạn import 10 MB và 50.000 dòng dữ liệu. Lỗi HTTP do proxy/Kestrel từ chối trước pipeline, kết nối đã ngắt hoặc response đã bắt đầu không thể bảo đảm được chuyển thành envelope.
- File báo cáo ghi vào file tạm rồi đổi tên; chỉ trả URL khi file hoàn tất. Thư mục chủ sở hữu băm từ issuer và subject; subject giống nhau ở issuer khác không dùng chung file. Chặn đường dẫn vượt thư mục và chỉ chủ file có quyền tải.
- File cũ theo UUID chỉ được tìm lại khi bật rõ `ParkingBusinessResult:AllowLegacyReportDownloads`; mặc định tắt để cách ly issuer/subject. Chỉ bật để chuyển tiếp trên bản cài Local tin cậy có dữ liệu cũ, không bật khi dùng nhiều issuer.
- File xuất chưa có lịch dọn tự động. Đơn vị vận hành cần chọn thời gian lưu, sao lưu và dung lượng; không tự xóa báo cáo khi chưa có chính sách.

## Kiểm tra và giới hạn

Workflow `.github/workflows/ci.yml` chỉ build Release. Thư mục `tests/` và hai project kiểm thử đã được bỏ theo yêu cầu; các kiểm tra trước đây còn trong lịch sử Git tại tag `vesion-3`. Kiểm tra thủ công bằng [collection Postman](../postman/ParkingBusinessResult.postman_collection.json).

Build thành công không chứng nhận toàn bộ ApiSpec/LLD đã khớp. Khác biệt nghiệp vụ nằm trong [API_CONTRACT_REVIEW.md](API_CONTRACT_REVIEW.md). SSO thực, quyền theo tổ chức, tải và vận hành vẫn cần môi trường phù hợp để xác minh.
