using System.ComponentModel.DataAnnotations;

namespace GiaoNhanHang.Models;

public class KhachHang
{
    [Key] public int MaKH { get; set; }
    [Required(ErrorMessage = "Nhập tên khách hàng"), StringLength(100)] public string TenKH { get; set; } = "";
    [Required(ErrorMessage = "Nhập số điện thoại"), StringLength(15)] public string SoDienThoai { get; set; } = "";
    [StringLength(250)] public string? DiaChi { get; set; }
    [StringLength(50)] public string? TenDangNhap { get; set; }
    [StringLength(64)] public string? MatKhauHash { get; set; }
    [StringLength(100)] public string? Email { get; set; }
    public decimal SoDu { get; set; } = 0;
}

public class BuuCuc
{
    [Key] public int MaBuuCuc { get; set; }
    [Required(ErrorMessage = "Nhập tên bưu cục"), StringLength(100)] public string TenBuuCuc { get; set; } = "";
    [StringLength(250)] public string? DiaChi { get; set; }
    [StringLength(15)] public string? Hotline { get; set; }
}

public class NhanVien
{
    [Key] public int MaNV { get; set; }
    [StringLength(100)] public string TenNV { get; set; } = "";
    [StringLength(50)] public string ChucVu { get; set; } = "";
    [StringLength(15)] public string? SoDienThoai { get; set; }
    public int? MaBuuCuc { get; set; }
    [StringLength(50)] public string TenDangNhap { get; set; } = "";
    [StringLength(64)] public string MatKhauHash { get; set; } = "";
    [StringLength(20)] public string VaiTro { get; set; } = "TiepNhan";
    public BuuCuc? BuuCuc { get; set; }
}

public class BangGia
{
    [Key] public int MaBangGia { get; set; }
    [Range(0, 100000, ErrorMessage = "Khối lượng từ không hợp lệ")] public decimal KhoiLuongTu { get; set; }
    [Range(0.01, 100000, ErrorMessage = "Khối lượng đến không hợp lệ")] public decimal KhoiLuongDen { get; set; }
    [Range(0, 100000000, ErrorMessage = "Đơn giá không hợp lệ")] public decimal DonGia { get; set; }
}

public class LoaiHangHoa
{
    [Key] public int MaLoaiHang { get; set; }
    [Required(ErrorMessage = "Nhập tên loại hàng"), StringLength(100)] public string TenLoaiHang { get; set; } = "";
    [Range(1, 10, ErrorMessage = "Hệ số phụ phí từ 1 đến 10")] public decimal HeSoPhuPhi { get; set; } = 1;
}

public class VanDon
{
    [Key, StringLength(20)] public string MaVanDon { get; set; } = "";
    public int MaKH_Gui { get; set; }
    public int MaKH_Nhan { get; set; }
    public int MaNV_Nhan { get; set; }
    public int? MaBangGia { get; set; }
    public DateTime NgayTao { get; set; } = DateTime.Now;
    public decimal TrongLuongTong { get; set; }
    public decimal TongTien { get; set; }
    [StringLength(30)] public string TrangThai { get; set; } = "Chờ lấy hàng";

    [StringLength(50)] public string? KichThuoc { get; set; }
    public decimal GiaTriBuuGui { get; set; } = 0;
    [StringLength(50)] public string? MaDonKhachHang { get; set; }
    public decimal ThuCOD { get; set; } = 0;
    [StringLength(20)] public string? NguoiTraCuoc { get; set; } = "NguoiGui";
    [StringLength(250)] public string? HuongDanGiaoHang { get; set; }
    [StringLength(50)] public string? LoaiDichVu { get; set; } = "TieuChuan";
    public DateTime? DuKienGiao { get; set; }

    public KhachHang? NguoiGui { get; set; }
    public KhachHang? NguoiNhan { get; set; }
    public NhanVien? NhanVienTiepNhan { get; set; }
    public BangGia? BangGia { get; set; }
    public List<ChiTietVanDon> ChiTiet { get; set; } = new();
    public List<HanhTrinhGiaoHang> HanhTrinh { get; set; } = new();
}

public class ChiTietVanDon
{
    [Key] public int MaChiTiet { get; set; }
    public string MaVanDon { get; set; } = "";
    public int MaLoaiHang { get; set; }
    public int SoLuong { get; set; }
    public decimal TrongLuong { get; set; }
    public string? GhiChu { get; set; }
    public VanDon? VanDon { get; set; }
    public LoaiHangHoa? LoaiHangHoa { get; set; }
}

public class HanhTrinhGiaoHang
{
    [Key] public int MaHanhTrinh { get; set; }
    public string MaVanDon { get; set; } = "";
    public int MaNV_Giao { get; set; }
    public DateTime ThoiGianCapNhat { get; set; } = DateTime.Now;
    [StringLength(250)] public string ViTri { get; set; } = "";
    [StringLength(30)] public string TrangThai { get; set; } = "";
    [StringLength(250)] public string? TrangThaiChiTiet { get; set; }
    public VanDon? VanDon { get; set; }
    public NhanVien? NhanVien { get; set; }
}

public class KhuyenMai
{
    [Key, StringLength(20)] public string MaKM { get; set; } = "";
    [StringLength(100)] public string TenKM { get; set; } = "";
    public int PhanTramGiam { get; set; }
    public decimal ToiDa { get; set; }
    public DateTime HanSuDung { get; set; }
}

public class GiaoDichTaiKhoan
{
    [Key] public int MaGD { get; set; }
    public int MaKH { get; set; }
    public decimal SoTien { get; set; }
    [StringLength(50)] public string LoaiGD { get; set; } = "";
    public DateTime ThoiGian { get; set; } = DateTime.Now;
    [StringLength(250)] public string? GhiChu { get; set; }
    public KhachHang? KhachHang { get; set; }
}
