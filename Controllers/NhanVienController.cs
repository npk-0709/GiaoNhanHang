using System.Security.Claims;
using GiaoNhanHang.Data;
using GiaoNhanHang.Helpers;
using GiaoNhanHang.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GiaoNhanHang.Controllers;

[Authorize(Roles = "QuanLy")]
public class NhanVienController(AppDbContext db) : Controller
{
    public async Task<IActionResult> Index()
    {
        ViewBag.BuuCuc = await db.BuuCucs.OrderBy(b => b.TenBuuCuc).ToListAsync();
        return View(await db.NhanViens.Include(n => n.BuuCuc).OrderBy(n => n.TenNV).ToListAsync());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Luu(NhanVienForm f)
    {
        if (!ModelState.IsValid) { TempData["loi"] = ModelState.Gop(); return RedirectToAction(nameof(Index)); }
        if (!new[] { "QuanLy", "TiepNhan", "GiaoHang" }.Contains(f.VaiTro))
        { TempData["loi"] = "Vai trò không hợp lệ."; return RedirectToAction(nameof(Index)); }

        var tenDN = f.TenDangNhap.Trim();
        if (await db.NhanViens.AnyAsync(n => n.TenDangNhap == tenDN && n.MaNV != f.MaNV))
        { TempData["loi"] = "Tên đăng nhập đã tồn tại."; return RedirectToAction(nameof(Index)); }

        NhanVien nv;
        if (f.MaNV == 0)
        {
            if (string.IsNullOrWhiteSpace(f.MatKhau) || f.MatKhau.Length < 6)
            { TempData["loi"] = "Nhân viên mới cần mật khẩu từ 6 ký tự."; return RedirectToAction(nameof(Index)); }
            nv = new NhanVien();
            db.NhanViens.Add(nv);
        }
        else
        {
            nv = await db.NhanViens.FindAsync(f.MaNV) ?? throw new InvalidOperationException();
        }

        nv.TenNV = f.TenNV.Trim();
        nv.ChucVu = f.ChucVu.Trim();
        nv.SoDienThoai = f.SoDienThoai;
        nv.MaBuuCuc = f.MaBuuCuc;
        nv.TenDangNhap = tenDN;
        nv.VaiTro = f.VaiTro;
        if (!string.IsNullOrWhiteSpace(f.MatKhau)) nv.MatKhauHash = Mk.Hash(f.MatKhau);

        await db.SaveChangesAsync();
        TempData["ok"] = "Đã lưu nhân viên.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Xoa(int id)
    {
        if (id.ToString() == User.FindFirstValue(ClaimTypes.NameIdentifier))
        { TempData["loi"] = "Không thể xóa tài khoản đang đăng nhập."; return RedirectToAction(nameof(Index)); }
        try
        {
            var m = await db.NhanViens.FindAsync(id);
            if (m != null) { db.NhanViens.Remove(m); await db.SaveChangesAsync(); TempData["ok"] = "Đã xóa nhân viên."; }
        }
        catch (DbUpdateException) { TempData["loi"] = "Không thể xóa: nhân viên đã tham gia xử lý vận đơn."; }
        return RedirectToAction(nameof(Index));
    }
}
