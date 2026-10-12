/* =====================================================================
   SQL OBJECTS: Functions, Views, Triggers, Stored Procedures
   Hệ thống Quản lý Giao Nhận Hàng
   Tương thích hoàn toàn SQL Server 2008 R2+
   ===================================================================== */
USE GiaoNhanHang;
GO

/* =====================================================================
   PHẦN 1: SCALAR FUNCTIONS (Hàm vô hướng)
   ===================================================================== */

-- -----------------------------------------------------------------------
-- FN_TinhCuocPhi: Tính cước phí dựa vào khối lượng (kg)
-- -----------------------------------------------------------------------
IF OBJECT_ID('dbo.FN_TinhCuocPhi', 'FN') IS NOT NULL
    DROP FUNCTION dbo.FN_TinhCuocPhi;
GO
CREATE FUNCTION dbo.FN_TinhCuocPhi (@KhoiLuong DECIMAL(10,2))
RETURNS DECIMAL(18,0)
AS
BEGIN
    DECLARE @DonGia DECIMAL(18,0);
    SELECT TOP 1 @DonGia = DonGia
    FROM BangGia
    WHERE @KhoiLuong > KhoiLuongTu AND @KhoiLuong <= KhoiLuongDen
    ORDER BY KhoiLuongTu;

    IF @DonGia IS NULL
        SELECT @DonGia = MAX(DonGia) FROM BangGia;

    RETURN ISNULL(@DonGia, 0);
END;
GO

-- -----------------------------------------------------------------------
-- FN_TinhCuocCoHeSo: Tính cước có nhân hệ số phụ phí loại hàng
-- -----------------------------------------------------------------------
IF OBJECT_ID('dbo.FN_TinhCuocCoHeSo', 'FN') IS NOT NULL
    DROP FUNCTION dbo.FN_TinhCuocCoHeSo;
GO
CREATE FUNCTION dbo.FN_TinhCuocCoHeSo (
    @KhoiLuong  DECIMAL(10,2),
    @MaLoaiHang INT
)
RETURNS DECIMAL(18,0)
AS
BEGIN
    DECLARE @DonGia DECIMAL(18,0);
    SET @DonGia = dbo.FN_TinhCuocPhi(@KhoiLuong);

    DECLARE @HeSo DECIMAL(4,2);
    SET @HeSo = 1.0;

    SELECT @HeSo = HeSoPhuPhi FROM LoaiHangHoa WHERE MaLoaiHang = @MaLoaiHang;

    RETURN CAST(@DonGia * @HeSo AS DECIMAL(18,0));
END;
GO

-- -----------------------------------------------------------------------
-- FN_TrangThaiVanDon: Trả về trạng thái hiện tại của vận đơn
-- -----------------------------------------------------------------------
IF OBJECT_ID('dbo.FN_TrangThaiVanDon', 'FN') IS NOT NULL
    DROP FUNCTION dbo.FN_TrangThaiVanDon;
GO
CREATE FUNCTION dbo.FN_TrangThaiVanDon (@MaVanDon VARCHAR(20))
RETURNS NVARCHAR(30)
AS
BEGIN
    DECLARE @TrangThai NVARCHAR(30);
    SELECT @TrangThai = TrangThai FROM VanDon WHERE MaVanDon = @MaVanDon;
    RETURN ISNULL(@TrangThai, N'Không tồn tại');
END;
GO

-- -----------------------------------------------------------------------
-- FN_SoDonTrongThang: Đếm số vận đơn trong tháng/năm cho một khách hàng
-- -----------------------------------------------------------------------
IF OBJECT_ID('dbo.FN_SoDonTrongThang', 'FN') IS NOT NULL
    DROP FUNCTION dbo.FN_SoDonTrongThang;
GO
CREATE FUNCTION dbo.FN_SoDonTrongThang (
    @MaKH  INT,
    @Thang INT,
    @Nam   INT
)
RETURNS INT
AS
BEGIN
    DECLARE @SoDon INT;
    SELECT @SoDon = COUNT(*)
    FROM VanDon
    WHERE MaKH_Gui = @MaKH
      AND MONTH(NgayTao) = @Thang
      AND YEAR(NgayTao)  = @Nam;
    RETURN ISNULL(@SoDon, 0);
END;
GO

-- -----------------------------------------------------------------------
-- FN_KiemTraSoDu: Kiểm tra khách hàng có đủ số dư không (1/0)
-- -----------------------------------------------------------------------
IF OBJECT_ID('dbo.FN_KiemTraSoDu', 'FN') IS NOT NULL
    DROP FUNCTION dbo.FN_KiemTraSoDu;
GO
CREATE FUNCTION dbo.FN_KiemTraSoDu (
    @MaKH   INT,
    @SoTien DECIMAL(18,0)
)
RETURNS BIT
AS
BEGIN
    DECLARE @SoDu DECIMAL(18,0);
    SET @SoDu = 0;
    SELECT @SoDu = SoDu FROM KhachHang WHERE MaKH = @MaKH;
    RETURN CASE WHEN @SoDu >= @SoTien THEN 1 ELSE 0 END;
END;
GO

/* =====================================================================
   PHẦN 2: TABLE-VALUED FUNCTION (Hàm trả về bảng)
   ===================================================================== */

-- -----------------------------------------------------------------------
-- FN_HanhTrinhVanDon: Lấy hành trình của một vận đơn theo thứ tự thời gian
-- -----------------------------------------------------------------------
IF OBJECT_ID('dbo.FN_HanhTrinhVanDon', 'IF') IS NOT NULL
    DROP FUNCTION dbo.FN_HanhTrinhVanDon;
GO
CREATE FUNCTION dbo.FN_HanhTrinhVanDon (@MaVanDon VARCHAR(20))
RETURNS TABLE
AS
RETURN
(
    SELECT
        h.MaHanhTrinh,
        h.MaVanDon,
        h.ThoiGianCapNhat,
        h.ViTri,
        h.TrangThai,
        h.TrangThaiChiTiet,
        n.TenNV          AS TenNhanVien,
        n.SoDienThoai    AS SdtNhanVien
    FROM HanhTrinhGiaoHang h
    INNER JOIN NhanVien n ON h.MaNV_Giao = n.MaNV
    WHERE h.MaVanDon = @MaVanDon
);
GO

-- -----------------------------------------------------------------------
-- FN_VanDonCuaKhach: Lấy danh sách vận đơn của một khách hàng
-- -----------------------------------------------------------------------
IF OBJECT_ID('dbo.FN_VanDonCuaKhach', 'IF') IS NOT NULL
    DROP FUNCTION dbo.FN_VanDonCuaKhach;
GO
CREATE FUNCTION dbo.FN_VanDonCuaKhach (
    @MaKH      INT,
    @TrangThai NVARCHAR(30) = NULL
)
RETURNS TABLE
AS
RETURN
(
    SELECT
        v.MaVanDon,
        v.NgayTao,
        v.TrongLuongTong,
        v.TongTien,
        v.TrangThai,
        v.ThuCOD,
        v.LoaiDichVu,
        kn.TenKH        AS TenNguoiNhan,
        kn.SoDienThoai  AS SdtNguoiNhan,
        kn.DiaChi       AS DiaChiNguoiNhan
    FROM VanDon v
    INNER JOIN KhachHang kn ON v.MaKH_Nhan = kn.MaKH
    WHERE v.MaKH_Gui = @MaKH
      AND (@TrangThai IS NULL OR v.TrangThai = @TrangThai)
);
GO

/* =====================================================================
   PHẦN 3: VIEWS (Khung nhìn)
   ===================================================================== */

-- -----------------------------------------------------------------------
-- VW_DanhSachVanDon: View tổng hợp vận đơn kèm thông tin liên quan
-- -----------------------------------------------------------------------
IF OBJECT_ID('dbo.VW_DanhSachVanDon', 'V') IS NOT NULL
    DROP VIEW dbo.VW_DanhSachVanDon;
GO
CREATE VIEW dbo.VW_DanhSachVanDon AS
SELECT
    v.MaVanDon,
    v.NgayTao,
    v.TrangThai,
    v.TrongLuongTong,
    v.TongTien,
    v.ThuCOD,
    v.GiaTriBuuGui,
    v.LoaiDichVu,
    v.HuongDanGiaoHang,
    v.DuKienGiao,
    -- Người gửi
    kg.TenKH            AS TenNguoiGui,
    kg.SoDienThoai      AS SdtNguoiGui,
    kg.DiaChi           AS DiaChiNguoiGui,
    -- Người nhận
    kn.TenKH            AS TenNguoiNhan,
    kn.SoDienThoai      AS SdtNguoiNhan,
    kn.DiaChi           AS DiaChiNguoiNhan,
    -- Nhân viên tiếp nhận
    nv.TenNV            AS TenNVTiepNhan,
    nv.VaiTro,
    bc.TenBuuCuc,
    -- Bảng giá áp dụng
    bg.KhoiLuongTu,
    bg.KhoiLuongDen,
    bg.DonGia
FROM VanDon v
INNER JOIN KhachHang  kg ON v.MaKH_Gui  = kg.MaKH
INNER JOIN KhachHang  kn ON v.MaKH_Nhan = kn.MaKH
INNER JOIN NhanVien   nv ON v.MaNV_Nhan = nv.MaNV
LEFT  JOIN BuuCuc     bc ON nv.MaBuuCuc = bc.MaBuuCuc
LEFT  JOIN BangGia    bg ON v.MaBangGia  = bg.MaBangGia;
GO

-- -----------------------------------------------------------------------
-- VW_ThongKeTrangThai: Thống kê số lượng vận đơn theo trạng thái
-- -----------------------------------------------------------------------
IF OBJECT_ID('dbo.VW_ThongKeTrangThai', 'V') IS NOT NULL
    DROP VIEW dbo.VW_ThongKeTrangThai;
GO
CREATE VIEW dbo.VW_ThongKeTrangThai AS
SELECT
    TrangThai,
    COUNT(*)                            AS SoDon,
    SUM(TongTien)                       AS TongDoanhThu,
    AVG(TrongLuongTong)                 AS KhoiLuongTrungBinh,
    MIN(NgayTao)                        AS DonDauTien,
    MAX(NgayTao)                        AS DonMoiNhat
FROM VanDon
GROUP BY TrangThai;
GO

-- -----------------------------------------------------------------------
-- VW_DoanhThuTheoNgay: Doanh thu từng ngày (không tính hoàn hàng)
-- -----------------------------------------------------------------------
IF OBJECT_ID('dbo.VW_DoanhThuTheoNgay', 'V') IS NOT NULL
    DROP VIEW dbo.VW_DoanhThuTheoNgay;
GO
CREATE VIEW dbo.VW_DoanhThuTheoNgay AS
SELECT
    CAST(NgayTao AS DATE)   AS Ngay,
    COUNT(*)                AS SoDon,
    COUNT(CASE WHEN TrangThai = N'Đã giao'    THEN 1 END) AS DaGiao,
    COUNT(CASE WHEN TrangThai = N'Hoàn hàng'  THEN 1 END) AS HoanHang,
    SUM(CASE WHEN TrangThai <> N'Hoàn hàng'
             THEN TongTien ELSE 0 END)          AS DoanhThu,
    SUM(ThuCOD)                                 AS TongCOD
FROM VanDon
GROUP BY CAST(NgayTao AS DATE);
GO

-- -----------------------------------------------------------------------
-- VW_HieuSuatNhanVien: Hiệu suất giao hàng của từng nhân viên
-- -----------------------------------------------------------------------
IF OBJECT_ID('dbo.VW_HieuSuatNhanVien', 'V') IS NOT NULL
    DROP VIEW dbo.VW_HieuSuatNhanVien;
GO
CREATE VIEW dbo.VW_HieuSuatNhanVien AS
SELECT
    n.MaNV,
    n.TenNV,
    n.VaiTro,
    n.SoDienThoai,
    bc.TenBuuCuc,
    COUNT(DISTINCT h.MaVanDon)                          AS TongVanDon,
    COUNT(CASE WHEN h.TrangThai = N'Đã giao'   THEN 1 END) AS SoDonDaGiao,
    COUNT(CASE WHEN h.TrangThai = N'Hoàn hàng' THEN 1 END) AS SoDonHoan,
    CASE WHEN COUNT(CASE WHEN h.TrangThai IN (N'Đã giao', N'Hoàn hàng') THEN 1 END) = 0
         THEN NULL
         ELSE CAST(
             100.0 * COUNT(CASE WHEN h.TrangThai = N'Đã giao' THEN 1 END)
             / COUNT(CASE WHEN h.TrangThai IN (N'Đã giao', N'Hoàn hàng') THEN 1 END)
             AS DECIMAL(5,2))
    END                                                 AS TyLeThanhCong
FROM NhanVien n
LEFT JOIN HanhTrinhGiaoHang h ON n.MaNV = h.MaNV_Giao
LEFT JOIN BuuCuc bc ON n.MaBuuCuc = bc.MaBuuCuc
GROUP BY n.MaNV, n.TenNV, n.VaiTro, n.SoDienThoai, bc.TenBuuCuc;
GO

-- -----------------------------------------------------------------------
-- VW_KhachHangThongKe: Thống kê giao dịch của từng khách hàng
-- -----------------------------------------------------------------------
IF OBJECT_ID('dbo.VW_KhachHangThongKe', 'V') IS NOT NULL
    DROP VIEW dbo.VW_KhachHangThongKe;
GO
CREATE VIEW dbo.VW_KhachHangThongKe AS
SELECT
    k.MaKH,
    k.TenKH,
    k.SoDienThoai,
    k.DiaChi,
    k.SoDu,
    COUNT(v.MaVanDon)                                   AS TongSoDon,
    COUNT(CASE WHEN v.TrangThai = N'Đã giao'   THEN 1 END) AS SoDonDaGiao,
    COUNT(CASE WHEN v.TrangThai = N'Hoàn hàng' THEN 1 END) AS SoDonHoan,
    ISNULL(SUM(CASE WHEN v.TrangThai <> N'Hoàn hàng'
                    THEN v.TongTien ELSE 0 END), 0)     AS TongCuocGui,
    MAX(v.NgayTao)                                      AS LanGuiGanNhat
FROM KhachHang k
LEFT JOIN VanDon v ON k.MaKH = v.MaKH_Gui
GROUP BY k.MaKH, k.TenKH, k.SoDienThoai, k.DiaChi, k.SoDu;
GO

-- -----------------------------------------------------------------------
-- VW_BaoCaoTongHop: Báo cáo tổng hợp theo tháng
-- -----------------------------------------------------------------------
IF OBJECT_ID('dbo.VW_BaoCaoTongHop', 'V') IS NOT NULL
    DROP VIEW dbo.VW_BaoCaoTongHop;
GO
CREATE VIEW dbo.VW_BaoCaoTongHop AS
SELECT
    YEAR(NgayTao)   AS Nam,
    MONTH(NgayTao)  AS Thang,
    COUNT(*)        AS TongDon,
    COUNT(CASE WHEN TrangThai = N'Đã giao'    THEN 1 END) AS DaGiao,
    COUNT(CASE WHEN TrangThai = N'Hoàn hàng'  THEN 1 END) AS HoanHang,
    COUNT(CASE WHEN TrangThai NOT IN (N'Đã giao', N'Hoàn hàng') THEN 1 END) AS DangXuLy,
    SUM(CASE WHEN TrangThai <> N'Hoàn hàng' THEN TongTien ELSE 0 END) AS DoanhThu,
    SUM(ThuCOD)     AS TongCOD,
    SUM(TrongLuongTong) AS TongKhoiLuong
FROM VanDon
GROUP BY YEAR(NgayTao), MONTH(NgayTao);
GO

/* =====================================================================
   PHẦN 4: TRIGGERS
   ===================================================================== */

-- -----------------------------------------------------------------------
-- TRG_VanDon_SauCapNhatTrangThai:
--   Khi VanDon.TrangThai thay đổi → tự động ghi vào HanhTrinhGiaoHang
-- -----------------------------------------------------------------------
IF OBJECT_ID('dbo.TRG_VanDon_SauCapNhatTrangThai', 'TR') IS NOT NULL
    DROP TRIGGER dbo.TRG_VanDon_SauCapNhatTrangThai;
GO
CREATE TRIGGER dbo.TRG_VanDon_SauCapNhatTrangThai
ON VanDon
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT UPDATE(TrangThai) RETURN;

    INSERT INTO HanhTrinhGiaoHang (MaVanDon, MaNV_Giao, ThoiGianCapNhat, ViTri, TrangThai, TrangThaiChiTiet)
    SELECT
        i.MaVanDon,
        i.MaNV_Nhan,
        GETDATE(),
        N'Hệ thống (tự động)',
        i.TrangThai,
        N'[AUTO] Cập nhật: ' + d.TrangThai + N' -> ' + i.TrangThai
    FROM inserted i
    INNER JOIN deleted d ON i.MaVanDon = d.MaVanDon
    WHERE i.TrangThai <> d.TrangThai;
END;
GO

-- -----------------------------------------------------------------------
-- TRG_ChiTietVanDon_CapNhatTongKL:
--   Sau khi thêm/sửa/xóa ChiTietVanDon → cập nhật TrongLuongTong và TongTien
-- -----------------------------------------------------------------------
IF OBJECT_ID('dbo.TRG_ChiTietVanDon_CapNhatTongKL', 'TR') IS NOT NULL
    DROP TRIGGER dbo.TRG_ChiTietVanDon_CapNhatTongKL;
GO
CREATE TRIGGER dbo.TRG_ChiTietVanDon_CapNhatTongKL
ON ChiTietVanDon
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @AffectedVanDon TABLE (MaVanDon VARCHAR(20));

    INSERT INTO @AffectedVanDon SELECT DISTINCT MaVanDon FROM inserted;
    INSERT INTO @AffectedVanDon SELECT DISTINCT MaVanDon FROM deleted;

    UPDATE v
    SET
        v.TrongLuongTong = ISNULL(agg.TongKL, 0),
        v.TongTien       = ISNULL(agg.TongTien, 0)
    FROM VanDon v
    INNER JOIN @AffectedVanDon af ON v.MaVanDon = af.MaVanDon
    LEFT JOIN (
        SELECT
            ct.MaVanDon,
            SUM(ct.TrongLuong)                                          AS TongKL,
            SUM(dbo.FN_TinhCuocCoHeSo(ct.TrongLuong, ct.MaLoaiHang))  AS TongTien
        FROM ChiTietVanDon ct
        WHERE ct.MaVanDon IN (SELECT MaVanDon FROM @AffectedVanDon)
        GROUP BY ct.MaVanDon
    ) agg ON v.MaVanDon = agg.MaVanDon;
END;
GO

-- -----------------------------------------------------------------------
-- TRG_GiaoDich_CapNhatSoDu:
--   Sau khi INSERT vào GiaoDichTaiKhoan → cộng/trừ SoDu KhachHang
-- -----------------------------------------------------------------------
IF OBJECT_ID('dbo.TRG_GiaoDich_CapNhatSoDu', 'TR') IS NOT NULL
    DROP TRIGGER dbo.TRG_GiaoDich_CapNhatSoDu;
GO
CREATE TRIGGER dbo.TRG_GiaoDich_CapNhatSoDu
ON GiaoDichTaiKhoan
AFTER INSERT
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE k
    SET k.SoDu = k.SoDu + i.SoTien
    FROM KhachHang k
    INNER JOIN inserted i ON k.MaKH = i.MaKH
    WHERE i.SoTien <> 0;
END;
GO

-- -----------------------------------------------------------------------
-- TRG_KhuyenMai_KiemTraHanSuDung:
--   Ngăn INSERT khuyến mãi đã hết hạn
-- -----------------------------------------------------------------------
IF OBJECT_ID('dbo.TRG_KhuyenMai_KiemTraHanSuDung', 'TR') IS NOT NULL
    DROP TRIGGER dbo.TRG_KhuyenMai_KiemTraHanSuDung;
GO
CREATE TRIGGER dbo.TRG_KhuyenMai_KiemTraHanSuDung
ON KhuyenMai
INSTEAD OF INSERT
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (SELECT 1 FROM inserted WHERE HanSuDung < GETDATE())
    BEGIN
        RAISERROR(N'Không thể thêm khuyến mãi đã hết hạn.', 16, 1);
        RETURN;
    END

    INSERT INTO KhuyenMai (MaKM, TenKM, PhanTramGiam, ToiDa, HanSuDung)
    SELECT MaKM, TenKM, PhanTramGiam, ToiDa, HanSuDung FROM inserted;
END;
GO

/* =====================================================================
   PHẦN 5: STORED PROCEDURES
   ===================================================================== */

-- -----------------------------------------------------------------------
-- SP_TaoVanDon: Tạo vận đơn mới (gộp toàn bộ logic thành 1 transaction)
-- -----------------------------------------------------------------------
IF OBJECT_ID('dbo.SP_TaoVanDon', 'P') IS NOT NULL
    DROP PROCEDURE dbo.SP_TaoVanDon;
GO
CREATE PROCEDURE dbo.SP_TaoVanDon
    @MaKH_Gui        INT,
    @MaKH_Nhan       INT,
    @MaNV_Nhan       INT,
    @KhoiLuong       DECIMAL(10,2),
    @MaLoaiHang      INT,
    @SoLuong         INT,
    @GhiChu          NVARCHAR(250) = NULL,
    @KichThuoc       VARCHAR(50)   = NULL,
    @GiaTriBuuGui    DECIMAL(18,0) = 0,
    @ThuCOD          DECIMAL(18,0) = 0,
    @NguoiTraCuoc    VARCHAR(20)   = 'NguoiGui',
    @HuongDanGH      NVARCHAR(250) = NULL,
    @LoaiDichVu      NVARCHAR(50)  = N'TieuChuan',
    @ViTri           NVARCHAR(250) = N'Bưu cục',
    -- OUTPUT
    @MaVanDon        VARCHAR(20)   OUTPUT,
    @TongTien        DECIMAL(18,0) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        BEGIN TRANSACTION;

        -- Tính cước phí
        DECLARE @DonGia DECIMAL(18,0);
        SET @DonGia = dbo.FN_TinhCuocCoHeSo(@KhoiLuong, @MaLoaiHang);
        SET @TongTien = @DonGia;

        -- Lấy MaBangGia phù hợp
        DECLARE @MaBangGia INT;
        SELECT TOP 1 @MaBangGia = MaBangGia
        FROM BangGia
        WHERE @KhoiLuong > KhoiLuongTu AND @KhoiLuong <= KhoiLuongDen
        ORDER BY KhoiLuongTu;

        -- Sinh mã vận đơn: 'VD' + yymmdd + 4 số
        DECLARE @Prefix VARCHAR(10);
        SET @Prefix = 'VD' + CONVERT(VARCHAR(6), GETDATE(), 12);

        DECLARE @SoDon INT;
        SELECT @SoDon = COUNT(*) FROM VanDon WHERE MaVanDon LIKE @Prefix + '%';
        SET @MaVanDon = @Prefix + RIGHT('0000' + CAST(@SoDon + 1 AS VARCHAR), 4);

        -- Tạo vận đơn
        INSERT INTO VanDon
            (MaVanDon, MaKH_Gui, MaKH_Nhan, MaNV_Nhan, MaBangGia,
             NgayTao, TrongLuongTong, TongTien, TrangThai,
             KichThuoc, GiaTriBuuGui, ThuCOD, NguoiTraCuoc,
             HuongDanGiaoHang, LoaiDichVu)
        VALUES
            (@MaVanDon, @MaKH_Gui, @MaKH_Nhan, @MaNV_Nhan, @MaBangGia,
             GETDATE(), @KhoiLuong, @TongTien, N'Chờ lấy hàng',
             @KichThuoc, @GiaTriBuuGui, @ThuCOD, @NguoiTraCuoc,
             @HuongDanGH, @LoaiDichVu);

        -- Tạo chi tiết vận đơn
        INSERT INTO ChiTietVanDon (MaVanDon, MaLoaiHang, SoLuong, TrongLuong, GhiChu)
        VALUES (@MaVanDon, @MaLoaiHang, @SoLuong, @KhoiLuong, @GhiChu);

        -- Ghi hành trình ban đầu
        INSERT INTO HanhTrinhGiaoHang
            (MaVanDon, MaNV_Giao, ThoiGianCapNhat, ViTri, TrangThai, TrangThaiChiTiet)
        VALUES
            (@MaVanDon, @MaNV_Nhan, GETDATE(), @ViTri, N'Chờ lấy hàng', N'Đã tiếp nhận vận đơn');

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        DECLARE @ErrMsg NVARCHAR(4000), @ErrSev INT, @ErrState INT;
        SELECT @ErrMsg = ERROR_MESSAGE(), @ErrSev = ERROR_SEVERITY(), @ErrState = ERROR_STATE();
        RAISERROR(@ErrMsg, @ErrSev, @ErrState);
    END CATCH
END;
GO

-- -----------------------------------------------------------------------
-- SP_CapNhatTrangThaiVanDon: Cập nhật trạng thái vận đơn
-- -----------------------------------------------------------------------
IF OBJECT_ID('dbo.SP_CapNhatTrangThaiVanDon', 'P') IS NOT NULL
    DROP PROCEDURE dbo.SP_CapNhatTrangThaiVanDon;
GO
CREATE PROCEDURE dbo.SP_CapNhatTrangThaiVanDon
    @MaVanDon   VARCHAR(20),
    @TrangThai  NVARCHAR(30),
    @MaNV       INT,
    @ViTri      NVARCHAR(250),
    @GhiChu     NVARCHAR(250) = NULL,
    @KetQua     NVARCHAR(200) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @TrangThaiHienTai NVARCHAR(30);
    SELECT @TrangThaiHienTai = TrangThai FROM VanDon WHERE MaVanDon = @MaVanDon;

    IF @TrangThaiHienTai IS NULL
    BEGIN
        SET @KetQua = N'Lỗi: Không tìm thấy vận đơn ' + @MaVanDon;
        RETURN;
    END

    IF @TrangThaiHienTai IN (N'Đã giao', N'Hoàn hàng')
    BEGIN
        SET @KetQua = N'Lỗi: Vận đơn ' + @MaVanDon + N' đã kết thúc, không thể cập nhật.';
        RETURN;
    END

    IF @TrangThai NOT IN (N'Chờ lấy hàng', N'Đã nhập kho', N'Đang vận chuyển', N'Đang giao', N'Đã giao', N'Hoàn hàng')
    BEGIN
        SET @KetQua = N'Lỗi: Trạng thái không hợp lệ.';
        RETURN;
    END

    BEGIN TRY
        BEGIN TRANSACTION;

        UPDATE VanDon SET TrangThai = @TrangThai WHERE MaVanDon = @MaVanDon;

        INSERT INTO HanhTrinhGiaoHang (MaVanDon, MaNV_Giao, ThoiGianCapNhat, ViTri, TrangThai, TrangThaiChiTiet)
        VALUES (@MaVanDon, @MaNV, GETDATE(), @ViTri, @TrangThai, @GhiChu);

        COMMIT TRANSACTION;
        SET @KetQua = N'Thành công: Đã cập nhật ' + @MaVanDon + N' -> ' + @TrangThai;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        SET @KetQua = N'Lỗi: ' + ERROR_MESSAGE();
    END CATCH
END;
GO

-- -----------------------------------------------------------------------
-- SP_DuyetRutTien: Duyệt hoặc từ chối yêu cầu rút tiền
-- -----------------------------------------------------------------------
IF OBJECT_ID('dbo.SP_DuyetRutTien', 'P') IS NOT NULL
    DROP PROCEDURE dbo.SP_DuyetRutTien;
GO
CREATE PROCEDURE dbo.SP_DuyetRutTien
    @MaYeuCau INT,
    @Hanh     NVARCHAR(10),   -- 'DaDuyet' | 'TuChoi'
    @KetQua   NVARCHAR(300) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @MaKH     INT;
    DECLARE @SoTien   DECIMAL(18,0);
    DECLARE @NganHang NVARCHAR(100);
    DECLARE @SoTK     NVARCHAR(30);
    DECLARE @TrangThai NVARCHAR(20);

    SELECT @MaKH = MaKH, @SoTien = SoTien, @TrangThai = TrangThai,
           @NganHang = NganHang, @SoTK = SoTaiKhoan
    FROM YeuCauRutTien WHERE MaYeuCau = @MaYeuCau;

    IF @MaKH IS NULL
    BEGIN
        SET @KetQua = N'Lỗi: Yêu cầu không tồn tại.';
        RETURN;
    END

    IF @TrangThai <> N'ChoDuyet'
    BEGIN
        SET @KetQua = N'Lỗi: Yêu cầu đã được xử lý trước đó.';
        RETURN;
    END

    IF @Hanh = N'DaDuyet'
    BEGIN
        IF dbo.FN_KiemTraSoDu(@MaKH, @SoTien) = 0
        BEGIN
            SET @KetQua = N'Lỗi: Số dư khách hàng không đủ.';
            RETURN;
        END

        BEGIN TRY
            BEGIN TRANSACTION;

            UPDATE YeuCauRutTien
            SET TrangThai = N'DaDuyet', ThoiGianDuyet = GETDATE()
            WHERE MaYeuCau = @MaYeuCau;

            -- Ghi giao dịch trừ tiền (Trigger TRG_GiaoDich_CapNhatSoDu sẽ cập nhật SoDu KhachHang)
            INSERT INTO GiaoDichTaiKhoan (MaKH, SoTien, LoaiGD, ThoiGian, GhiChu)
            VALUES (@MaKH, -@SoTien, N'Rút tiền', GETDATE(),
                    N'Rút tiền về ' + @NganHang + N' - STK ' + @SoTK + N' (YC #' + CAST(@MaYeuCau AS VARCHAR) + N')');

            COMMIT TRANSACTION;
            SET @KetQua = N'Thành công: Đã duyệt rút tiền thành công cho yêu cầu #' + CAST(@MaYeuCau AS VARCHAR);
        END TRY
        BEGIN CATCH
            IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
            SET @KetQua = N'Lỗi: ' + ERROR_MESSAGE();
        END CATCH
    END
    ELSE IF @Hanh = N'TuChoi'
    BEGIN
        UPDATE YeuCauRutTien
        SET TrangThai = N'TuChoi', ThoiGianDuyet = GETDATE()
        WHERE MaYeuCau = @MaYeuCau;

        SET @KetQua = N'Thành công: Đã từ chối yêu cầu rút tiền #' + CAST(@MaYeuCau AS VARCHAR);
    END
    ELSE
    BEGIN
        SET @KetQua = N'Lỗi: Hành động không hợp lệ (DaDuyet/TuChoi).';
    END
END;
GO

-- -----------------------------------------------------------------------
-- SP_BaoCaoDoanhThu: Lấy báo cáo doanh thu theo khoảng ngày
-- -----------------------------------------------------------------------
IF OBJECT_ID('dbo.SP_BaoCaoDoanhThu', 'P') IS NOT NULL
    DROP PROCEDURE dbo.SP_BaoCaoDoanhThu;
GO
CREATE PROCEDURE dbo.SP_BaoCaoDoanhThu
    @TuNgay  DATE = NULL,
    @DenNgay DATE = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF @TuNgay  IS NULL SET @TuNgay  = CAST(DATEADD(DAY, -29, GETDATE()) AS DATE);
    IF @DenNgay IS NULL SET @DenNgay = CAST(GETDATE() AS DATE);

    DECLARE @DenExcl DATE;
    SET @DenExcl = DATEADD(DAY, 1, @DenNgay);

    SELECT
        CAST(NgayTao AS DATE)   AS Ngay,
        COUNT(*)                AS SoDon,
        COUNT(CASE WHEN TrangThai = N'Đã giao'   THEN 1 END) AS DaGiao,
        COUNT(CASE WHEN TrangThai = N'Hoàn hàng' THEN 1 END) AS HoanHang,
        SUM(CASE WHEN TrangThai <> N'Hoàn hàng'
                 THEN TongTien ELSE 0 END)        AS DoanhThu,
        SUM(ThuCOD)                               AS TongCOD
    FROM VanDon
    WHERE NgayTao >= @TuNgay AND NgayTao < @DenExcl
    GROUP BY CAST(NgayTao AS DATE)
    ORDER BY Ngay;
END;
GO

-- -----------------------------------------------------------------------
-- SP_ThongKeNhanVien: Thống kê hiệu suất nhân viên theo khoảng ngày
-- -----------------------------------------------------------------------
IF OBJECT_ID('dbo.SP_ThongKeNhanVien', 'P') IS NOT NULL
    DROP PROCEDURE dbo.SP_ThongKeNhanVien;
GO
CREATE PROCEDURE dbo.SP_ThongKeNhanVien
    @TuNgay  DATE = NULL,
    @DenNgay DATE = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF @TuNgay  IS NULL SET @TuNgay  = CAST(DATEADD(DAY, -29, GETDATE()) AS DATE);
    IF @DenNgay IS NULL SET @DenNgay = CAST(GETDATE() AS DATE);

    DECLARE @DenExcl DATETIME;
    SET @DenExcl = DATEADD(DAY, 1, CAST(@DenNgay AS DATETIME));

    SELECT
        n.MaNV,
        n.TenNV,
        n.VaiTro,
        bc.TenBuuCuc,
        COUNT(DISTINCT h.MaVanDon)                              AS TongVanDon,
        COUNT(CASE WHEN h.TrangThai = N'Đã giao'   THEN 1 END) AS SoDaGiao,
        COUNT(CASE WHEN h.TrangThai = N'Hoàn hàng' THEN 1 END) AS SoHoan,
        CASE
            WHEN COUNT(CASE WHEN h.TrangThai IN (N'Đã giao', N'Hoàn hàng') THEN 1 END) = 0
            THEN NULL
            ELSE ROUND(
                100.0 * COUNT(CASE WHEN h.TrangThai = N'Đã giao' THEN 1 END)
                / COUNT(CASE WHEN h.TrangThai IN (N'Đã giao', N'Hoàn hàng') THEN 1 END), 1)
        END AS TyLeThanhCong
    FROM NhanVien n
    LEFT JOIN HanhTrinhGiaoHang h
        ON n.MaNV = h.MaNV_Giao
        AND h.ThoiGianCapNhat >= @TuNgay
        AND h.ThoiGianCapNhat <  @DenExcl
    LEFT JOIN BuuCuc bc ON n.MaBuuCuc = bc.MaBuuCuc
    GROUP BY n.MaNV, n.TenNV, n.VaiTro, bc.TenBuuCuc
    ORDER BY SoDaGiao DESC;
END;
GO

-- -----------------------------------------------------------------------
-- SP_TraCuuVanDon: Tra cứu vận đơn (dùng cho tra cứu nhanh)
-- -----------------------------------------------------------------------
IF OBJECT_ID('dbo.SP_TraCuuVanDon', 'P') IS NOT NULL
    DROP PROCEDURE dbo.SP_TraCuuVanDon;
GO
CREATE PROCEDURE dbo.SP_TraCuuVanDon
    @MaVanDon VARCHAR(20)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT * FROM VW_DanhSachVanDon
    WHERE MaVanDon = @MaVanDon;

    SELECT * FROM dbo.FN_HanhTrinhVanDon(@MaVanDon)
    ORDER BY ThoiGianCapNhat;
END;
GO
