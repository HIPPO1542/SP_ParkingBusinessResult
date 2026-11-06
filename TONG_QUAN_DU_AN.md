# Tổng quan dự án SP_ParkingBusinessResult

## 1. Dự án làm gì?

Đây là dự án quản lý kết quả kinh doanh bãi đỗ xe và báo cáo định kỳ, gồm frontend cơ bản và backend. Dự án cho phép quản lý kết quả tổng hợp, nhập từng lượt xe từ file, thống kê doanh thu và xuất báo cáo.

**Quy tắc đã thống nhất:** một lượt hoàn tất là một xe đã vào rồi ra. Xe chưa có giờ ra vẫn đang trong bãi và chưa được tính là lượt hoàn tất. Doanh thu từ lượt xe dùng **số tiền thực thu trong file**; bảng giá dùng để đối chiếu.

> Trạng thái hiện tại: các API và tài khoản mới dành cho **Development**. Route nghiệp vụ chính đã theo ví dụ trong tài liệu, nhưng body, response, phân quyền và một số chức năng chưa khớp đầy đủ ApiSpec/LLD. Xem [bảng đối chiếu](docs/API_CONTRACT_REVIEW.md).

## 2. Công nghệ và cấu trúc

- **Ngôn ngữ và nền tảng:** C#, ASP.NET Core, .NET 10.
- **API:** Minimal API; endpoint nhận HTTP và gọi handler.
- **Tổ chức xử lý:** Clean Architecture kết hợp CQRS: `Command` cho thao tác ghi, `Query` cho thao tác đọc. Hiện handler được gọi trực tiếp qua dependency injection, chưa dùng MediatR.
- **Cơ sở dữ liệu:** SQLite cục bộ, truy cập bằng SQL qua repository; chưa dùng EF Core.
- **File:** ExcelDataReader đọc file nhập; dự án tạo Excel và dùng PDFsharp tạo PDF. Các package đọc/tạo file được đặt ở Infrastructure.

### 2.1. Cây thư mục

```text
SP_ParkingBusinessResult/
├── Frontend/                         # Giao diện HTML, CSS, JavaScript
│   ├── index.html                    # Đăng nhập, nhập file, thống kê và xuất báo cáo
│   └── README.md                     # Hướng dẫn sử dụng giao diện
├── Backend/
│   ├── ParkingBusinessResult.Api/
│   │   ├── Contracts/                 # Cấu trúc response API
│   │   ├── Endpoint/
│   │   │   ├── BusinessResults/      # Thêm, sửa, xóa, xem kết quả
│   │   │   ├── BusinessReports/      # Danh sách, tổng hợp, xuất báo cáo tổng hợp
│   │   │   ├── ParkingVisits/        # Import, xem, thống kê, xuất, đối chiếu lượt xe
│   │   │   └── DevelopmentAccounts/  # Đăng nhập, tạo tài khoản thử nghiệm
│   │   ├── Extensions/               # Đăng ký handler, repository và xác thực
│   │   ├── Middlewares/              # Thư mục dự phòng, hiện chưa có middleware riêng
│   │   └── Program.cs               # Khởi tạo ứng dụng
│   │
│   ├── ParkingBusinessResult.Application/
│   │   ├── Common/                   # Dành cho thành phần dùng chung toàn Application
│   │   ├── DTOs/                     # Các request hiện có
│   │   └── Features/
│   │       ├── BusinessResults/      # Commands, Queries, Common của kết quả kinh doanh
│   │       ├── BusinessReports/      # Queries và Common của báo cáo tổng hợp
│   │       ├── ParkingVisits/        # Import và các query theo từng lượt xe
│   │       └── DevelopmentAccounts/  # Commands và Common của tài khoản thử nghiệm
│   │
│   ├── ParkingBusinessResult.Domain/
│   │   ├── Common/                   # Quy tắc tính dùng trong miền
│   │   ├── Entities/                 # ParkingBusinessResultRecord
│   │   ├── Repositories/             # Hợp đồng repository kết quả kinh doanh
│   │   ├── Exceptions/
│   │   ├── Enums/                    # Hiện là thư mục dự phòng
│   │   └── ValueObjects/             # Kỳ báo cáo và tính kỳ trước
│   │
│   └── ParkingBusinessResult.Infrastructure/
│       ├── Repositories/             # Đọc/ghi SQLite, đọc file Excel/CSV
│       ├── Reports/                  # Tạo file Excel và PDF
│       ├── Data/                     # Hiện là thư mục dự phòng
│       └── EntityConfigs/            # Hiện là thư mục dự phòng
│
├── database/sqlite/                  # SQL, migration và script tạo dữ liệu mẫu
├── docs/                             # Tài liệu API, dữ liệu mẫu và import
├── outputs/parking-import/           # File Excel mẫu nhập lượt xe
├── outputs/mau-import/               # Dữ liệu mẫu 10 lượt xe để nhập
├── postman/                          # Collection Postman và hướng dẫn
├── tests/                            # Kiểm thử database, API, file và Postman
├── SP_ParkingBusinessResult.sln      # Solution .NET
├── README.md                         # Hướng dẫn chạy dự án
└── TONG_QUAN_DU_AN.md                # Tài liệu này
```

`bin/` và `obj/` là thư mục do .NET tạo khi build; chúng không phải mã nguồn nghiệp vụ. Những thư mục dự phòng ở trên hiện chỉ có `.gitkeep` để giữ cấu trúc.

### 2.2. Một request đi qua dự án như thế nào?

1. Client hoặc Postman gửi request đến **Minimal API endpoint**.
2. API kiểm tra xác thực, quyền và dữ liệu đầu vào cần thiết.
3. Endpoint tạo **Command** hoặc **Query**, rồi gọi handler tương ứng trong Application.
4. Handler xử lý quy tắc của chức năng; khi cần dữ liệu, handler gọi hợp đồng repository.
5. Infrastructure thực hiện truy vấn SQLite hoặc đọc/tạo file, rồi trả kết quả về cho API.

`Application/Common` dành cho phần thực sự dùng chung toàn Application; `Features/<tên chức năng>/Common` dành cho các kiểu hoặc hợp đồng dùng chung trong feature đó. Domain hiện chỉ có một entity kết quả kinh doanh; việc database có nhiều bảng hơn không có nghĩa mọi bảng đã có entity trong code.

## 3. Hai nguồn dữ liệu báo cáo cần phân biệt

| Nguồn | Dữ liệu | API chính | Cách hoạt động |
| --- | --- | --- | --- |
| **Kết quả đã tổng hợp** | Các bản ghi đã lưu trong `SP_ketQuaKinhDoanhBaiDoXe`, mỗi bản ghi có bãi, kỳ, tổng doanh thu và số lượt hoàn tất. | `businessOperation` và `parkingBusinessReport` | Đọc hoặc chỉnh sửa bản tổng hợp đã lưu. Bản này **không tự cập nhật** theo lượt xe import. |
| **Lượt xe đã import** | Từng lượt trong `SP_datTruocChoDoXe`: bãi, biển số, loại xe, giờ vào/ra và số tiền đã thu. | Import qua `businessOperation/import`; xem và thống kê qua `parkingVisit` | Tính trực tiếp từ các lượt xe đã lưu, không tự cộng thêm bản tổng hợp cũ hoặc lượt giả lập. |

Import thêm xe vào `SP_datTruocChoDoXe` sẽ thay đổi thống kê của `parkingVisit/summary`, nhưng không tự sửa số liệu đã có trong `SP_ketQuaKinhDoanhBaiDoXe`. Giao diện thống kê trực tiếp theo bảng booking, chọn khoảng ngày và một hoặc nhiều bãi.

## 4. Các chức năng đã làm

### 4.1. Đăng nhập và phân quyền thử nghiệm

- Đăng nhập bằng tài khoản cục bộ qua `POST /dev/auth/login`; thành công trả JWT có hiệu lực 30 phút.
- Quản trị viên có thể tạo tài khoản thử nghiệm qua `POST /dev/auth/users`.
- `ROLE_ADMIN` có thể truy cập tất cả bãi trong môi trường thử nghiệm. `ROLE_DISTRICT_ADMIN` và `ROLE_PARKING_OPERATOR` chỉ truy cập bãi được gán cho tài khoản; mỗi tài khoản cục bộ hiện gán tối đa một bãi.
- Xuất báo cáo chỉ cho `ROLE_ADMIN` hoặc `ROLE_DISTRICT_ADMIN`, trong phạm vi bãi được phép.

Đây là cơ chế Development, chưa phải hệ thống xác thực và phân quyền chính thức của nhóm.

### 4.2. Quản lý kết quả kinh doanh đã tổng hợp — UC 1.5

Tiền tố: `/gdci/parking/parkingLot/businessOperation`.

| Thao tác | Cách gọi | Cách hoạt động |
| --- | --- | --- |
| Xem danh sách | `GET /businessOperation` | Đọc các bản ghi chưa xóa trong phạm vi bãi được phép. |
| Xem chi tiết | `GET /businessOperation/{id}` | Đọc một bản ghi chưa xóa theo ID. |
| Thêm | `POST /businessOperation` | Lưu bãi, kỳ, doanh thu, lượt hoàn tất và ghi chú. Một bãi không có hai bản ghi **chưa xóa** cho cùng kỳ. |
| Sửa | `PUT /businessOperation/{id}` | Cập nhật doanh thu, lượt hoàn tất và ghi chú; body thiếu trường bắt buộc hoặc có trường chưa hỗ trợ bị từ chối. |
| Xóa mềm | `DELETE /businessOperation/{id}` | Đánh dấu `daXoa`, ẩn bản ghi khỏi API nhưng giữ trong SQLite; có thể tạo bản mới cho cùng bãi và kỳ. |

Trong bảng trên, các đường dẫn ngắn như `/businessOperation/{id}` nằm sau `/gdci/parking/parkingLot`. Body thêm hiện dùng `parkingLotId`, `reportingPeriod`, `revenue`, `completedVehicleCount`, `note`; body sửa dùng `revenue`, `completedVehicleCount`, `note`. Các body này chưa giống đầy đủ ApiSpec.

**Vì sao body Postman khác tài liệu?** Các request **Thêm kết quả** và **Cập nhật kết quả** trong Postman được viết theo body mà code hiện tại chấp nhận, để có thể chạy thử. Trong khi đó, đặc tả thêm yêu cầu `reportDate`, `periodType`, `totalCheckIn`, `totalCheckOut`, `averageOccupancyRate`, `peakOccupancyRate`, `totalRevenue`, `cashRevenue`, `electronicRevenue` và có thể thêm `operatingExpenses`, `notes`; đặc tả sửa cũng yêu cầu các chỉ số tương tự. Bảng kết quả theo Excel chỉ mô tả một tổng lượt và một tổng doanh thu nên chưa có đủ nơi lưu các trường riêng đó. Vì API thêm/sửa chưa triển khai hợp đồng đầy đủ, gửi body theo đặc tả hiện trả `code: 7`; không nên chỉ đổi nội dung Postman rồi coi chức năng đã khớp. Hai request **Đối chiếu body ... theo tài liệu** trong collection dùng để kiểm tra việc API từ chối body chưa hỗ trợ, không phải kiểm thử thành công theo đặc tả.

### 4.3. Import từng lượt xe

Gọi `POST /gdci/parking/parkingLot/businessOperation/import` dưới dạng `multipart/form-data`, với trường file tên `file` và Bearer token.

1. Điền file theo [mẫu lượt xe](outputs/parking-import/mau-luot-xe.xlsx): mã bãi, biển số, loại xe, giờ vào, giờ ra và số tiền thực thu. Cột phương thức thanh toán trong mẫu cũ được bỏ qua.
2. API đọc `.xlsx`, `.xls` hoặc `.csv`, tối đa 10 MB và 50.000 dòng; tên và thứ tự cột phải theo mẫu.
3. Hệ thống kiểm tra từng dòng: mã bãi và quyền truy cập, biển số, loại xe, giờ vào, giờ ra sau giờ vào, số tiền không âm. Dòng lỗi được báo theo số dòng.
4. Dòng hợp lệ được lưu trong `SP_datTruocChoDoXe`. Nếu cùng **bãi + biển số + giờ vào** đã có, hệ thống báo lỗi trùng và không thay đổi lượt cũ.
5. Kết quả trả số dòng thành công, số dòng lỗi, số dòng trùng, ID và thông tin lượt đã lưu cùng danh sách lỗi. Dòng trùng được tính vào số lỗi. Giao diện hiển thị các dòng này và cho phép chọn tiếp file khác. Nếu xảy ra lỗi hệ thống khi đang ghi, một số dòng hợp lệ trước đó có thể đã được lưu.

Giờ vào và giờ ra phải có ngày/tháng/năm và giờ/phút, ví dụ `2026-11-05 08:30` hoặc `05/11/2026 08:30`. Excel cũng có thể chứa ô ngày giờ thực. Giờ ra để trống nếu xe chưa ra; số tiền có thể để trống nếu chưa có thông tin thu.

### 4.4. Xem các lượt đã import

Gọi `GET /gdci/parking/parkingLot/parkingVisit`:

- `status=PARKED`: lượt chưa có giờ ra, tức xe đang được ghi nhận trong bãi.
- `status=COMPLETED`: lượt đã có giờ ra.
- `parkingLotId`: lọc theo một bãi; kết quả vẫn bị giới hạn theo quyền tài khoản.
- `vehicleType`: lọc theo loại xe; có thể kết hợp với bãi và trạng thái.
- `pageNumber` và `pageSize`: phân trang; mặc định trang 1, 10 dòng, tối đa 100 dòng/trang.

API trả mã, tên bãi trong mỗi lượt, cùng `items` và `totalCount` sau khi lọc. API `parkingVisit/filters` cung cấp các bãi được phép truy cập và loại xe đã có; giao diện dùng các giá trị này để lọc. Đây là danh sách trong `SP_datTruocChoDoXe`, không tự bao gồm 250 lượt giả lập trong `dev_parking_sessions`.

### 4.5. Thống kê từ lượt xe đã import

Gọi `GET /gdci/parking/parkingLot/parkingVisit/summary` và chọn **một trong hai cách**:

```text
?reportingPeriod=2026-11
?fromDate=2026-11-01&toDate=2026-11-30
```

Hệ thống chọn lượt theo **ngày xe ra**, gồm hai ngày đầu/cuối nếu lọc khoảng ngày. Có thể lọc thêm `parkingLotId` hoặc `district` theo chuỗi địa chỉ bãi. Kết quả gồm:

- Số lượt đã hoàn tất; tổng tiền thực thu đã biết; số lượt hoàn tất chưa có số tiền (`missingAmountCount`).
- Phương thức thanh toán không được lưu hoặc phân loại trong chức năng này.
- Doanh thu bình quân ngày và top 5 bãi khi không thiếu tiền thực thu. Tăng trưởng tháng chỉ tính khi có dữ liệu kỳ trước, hai kỳ không thiếu tiền và doanh thu kỳ trước lớn hơn 0; nếu không đủ điều kiện thì trả `null`.
- Cơ cấu doanh thu theo quận/huyện khi không thiếu tiền. Hệ thống tìm thành phần bắt đầu bằng `Quận ` hoặc `Huyện ` trong địa chỉ phân tách bằng dấu phẩy; không nhận diện được thì nhóm `district: null`. Đây là cách suy từ địa chỉ chữ, chưa đối chiếu mã hành chính chính thức.

`collectedRevenue` chỉ cộng tiền đã có. Nếu `missingAmountCount` lớn hơn 0 thì chưa nên hiểu giá trị đó là doanh thu đầy đủ.

### 4.6. Báo cáo từ bản tổng hợp đã lưu — UC 3.4

Tiền tố: `/gdci/parking/parkingLot/parkingBusinessReport`.

- `GET` danh sách: lọc theo chuỗi kỳ, bãi, từ khóa tên/địa chỉ, địa bàn theo địa chỉ và phân trang.
- `GET /summary`: tính tổng doanh thu, lượt hoàn tất, doanh thu bình quân/lượt và/ngày, tăng trưởng, top 5 bãi và cơ cấu theo địa bàn từ **bản tổng hợp đã lưu**. Tăng trưởng có thể `null` nếu thiếu số liệu phù hợp; địa bàn được suy từ địa chỉ và có thể không nhận diện được.
- `POST /export`: tạo Excel/PDF từ nguồn tổng hợp này và trả JSON có `fileUrl`; gọi GET URL đó với token của người đã xuất để tải file. PDF thử nghiệm cần Windows và font Arial.

Nhóm API này và nhóm thống kê theo lượt import có nguồn dữ liệu khác nhau như mục 3. API báo cáo tổng hợp hiện còn thiếu một số bộ lọc, chỉ số và cấu trúc response theo tài liệu.

### 4.7. Xuất báo cáo từ lượt xe import

Gọi `POST /gdci/parking/parkingLot/parkingVisit/export` với body ví dụ:

```json
{
  "fileFormat": "EXCEL",
  "reportingPeriod": "2026-11"
}
```

Có thể đổi `EXCEL` thành `PDF`, dùng `fromDate` và `toDate` thay cho `reportingPeriod`, hoặc thêm `parkingLotId`, `district`, `reportTitle`. Hệ thống gom các lượt đã ra theo bãi, tính số lượt và tiền thực thu, rồi trả trực tiếp file. Nếu có lượt hoàn tất thiếu số tiền, API báo lỗi thay vì xuất báo cáo doanh thu chưa đầy đủ.

### 4.8. Đối chiếu tiền thực thu với bảng giá

Gọi `GET /gdci/parking/parkingLot/parkingVisit/{id}/tariff-comparison?dayType=WEEKDAY`. `dayType` phải là `WEEKDAY`, `WEEKEND` hoặc `HOLIDAY`; hệ thống chưa có lịch ngày lễ để tự xác định.

API tìm mức giá theo bãi, loại xe, loại ngày và thời gian hiệu lực. Nếu có duy nhất một mức giá phù hợp, lượt đỗ trong cùng một ngày và tròn giờ, hệ thống tính số tiền theo giờ đầu và các giờ tiếp theo, rồi so với tiền thực thu. Trường hợp xe chưa ra, qua ngày, giờ lẻ, không có giá hoặc nhiều giá phù hợp thì trả `canCompare: false` cùng lý do. Không tự thay số tiền thực thu trong database.

Loại xe trong file cần khớp mã bảng giá, ví dụ `MOTORBIKE`, `CAR_9_SEATS`. Giá đang được seed là **giá giả lập**, không phải biểu giá vận hành thật.

## 5. Database và dữ liệu mẫu

Database phát triển nằm ở `database/sqlite/parking_business_result.db`; file `.db` không đưa lên Git. Các bảng chính liên quan gồm:

| Bảng | Nội dung |
| --- | --- |
| `SP_donViKinhDoanhQuanLy` | Đơn vị quản lý bãi. |
| `DL_KTHT_baiDoXe` | Thông tin bãi đỗ xe. |
| `SP_ketQuaKinhDoanhBaiDoXe` | Kết quả kinh doanh tổng hợp theo bãi và kỳ; có cột xóa mềm `daXoa`. |
| `SP_khungGiaThuPhi` | Khung giá của bãi. |
| `SP_chiTietMucThuKhungGia` | Mức giá theo loại xe và loại ngày. |
| `SP_apDungKhungGia` | Thông tin áp dụng khung giá cho bãi. |
| `SP_datTruocChoDoXe` | Bảng booking theo Excel, dùng lưu các lượt xe vào/ra và số tiền đã thu. |
| `dev_parking_sessions` | Lượt xe **giả lập** chỉ dùng phát triển, không phải dữ liệu vận hành. |
| `dev_auth_users` | Tài khoản và mật khẩu băm phục vụ Development. |

Script dữ liệu mẫu tạo 5 bãi, mỗi bãi có 50 lượt giả lập: 30 lượt hoàn tất và 20 lượt đang trong bãi tại thời điểm tạo. Đây là **dữ liệu minh họa**, không phải biển số, giá hoặc số liệu thực tế. Chi tiết ở [DEVELOPMENT_PARKING_DATA.md](docs/DEVELOPMENT_PARKING_DATA.md); phần mô tả chức năng tại cuối file đó có thể cũ, hãy dùng tài liệu này và mã hiện tại để xem trạng thái API.

Các script migration nằm trong `database/sqlite/`. `migrate_visits_to_booking.py` sao lưu rồi chuyển bảng `parking_visit` cũ sang `SP_datTruocChoDoXe`; dữ liệu phương thức thanh toán cũ chỉ nằm trong bản sao lưu. Không đưa bản sao lưu hoặc database cục bộ lên Git.

## 6. Chạy và kiểm thử

1. Cài .NET 10 SDK và chuẩn bị SQLite theo hướng dẫn trong [README.md](README.md).
2. Cấu hình issuer, audience, khóa ký JWT và tài khoản quản trị Development bằng biến môi trường như phần **Tài khoản cục bộ cho Development** trong README. Không đưa mật khẩu hoặc khóa thật vào Git.
3. Chạy:

   ```powershell
   dotnet build SP_ParkingBusinessResult.sln
   dotnet run --project Backend/ParkingBusinessResult.Api
   ```

4. Import [collection kết quả kinh doanh](postman/ParkingBusinessResult.postman_collection.json) hoặc [collection lượt xe](postman/ParkingVisits.postman_collection.json) vào Postman. Cách cấu hình biến, đăng nhập và chọn file nằm ở [postman/README.md](postman/README.md).

Một số bài kiểm tra liên quan:

```powershell
dotnet run --project tests/RevenueMetrics.Checks/RevenueMetrics.Checks.csproj
powershell -NoProfile -ExecutionPolicy Bypass -File tests/parking_visit_import_checks.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File tests/seeded_parking_api_checks.ps1
```

Các bài kiểm tra HTTP này dùng database tạm. Kiểm thử XLS cần công cụ tạo file `.xls` cho fixture; nếu không có thì phần XLS có thể được bỏ qua. Collection Postman đã có kiểm thử cấu trúc và có thể chạy bằng Newman theo script trong `tests/`.

## 7. Những phần chưa hoàn chỉnh

- API chỉ được đăng ký ở Development; cơ chế định danh, phân quyền và cấu hình production chưa tích hợp.
- Body/response của một số API chưa khớp ApiSpec. Import từng lượt xe là yêu cầu mới, trong khi tài liệu gốc mô tả file số liệu tổng hợp.
- Hai nguồn báo cáo chưa được tự đồng bộ; không nên cộng chúng với nhau. Một số bộ lọc và chỉ số của API tổng hợp cũ còn thiếu.
- Chưa có nguồn dữ liệu chính thức đầy đủ cho chi phí vận hành và tỷ lệ lấp đầy; chưa thể coi các chỉ số đó là đã làm.
- Đối chiếu giá chưa tính giờ lẻ, qua ngày hoặc tự xác định ngày lễ. Dữ liệu giá mẫu không phải giá thực tế; PDF thử nghiệm hiện cần Windows.
- Chưa có chức năng nhập/quản lý bãi và đơn vị quản lý trong module này; GIS và khóa ngoại quy hoạch chưa được chốt đầy đủ.

Các khác biệt cụ thể theo route, body, response và vai trò được ghi tại [API_CONTRACT_REVIEW.md](docs/API_CONTRACT_REVIEW.md); những điểm cần thống nhất thêm nằm ở [CONTRACT_ISSUES.md](CONTRACT_ISSUES.md).
