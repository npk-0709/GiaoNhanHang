using GiaoNhanHang.Data;
using GiaoNhanHang.Helpers;
using GiaoNhanHang.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GiaoNhanHang.Controllers;

[Authorize(Roles = "QuanLy")]
public class BangGiaController(AppDbContext db) : Controller
{
    public async Task<IActionResult> Index() => View(await db.BangGias.OrderBy(b => b.KhoiLuongTu).ToListAsync());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Luu(BangGia m)
    {
        if (!ModelState.IsValid) { TempData["loi"] = ModelState.Gop(); return RedirectToAction(nameof(Index)); }
        if (m.KhoiLuongDen <= m.KhoiLuongTu)
        { TempData["loi"] = "Khối lượng đến phải lớn hơn khối lượng từ."; return RedirectToAction(nameof(Index)); }

        bool chong = await db.BangGias.AnyAsync(b => b.MaBangGia != m.MaBangGia
            && b.KhoiLuongTu < m.KhoiLuongDen && m.KhoiLuongTu < b.KhoiLuongDen);
        if (chong) { TempData["loi"] = "Khoảng khối lượng bị trùng với một bậc giá khác."; return RedirectToAction(nameof(Index)); }

        if (m.MaBangGia == 0) db.BangGias.Add(m); else db.BangGias.Update(m);
        await db.SaveChangesAsync();
        TempData["ok"] = "Đã lưu bậc giá.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Xoa(int id)
    {
        try
        {
            var m = await db.BangGias.FindAsync(id);
            if (m != null) { db.BangGias.Remove(m); await db.SaveChangesAsync(); TempData["ok"] = "Đã xóa bậc giá."; }
        }
        catch (DbUpdateException) { TempData["loi"] = "Không thể xóa: bậc giá đã được áp dụng cho vận đơn."; }
        return RedirectToAction(nameof(Index));
    }
}
