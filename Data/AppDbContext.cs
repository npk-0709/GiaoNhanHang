using GiaoNhanHang.Models;
using Microsoft.EntityFrameworkCore;

namespace GiaoNhanHang.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    /* ---- Bảng chính ---- */
    public DbSet<KhachHang>          KhachHangs          => Set<KhachHang>();
    public DbSet<BuuCuc>             BuuCucs             => Set<BuuCuc>();
    public DbSet<NhanVien>           NhanViens           => Set<NhanVien>();
    public DbSet<BangGia>            BangGias            => Set<BangGia>();
    public DbSet<LoaiHangHoa>        LoaiHangs           => Set<LoaiHangHoa>();
    public DbSet<VanDon>             VanDons             => Set<VanDon>();
    public DbSet<ChiTietVanDon>      ChiTiets            => Set<ChiTietVanDon>();
    public DbSet<HanhTrinhGiaoHang>  HanhTrinhs          => Set<HanhTrinhGiaoHang>();
    public DbSet<KhuyenMai>          KhuyenMais          => Set<KhuyenMai>();
    public DbSet<GiaoDichTaiKhoan>   GiaoDichTaiKhoans   => Set<GiaoDichTaiKhoan>();
    public DbSet<YeuCauRutTien>      YeuCauRutTiens      => Set<YeuCauRutTien>();

    /* ---- SQL VIEWS (Keyless Entities) ---- */
    public DbSet<VW_DanhSachVanDon>   VW_DanhSachVanDons   => Set<VW_DanhSachVanDon>();
    public DbSet<VW_ThongKeTrangThai> VW_ThongKeTrangThais => Set<VW_ThongKeTrangThai>();
    public DbSet<VW_DoanhThuTheoNgay> VW_DoanhThuTheoNgays => Set<VW_DoanhThuTheoNgay>();
    public DbSet<VW_HieuSuatNhanVien> VW_HieuSuatNhanViens => Set<VW_HieuSuatNhanVien>();
    public DbSet<VW_KhachHangThongKe> VW_KhachHangThongKes => Set<VW_KhachHangThongKe>();
    public DbSet<VW_BaoCaoTongHop>    VW_BaoCaoTongHops    => Set<VW_BaoCaoTongHop>();

    /* ---- TVF & SP result rows (dùng FromSqlRaw) ---- */
    public DbSet<HanhTrinhVanDonRow> HanhTrinhVanDonRows => Set<HanhTrinhVanDonRow>();
    public DbSet<SpDoanhThuRow>      SpDoanhThuRows      => Set<SpDoanhThuRow>();
    public DbSet<SpNhanVienRow>      SpNhanVienRows      => Set<SpNhanVienRow>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.Entity<KhachHang>().ToTable("KhachHang");
        mb.Entity<BuuCuc>().ToTable("BuuCuc");
        mb.Entity<LoaiHangHoa>(e => { e.ToTable("LoaiHangHoa"); e.Property(x => x.HeSoPhuPhi).HasColumnType("decimal(4,2)"); });
        mb.Entity<BangGia>(e =>
        {
            e.ToTable("BangGia");
            e.Property(x => x.KhoiLuongTu).HasColumnType("decimal(10,2)");
            e.Property(x => x.KhoiLuongDen).HasColumnType("decimal(10,2)");
            e.Property(x => x.DonGia).HasColumnType("decimal(18,0)");
        });
        mb.Entity<NhanVien>(e =>
        {
            e.ToTable("NhanVien");
            e.Property(x => x.TenDangNhap).IsUnicode(false);
            e.Property(x => x.MatKhauHash).IsUnicode(false);
            e.Property(x => x.VaiTro).IsUnicode(false);
            e.HasOne(x => x.BuuCuc).WithMany().HasForeignKey(x => x.MaBuuCuc).OnDelete(DeleteBehavior.Restrict);
        });
        mb.Entity<VanDon>(e =>
        {
            e.ToTable("VanDon");
            e.Property(x => x.MaVanDon).HasMaxLength(20).IsUnicode(false);
            e.Property(x => x.TrongLuongTong).HasColumnType("decimal(10,2)");
            e.Property(x => x.TongTien).HasColumnType("decimal(18,0)");
            e.HasOne(x => x.NguoiGui).WithMany().HasForeignKey(x => x.MaKH_Gui).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.NguoiNhan).WithMany().HasForeignKey(x => x.MaKH_Nhan).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.NhanVienTiepNhan).WithMany().HasForeignKey(x => x.MaNV_Nhan).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.BangGia).WithMany().HasForeignKey(x => x.MaBangGia).OnDelete(DeleteBehavior.Restrict);
        });
        mb.Entity<ChiTietVanDon>(e =>
        {
            e.ToTable("ChiTietVanDon");
            e.Property(x => x.MaVanDon).HasMaxLength(20).IsUnicode(false);
            e.Property(x => x.TrongLuong).HasColumnType("decimal(10,2)");
            e.HasOne(x => x.VanDon).WithMany(v => v.ChiTiet).HasForeignKey(x => x.MaVanDon).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.LoaiHangHoa).WithMany().HasForeignKey(x => x.MaLoaiHang).OnDelete(DeleteBehavior.Restrict);
        });
        mb.Entity<HanhTrinhGiaoHang>(e =>
        {
            e.ToTable("HanhTrinhGiaoHang");
            e.Property(x => x.MaVanDon).HasMaxLength(20).IsUnicode(false);
            e.HasOne(x => x.VanDon).WithMany(v => v.HanhTrinh).HasForeignKey(x => x.MaVanDon).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.NhanVien).WithMany().HasForeignKey(x => x.MaNV_Giao).OnDelete(DeleteBehavior.Restrict);
        });
        mb.Entity<KhuyenMai>(e =>
        {
            e.ToTable("KhuyenMai");
            e.Property(x => x.MaKM).HasMaxLength(20).IsUnicode(false);
            e.Property(x => x.ToiDa).HasColumnType("decimal(18,0)");
        });
        mb.Entity<GiaoDichTaiKhoan>(e =>
        {
            e.ToTable("GiaoDichTaiKhoan");
            e.Property(x => x.SoTien).HasColumnType("decimal(18,0)");
            e.HasOne(x => x.KhachHang).WithMany().HasForeignKey(x => x.MaKH).OnDelete(DeleteBehavior.Restrict);
        });
        mb.Entity<YeuCauRutTien>(e =>
        {
            e.ToTable("YeuCauRutTien");
            e.Property(x => x.SoTien).HasColumnType("decimal(18,0)");
            e.HasOne(x => x.KhachHang).WithMany().HasForeignKey(x => x.MaKH).OnDelete(DeleteBehavior.Restrict);
        });

        /* ---- SQL VIEWS mapping ---- */
        mb.Entity<VW_DanhSachVanDon>().HasNoKey().ToView("VW_DanhSachVanDon");
        mb.Entity<VW_ThongKeTrangThai>().HasNoKey().ToView("VW_ThongKeTrangThai");
        mb.Entity<VW_DoanhThuTheoNgay>().HasNoKey().ToView("VW_DoanhThuTheoNgay");
        mb.Entity<VW_HieuSuatNhanVien>().HasNoKey().ToView("VW_HieuSuatNhanVien");
        mb.Entity<VW_KhachHangThongKe>().HasNoKey().ToView("VW_KhachHangThongKe");
        mb.Entity<VW_BaoCaoTongHop>().HasNoKey().ToView("VW_BaoCaoTongHop");

        /* ---- TVF & SP result sets ---- */
        mb.Entity<HanhTrinhVanDonRow>().HasNoKey();
        mb.Entity<SpDoanhThuRow>().HasNoKey();
        mb.Entity<SpNhanVienRow>().HasNoKey();
    }
}
