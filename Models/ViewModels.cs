using System.ComponentModel.DataAnnotations;

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
