# Giao Nhận — Quản lý vận đơn và điều phối giao nhận hàng hóa

Ứng dụng web ASP.NET Core MVC (.NET 8) + Entity Framework Core + SQL Server, xây theo
Chương 3 và Chương 4 của đề tài. Bố cục tham khảo Haravan Ship: trang giới thiệu, tra cứu
vận đơn công khai, khu quản trị có thanh bên.

## Chạy thử
1. Cài .NET 8 SDK và SQL Server (hoặc SQL Server Express / LocalDB).
2. Mở `Database/GiaoNhanHang.sql` trong SSMS và chạy toàn bộ (tạo CSDL, 8 bảng, dữ liệu mẫu).
3. Sửa chuỗi kết nối trong `appsettings.json` nếu cần:
   - Windows Authentication: `Server=localhost;Database=GiaoNhanHang;Trusted_Connection=True;TrustServerCertificate=True;`
   - SQL Server Express: đổi `Server=localhost\SQLEXPRESS`
   - SQL login: `Server=localhost;Database=GiaoNhanHang;User Id=sa;Password=...;TrustServerCertificate=True;`
4. Trong thư mục dự án chạy: `dotnet restore` rồi `dotnet run`, mở địa chỉ hiện ra (ví dụ https://localhost:5001).
   Hoặc mở `GiaoNhanHang.csproj` bằng Visual Studio 2022 và bấm F5.

## Tài khoản mẫu
| Vai trò | Tên đăng nhập | Mật khẩu |
|---|---|---|
| Quản lý | admin | admin123 |
| Nhân viên tiếp nhận | tiepnhan | 123456 |
| Nhân viên giao hàng | giaohang | 123456 |

Mã vận đơn mẫu để thử tra cứu: xem trong menu Vận đơn (dạng `VD` + yyMMdd + 4 số).

## Phân quyền
- Quản lý: toàn bộ chức năng, danh mục, báo cáo.
- Tiếp nhận: lập vận đơn, khách hàng, cập nhật trạng thái.
- Giao hàng: xem vận đơn và cập nhật trạng thái.

## Ánh xạ với đề tài
| Đề tài | Trong ứng dụng |
|---|---|
| Quản lý danh mục | Khách hàng, Nhân viên, Bưu cục/kho, Bảng giá, Loại hàng hóa |
| Lập vận đơn nhận hàng | Vận đơn → Lập vận đơn (tự tính cước theo Bảng giá) |
| Điều phối, xuất/nhập kho, cập nhật trạng thái | Chi tiết vận đơn và “Cập nhật nhanh” (quét mã vạch hoặc nhập tay) |
| Tra cứu vận đơn | Trang chủ → Tra cứu (không cần đăng nhập, che bớt thông tin cá nhân) |
| Báo cáo doanh thu, hiệu suất giao hàng | Báo cáo (theo khoảng ngày, theo nhân viên) |

## Khác biệt so với 8 bảng trong tài liệu (cần ghi vào báo cáo)
- `NhanVien` thêm `TenDangNhap`, `MatKhauHash`, `VaiTro` để đăng nhập và phân quyền.
- `VanDon` thêm `MaBangGia` (khớp quan hệ BangGia–VanDon trong ERD) và `TrongLuongTong`.
- `LoaiHangHoa` thêm `HeSoPhuPhi` (hàng dễ vỡ, cồng kềnh tính phụ phí).
- `ChiTietVanDon` dùng khóa chính riêng `MaChiTiet` để một đơn có thể có nhiều dòng cùng loại hàng.
- `HanhTrinhGiaoHang` thêm cột `TrangThai` (trạng thái mới), `TrangThaiChiTiet` là mô tả.

## Công thức cước
Cước = đơn giá của bậc giá chứa tổng khối lượng × hệ số phụ phí lớn nhất của các loại hàng trong đơn.
Vượt bậc cuối thì dùng bậc cuối (xem `Data/CuocPhi.cs`).

## Lưu ý
- Mật khẩu băm SHA-256 không salt, chỉ phù hợp bài tập. Hệ thống thật nên dùng ASP.NET Core Identity.
- Giao diện dùng Bootstrap và font Google qua CDN nên cần có Internet khi chạy.
