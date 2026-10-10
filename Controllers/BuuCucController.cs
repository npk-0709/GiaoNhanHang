using GiaoNhanHang.Data;
using GiaoNhanHang.Helpers;
using GiaoNhanHang.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GiaoNhanHang.Controllers;

[Authorize(Roles = "QuanLy")]
public class BuuCucController(AppDbContext db) : Controller
{
    public async Task<IActionResult> Index() => View(await db.BuuCucs.OrderBy(b => b.TenBuuCuc).ToListAsync());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Luu(BuuCuc m)
    {
        if (!ModelState.IsValid) { TempData["loi"] = ModelState.Gop(); return RedirectToAction(nameof(Index)); }
        if (m.MaBuuCuc == 0) db.BuuCucs.Add(m); else db.BuuCucs.Update(m);
        await db.SaveChangesAsync();
        TempData["ok"] = "Đã lưu bưu cục.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Xoa(int id)
    {
        try
        {
            var m = await db.BuuCucs.FindAsync(id);
            if (m != null) { db.BuuCucs.Remove(m); await db.SaveChangesAsync(); TempData["ok"] = "Đã xóa bưu cục."; }
        }
        catch (DbUpdateException) { TempData["loi"] = "Không thể xóa: bưu cục đang có nhân viên."; }
        return RedirectToAction(nameof(Index));
    }
}
