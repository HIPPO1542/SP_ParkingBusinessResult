# SP_ParkingBusinessResult

Khung ASP.NET Core Minimal API theo DDD, CQRS và Clean Architecture. Dự án dùng .NET 10 SDK hiện có trên máy; phiên bản dùng chung của nhóm cần được xác nhận trước khi tích hợp.

- Domain: mô hình miền và hợp đồng repository.
- Application: command, query và handler theo từng tác vụ.
- Infrastructure: ánh xạ và truy cập dữ liệu.
- Api: Minimal API và hợp đồng response.

Thành công trả HTTP 200 với code 0, message, data. Lỗi nghiệp vụ trả HTTP 200 với code 7 và message; không có metadata. Hợp đồng response đã khai báo nhưng xử lý lỗi, xác thực và endpoint chưa được kết nối.

Theo sheet SP_ParkingBusinessResult, tên bảng là SP_ketQuaKinhDoanhBaiDoXe; các cột gồm id, baiDoXeId, kyBaoCao, doanhThu, soLuotXe, idNguoiTao, idNguoiCapNhat, nguoiCapNhat, ngayTao, nguoiTao, ngayCapNhat, ghiChu. Chưa có schema CSDL thực tế để xác minh tên vật lý, ràng buộc và kết nối.

Cần chốt nguồn dữ liệu cho các chỉ số, quy tắc kỳ báo cáo, cơ chế xác thực, phân quyền và xử lý trùng trong cùng file. Skill đề xuất cột phiên bản để kiểm soát đồng thời, nhưng sheet không có cột này; không tự thay đổi schema.

Chạy `dotnet build SP_ParkingBusinessResult.sln` và `dotnet run --project src/ParkingBusinessResult.Api`. Chưa đăng ký endpoint nghiệp vụ, kết nối CSDL, MediatR, Swagger hoặc kiểm thử tích hợp.

Mô hình kết quả kinh doanh sử dụng tổng số lượt xe hoàn tất (một xe vào rồi ra là một lượt). Doanh thu và số lượt không được âm. Phép tổng hợp tính doanh thu bình quân từ tổng doanh thu chia tổng số lượt, làm tròn hai chữ số; khi không có lượt xe, giá trị bằng 0 theo LLD UC 3.4. Kiểm tra các quy tắc này bằng `dotnet run --project tests/RevenueMetrics.Checks`.
