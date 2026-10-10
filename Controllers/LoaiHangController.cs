using GiaoNhanHang.Data;
using GiaoNhanHang.Helpers;
using GiaoNhanHang.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GiaoNhanHang.Controllers;

[Authorize(Roles = "QuanLy")]
public class LoaiHangController(AppDbContext db) : Controller
{
    public async Task<IActionResult> Index() => View(await db.LoaiHangs.OrderBy(l => l.TenLoaiHang).ToListAsync());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Luu(LoaiHangHoa m)
    {
        if (!ModelState.IsValid) { TempData["loi"] = ModelState.Gop(); return RedirectToAction(nameof(Index)); }
        if (m.MaLoaiHang == 0) db.LoaiHangs.Add(m); else db.LoaiHangs.Update(m);
        await db.SaveChangesAsync();
        TempData["ok"] = "Đã lưu loại hàng.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Xoa(int id)
    {
        try
        {
            var m = await db.LoaiHangs.FindAsync(id);
            if (m != null) { db.LoaiHangs.Remove(m); await db.SaveChangesAsync(); TempData["ok"] = "Đã xóa loại hàng."; }
        }
        catch (DbUpdateException) { TempData["loi"] = "Không thể xóa: loại hàng đã được dùng trong vận đơn."; }
        return RedirectToAction(nameof(Index));
    }
}
