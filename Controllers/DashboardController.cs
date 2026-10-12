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
        var homNay  = DateTime.Today;
        var ngayMai = homNay.AddDays(1);
        var dauThang = new DateTime(homNay.Year, homNay.Month, 1);

        // ── Dùng VW_ThongKeTrangThai để lấy số lượng theo trạng thái ────────
        var theoTT = await db.VW_ThongKeTrangThais
            .ToDictionaryAsync(x => x.TrangThai, x => x.SoDon);

        int giao = theoTT.GetValueOrDefault(TT.DaGiao);
        int hoan = theoTT.GetValueOrDefault(TT.Hoan);

        // Dùng VW_DoanhThuTheoNgay để tính doanh thu ─────────────────────────
        var dtHomNay = await db.VW_DoanhThuTheoNgays
            .Where(x => x.Ngay == homNay)
            .FirstOrDefaultAsync();

        var dtThang = await db.VW_DoanhThuTheoNgays
            .Where(x => x.Ngay >= dauThang)
            .SumAsync(x => (decimal?)x.DoanhThu) ?? 0;

        var vm = new DashboardVM
        {
            DonHomNay      = dtHomNay?.SoDon ?? 0,
            DoanhThuHomNay = dtHomNay?.DoanhThu ?? 0,
            DoanhThuThang  = dtThang,
            TyLeThanhCong  = giao + hoan == 0
                             ? null
                             : Math.Round(100.0 * giao / (giao + hoan), 1),
            TheoTrangThai  = theoTT,

            // Vận đơn gần đây vẫn lấy qua EF (cần include navigation)
            GanDay = await db.VanDons
                .Include(v => v.NguoiGui).Include(v => v.NguoiNhan)
                .OrderByDescending(v => v.NgayTao).Take(8).ToListAsync()
        };

        return View(vm);
    }
}
