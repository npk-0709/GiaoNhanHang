using GiaoNhanHang.Models;
using Microsoft.EntityFrameworkCore;

namespace GiaoNhanHang.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<KhachHang> KhachHangs => Set<KhachHang>();
    public DbSet<BuuCuc> BuuCucs => Set<BuuCuc>();
    public DbSet<NhanVien> NhanViens => Set<NhanVien>();
    public DbSet<BangGia> BangGias => Set<BangGia>();
    public DbSet<LoaiHangHoa> LoaiHangs => Set<LoaiHangHoa>();
    public DbSet<VanDon> VanDons => Set<VanDon>();
    public DbSet<ChiTietVanDon> ChiTiets => Set<ChiTietVanDon>();
    public DbSet<HanhTrinhGiaoHang> HanhTrinhs => Set<HanhTrinhGiaoHang>();
    public DbSet<KhuyenMai> KhuyenMais => Set<KhuyenMai>();
    public DbSet<GiaoDichTaiKhoan> GiaoDichTaiKhoans => Set<GiaoDichTaiKhoan>();
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
    }
}
