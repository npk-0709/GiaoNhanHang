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

## Đối tượng CSDL nâng cao (Functions, Views, Triggers, Stored Procedures)
File script: [`Database/sql_objects.sql`](file:///c:/Users/Khuong/Desktop/ThuHuyen/GiaoNhanHang/GiaoNhanHang/Database/sql_objects.sql) (tương thích SQL Server 2008 R2, 2012, 2014, 2016+).

### 1. Functions (Hàm SQL)
- **Scalar Functions**:
  - `dbo.FN_TinhCuocPhi(@KhoiLuong)`: Tính đơn giá cước từ bảng giá theo khối lượng (kg).
  - `dbo.FN_TinhCuocCoHeSo(@KhoiLuong, @MaLoaiHang)`: Tính cước có nhân hệ số phụ phí theo loại hàng.
  - `dbo.FN_TrangThaiVanDon(@MaVanDon)`: Trả về trạng thái hiện tại của vận đơn.
  - `dbo.FN_SoDonTrongThang(@MaKH, @Thang, @Nam)`: Đếm số đơn hàng khách tạo trong tháng.
  - `dbo.FN_KiemTraSoDu(@MaKH, @SoTien)`: Kiểm tra khách hàng có đủ số dư ví để rút không.
- **Table-Valued Functions (TVF)**:
  - `dbo.FN_HanhTrinhVanDon(@MaVanDon)`: Trả về bảng hành trình giao nhận kèm tên và SĐT shipper.
  - `dbo.FN_VanDonCuaKhach(@MaKH, @TrangThai)`: Trả về danh sách đơn hàng của khách hàng theo trạng thái.

### 2. Views (Khung nhìn)
- `VW_DanhSachVanDon`: Tổng hợp thông tin vận đơn kèm thông tin người gửi, người nhận, nhân viên, bảng giá.
- `VW_ThongKeTrangThai`: Thống kê số đơn, tổng tiền, khối lượng TB theo từng trạng thái.
- `VW_DoanhThuTheoNgay`: Thống kê doanh thu, số đơn, đơn giao, đơn hoàn và COD theo từng ngày.
- `VW_HieuSuatNhanVien`: Thống kê số đơn giao thành công, hoàn hàng và tỷ lệ thành công của từng nhân viên.
- `VW_KhachHangThongKe`: Thống kê số dư, tổng đơn, tổng cước gửi của từng khách hàng.
- `VW_BaoCaoTongHop`: Báo cáo doanh thu, sản lượng, COD tổng hợp theo tháng/năm.

### 3. Triggers (Ràng buộc & Tự động hóa)
- `TRG_VanDon_SauCapNhatTrangThai`: Tự động ghi vết hành trình vào `HanhTrinhGiaoHang` khi trạng thái đơn đổi.
- `TRG_ChiTietVanDon_CapNhatTongKL`: Tự động tính lại `TrongLuongTong` và `TongTien` trong `VanDon` khi thêm/sửa/xóa dòng mặt hàng `ChiTietVanDon`.
- `TRG_GiaoDich_CapNhatSoDu`: Tự động cộng/trừ số dư `KhachHang.SoDu` ngay khi có bản ghi mới trong `GiaoDichTaiKhoan`.
- `TRG_KhuyenMai_KiemTraHanSuDung`: Ngăn chặn tạo mã khuyến mãi đã hết hạn.

### 4. Stored Procedures (Thủ tục lưu trữ)
- `SP_TaoVanDon`: Tạo vận đơn hoàn chỉnh trong 1 transaction (sinh mã, tính cước, thêm chi tiết, ghi hành trình).
- `SP_CapNhatTrangThaiVanDon`: Cập nhật trạng thái vận đơn, kiểm tra tính hợp lệ và ghi vết hành trình.
- `SP_DuyetRutTien`: Duyệt hoặc từ chối yêu cầu rút tiền của khách hàng (kiểm tra số dư bằng FN, ghi giao dịch ví, kích hoạt trigger trừ tiền).
- `SP_BaoCaoDoanhThu`: Truy vấn báo cáo doanh thu theo khoảng ngày chọn lọc.
- `SP_ThongKeNhanVien`: Báo cáo xếp hạng hiệu suất shipper theo khoảng ngày.
- `SP_TraCuuVanDon`: Tra cứu thông tin vận đơn và toàn bộ lịch sử hành trình giao nhận.

## Lưu ý
- Mật khẩu băm SHA-256 không salt, chỉ phù hợp bài tập. Hệ thống thật nên dùng ASP.NET Core Identity.
- Giao diện dùng Bootstrap và font Google qua CDN nên cần có Internet khi chạy.

