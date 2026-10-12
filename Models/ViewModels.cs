using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace GiaoNhanHang.Models;

public class ItemVM
{
    public int MaLoaiHang { get; set; }
    [Range(1, 100000, ErrorMessage = "Số lượng từ 1 trở lên")] public int SoLuong { get; set; } = 1;
    [Range(0.01, 100000, ErrorMessage = "Nhập trọng lượng (kg)")] public decimal TrongLuong { get; set; }
    [StringLength(250)] public string? GhiChu { get; set; }
}

public class VanDonCreateVM
{
    public int MaKH_Gui { get; set; }
    public int? MaKH_Nhan { get; set; }
    public List<ItemVM> Items { get; set; } = new();

    // Thông tin người gửi (hiển thị từ hồ sơ)
    public string? TenNguoiGui { get; set; }
    public string? SdtGui { get; set; }
    public string? DiaChiGui { get; set; }

    // Thông tin người nhận
    [Required(ErrorMessage = "Vui lòng nhập tên người nhận")]
    public string TenNhan { get; set; } = "";
    [Required(ErrorMessage = "Vui lòng nhập số điện thoại người nhận")]
    public string SdtNhan { get; set; } = "";
    [Required(ErrorMessage = "Vui lòng nhập địa chỉ người nhận")]
    public string DiaChiNhan { get; set; } = "";
    public string? EmailNhan { get; set; }

    [StringLength(50)] public string? KichThuoc { get; set; }
    public decimal GiaTriBuuGui { get; set; } = 0;
    public decimal ThuCOD { get; set; } = 0;
    [StringLength(50)] public string? MaDonKhachHang { get; set; }
    [StringLength(20)] public string? NguoiTraCuoc { get; set; } = "NguoiGui";
    [StringLength(250)] public string? HuongDanGiaoHang { get; set; }
    [StringLength(50)] public string? LoaiDichVu { get; set; } = "TieuChuan";
}

public class NhanVienForm
{
    public int MaNV { get; set; }
    [Required(ErrorMessage = "Nhập tên nhân viên"), StringLength(100)] public string TenNV { get; set; } = "";
    [Required(ErrorMessage = "Nhập chức vụ"), StringLength(50)] public string ChucVu { get; set; } = "";
    [StringLength(15)] public string? SoDienThoai { get; set; }
    public int? MaBuuCuc { get; set; }
    [Required(ErrorMessage = "Nhập tên đăng nhập"), StringLength(50)] public string TenDangNhap { get; set; } = "";
    [Required] public string VaiTro { get; set; } = "TiepNhan";
    public string? MatKhau { get; set; }
}

public class DashboardVM
{
    public int DonHomNay { get; set; }
    public decimal DoanhThuHomNay { get; set; }
    public decimal DoanhThuThang { get; set; }
    public double? TyLeThanhCong { get; set; }
    public Dictionary<string, int> TheoTrangThai { get; set; } = new();
    public List<VanDon> GanDay { get; set; } = new();
}

public class NgayDoanhThu
{
    public DateTime Ngay { get; set; }
    public int SoDon { get; set; }
    public decimal DoanhThu { get; set; }
}

public class NvHieuSuat
{
    public string Ten { get; set; } = "";
    public int DaGiao { get; set; }
    public int Hoan { get; set; }
}

public class BaoCaoVM
{
    public DateTime Tu { get; set; }
    public DateTime Den { get; set; }
    public int TongDon { get; set; }
    public decimal TongDoanhThu { get; set; }
    public int DaGiao { get; set; }
    public int Hoan { get; set; }
    public int DangXuLy { get; set; }
    public double? TyLeThanhCong { get; set; }
    public List<NgayDoanhThu> TheoNgay { get; set; } = new();
    public List<NvHieuSuat> TheoNhanVien { get; set; } = new();
}

/* =====================================================================
   SQL VIEW / TVF / SP – Mapped Models
   ===================================================================== */

/// <summary>Map với VW_DanhSachVanDon</summary>
[Keyless]
public class VW_DanhSachVanDon
{
    public string MaVanDon { get; set; } = "";
    public DateTime NgayTao { get; set; }
    public string TrangThai { get; set; } = "";
    public decimal TrongLuongTong { get; set; }
    public decimal TongTien { get; set; }
    public decimal ThuCOD { get; set; }
    public decimal GiaTriBuuGui { get; set; }
    public string? LoaiDichVu { get; set; }
    public string? HuongDanGiaoHang { get; set; }
    public DateTime? DuKienGiao { get; set; }
    // Người gửi
    public string TenNguoiGui { get; set; } = "";
    public string SdtNguoiGui { get; set; } = "";
    public string? DiaChiNguoiGui { get; set; }
    // Người nhận
    public string TenNguoiNhan { get; set; } = "";
    public string SdtNguoiNhan { get; set; } = "";
    public string? DiaChiNguoiNhan { get; set; }
    // Nhân viên
    public string TenNVTiepNhan { get; set; } = "";
    public string VaiTro { get; set; } = "";
    public string? TenBuuCuc { get; set; }
    // Bảng giá
    public decimal? KhoiLuongTu { get; set; }
    public decimal? KhoiLuongDen { get; set; }
    public decimal? DonGia { get; set; }
}

/// <summary>Map với VW_ThongKeTrangThai</summary>
[Keyless]
public class VW_ThongKeTrangThai
{
    public string TrangThai { get; set; } = "";
    public int SoDon { get; set; }
    public decimal TongDoanhThu { get; set; }
    public decimal KhoiLuongTrungBinh { get; set; }
    public DateTime? DonDauTien { get; set; }
    public DateTime? DonMoiNhat { get; set; }
}

/// <summary>Map với VW_DoanhThuTheoNgay</summary>
[Keyless]
public class VW_DoanhThuTheoNgay
{
    public DateTime Ngay { get; set; }
    public int SoDon { get; set; }
    public int DaGiao { get; set; }
    public int HoanHang { get; set; }
    public decimal DoanhThu { get; set; }
    public decimal TongCOD { get; set; }
}

/// <summary>Map với VW_HieuSuatNhanVien</summary>
[Keyless]
public class VW_HieuSuatNhanVien
{
    public int MaNV { get; set; }
    public string TenNV { get; set; } = "";
    public string VaiTro { get; set; } = "";
    public string? SoDienThoai { get; set; }
    public string? TenBuuCuc { get; set; }
    public int TongVanDon { get; set; }
    public int SoDonDaGiao { get; set; }
    public int SoDonHoan { get; set; }
    public decimal? TyLeThanhCong { get; set; }
}

/// <summary>Map với VW_KhachHangThongKe</summary>
[Keyless]
public class VW_KhachHangThongKe
{
    public int MaKH { get; set; }
    public string TenKH { get; set; } = "";
    public string SoDienThoai { get; set; } = "";
    public string? DiaChi { get; set; }
    public decimal SoDu { get; set; }
    public int TongSoDon { get; set; }
    public int SoDonDaGiao { get; set; }
    public int SoDonHoan { get; set; }
    public decimal TongCuocGui { get; set; }
    public DateTime? LanGuiGanNhat { get; set; }
}

/// <summary>Map với VW_BaoCaoTongHop</summary>
[Keyless]
public class VW_BaoCaoTongHop
{
    public int Nam { get; set; }
    public int Thang { get; set; }
    public int TongDon { get; set; }
    public int DaGiao { get; set; }
    public int HoanHang { get; set; }
    public int DangXuLy { get; set; }
    public decimal DoanhThu { get; set; }
    public decimal TongCOD { get; set; }
    public decimal TongKhoiLuong { get; set; }
}

/// <summary>Map với FN_HanhTrinhVanDon (TVF)</summary>
[Keyless]
public class HanhTrinhVanDonRow
{
    public int MaHanhTrinh { get; set; }
    public string MaVanDon { get; set; } = "";
    public DateTime ThoiGianCapNhat { get; set; }
    public string ViTri { get; set; } = "";
    public string TrangThai { get; set; } = "";
    public string? TrangThaiChiTiet { get; set; }
    public string TenNhanVien { get; set; } = "";
    public string? SdtNhanVien { get; set; }
}

/// <summary>Map với SP_BaoCaoDoanhThu / SP_ThongKeNhanVien</summary>
[Keyless]
public class SpDoanhThuRow
{
    public DateTime Ngay { get; set; }
    public int SoDon { get; set; }
    public int DaGiao { get; set; }
    public int HoanHang { get; set; }
    public decimal DoanhThu { get; set; }
    public decimal TongCOD { get; set; }
}

[Keyless]
public class SpNhanVienRow
{
    public int MaNV { get; set; }
    public string TenNV { get; set; } = "";
    public string VaiTro { get; set; } = "";
    public string? TenBuuCuc { get; set; }
    public int TongVanDon { get; set; }
    public int SoDaGiao { get; set; }
    public int SoHoan { get; set; }
    public double? TyLeThanhCong { get; set; }
}

