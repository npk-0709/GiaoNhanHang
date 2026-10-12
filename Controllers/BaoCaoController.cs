using GiaoNhanHang.Data;
using GiaoNhanHang.Helpers;
using GiaoNhanHang.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace GiaoNhanHang.Controllers;

[Authorize(Roles = "QuanLy")]
public class BaoCaoController(AppDbContext db) : Controller
{
    /// <summary>
    /// Sử dụng SP_BaoCaoDoanhThu và VW_HieuSuatNhanVien để lấy dữ liệu báo cáo
    /// </summary>
    public async Task<IActionResult> Index(DateTime? tu, DateTime? den)
    {
        var t = (tu ?? DateTime.Today.AddDays(-29)).Date;
        var d = (den ?? DateTime.Today).Date;
        if (d < t) (t, d) = (d, t);

        // ── Gọi Stored Procedure SP_BaoCaoDoanhThu ──────────────────────────
        var theoNgay = await db.SpDoanhThuRows
            .FromSqlRaw(
                "EXEC SP_BaoCaoDoanhThu @TuNgay = {0}, @DenNgay = {1}",
                t, d)
            .ToListAsync();

        // ── Gọi Stored Procedure SP_ThongKeNhanVien ─────────────────────────
        var theoNhanVien = await db.SpNhanVienRows
            .FromSqlRaw(
                "EXEC SP_ThongKeNhanVien @TuNgay = {0}, @DenNgay = {1}",
                t, d)
            .ToListAsync();

        // ── Tổng hợp từ kết quả SP ──────────────────────────────────────────
        int tongDon    = theoNgay.Sum(x => x.SoDon);
        int daGiao     = theoNgay.Sum(x => x.DaGiao);
        int hoanHang   = theoNgay.Sum(x => x.HoanHang);
        decimal dthu   = theoNgay.Sum(x => x.DoanhThu);

        var vm = new BaoCaoVM
        {
            Tu             = t,
            Den            = d,
            TongDon        = tongDon,
            TongDoanhThu   = dthu,
            DaGiao         = daGiao,
            Hoan           = hoanHang,
            DangXuLy       = tongDon - daGiao - hoanHang,
            TyLeThanhCong  = daGiao + hoanHang == 0
                             ? null
                             : Math.Round(100.0 * daGiao / (daGiao + hoanHang), 1),

            // Map SP_BaoCaoDoanhThu → NgayDoanhThu list
            TheoNgay = theoNgay.Select(x => new NgayDoanhThu
            {
                Ngay     = x.Ngay,
                SoDon    = x.SoDon,
                DoanhThu = x.DoanhThu
            }).ToList(),

            // Map SP_ThongKeNhanVien → NvHieuSuat list
            TheoNhanVien = theoNhanVien.Select(x => new NvHieuSuat
            {
                Ten     = x.TenNV,
                DaGiao  = x.SoDaGiao,
                Hoan    = x.SoHoan
            }).OrderByDescending(x => x.DaGiao).ToList()
        };

        return View(vm);
    }

    /// <summary>
    /// Báo cáo tổng hợp theo tháng – dùng VW_BaoCaoTongHop
    /// </summary>
    public async Task<IActionResult> TongHopThang()
    {
        var data = await db.VW_BaoCaoTongHops
            .OrderByDescending(x => x.Nam).ThenByDescending(x => x.Thang)
            .Take(12)
            .ToListAsync();
        return View(data);
    }

    /// <summary>
    /// Hiệu suất nhân viên – dùng VW_HieuSuatNhanVien
    /// </summary>
    public async Task<IActionResult> HieuSuatNhanVien()
    {
        var data = await db.VW_HieuSuatNhanViens
            .OrderByDescending(x => x.SoDonDaGiao)
            .ToListAsync();
        return View(data);
    }
}
