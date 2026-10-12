using System.Security.Claims;
using GiaoNhanHang.Data;
using GiaoNhanHang.Helpers;
using GiaoNhanHang.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GiaoNhanHang.Controllers;

[Authorize]
public class VanDonController(AppDbContext db) : Controller
{
    int MaNVHienTai => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    public async Task<IActionResult> Index(string? q, string? trangThai)
    {
        var qry = db.VanDons.Include(v => v.NguoiGui).Include(v => v.NguoiNhan).AsQueryable();
        if (!string.IsNullOrWhiteSpace(q))
        {
            q = q.Trim();
            qry = qry.Where(v => v.MaVanDon.Contains(q)
                || v.NguoiGui!.TenKH.Contains(q) || v.NguoiNhan!.TenKH.Contains(q)
                || v.NguoiNhan!.SoDienThoai.Contains(q));
        }
        if (!string.IsNullOrEmpty(trangThai)) qry = qry.Where(v => v.TrangThai == trangThai);

        ViewBag.q = q;
        ViewBag.tt = trangThai;
        return View(await qry.OrderByDescending(v => v.NgayTao).Take(200).ToListAsync());
    }

    // ---------- Lập vận đơn ----------
    [Authorize(Roles = "QuanLy,TiepNhan")]
    public async Task<IActionResult> Create()
    {
        await NapDanhMuc();
        var vm = new VanDonCreateVM();
        vm.Items.Add(new ItemVM());
        return View(vm);
    }

    [Authorize(Roles = "QuanLy,TiepNhan"), HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(VanDonCreateVM vm)
    {
        vm.Items = vm.Items.Where(i => i.MaLoaiHang > 0).ToList();
        if (vm.Items.Count == 0) ModelState.AddModelError("", "Thêm ít nhất một mặt hàng.");
        if (vm.MaKH_Gui > 0 && vm.MaKH_Gui == vm.MaKH_Nhan)
            ModelState.AddModelError("", "Người gửi và người nhận phải khác nhau.");

        if (!ModelState.IsValid)
        {
            await NapDanhMuc();
            if (vm.Items.Count == 0) vm.Items.Add(new ItemVM());
            return View(vm);
        }

        var tongKg = vm.Items.Sum(i => i.TrongLuong);
        var (bac, tien) = await CuocPhi.Tinh(db, tongKg, vm.Items.Select(i => i.MaLoaiHang));

        var prefix = "VD" + DateTime.Now.ToString("yyMMdd");
        var soDon = await db.VanDons.CountAsync(v => v.MaVanDon.StartsWith(prefix));
        var ma = prefix + (soDon + 1).ToString("0000");

        var nv = await db.NhanViens.Include(n => n.BuuCuc).FirstAsync(n => n.MaNV == MaNVHienTai);
        var viTri = nv.BuuCuc?.TenBuuCuc ?? "Bưu cục";

        var vd = new VanDon
        {
            MaVanDon = ma,
            MaKH_Gui = vm.MaKH_Gui,
            MaKH_Nhan = vm.MaKH_Nhan ?? 0,
            MaNV_Nhan = nv.MaNV,
            MaBangGia = bac?.MaBangGia,
            NgayTao = DateTime.Now,
            TrongLuongTong = tongKg,
            TongTien = tien,
            TrangThai = TT.ChoLay,
            ChiTiet = vm.Items.Select(i => new ChiTietVanDon
            {
                MaLoaiHang = i.MaLoaiHang, SoLuong = i.SoLuong, TrongLuong = i.TrongLuong, GhiChu = i.GhiChu
            }).ToList(),
            HanhTrinh = new List<HanhTrinhGiaoHang>
            {
                new() { MaNV_Giao = nv.MaNV, ThoiGianCapNhat = DateTime.Now, ViTri = viTri,
                        TrangThai = TT.ChoLay, TrangThaiChiTiet = "Đã tiếp nhận vận đơn" }
            }
        };
        db.VanDons.Add(vd);
        await db.SaveChangesAsync();

        TempData["ok"] = $"Đã lập vận đơn {ma}. Cước phí {Fmt.Tien(tien)}.";
        return RedirectToAction(nameof(Details), new { id = ma });
    }

    // Ước tính cước (gọi bằng fetch từ form lập vận đơn)
    [HttpGet]
    public async Task<IActionResult> TinhCuoc(decimal kg, string? loai)
    {
        var ids = (loai ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => int.TryParse(s, out var n) ? n : 0).Where(n => n > 0);
        var (bac, tien) = await CuocPhi.Tinh(db, kg, ids);
        return Json(new
        {
            tien,
            bac = bac == null ? null : $"{Fmt.So(bac.KhoiLuongTu)} – {Fmt.So(bac.KhoiLuongDen)} kg"
        });
    }

    // ---------- Chi tiết + cập nhật hành trình ----------
    public async Task<IActionResult> Details(string id)
    {
        var vd = await db.VanDons
            .Include(v => v.NguoiGui).Include(v => v.NguoiNhan)
            .Include(v => v.NhanVienTiepNhan).Include(v => v.BangGia)
            .Include(v => v.ChiTiet).ThenInclude(c => c.LoaiHangHoa)
            .Include(v => v.HanhTrinh).ThenInclude(h => h.NhanVien)
            .FirstOrDefaultAsync(v => v.MaVanDon == id);
        if (vd == null) return NotFound();

        try
        {
            // Minh họa gọi Table-Valued Function FN_HanhTrinhVanDon
            var pId = new Microsoft.Data.SqlClient.SqlParameter("@MaVD", id);
            ViewBag.HanhTrinhSql = await db.HanhTrinhVanDonRows
                .FromSqlRaw("SELECT * FROM dbo.FN_HanhTrinhVanDon(@MaVD) ORDER BY ThoiGianCapNhat", pId)
                .ToListAsync();
        }
        catch { }

        return View(vd);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CapNhat(string id, string trangThai, string viTri, string? ghiChu)
    {
        var (ok, msg) = await GhiHanhTrinh(id, trangThai, viTri, ghiChu);
        TempData[ok ? "ok" : "loi"] = msg;
        return RedirectToAction(nameof(Details), new { id });
    }

    // ---------- Cập nhật nhanh (quét mã vạch hoặc nhập tay) ----------
    public IActionResult CapNhatNhanh() => View();

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CapNhatNhanh(string? maVanDon, string trangThai, string viTri, string? ghiChu)
    {
        var (ok, msg) = await GhiHanhTrinh((maVanDon ?? "").Trim().ToUpperInvariant(), trangThai, viTri, ghiChu);
        TempData[ok ? "ok" : "loi"] = msg;
        ViewBag.trangThai = trangThai;
        ViewBag.viTri = viTri;
        return View();
    }

    async Task<(bool, string)> GhiHanhTrinh(string maVanDon, string trangThai, string? viTri, string? ghiChu)
    {
        if (string.IsNullOrWhiteSpace(maVanDon)) return (false, "Nhập mã vận đơn.");
        if (!TT.Tat.Contains(trangThai)) return (false, "Trạng thái không hợp lệ.");
        if (string.IsNullOrWhiteSpace(viTri)) return (false, "Nhập vị trí hiện tại của hàng.");

        try
        {
            // ── Sử dụng Stored Procedure SP_CapNhatTrangThaiVanDon ─────────
            var pMa = new Microsoft.Data.SqlClient.SqlParameter("@MaVanDon", maVanDon.Trim().ToUpperInvariant());
            var pTT = new Microsoft.Data.SqlClient.SqlParameter("@TrangThai", trangThai);
            var pNV = new Microsoft.Data.SqlClient.SqlParameter("@MaNV", MaNVHienTai);
            var pVT = new Microsoft.Data.SqlClient.SqlParameter("@ViTri", viTri.Trim());
            var pGC = new Microsoft.Data.SqlClient.SqlParameter("@GhiChu", (object?)ghiChu?.Trim() ?? DBNull.Value);
            var pKQ = new Microsoft.Data.SqlClient.SqlParameter
            {
                ParameterName = "@KetQua",
                SqlDbType = System.Data.SqlDbType.NVarChar,
                Size = 200,
                Direction = System.Data.ParameterDirection.Output
            };

            await db.Database.ExecuteSqlRawAsync(
                "EXEC SP_CapNhatTrangThaiVanDon @MaVanDon, @TrangThai, @MaNV, @ViTri, @GhiChu, @KetQua OUTPUT",
                pMa, pTT, pNV, pVT, pGC, pKQ);

            var kq = pKQ.Value?.ToString() ?? "";
            if (kq.StartsWith("Thành công:")) return (true, kq.Replace("Thành công: ", ""));
            return (false, kq);
        }
        catch
        {
            // Dự phòng (fallback EF)
            var vd = await db.VanDons.FirstOrDefaultAsync(v => v.MaVanDon == maVanDon);
            if (vd == null) return (false, $"Không tìm thấy vận đơn {maVanDon}.");
            if (TT.Cuoi(vd.TrangThai)) return (false, $"Vận đơn {maVanDon} đã kết thúc ({vd.TrangThai}), không thể cập nhật thêm.");

            db.HanhTrinhs.Add(new HanhTrinhGiaoHang
            {
                MaVanDon = vd.MaVanDon,
                MaNV_Giao = MaNVHienTai,
                ThoiGianCapNhat = DateTime.Now,
                ViTri = viTri.Trim(),
                TrangThai = trangThai,
                TrangThaiChiTiet = string.IsNullOrWhiteSpace(ghiChu) ? null : ghiChu.Trim()
            });
            vd.TrangThai = trangThai;
            await db.SaveChangesAsync();
            return (true, $"Đã cập nhật {maVanDon}: {trangThai}.");
        }
    }

    async Task NapDanhMuc()
    {
        ViewBag.KhachHang = await db.KhachHangs.OrderBy(k => k.TenKH).ToListAsync();
        ViewBag.LoaiHang = await db.LoaiHangs.OrderBy(l => l.MaLoaiHang).ToListAsync();
    }
}
