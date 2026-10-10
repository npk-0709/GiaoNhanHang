using GiaoNhanHang.Data;
using GiaoNhanHang.Helpers;
using GiaoNhanHang.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GiaoNhanHang.Controllers;

[Authorize(Roles = "QuanLy")]
public class BaoCaoController(AppDbContext db) : Controller
{
    public async Task<IActionResult> Index(DateTime? tu, DateTime? den)
    {
        var t = (tu ?? DateTime.Today.AddDays(-29)).Date;
        var d = (den ?? DateTime.Today).Date;
        if (d < t) (t, d) = (d, t);
        var denExcl = d.AddDays(1);

        var ds = await db.VanDons.Where(v => v.NgayTao >= t && v.NgayTao < denExcl)
            .Select(v => new { v.NgayTao, v.TongTien, v.TrangThai }).ToListAsync();

        int giao = ds.Count(x => x.TrangThai == TT.DaGiao);
        int hoan = ds.Count(x => x.TrangThai == TT.Hoan);

        var vm = new BaoCaoVM
        {
            Tu = t, Den = d,
            TongDon = ds.Count,
            TongDoanhThu = ds.Where(x => x.TrangThai != TT.Hoan).Sum(x => x.TongTien),
            DaGiao = giao, Hoan = hoan,
            DangXuLy = ds.Count - giao - hoan,
            TyLeThanhCong = giao + hoan == 0 ? null : Math.Round(100.0 * giao / (giao + hoan), 1),
            TheoNgay = ds.GroupBy(x => x.NgayTao.Date).OrderBy(g => g.Key)
                .Select(g => new NgayDoanhThu
                {
                    Ngay = g.Key, SoDon = g.Count(),
                    DoanhThu = g.Where(x => x.TrangThai != TT.Hoan).Sum(x => x.TongTien)
                }).ToList()
        };

        var kq = await db.HanhTrinhs
            .Where(h => h.ThoiGianCapNhat >= t && h.ThoiGianCapNhat < denExcl
                        && (h.TrangThai == TT.DaGiao || h.TrangThai == TT.Hoan))
            .GroupBy(h => new { h.MaNV_Giao, h.TrangThai })
            .Select(g => new { g.Key.MaNV_Giao, g.Key.TrangThai, N = g.Count() })
            .ToListAsync();
        var ten = await db.NhanViens.ToDictionaryAsync(n => n.MaNV, n => n.TenNV);

        vm.TheoNhanVien = kq.GroupBy(x => x.MaNV_Giao).Select(g => new NvHieuSuat
        {
            Ten = ten.GetValueOrDefault(g.Key, "?"),
            DaGiao = g.Where(x => x.TrangThai == TT.DaGiao).Sum(x => x.N),
            Hoan = g.Where(x => x.TrangThai == TT.Hoan).Sum(x => x.N)
        }).OrderByDescending(x => x.DaGiao).ToList();

        return View(vm);
    }
}
