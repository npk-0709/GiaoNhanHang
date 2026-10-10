using GiaoNhanHang.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication;

namespace GiaoNhanHang.Controllers;

public class HomeController(AppDbContext db) : Controller
{
    // Khách hàng tra cứu vận đơn không cần đăng nhập
    public async Task<IActionResult> TraCuu(string? ma)
    {
        ma = ma?.Trim().ToUpperInvariant();
        if (string.IsNullOrEmpty(ma)) return RedirectToAction("UserLogin", "Account");

        var vd = await db.VanDons
            .Include(v => v.NguoiGui).Include(v => v.NguoiNhan)
            .Include(v => v.HanhTrinh)
            .FirstOrDefaultAsync(v => v.MaVanDon == ma);

        ViewData["ma"] = ma;
        return View(vd);
    }
}
