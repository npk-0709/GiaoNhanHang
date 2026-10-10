using GiaoNhanHang.Data;
using GiaoNhanHang.Helpers;
using GiaoNhanHang.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GiaoNhanHang.Controllers;

[Authorize]
public class DashboardController(AppDbContext db) : Controller
{
    public async Task<IActionResult> Index()
    {
        var homNay = DateTime.Today;
        var ngayMai = homNay.AddDays(1);
        var dauThang = new DateTime(homNay.Year, homNay.Month, 1);

        var theoTT = await db.VanDons.GroupBy(v => v.TrangThai)
            .Select(g => new { g.Key, N = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.N);

        int giao = theoTT.GetValueOrDefault(TT.DaGiao);
        int hoan = theoTT.GetValueOrDefault(TT.Hoan);

        var vm = new DashboardVM
        {
            DonHomNay = await db.VanDons.CountAsync(v => v.NgayTao >= homNay && v.NgayTao < ngayMai),
            DoanhThuHomNay = await db.VanDons
                .Where(v => v.NgayTao >= homNay && v.NgayTao < ngayMai && v.TrangThai != TT.Hoan)
                .SumAsync(v => (decimal?)v.TongTien) ?? 0,
            DoanhThuThang = await db.VanDons
                .Where(v => v.NgayTao >= dauThang && v.TrangThai != TT.Hoan)
                .SumAsync(v => (decimal?)v.TongTien) ?? 0,
            TyLeThanhCong = giao + hoan == 0 ? null : Math.Round(100.0 * giao / (giao + hoan), 1),
            TheoTrangThai = theoTT,
            GanDay = await db.VanDons.Include(v => v.NguoiGui).Include(v => v.NguoiNhan)
                .OrderByDescending(v => v.NgayTao).Take(8).ToListAsync()
        };
        return View(vm);
    }
}
