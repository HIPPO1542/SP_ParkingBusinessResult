# Giải thích chức năng từng file trong dự án

Tài liệu này mô tả mã nguồn hiện tại của `SP_ParkingBusinessResult`, phục vụ việc đọc code và trình bày báo cáo. File trong cùng một bảng được tính theo **đường dẫn thư mục ngay phía trên bảng**.

## 1. Đọc nhanh trước khi xem từng file

Luồng chung của một yêu cầu:

```text
Người dùng → Frontend → Endpoint → Command/Query → Handler
           → Repository → SQLite hoặc file → kết quả trả ngược về giao diện
```

Các tên thường gặp:

- **Endpoint:** nhận yêu cầu HTTP và trả kết quả.
- **Command:** mang dữ liệu cho thao tác làm thay đổi dữ liệu, ví dụ import.
- **Query:** mang điều kiện cho thao tác đọc, ví dụ chọn thời gian thống kê.
- **Handler:** xử lý command hoặc query tương ứng.
- **Repository:** nơi đọc/ghi dữ liệu; interface mô tả khả năng cần có, implementation thực hiện với SQLite.
- **DTO/record:** kiểu dữ liệu mang thông tin giữa các phần hoặc trả về API.
- **Migration:** script thay đổi cấu trúc database và có thể chuyển dữ liệu cũ.

Project dùng Minimal API, CQRS và dependency injection. Các handler được gọi trực tiếp; dự án **không dùng MediatR hoặc EF Core**. API, giao diện và tài khoản Local chạy được ở Production; Authority là chế độ xác thực tùy chọn khi tích hợp SSO.

## 2. File tại thư mục gốc

| File | Chức năng |
|---|---|
| `.gitignore` | Cho Git bỏ qua file tạo tự động hoặc dữ liệu cục bộ, chẳng hạn `bin`, `obj`, database SQLite, file báo cáo và file tạm Excel. |
| `SP_ParkingBusinessResult.sln` | Solution gom các project .NET để build và mở trong IDE. |
| `start.ps1` | Chạy Release/Production, dùng database và User Secrets của bản cài hiện có; không reset dữ liệu. |
| `Directory.Build.props` | Build xác định được kết quả, bật cảnh báo thành lỗi và cấu hình CI. |
| `.github/workflows/ci.yml` | Chạy kiểm tra kiến trúc, dữ liệu và API tự động khi push hoặc mở PR. |
| `README.md` | Giới thiệu dự án, database, cách cấu hình tài khoản và chạy backend. |
| `TONG_QUAN_DU_AN.md` | Tổng quan kiến trúc, chức năng, database, cách chạy và những phần chưa hoàn chỉnh. |
| `GIAI_THICH_TUNG_FILE.md` | Tài liệu đang đọc: giải thích chức năng các file. |

## 3. Frontend

Thư mục `Frontend/`:

| File | Chức năng |
|---|---|
| `index.html` | Chứa giao diện, CSS và JavaScript: đăng nhập, nhập file, xem thống kê theo ngày và bãi, lọc danh sách lượt xe và xuất báo cáo. JavaScript dùng `fetch` gọi API và hiển thị kết quả. |
| `README.md` | Hướng dẫn mở và sử dụng giao diện. |

Backend nhúng `index.html` vào project API và phục vụ tại `/` khi dùng xác thực Local, kể cả Production; không cần chạy server frontend riêng.

## 4. Backend — ParkingBusinessResult.Api

Đây là tầng tiếp nhận HTTP. Đường dẫn trong mục này tính từ `Backend/ParkingBusinessResult.Api/`.

### 4.1. Khởi động, cấu hình và hỗ trợ

| File | Chức năng |
|---|---|
| `Program.cs` | Điểm bắt đầu backend; đọc cấu hình, tùy chọn User Secrets, đăng ký ứng dụng và chạy web server. |
| `ParkingBusinessResult.Api.csproj` | Khai báo project API, tham chiếu Application và Infrastructure, package JWT và tài nguyên giao diện được nhúng. |
| `appsettings.json` | Cấu hình chung hiện có, gồm logging và host. |
| `appsettings.Development.json` | Cấu hình logging dành cho Development. |
| `Properties/launchSettings.json` | Các profile `http`/`https` chạy Production, cổng và tùy chọn đọc User Secrets. |
| `Contracts/ApiResponse.cs` | Định nghĩa phong bì JSON `code`, `message`, `data` cho response của API. |
| `Endpoint/EndpointRegistration.cs` | Nhóm và đăng ký các route; phục vụ trang `/` và route tải file báo cáo đã lưu. |
| `Endpoint/EndpointSupport.cs` | Lấy phạm vi quyền từ thông tin người dùng và chuyển một số kết quả nghiệp vụ thành response chung. |
| `Extensions/BusinessResultRegistration.cs` | Đăng ký các handler để endpoint có thể nhận chúng qua dependency injection. |
| `Extensions/ApplicationSetup.cs` | Cấu hình repository đọc/ghi, reader/writer file, health, middleware và đăng ký API ở mọi môi trường. |
| `Extensions/AuthenticationSetup.cs` | Cấu hình JWT Local hoặc Authority, chính sách quyền và giới hạn đăng nhập. |
| `Extensions/LocalCredentials.cs` | Băm/kiểm tra mật khẩu và phát hành JWT cho tài khoản Local, dùng cả Production. |
| `Middlewares/ApiExceptionMiddleware.cs` | Xử lý lỗi tập trung, không lộ chi tiết nội bộ; gắn request ID và ghi log thời gian. |

### 4.2. Endpoint kết quả kinh doanh

Thư mục `Endpoint/BusinessResults/`:

| File | Chức năng |
|---|---|
| `CreateBusinessResultEndpoint.cs` | Nhận yêu cầu thêm một bản kết quả kinh doanh theo bãi và kỳ. |
| `UpdateBusinessResultEndpoint.cs` | Nhận yêu cầu sửa bản kết quả kinh doanh. |
| `DeleteBusinessResultEndpoint.cs` | Nhận yêu cầu xóa mềm bản kết quả. |
| `GetBusinessResultEndpoint.cs` | Nhận yêu cầu lấy chi tiết một kết quả theo ID. |
| `ListBusinessResultsEndpoint.cs` | Nhận yêu cầu lấy danh sách kết quả kinh doanh. |

Nhóm này làm việc với bản tổng hợp trong `SP_ketQuaKinhDoanhBaiDoXe`; không phải danh sách từng lượt xe.

### 4.3. Endpoint báo cáo từ kết quả đã tổng hợp

Thư mục `Endpoint/BusinessReports/`:

| File | Chức năng |
|---|---|
| `ListBusinessReportEndpoint.cs` | Nhận bộ lọc và phân trang, trả danh sách báo cáo với thông tin bãi, đơn vị quản lý và audit. |
| `GetBusinessReportSummaryEndpoint.cs` | Nhận yêu cầu thống kê tổng hợp theo kỳ/bãi và gọi query handler. |
| `ExportBusinessReportEndpoint.cs` | Kiểm tra request/quyền, gọi Command xuất báo cáo và trả metadata cùng URL tải. |
| `DownloadBusinessReportEndpoint.cs` | Gọi Query tìm báo cáo thuộc tài khoản đăng nhập rồi trả file. |

### 4.4. Endpoint lượt xe

Thư mục `Endpoint/ParkingBookings/`:

| File | Chức năng |
|---|---|
| `ImportParkingBookingsEndpoint.cs` | Nhận file Excel/CSV, kiểm tra định dạng/kích thước, gọi command import và trả số dòng thành công/lỗi. |
| `ListParkingBookingsEndpoint.cs` | Nhận điều kiện bãi, loại xe, trạng thái và phân trang để lấy danh sách lượt. |
| `GetParkingBookingFiltersEndpoint.cs` | Cung cấp danh sách bãi và loại xe theo phạm vi quyền để giao diện tạo bộ lọc. |
| `GetParkingBookingSummaryEndpoint.cs` | Nhận kỳ hoặc khoảng ngày, một/nhiều bãi và gọi handler thống kê lượt xe. |
| `ExportParkingBookingReportEndpoint.cs` | Nhận điều kiện thời gian/bãi, kiểm tra quyền và trả file Excel/PDF từ lượt xe đã ra. |
| `CompareParkingBookingTariffEndpoint.cs` | Nhận yêu cầu đối chiếu tiền một lượt xe với bảng giá theo loại ngày. |
| `ParkingBookingPeriodParser.cs` | Chuyển tháng hoặc khoảng ngày từ request thành khoảng thời gian dùng cho thống kê/xuất. |

### 4.5. Endpoint tài khoản Local

Thư mục `Endpoint/LocalAccounts/`:

| File | Chức năng |
|---|---|
| `LoginEndpoint.cs` | Nhận tên đăng nhập/mật khẩu và trả token khi thông tin hợp lệ. |
| `CreateAccountEndpoint.cs` | Nhận yêu cầu tạo tài khoản Local; chỉ admin được thực hiện. |

## 5. Backend — ParkingBusinessResult.Application

Đây là tầng xử lý use case. Đường dẫn trong mục này tính từ `Backend/ParkingBusinessResult.Application/`.

### 5.1. Project, dữ liệu đầu vào và thư mục chung

| File | Chức năng |
|---|---|
| `ParkingBusinessResult.Application.csproj` | Khai báo project Application và tham chiếu Domain. |
| `Common/.gitkeep` | Giữ thư mục chung rỗng trong Git; không có logic chạy. |
| `DTOs/BusinessResultRequests.cs` | Định nghĩa dữ liệu request để thêm/sửa kết quả kinh doanh. |
| `DTOs/LocalAccountRequests.cs` | Định nghĩa request đăng nhập và tạo tài khoản Local. |

### 5.2. Feature BusinessResults

Đường dẫn gốc: `Features/BusinessResults/`. Nhóm này xử lý kết quả kinh doanh đã tổng hợp theo bãi và kỳ.

| File | Chức năng |
|---|---|
| `Commands/Create/CreateBusinessResultCommand.cs` | Mang dữ liệu cần thiết để thêm kết quả. |
| `Commands/Create/CreateBusinessResultHandler.cs` | Kiểm tra quyền và dữ liệu, tạo kết quả, gọi repository lưu; xử lý bãi không tồn tại hoặc trùng kỳ. |
| `Commands/Update/UpdateBusinessResultCommand.cs` | Mang dữ liệu cần thiết để sửa kết quả. |
| `Commands/Update/UpdateBusinessResultHandler.cs` | Kiểm tra và cập nhật bản kết quả được phép sửa. |
| `Commands/Delete/DeleteBusinessResultCommand.cs` | Mang ID và quyền cho yêu cầu xóa. |
| `Commands/Delete/DeleteBusinessResultHandler.cs` | Xử lý yêu cầu xóa mềm sau khi kiểm tra phạm vi truy cập. |
| `Queries/GetById/GetBusinessResultQuery.cs` | Mang ID và quyền cho yêu cầu xem chi tiết. |
| `Queries/GetById/GetBusinessResultHandler.cs` | Lấy và kiểm tra quyền đối với một kết quả. |
| `Queries/List/ListBusinessResultsQuery.cs` | Mang điều kiện cho yêu cầu danh sách kết quả. |
| `Queries/List/ListBusinessResultsHandler.cs` | Lấy danh sách và giới hạn theo quyền của người dùng. |
| `Common/BusinessAccess.cs` | Mô tả quyền admin hoặc tập bãi người dùng được phép truy cập. |
| `Common/BusinessResultOutcome.cs` | Kiểu kết quả chứa dữ liệu thành công hoặc thông báo lỗi nghiệp vụ. |

### 5.3. Feature BusinessReports

Đường dẫn gốc: `Features/BusinessReports/`. Nhóm này đọc và xuất báo cáo từ bản kết quả kinh doanh tổng hợp.

| File | Chức năng |
|---|---|
| `Common/BusinessReportRow.cs` | Một dòng số liệu dùng để tạo báo cáo, kèm mã/tên bãi, kỳ, doanh thu và lượt xe. |
| `Common/BusinessSummary.cs` | Kiểu kết quả chứa các chỉ số tổng hợp cơ bản. |
| `Common/BusinessSummaryEntry.cs` | Một đầu vào doanh thu và lượt xe cho phép tính tổng hợp. |
| `Common/BusinessSummaryCalculator.cs` | Tính tổng doanh thu, tổng lượt và doanh thu bình quân từ các dòng đầu vào. |
| `Common/ReportParkingLot.cs` | Kiểu thông tin bãi dùng trong báo cáo: ID, mã, tên và địa chỉ. |
| `Common/IBusinessReportParkingLotSearch.cs` | Hợp đồng tra cứu thông tin bãi hoặc tìm bãi theo điều kiện báo cáo. |
| `Queries/List/ListBusinessReportQuery.cs` | Mang điều kiện lọc cho danh sách báo cáo. |
| `Queries/List/ListBusinessReportHandler.cs` | Lọc bản tổng hợp theo quyền, kỳ, bãi, từ khóa và địa bàn. |
| `Queries/List/BusinessReportListItem.cs` | Định nghĩa dòng JSON danh sách, thông tin bổ sung và hợp đồng đọc metadata của báo cáo. |
| `Queries/List/BusinessReportListMapper.cs` | Ghép bản kết quả với thông tin bãi/đơn vị/audit và tính doanh thu bình quân để tạo response. |
| `Queries/Summary/GetBusinessReportSummaryQuery.cs` | Mang điều kiện yêu cầu thống kê bản tổng hợp. |
| `Queries/Summary/GetBusinessReportSummaryHandler.cs` | Tính tổng, bình quân, tăng trưởng khi đủ dữ liệu, top bãi và nhóm địa bàn từ bản tổng hợp. |
| `Queries/Summary/BusinessReportSummary.cs` | Định nghĩa cấu trúc response tổng hợp, top bãi và doanh thu theo địa bàn. |
| `Commands/Export/ExportBusinessReportCommand.cs` | Mang điều kiện và người xuất báo cáo tổng hợp có lưu file. |
| `Commands/Export/ExportBusinessReportHandler.cs` | Lấy số liệu, dùng interface tạo/lưu file và trả metadata; không phụ thuộc thư viện Excel/PDF. |
| `Common/ReportDocuments.cs` | Kiểu nội dung/metadata file, người xuất và interface tạo/lưu/tải báo cáo. |
| `Queries/Download/DownloadBusinessReportQuery.cs` | Query và handler tìm file báo cáo thuộc issuer/subject hiện tại. |

### 5.4. Feature ParkingBookings

Đường dẫn gốc: `Features/ParkingBookings/`. Tên feature là ParkingBookings, còn dữ liệu hiện được lưu trong bảng booking `SP_datTruocChoDoXe`.

| File | Chức năng |
|---|---|
| `Commands/Import/ImportParkingBookingsCommand.cs` | Mang file, định dạng và phạm vi quyền cho yêu cầu nhập lượt xe. |
| `Commands/Import/ImportParkingBookingsHandler.cs` | Đọc từng dòng, kiểm tra dữ liệu, từ chối dòng trùng, lưu dòng hợp lệ và tổng hợp kết quả import. |
| `Common/ParkingBookingImportContracts.cs` | Định nghĩa dữ liệu dòng import, kết quả/lỗi và hợp đồng đọc file, lưu lượt xe. |
| `Common/ParkingBookingListContracts.cs` | Định nghĩa một lượt xe trong danh sách và hợp đồng đọc danh sách. |
| `Common/ParkingBookingSummaryContracts.cs` | Định nghĩa dữ liệu lượt hoàn tất và hợp đồng lấy chúng cho thống kê. |
| `Common/ParkingBookingTariffContracts.cs` | Định nghĩa dữ liệu lượt xe, mức giá và hợp đồng đọc dữ liệu phục vụ đối chiếu. |
| `Queries/List/ListParkingBookingsQuery.cs` | Mang bãi, loại xe, trạng thái, phân trang và quyền cho danh sách. |
| `Queries/List/ListParkingBookingsHandler.cs` | Lọc, sắp xếp và phân trang danh sách lượt xe. |
| `Queries/Filters/GetParkingBookingFiltersHandler.cs` | Chứa query và handler lấy các bãi, loại xe có thể chọn theo quyền người dùng. |
| `Queries/Summary/GetParkingBookingSummaryQuery.cs` | Mang khoảng thời gian, bãi và quyền cho yêu cầu thống kê. |
| `Queries/Summary/GetParkingBookingSummaryHandler.cs` | Thống kê các lượt đã ra, tiền thực thu, số lượt thiếu tiền, bình quân, tăng trưởng khi đủ dữ liệu, top bãi và địa bàn. |
| `Queries/Export/ExportParkingBookingReportQuery.cs` | Mang thời gian, bãi và quyền cho báo cáo từ lượt xe. |
| `Queries/Export/ExportParkingBookingReportHandler.cs` | Lọc lượt đã ra, nhóm theo bãi thành các dòng cho báo cáo và đếm lượt thiếu tiền. |
| `Queries/CompareTariff/CompareParkingBookingTariffQuery.cs` | Mang ID lượt xe, loại ngày và quyền cho yêu cầu đối chiếu. |
| `Queries/CompareTariff/CompareParkingBookingTariffHandler.cs` | Áp dụng các điều kiện hỗ trợ, tính giá dự kiến và so với tiền thực thu nếu đủ dữ liệu. |

### 5.5. Feature LocalAccounts

Đường dẫn gốc: `Features/LocalAccounts/`:

| File | Chức năng |
|---|---|
| `Commands/Bootstrap/BootstrapAdministratorCommand.cs` | Mang thông tin cấu hình để khởi tạo tài khoản quản trị đầu tiên. |
| `Commands/Bootstrap/BootstrapAdministratorHandler.cs` | Chuẩn bị kho tài khoản và tạo admin đầu tiên nếu phù hợp. |
| `Commands/Create/CreateAccountCommand.cs` | Mang dữ liệu cho yêu cầu tạo tài khoản. |
| `Commands/Create/CreateAccountHandler.cs` | Kiểm tra dữ liệu, băm mật khẩu và yêu cầu lưu tài khoản Local. |
| `Commands/SignIn/SignInCommand.cs` | Mang tên đăng nhập và mật khẩu cho yêu cầu đăng nhập. |
| `Commands/SignIn/SignInHandler.cs` | Tìm tài khoản, kiểm tra mật khẩu và yêu cầu phát hành token. |
| `Common/LocalAccount.cs` | Kiểu dữ liệu tài khoản Local, bao gồm vai trò và bãi được gán. |
| `Common/ILocalAccountRepository.cs` | Hợp đồng lưu và tìm tài khoản Local. |
| `Common/ILocalCredentials.cs` | Hợp đồng băm/kiểm tra mật khẩu và phát hành token. |

## 6. Backend — ParkingBusinessResult.Domain

Đường dẫn gốc: `Backend/ParkingBusinessResult.Domain/`. Domain chứa đối tượng và quy tắc nghiệp vụ cốt lõi; số entity trong code không nhất thiết bằng số bảng trong database.

| File | Chức năng |
|---|---|
| `ParkingBusinessResult.Domain.csproj` | Khai báo project Domain. |
| `Entities/ParkingBusinessResultRecord.cs` | Đối tượng kết quả kinh doanh theo bãi/kỳ và các kiểm tra liên quan đến dữ liệu của đối tượng. |
| `Repositories/IBusinessResultRepository.cs` | Hợp đồng thao tác với bản kết quả kinh doanh, không quy định cách lưu cụ thể. |
| `Common/RevenueMetrics.cs` | Tính doanh thu bình quân trên lượt xe hoàn tất, xử lý trường hợp không có lượt. |
| `Exceptions/ParkingLotNotFoundException.cs` | Kiểu lỗi báo bãi xe được tham chiếu không tồn tại. |
| `ValueObjects/BusinessReportingPeriod.cs` | Biểu diễn kỳ báo cáo và hỗ trợ phân tích kỳ, kỳ trước, số ngày trong kỳ. |
| `Enums/.gitkeep` | Giữ thư mục `Enums` rỗng trong Git; không có logic chạy. |

## 7. Backend — ParkingBusinessResult.Infrastructure

Đường dẫn gốc: `Backend/ParkingBusinessResult.Infrastructure/`. Đây là nơi hiện thực phần làm việc với SQLite, Excel, PDF và file.

| File | Chức năng |
|---|---|
| `ParkingBusinessResult.Infrastructure.csproj` | Khai báo project, tham chiếu Application và các package SQLite, đọc Excel, tạo PDF. |
| `Repositories/SqliteBusinessResultRepository.cs` | Đọc/ghi kết quả kinh doanh tổng hợp trong SQLite, gồm xóa mềm và kiểm tra dữ liệu liên quan. |
| `Repositories/SqliteBusinessReportParkingLotSearch.cs` | Tìm bãi hoặc lấy thông tin bãi phục vụ danh sách và báo cáo. |
| `Repositories/SqliteBusinessReportMetadataReader.cs` | Ghép dữ liệu kết quả kinh doanh với bãi, đơn vị quản lý và thông tin audit để trả danh sách báo cáo. |
| `Repositories/SqliteParkingBookingImportRepository.cs` | Mở session import, kiểm tra bãi và ghi các dòng hợp lệ trong một giao dịch; rollback nếu lỗi hoặc hủy. |
| `Repositories/SqliteParkingBookingQueryRepository.cs` | Chỉ đọc booking: lọc quyền, lọc nhiều tiêu chí, đếm/phân trang bằng SQL và lấy dữ liệu thống kê đúng phạm vi. |
| `Repositories/SqliteParkingBookingTariffRepository.cs` | Lấy thông tin lượt xe và các quy tắc giá liên quan để đối chiếu. |
| `Repositories/ExcelParkingBookingSpreadsheetReader.cs` | Đọc file `.xlsx`, `.xls`, `.csv`, kiểm tra tiêu đề và chuyển dữ liệu thành các dòng import. |
| `Repositories/SqliteLocalAccountRepository.cs` | Lưu/tìm tài khoản Local trong bảng `dev_auth_users` cũ để giữ tài khoản hiện có; hoạt động cả Production. |
| `Repositories/InMemoryBusinessResultRepository.cs` | Cài đặt repository lưu tạm trong bộ nhớ; được dùng trong kiểm tra, không phải kho SQLite đang cấu hình cho API. |
| `Reports/BusinessReportExcelWriter.cs` | Tạo nội dung file báo cáo Excel từ các dòng kết quả. |
| `Reports/BusinessReportPdfWriter.cs` | Tạo nội dung file báo cáo PDF từ các dòng kết quả; bản hiện tại cần Windows. |
| `Reports/BusinessReportExportStore.cs` | Ghi file tạm rồi đổi tên, tạo metadata; chỉ cho chủ sở hữu issuer/subject tải lại. |
| `Reports/BusinessReportDocumentWriter.cs` | Chọn bộ tạo Excel/PDF và trả nội dung qua hợp đồng Application. |
| `Data/SqliteStorageProbe.cs` | Kiểm tra database đọc được và có ba bảng lõi cho readiness. |
| `EntityConfigs/.gitkeep` | Giữ thư mục dự phòng `EntityConfigs`; hiện chưa dùng EF Core configuration. |

## 8. Database và dữ liệu mẫu

Thư mục `database/sqlite/`:

| File | Chức năng |
|---|---|
| `001_create_business_result.sql` | Tạo phiên bản đầu của bảng kết quả kinh doanh. |
| `002_business_reference_tables.sql` | Bổ sung bảng tham chiếu như bãi xe, đơn vị quản lý và liên kết cần thiết. |
| `003_soft_delete_business_result.sql` | Chuyển cấu trúc kết quả kinh doanh để hỗ trợ xóa mềm. |
| `004_parking_price_tables.sql` | Tạo các bảng liên quan đến khung giá và áp dụng giá. |
| `005_dev_parking_sessions.sql` | Tạo bảng phiên đỗ giả lập phục vụ dữ liệu Development. |
| `006_parking_visits.sql` | Cấu trúc lịch sử của bảng `parking_visit` cũ; được giữ để hỗ trợ migration cũ và kiểm tra chuyển dữ liệu. |
| `007_booking_table.sql` | Tạo bảng `SP_datTruocChoDoXe` với 16 cột theo sheet booking; cho phép giờ ra trống đối với xe chưa ra. |
| `apply_business_reference_migration.py` | Chạy migration bổ sung bảng tham chiếu theo các điều kiện an toàn của script. |
| `apply_soft_delete_migration.py` | Sao lưu và áp dụng thay đổi hỗ trợ xóa mềm cho database cũ. |
| `apply_parking_visit_migration.py` | Script lịch sử sao lưu và tạo bảng `parking_visit` cũ. |
| `migrate_visits_to_booking.py` | Sao lưu database, chuyển lượt cũ sang booking, đối chiếu dữ liệu và xóa bảng cũ nếu thành công. |
| `seed_development_parking_data.py` | Tạo bãi, bảng giá và lượt giả lập cho môi trường Development. |

`database/sqlite/parking_business_result.db` là **database cục bộ**, không được đưa lên Git. Các file `*.backup-...db` là bản sao lưu; không phải mã nguồn và có thể cần để phục hồi. Trong bảng booking, `tongTien` được dùng là số tiền đã thu theo xác nhận của người dùng; phương thức thanh toán không được quản lý trong module này.

Thư mục `outputs/`:

| File | Chức năng |
|---|---|
| `parking-import/mau-luot-xe.xlsx` | File mẫu định dạng import; có cột phương thức thanh toán cũ được bộ đọc bỏ qua. |
| `mau-import/du-lieu-luot-xe-mau.xlsx` | File dữ liệu mẫu gồm 10 lượt xe để thử import. |

## 9. Tài liệu và Postman

Thư mục `docs/`:

| File | Chức năng |
|---|---|
| `API_CONTRACT_REVIEW.md` | Đối chiếu các API hiện tại với đặc tả và ghi lại các chỗ chưa khớp. |
| `ENTERPRISE_DEPLOYMENT.md` | Giải thích luồng kiến trúc, giao dịch import, chế độ Local/Authority, cách triển khai Production và các giới hạn đã biết. |
| `DEVELOPMENT_BUSINESS_REPORTS.md` | Hướng dẫn gọi API danh sách, tổng hợp và xuất bản báo cáo kinh doanh. |
| `DEVELOPMENT_PARKING_DATA.md` | Giải thích dữ liệu bãi, bảng giá và lượt giả lập dùng trong Development. |
| `PARKING_BOOKING_IMPORT_DESIGN.md` | Hướng dẫn cấu trúc file nhập, bảng booking, thống kê và các giới hạn hiện tại. |

Thư mục `postman/`:

| File | Chức năng |
|---|---|
| `ParkingBusinessResult.postman_collection.json` | Collection gọi API kết quả kinh doanh và đăng nhập Local qua `/auth/login`. |
| `ParkingBookings.postman_collection.json` | Collection gọi API import, danh sách, thống kê và đối chiếu lượt xe. |
| `README.md` | Hướng dẫn cấu hình và sử dụng các collection. |
| `seed-demo-parking-lot.sql` | SQL tạo dữ liệu bãi mẫu phục vụ Postman. |
| `seed_demo.py` | Script hỗ trợ đưa bãi mẫu vào database Development. |

## 10. Các file kiểm tra

Thư mục `tests/` không phải giao diện hay chức năng nghiệp vụ chạy thường xuyên. Các file này tạo dữ liệu tạm và xác nhận hành vi đã lập trình.

| File | Chức năng |
|---|---|
| `RevenueMetrics.Checks/RevenueMetrics.Checks.csproj` | Khai báo project kiểm tra quy tắc tính và repository. |
| `RevenueMetrics.Checks/Program.cs` | Chạy các kiểm tra quy tắc doanh thu, dữ liệu và repository liên quan. |
| `Enterprise.Checks/Enterprise.Checks.csproj` và `Program.cs` | Kiểm tra rollback/hủy import, audit, phân trang, phạm vi bãi và quyền sở hữu file trên dữ liệu tạm. |
| `runtime_checks.ps1` | Chạy host thật trên database tạm: lỗi JSON, lỗi storage, request ID, readiness, UI/đăng nhập/API Production và chế độ Authority. |
| `test_architecture.py` | Chặn tham chiếu sai chiều, Endpoint gọi Infrastructure và thư viện công nghệ trong Domain/Application. |
| `TestRuntime.ps1` | Chọn Python theo cấu hình hoặc PATH để bài kiểm tra không phụ thuộc riêng máy hiện tại. |
| `requirements.txt` | Khóa phiên bản thư viện Python dùng tạo/đọc file khi kiểm tra. |
| `prepare_sqlite_fixture.py` | Tạo database SQLite nền cho các bài kiểm tra. |
| `sqlite_schema_checks.py` | Kiểm tra cấu trúc SQLite và các ràng buộc liên quan. |
| `test_sqlite_migration.py` | Kiểm tra migration bổ sung các bảng tham chiếu. |
| `test_soft_delete_migration.py` | Kiểm tra migration xóa mềm và việc bảo toàn dữ liệu. |
| `test_parking_visit_migration.py` | Kiểm tra migration lịch sử tạo bảng `parking_visit` cũ. |
| `test_booking_migration.py` | Kiểm tra chuyển lượt cũ sang booking, 16 cột, dữ liệu và bản sao lưu. |
| `test_development_parking_data.py` | Kiểm tra cấu trúc và số liệu dữ liệu giả lập Development. |
| `api_sqlite_checks.ps1` | Chạy các kiểm tra API kết quả kinh doanh với database tạm. |
| `seeded_parking_api_checks.ps1` | Kiểm tra các API báo cáo bằng dữ liệu giả lập, gồm phân quyền, xuất file và xóa mềm. |
| `parking_booking_import_fixture.py` | Tạo file và dữ liệu mẫu phục vụ kiểm tra luồng import lượt xe. |
| `parking_booking_import_checks.ps1` | Kiểm tra import, dữ liệu trùng, danh sách, thống kê, xuất và quyền trên database tạm. |
| `check_business_report_xlsx.py` | Kiểm tra nội dung Excel báo cáo kết quả kinh doanh tổng hợp. |
| `check_business_report_pdf.py` | Kiểm tra nội dung PDF báo cáo kết quả kinh doanh tổng hợp. |
| `check_visit_export.py` | Kiểm tra nội dung file xuất từ lượt xe. |
| `check_visit_district_export.py` | Kiểm tra file xuất khi giới hạn theo địa bàn hoặc bãi. |
| `test_postman_collection.py` | Kiểm tra cấu trúc/thiết lập liên quan đến collection Postman và dữ liệu mẫu. |
| `postman_newman_checks.ps1` | Chạy các kiểm tra collection Postman bằng Newman trên môi trường kiểm tra. |
| `read_postman_deleted_record.py` | Hỗ trợ bài kiểm tra Postman xác nhận bản ghi đã được xóa mềm. |

Các thư mục `bin/` và `obj/` trong project do .NET tạo khi build; không phải các file chức năng cần giải thích từng cái.

## 11. Ví dụ đi qua các file khi nhập Excel

1. Người dùng chọn file trong `Frontend/index.html`.
2. JavaScript gửi file tới `ImportParkingBookingsEndpoint.cs`.
3. Endpoint tạo `ImportParkingBookingsCommand.cs` và gọi `ImportParkingBookingsHandler.cs`.
4. Handler dùng hợp đồng trong `ParkingBookingImportContracts.cs`; `ExcelParkingBookingSpreadsheetReader.cs` thực sự đọc file.
5. Handler kiểm tra từng dòng; `SqliteParkingBookingImportRepository.cs` ghi dòng hợp lệ vào `SP_datTruocChoDoXe`.
6. Handler trả số dòng thành công, trùng và lỗi; endpoint bọc bằng `ApiResponse.cs`.
7. `Frontend/index.html` nhận JSON và hiển thị các dòng, lỗi cho người dùng.

Phần thống kê đi tương tự nhưng dùng **Query**: `GetParkingBookingSummaryEndpoint.cs` → `GetParkingBookingSummaryQuery.cs` → `GetParkingBookingSummaryHandler.cs` → repository đọc bảng booking → trả kết quả cho giao diện.

## 12. Những giới hạn cần nói đúng khi báo cáo

- Database cục bộ đã chuyển lượt xe sang `SP_datTruocChoDoXe`; API ở dự án khác chỉ thấy dữ liệu khi cùng dùng database tương ứng.
- Thời gian kết thúc trong bảng booking được phép trống cho xe chưa ra, dù sheet Excel đánh dấu trường này bắt buộc.
- Lượt xe trong bảng booking và kết quả đã tổng hợp trong `SP_ketQuaKinhDoanhBaiDoXe` chưa tự đồng bộ với nhau.
- API và đăng nhập Local chạy Production; SSO và chính sách quyền hệ thống chung chưa xác minh. Một số body/response còn khác tài liệu gốc theo bảng đối chiếu hợp đồng.
- Tăng trưởng, doanh thu và đối chiếu giá chỉ có ý nghĩa khi dữ liệu đầu vào và quy tắc cần thiết đã đầy đủ; số liệu giả lập không phải dữ liệu vận hành thực tế.
