/* =====================================================================
   UPDATE SCRIPT - Thêm bảng YeuCauRutTien cho tính năng rút tiền
   Tương thích SQL Server 2008+
   Chạy script này sau khi đã có database GiaoNhanHang
   ===================================================================== */
USE GiaoNhanHang;
GO

/* ---------- Thêm bảng YeuCauRutTien ---------- */
IF NOT EXISTS (
    SELECT 1 FROM sys.tables WHERE name = N'YeuCauRutTien'
)
BEGIN
    CREATE TABLE YeuCauRutTien (
        MaYeuCau     INT IDENTITY(1,1) CONSTRAINT PK_YeuCauRutTien PRIMARY KEY,
        MaKH         INT NOT NULL CONSTRAINT FK_YeuCauRutTien_KH REFERENCES KhachHang(MaKH),
        SoTien       DECIMAL(18,0) NOT NULL,
        SoTaiKhoan   NVARCHAR(30)  NOT NULL,
        NganHang     NVARCHAR(100) NOT NULL,
        TrangThai    NVARCHAR(20)  NOT NULL CONSTRAINT DF_YCRT_TrangThai DEFAULT N'ChoDuyet'
                     CONSTRAINT CK_YCRT_TrangThai CHECK (TrangThai IN (N'ChoDuyet', N'DaDuyet', N'TuChoi')),
        ThoiGianTao  DATETIME NOT NULL CONSTRAINT DF_YCRT_ThoiGianTao DEFAULT GETDATE(),
        ThoiGianDuyet DATETIME NULL,
        GhiChu       NVARCHAR(250) NULL
    );
    PRINT N'[OK] Da tao bang YeuCauRutTien';
END
ELSE
BEGIN
    PRINT N'[SKIP] Bang YeuCauRutTien da ton tai';
END
GO

/* ---------- Đảm bảo cột SoDu tồn tại trong KhachHang ---------- */
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'KhachHang') AND name = N'SoDu'
)
BEGIN
    ALTER TABLE KhachHang ADD SoDu DECIMAL(18,0) NOT NULL CONSTRAINT DF_KhachHang_SoDu DEFAULT 0;
    PRINT N'[OK] Da them cot SoDu vao bang KhachHang';
END
ELSE
BEGIN
    PRINT N'[SKIP] Cot SoDu da ton tai trong KhachHang';
END
GO

/* ---------- Dữ liệu mẫu: nạp tiền cho khách hàng ID=1 để test ---------- */
IF NOT EXISTS (SELECT 1 FROM GiaoDichTaiKhoan WHERE MaKH = 1 AND LoaiGD = N'Nạp tiền (mẫu)')
BEGIN
    -- Cộng số dư mẫu cho KH 1
    UPDATE KhachHang SET SoDu = 500000 WHERE MaKH = 1;

    INSERT INTO GiaoDichTaiKhoan (MaKH, SoTien, LoaiGD, ThoiGian, GhiChu) VALUES
    (1, 500000, N'Nạp tiền (mẫu)', DATEADD(DAY, -5, GETDATE()), N'Nạp tiền demo ban đầu'),
    (1, 200000, N'Nạp tiền (mẫu)', DATEADD(DAY, -2, GETDATE()), N'Nạp tiền lần 2');

    PRINT N'[OK] Da them du lieu mau GiaoDichTaiKhoan';
END
ELSE
BEGIN
    PRINT N'[SKIP] Du lieu mau da ton tai';
END
GO

/* ---------- Ví dụ yêu cầu rút tiền mẫu ---------- */
IF NOT EXISTS (SELECT 1 FROM YeuCauRutTien WHERE MaKH = 1)
BEGIN
    INSERT INTO YeuCauRutTien (MaKH, SoTien, SoTaiKhoan, NganHang, TrangThai, ThoiGianTao) VALUES
    (1, 100000, N'1234567890', N'Vietcombank', N'ChoDuyet', DATEADD(HOUR, -2, GETDATE())),
    (2, 250000, N'9876543210', N'MB Bank',     N'DaDuyet',  DATEADD(DAY, -1, GETDATE()));

    -- Cập nhật thời gian duyệt cho bản ghi DaDuyet
    UPDATE YeuCauRutTien
    SET ThoiGianDuyet = DATEADD(HOUR, -20, GETDATE())
    WHERE TrangThai = N'DaDuyet' AND MaKH = 2;

    PRINT N'[OK] Da them du lieu mau YeuCauRutTien';
END
ELSE
BEGIN
    PRINT N'[SKIP] Du lieu mau YeuCauRutTien da ton tai';
END
GO

PRINT N'';
PRINT N'=== Update hoan tat! ===';
PRINT N'Cac bang da cap nhat:';
PRINT N'  - YeuCauRutTien (moi): quan ly yeu cau rut tien cua khach hang';
PRINT N'  - KhachHang.SoDu     : dam bao ton tai (khong thay doi neu da co)';
PRINT N'  - GiaoDichTaiKhoan   : them du lieu mau nap tien';
GO
