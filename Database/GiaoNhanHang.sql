/* =====================================================================
   CSDL: Quản lý vận đơn và điều phối giao nhận hàng hóa (SQL Server)
   Chạy toàn bộ script này trong SSMS. Có thể chạy lại nhiều lần (xóa và tạo mới).
   Tài khoản mẫu: admin/admin123, tiepnhan/123456, giaohang/123456
   ===================================================================== */
IF DB_ID(N'GiaoNhanHang') IS NULL
    CREATE DATABASE GiaoNhanHang COLLATE Vietnamese_CI_AS;
GO
USE GiaoNhanHang;
GO

DROP TABLE IF EXISTS GiaoDichTaiKhoan;
DROP TABLE IF EXISTS KhuyenMai;
DROP TABLE IF EXISTS HanhTrinhGiaoHang;
DROP TABLE IF EXISTS ChiTietVanDon;
DROP TABLE IF EXISTS VanDon;
DROP TABLE IF EXISTS NhanVien;
DROP TABLE IF EXISTS BuuCuc;
DROP TABLE IF EXISTS LoaiHangHoa;
DROP TABLE IF EXISTS BangGia;
DROP TABLE IF EXISTS KhachHang;
GO

/* ---------- 1. KhachHang ---------- */
CREATE TABLE KhachHang (
    MaKH        INT IDENTITY(1,1) CONSTRAINT PK_KhachHang PRIMARY KEY,
    TenKH       NVARCHAR(100) NOT NULL,
    SoDienThoai VARCHAR(15)   NOT NULL,
    DiaChi      NVARCHAR(250) NULL,
    TenDangNhap VARCHAR(50)   NULL CONSTRAINT UQ_KhachHang_TenDangNhap UNIQUE,
    MatKhauHash VARCHAR(64)   NULL,
    Email       VARCHAR(100)  NULL,
    SoDu        DECIMAL(18,0) NOT NULL CONSTRAINT DF_KhachHang_SoDu DEFAULT 0
);

/* ---------- 2. BuuCuc (kho hàng) ---------- */
CREATE TABLE BuuCuc (
    MaBuuCuc  INT IDENTITY(1,1) CONSTRAINT PK_BuuCuc PRIMARY KEY,
    TenBuuCuc NVARCHAR(100) NOT NULL,
    DiaChi    NVARCHAR(250) NULL,
    Hotline   VARCHAR(15)   NULL
);

/* ---------- 3. NhanVien (có thêm cột đăng nhập) ---------- */
CREATE TABLE NhanVien (
    MaNV        INT IDENTITY(1,1) CONSTRAINT PK_NhanVien PRIMARY KEY,
    TenNV       NVARCHAR(100) NOT NULL,
    ChucVu      NVARCHAR(50)  NOT NULL,
    SoDienThoai VARCHAR(15)   NULL,
    MaBuuCuc    INT NULL CONSTRAINT FK_NhanVien_BuuCuc REFERENCES BuuCuc(MaBuuCuc),
    TenDangNhap VARCHAR(50)   NOT NULL CONSTRAINT UQ_NhanVien_TenDangNhap UNIQUE,
    MatKhauHash VARCHAR(64)   NOT NULL,   -- SHA-256 dạng HEX viết hoa
    VaiTro      VARCHAR(20)   NOT NULL CONSTRAINT CK_NhanVien_VaiTro CHECK (VaiTro IN ('QuanLy','TiepNhan','GiaoHang'))
);

/* ---------- 4. BangGia (cước theo bậc khối lượng, kg) ---------- */
CREATE TABLE BangGia (
    MaBangGia    INT IDENTITY(1,1) CONSTRAINT PK_BangGia PRIMARY KEY,
    KhoiLuongTu  DECIMAL(10,2) NOT NULL,
    KhoiLuongDen DECIMAL(10,2) NOT NULL,
    DonGia       DECIMAL(18,0) NOT NULL,
    CONSTRAINT CK_BangGia_KhoangKL CHECK (KhoiLuongDen > KhoiLuongTu)
);

/* ---------- 5. LoaiHangHoa ---------- */
CREATE TABLE LoaiHangHoa (
    MaLoaiHang  INT IDENTITY(1,1) CONSTRAINT PK_LoaiHangHoa PRIMARY KEY,
    TenLoaiHang NVARCHAR(100) NOT NULL,
    HeSoPhuPhi  DECIMAL(4,2)  NOT NULL CONSTRAINT DF_LoaiHang_HeSo DEFAULT 1
);

/* ---------- 6. VanDon ---------- */
CREATE TABLE VanDon (
    MaVanDon       VARCHAR(20)   NOT NULL CONSTRAINT PK_VanDon PRIMARY KEY,
    MaKH_Gui       INT NOT NULL CONSTRAINT FK_VanDon_KHGui  REFERENCES KhachHang(MaKH),
    MaKH_Nhan      INT NOT NULL CONSTRAINT FK_VanDon_KHNhan REFERENCES KhachHang(MaKH),
    MaNV_Nhan      INT NOT NULL CONSTRAINT FK_VanDon_NV     REFERENCES NhanVien(MaNV),
    MaBangGia      INT NULL     CONSTRAINT FK_VanDon_BangGia REFERENCES BangGia(MaBangGia),
    NgayTao        DATETIME      NOT NULL CONSTRAINT DF_VanDon_NgayTao DEFAULT GETDATE(),
    TrongLuongTong DECIMAL(10,2) NOT NULL CONSTRAINT DF_VanDon_TL DEFAULT 0,
    TongTien       DECIMAL(18,0) NOT NULL CONSTRAINT DF_VanDon_Tien DEFAULT 0,
    TrangThai      NVARCHAR(30)  NOT NULL CONSTRAINT DF_VanDon_TT DEFAULT N'Chờ lấy hàng'
        CONSTRAINT CK_VanDon_TT CHECK (TrangThai IN (N'Chờ lấy hàng',N'Đã nhập kho',N'Đang vận chuyển',N'Đang giao',N'Đã giao',N'Hoàn hàng')),
    KichThuoc      VARCHAR(50)   NULL,
    GiaTriBuuGui   DECIMAL(18,0) NOT NULL CONSTRAINT DF_VanDon_GiaTri DEFAULT 0,
    MaDonKhachHang VARCHAR(50)   NULL,
    ThuCOD         DECIMAL(18,0) NOT NULL CONSTRAINT DF_VanDon_ThuCOD DEFAULT 0,
    NguoiTraCuoc   VARCHAR(20)   NOT NULL CONSTRAINT DF_VanDon_NguoiTraCuoc DEFAULT 'NguoiGui',
    HuongDanGiaoHang NVARCHAR(250) NULL,
    LoaiDichVu     NVARCHAR(50)  NOT NULL CONSTRAINT DF_VanDon_LoaiDichVu DEFAULT 'TieuChuan',
    DuKienGiao     DATETIME      NULL
);
CREATE INDEX IX_VanDon_TrangThai ON VanDon(TrangThai);
CREATE INDEX IX_VanDon_NgayTao   ON VanDon(NgayTao);

/* ---------- 7. ChiTietVanDon ---------- */
CREATE TABLE ChiTietVanDon (
    MaChiTiet  INT IDENTITY(1,1) CONSTRAINT PK_ChiTietVanDon PRIMARY KEY,
    MaVanDon   VARCHAR(20) NOT NULL CONSTRAINT FK_CT_VanDon  REFERENCES VanDon(MaVanDon) ON DELETE CASCADE,
    MaLoaiHang INT NOT NULL CONSTRAINT FK_CT_LoaiHang REFERENCES LoaiHangHoa(MaLoaiHang),
    SoLuong    INT NOT NULL CONSTRAINT CK_CT_SoLuong CHECK (SoLuong > 0),
    TrongLuong DECIMAL(10,2) NOT NULL CONSTRAINT CK_CT_TL CHECK (TrongLuong > 0),
    GhiChu     NVARCHAR(250) NULL
);

/* ---------- 8. HanhTrinhGiaoHang ---------- */
CREATE TABLE HanhTrinhGiaoHang (
    MaHanhTrinh      INT IDENTITY(1,1) CONSTRAINT PK_HanhTrinh PRIMARY KEY,
    MaVanDon         VARCHAR(20) NOT NULL CONSTRAINT FK_HT_VanDon REFERENCES VanDon(MaVanDon) ON DELETE CASCADE,
    MaNV_Giao        INT NOT NULL CONSTRAINT FK_HT_NV REFERENCES NhanVien(MaNV),
    ThoiGianCapNhat  DATETIME NOT NULL CONSTRAINT DF_HT_Time DEFAULT GETDATE(),
    ViTri            NVARCHAR(250) NOT NULL,
    TrangThai        NVARCHAR(30)  NOT NULL,
    TrangThaiChiTiet NVARCHAR(250) NULL
);
CREATE INDEX IX_HanhTrinh_VanDon ON HanhTrinhGiaoHang(MaVanDon, ThoiGianCapNhat);

/* ---------- 9. KhuyenMai ---------- */
CREATE TABLE KhuyenMai (
    MaKM         VARCHAR(20) NOT NULL CONSTRAINT PK_KhuyenMai PRIMARY KEY,
    TenKM        NVARCHAR(100) NOT NULL,
    PhanTramGiam INT NOT NULL CONSTRAINT CK_KhuyenMai_PhanTram CHECK (PhanTramGiam > 0 AND PhanTramGiam <= 100),
    ToiDa        DECIMAL(18,0) NOT NULL,
    HanSuDung    DATETIME NOT NULL
);

/* ---------- 10. GiaoDichTaiKhoan ---------- */
CREATE TABLE GiaoDichTaiKhoan (
    MaGD         INT IDENTITY(1,1) CONSTRAINT PK_GiaoDich PRIMARY KEY,
    MaKH         INT NOT NULL CONSTRAINT FK_GiaoDich_KH REFERENCES KhachHang(MaKH),
    SoTien       DECIMAL(18,0) NOT NULL,
    LoaiGD       NVARCHAR(50) NOT NULL,
    ThoiGian     DATETIME NOT NULL CONSTRAINT DF_GiaoDich_ThoiGian DEFAULT GETDATE(),
    GhiChu       NVARCHAR(250) NULL
);
GO

/* ===================== DỮ LIỆU MẪU ===================== */
INSERT INTO BuuCuc (TenBuuCuc, DiaChi, Hotline) VALUES
 (N'Bưu cục Quận 1',     N'12 Nguyễn Huệ, Quận 1, TP.HCM',         '19001001'),
 (N'Bưu cục Thủ Đức',    N'88 Võ Văn Ngân, TP. Thủ Đức, TP.HCM',   '19001002'),
 (N'Kho trung chuyển Bình Dương', N'KCN Sóng Thần, Dĩ An, Bình Dương', '19001003');

-- Mật khẩu = SHA-256 của chuỗi gốc (dùng VARCHAR để khớp mã hóa UTF-8 trong C#)
INSERT INTO NhanVien (TenNV, ChucVu, SoDienThoai, MaBuuCuc, TenDangNhap, MatKhauHash, VaiTro) VALUES
 (N'Nguyễn Quản Lý',  N'Trưởng bưu cục',          '0901000001', 1, 'admin',    CONVERT(VARCHAR(64), HASHBYTES('SHA2_256', 'admin123'), 2), 'QuanLy'),
 (N'Trần Thị Tiếp',   N'Nhân viên tiếp nhận',     '0901000002', 1, 'tiepnhan', CONVERT(VARCHAR(64), HASHBYTES('SHA2_256', '123456'),   2), 'TiepNhan'),
 (N'Lê Văn Giao',     N'Nhân viên giao hàng',     '0901000003', 2, 'giaohang', CONVERT(VARCHAR(64), HASHBYTES('SHA2_256', '123456'),   2), 'GiaoHang'),
 (N'Phạm Minh Tài',   N'Nhân viên giao hàng',     '0901000004', 2, 'giaohang2',CONVERT(VARCHAR(64), HASHBYTES('SHA2_256', '123456'),   2), 'GiaoHang');

INSERT INTO LoaiHangHoa (TenLoaiHang, HeSoPhuPhi) VALUES
 (N'Hàng thông thường', 1.00),
 (N'Hàng dễ vỡ',        1.20),
 (N'Hàng cồng kềnh',    1.50),
 (N'Hàng giá trị cao',  1.30),
 (N'Thực phẩm khô',     1.10);

INSERT INTO BangGia (KhoiLuongTu, KhoiLuongDen, DonGia) VALUES
 (0,    0.5,  18000),
 (0.5,  1,    22000),
 (1,    2,    28000),
 (2,    5,    40000),
 (5,    10,   65000),
 (10,   20,   110000),
 (20,   50,   220000),
 (50,   9999, 500000);

INSERT INTO KhachHang (TenKH, SoDienThoai, DiaChi, TenDangNhap, MatKhauHash) VALUES
 (N'Công ty TNHH Mộc Lam',  '0281234567', N'25 Lý Chính Thắng, Quận 3, TP.HCM', 'moclam', CONVERT(VARCHAR(64), HASHBYTES('SHA2_256', '123456'), 2)),
 (N'Nguyễn Thị Hoa',        '0912345678', N'14 Trần Hưng Đạo, TP. Biên Hòa, Đồng Nai', 'hoanguyen', CONVERT(VARCHAR(64), HASHBYTES('SHA2_256', '123456'), 2)),
 (N'Trần Quốc Bảo',         '0987654321', N'102 Nguyễn Trãi, Quận 5, TP.HCM', 'baotran', CONVERT(VARCHAR(64), HASHBYTES('SHA2_256', '123456'), 2)),
 (N'Cửa hàng Gốm Bát Tràng','0933222111', N'5 Lê Lợi, TP. Vũng Tàu', 'gombattrang', CONVERT(VARCHAR(64), HASHBYTES('SHA2_256', '123456'), 2)),
 (N'Phạm Thu Trang',        '0977111222', N'77 Hai Bà Trưng, TP. Thủ Dầu Một, Bình Dương', 'trangpham', CONVERT(VARCHAR(64), HASHBYTES('SHA2_256', '123456'), 2)),
 (N'Lê Hoàng Nam',          '0966333444', N'9 Điện Biên Phủ, Bình Thạnh, TP.HCM', 'namle', CONVERT(VARCHAR(64), HASHBYTES('SHA2_256', '123456'), 2));

/* Vận đơn mẫu (mã theo dạng VD + yyMMdd + 4 số) */
DECLARE @d0 VARCHAR(6) = FORMAT(GETDATE(), 'yyMMdd');
DECLARE @d1 VARCHAR(6) = FORMAT(DATEADD(DAY,-1,GETDATE()), 'yyMMdd');
DECLARE @d2 VARCHAR(6) = FORMAT(DATEADD(DAY,-2,GETDATE()), 'yyMMdd');
DECLARE @d3 VARCHAR(6) = FORMAT(DATEADD(DAY,-3,GETDATE()), 'yyMMdd');

INSERT INTO VanDon (MaVanDon, MaKH_Gui, MaKH_Nhan, MaNV_Nhan, MaBangGia, NgayTao, TrongLuongTong, TongTien, TrangThai) VALUES
 ('VD'+@d3+'0001', 1, 2, 2, 4, DATEADD(DAY,-3,GETDATE()), 3.50,  40000,  N'Đã giao'),
 ('VD'+@d3+'0002', 4, 3, 2, 3, DATEADD(DAY,-3,GETDATE()), 1.80,  33600,  N'Đã giao'),
 ('VD'+@d2+'0001', 1, 5, 2, 5, DATEADD(DAY,-2,GETDATE()), 7.00,  97500,  N'Hoàn hàng'),
 ('VD'+@d1+'0001', 3, 6, 2, 2, DATEADD(DAY,-1,GETDATE()), 0.80,  28600,  N'Đang giao'),
 ('VD'+@d0+'0001', 2, 1, 2, 3, GETDATE(),                 1.20,  30800,  N'Đang vận chuyển'),
 ('VD'+@d0+'0002', 5, 4, 2, 4, GETDATE(),                 4.00,  40000,  N'Đã nhập kho'),
 ('VD'+@d0+'0003', 6, 3, 2, 1, GETDATE(),                 0.40,  18000,  N'Chờ lấy hàng');

INSERT INTO ChiTietVanDon (MaVanDon, MaLoaiHang, SoLuong, TrongLuong, GhiChu) VALUES
 ('VD'+@d3+'0001', 1, 5, 3.50, N'Quần áo'),
 ('VD'+@d3+'0002', 2, 2, 1.80, N'Bình gốm, bọc xốp'),
 ('VD'+@d2+'0001', 3, 1, 7.00, N'Ghế gỗ tháo rời'),
 ('VD'+@d1+'0001', 4, 1, 0.80, N'Đồng hồ'),
 ('VD'+@d0+'0001', 5, 3, 1.20, N'Trà, bánh'),
 ('VD'+@d0+'0002', 1, 4, 4.00, N'Sách'),
 ('VD'+@d0+'0003', 1, 1, 0.40, N'Hồ sơ');

INSERT INTO HanhTrinhGiaoHang (MaVanDon, MaNV_Giao, ThoiGianCapNhat, ViTri, TrangThai, TrangThaiChiTiet) VALUES
 ('VD'+@d3+'0001', 2, DATEADD(HOUR,-72,GETDATE()), N'Bưu cục Quận 1',  N'Chờ lấy hàng',    N'Đã tiếp nhận vận đơn'),
 ('VD'+@d3+'0001', 2, DATEADD(HOUR,-70,GETDATE()), N'Bưu cục Quận 1',  N'Đã nhập kho',     N'Hàng đã nhập kho'),
 ('VD'+@d3+'0001', 3, DATEADD(HOUR,-50,GETDATE()), N'Đang trên đường',  N'Đang vận chuyển', N'Xuất kho, chuyển đến Biên Hòa'),
 ('VD'+@d3+'0001', 3, DATEADD(HOUR,-30,GETDATE()), N'TP. Biên Hòa',     N'Đang giao',       N'Nhân viên đang giao hàng'),
 ('VD'+@d3+'0001', 3, DATEADD(HOUR,-28,GETDATE()), N'TP. Biên Hòa',     N'Đã giao',         N'Người nhận đã ký nhận'),
 ('VD'+@d3+'0002', 2, DATEADD(HOUR,-71,GETDATE()), N'Bưu cục Quận 1',  N'Chờ lấy hàng',    N'Đã tiếp nhận vận đơn'),
 ('VD'+@d3+'0002', 4, DATEADD(HOUR,-40,GETDATE()), N'Quận 5',           N'Đã giao',         N'Giao thành công'),
 ('VD'+@d2+'0001', 2, DATEADD(HOUR,-48,GETDATE()), N'Bưu cục Quận 1',  N'Chờ lấy hàng',    N'Đã tiếp nhận vận đơn'),
 ('VD'+@d2+'0001', 4, DATEADD(HOUR,-20,GETDATE()), N'TP. Thủ Dầu Một',  N'Hoàn hàng',       N'Người nhận từ chối nhận hàng'),
 ('VD'+@d1+'0001', 2, DATEADD(HOUR,-24,GETDATE()), N'Bưu cục Quận 1',  N'Chờ lấy hàng',    N'Đã tiếp nhận vận đơn'),
 ('VD'+@d1+'0001', 3, DATEADD(HOUR,-3, GETDATE()), N'Bình Thạnh',       N'Đang giao',       N'Nhân viên đang giao hàng'),
 ('VD'+@d0+'0001', 2, DATEADD(HOUR,-6, GETDATE()), N'Bưu cục Thủ Đức', N'Chờ lấy hàng',    N'Đã tiếp nhận vận đơn'),
 ('VD'+@d0+'0001', 3, DATEADD(HOUR,-2, GETDATE()), N'Đang trên đường',  N'Đang vận chuyển', N'Xuất kho, chuyển đến Quận 3'),
 ('VD'+@d0+'0002', 2, DATEADD(HOUR,-4, GETDATE()), N'Bưu cục Quận 1',  N'Chờ lấy hàng',    N'Đã tiếp nhận vận đơn'),
 ('VD'+@d0+'0002', 2, DATEADD(HOUR,-1, GETDATE()), N'Bưu cục Quận 1',  N'Đã nhập kho',     N'Hàng đã nhập kho'),
 ('VD'+@d0+'0003', 2, GETDATE(),                   N'Bưu cục Quận 1',  N'Chờ lấy hàng',    N'Đã tiếp nhận vận đơn');
GO
