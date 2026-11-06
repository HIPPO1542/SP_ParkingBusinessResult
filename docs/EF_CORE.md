# EF Core và migration

Ứng dụng dùng EF Core SQLite **10.0.14** trong Infrastructure. Domain và Application giữ entity, interface và handler CQRS, không tham chiếu thư viện EF. SQLite hiện có được tiếp tục sử dụng.

## Cấu trúc

- `Infrastructure/Persistence/ParkingDbContext.cs`: DbSet và đăng ký các configuration trong assembly.
- `Infrastructure/Persistence/Models/`: model lưu trữ của 8 bảng đang được ứng dụng sử dụng, tách khỏi entity miền. Model lượt xe cũ `DevParkingSessions` đã được gỡ.
- `Infrastructure/Persistence/Configurations/`: mỗi model có một IEntityTypeConfiguration, gồm cột, quan hệ, index, default và check constraint.
- `Infrastructure/Persistence/ParkingDbContext.Functions.cs`: hàm SQL tìm chuỗi không phân biệt hoa/thường, giữ cách tìm tiếng Việt.
- `Infrastructure/Persistence/ParkingContextFactory.cs`: factory runtime được đăng ký qua DI, tạo context riêng cho mỗi thao tác/session. `ParkingDesignTimeFactory.cs` cung cấp context cho công cụ migration.
- `Infrastructure/Persistence/Migrations/`: initial migration, designer và model snapshot.

Tất cả repository nghiệp vụ dùng DbContext. Phần đọc dùng LINQ và AsNoTracking; thêm bản ghi dùng SaveChanges; cập nhật/xóa mềm dùng ExecuteUpdate. Import dùng ExecuteSqlInterpolated trong transaction EF để giữ ON CONFLICT DO NOTHING của SQLite. Đối chiếu giá dùng SqlQuery có tham số để giữ cách tính ngày hiệu lực bằng julianday. Health probe dùng truy vấn SQLite nhỏ để kiểm tra storage.

Danh sách booking dùng COUNT và truy vấn trang riêng trong một giao dịch đọc ngắn, bảo đảm cùng một snapshot. Import giữ một giao dịch cho cả batch: lỗi hệ thống/hủy trước commit rollback toàn bộ phần đang ghi; dòng sai nghiệp vụ hoặc trùng trả lỗi riêng. Các trường audit lấy từ token.

Bảng booking giữ 16 cột. ID và ngày audit dạng text, tiền dạng REAL được giữ theo schema cũ. Repository chuyển model lưu trữ string/double sang kiểu Guid/decimal của Domain/Application. `dev_auth_users` tiếp tục phục vụ tài khoản Local; `dev_parking_sessions` chỉ còn trong lịch sử migration và có thể còn vật lý trên database cũ, không thuộc EF model hiện tại.

## Công cụ và lệnh migration

Migration `RemoveLegacyParkingSessions` chỉ xóa bảng `dev_parking_sessions`. Bản cài cục bộ đã áp dụng sau khi người dùng phê duyệt và database được backup. Với database khác, cần duyệt việc loại bỏ dữ liệu cũ và backup trước khi cho phép cập nhật đến migration này; không tự chạy `database update` trên database đang sử dụng. `Down` chỉ tái tạo cấu trúc bảng, không phục hồi các dòng đã xóa.

Chạy từ thư mục gốc:

```powershell
dotnet tool restore
$env:ParkingBusinessResult__DatabasePath = 'H:\ParkingData\parking_business_result.db'
dotnet ef migrations list --project Backend/ParkingBusinessResult.Infrastructure
```

Database mới chưa có bảng được tạo bằng:

```powershell
dotnet ef database update --project Backend/ParkingBusinessResult.Infrastructure
```

Initial migration dùng DDL gốc, giữ check constraint, collation, khóa ngoại và index. Không seed tài khoản, bãi hay lượt xe.

Database cũ đã có bảng cần ghi nhận baseline trước khi cập nhật bằng EF. Tạo một database đối chiếu rỗng bằng migration rồi chạy:

```powershell
$env:ParkingBusinessResult__DatabasePath = 'H:\ParkingData\ef-schema.db'
dotnet ef database update InitialParkingSchema --project Backend/ParkingBusinessResult.Infrastructure
python database/sqlite/adopt_ef_baseline.py --database 'H:\ParkingData\parking_business_result.db' --template 'H:\ParkingData\ef-schema.db'
$env:ParkingBusinessResult__DatabasePath = 'H:\ParkingData\parking_business_result.db'
```

Script đối chiếu bảng, cột, khóa ngoại, index, predicate và DDL. Schema khác sẽ dừng. Schema khớp sẽ được backup, sau đó chỉ thêm lịch sử __EFMigrationsHistory, giữ nguyên các dòng nghiệp vụ. Không chạy initial CREATE TABLE trực tiếp lên database cũ. Chặn thay đổi schema trong thời gian ghi nhận baseline. Chạy lại được nếu lịch sử đã trùng.

Thay đổi schema tiếp theo phải có migration riêng:

```powershell
dotnet ef migrations add TenThayDoi --project Backend/ParkingBusinessResult.Infrastructure --output-dir Persistence/Migrations
dotnet ef migrations script --project Backend/ParkingBusinessResult.Infrastructure
```

Review SQL và backup trước khi áp dụng. Ứng dụng Production không tự gọi Migrate hoặc EnsureCreated. Các script SQL/Python trước EF được giữ làm lịch sử chuyển đổi; không chạy lại sau khi đã ghi nhận baseline.

Migration `AlignSoftDeleteIndex` đồng bộ model/snapshot với index đã lọc `daXoa = 0`. Sau khi ghi nhận baseline trên database cũ, chạy `database update` để áp dụng các migration tiếp theo. Báo cáo tổng hợp lọc quyền, kỳ, bãi, từ khóa và phân trang ngay trong truy vấn EF.
