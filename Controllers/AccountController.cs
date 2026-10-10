using System.Security.Claims;
using GiaoNhanHang.Data;
using GiaoNhanHang.Helpers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GiaoNhanHang.Models;

namespace GiaoNhanHang.Controllers;

public class AccountController(AppDbContext db) : Controller
{
    [HttpGet]
    public IActionResult UserLogin(string? returnUrl)
    {
        if (User.Identity?.IsAuthenticated == true) return RedirectToAction("Dashboard", "KhachHang");
        ViewData["returnUrl"] = returnUrl;
        return View();
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UserLogin(string? tenDangNhap, string? matKhau, string? returnUrl)
    {
        var hash = Mk.Hash(matKhau ?? "");
        var u = (tenDangNhap ?? "").Trim();
        var kh = await db.KhachHangs.FirstOrDefaultAsync(x => (x.TenDangNhap == u || x.SoDienThoai == u) && x.MatKhauHash == hash);
        if (kh == null)
        {
            ViewData["loi"] = "Tên đăng nhập hoặc mật khẩu không đúng.";
            ViewData["returnUrl"] = returnUrl;
            return View();
        }

        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, kh.MaKH.ToString()), new(ClaimTypes.Name, kh.TenKH), new(ClaimTypes.Role, "KhachHang") };
        var id = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(id));
        return Url.IsLocalUrl(returnUrl) ? Redirect(returnUrl!) : RedirectToAction("Dashboard", "KhachHang");
    }

    [HttpGet]
    public IActionResult AdminLogin(string? returnUrl)
    {
        if (User.Identity?.IsAuthenticated == true) return RedirectToAction("Index", "Dashboard");
        ViewData["returnUrl"] = returnUrl;
        return View();
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AdminLogin(string? tenDangNhap, string? matKhau, string? returnUrl)
    {
        var hash = Mk.Hash(matKhau ?? "");
        var u = (tenDangNhap ?? "").Trim();
        
        var nv = await db.NhanViens.FirstOrDefaultAsync(x => x.TenDangNhap == u && x.MatKhauHash == hash);
        if (nv != null)
        {
            var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, nv.MaNV.ToString()), new(ClaimTypes.Name, nv.TenNV), new(ClaimTypes.Role, nv.VaiTro) };
            var id = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(id));
            return Url.IsLocalUrl(returnUrl) ? Redirect(returnUrl!) : RedirectToAction("Index", "Dashboard");
        }

        ViewData["loi"] = "Sai tên đăng nhập hoặc mật khẩu quản trị.";
        ViewData["returnUrl"] = returnUrl;
        return View();
    }

    [HttpGet]
    public IActionResult Register()
    {
        if (User.Identity?.IsAuthenticated == true) return RedirectToAction("Index", "Home");
        return View();
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(string tenKH, string soDienThoai, string matKhau, string? diaChi, string? email)
    {
        if (await db.KhachHangs.AnyAsync(k => k.SoDienThoai == soDienThoai))
        {
            ViewData["loi"] = "Số điện thoại đã được đăng ký.";
            return View();
        }

        var kh = new KhachHang
        {
            TenKH = tenKH,
            SoDienThoai = soDienThoai,
            MatKhauHash = Mk.Hash(matKhau),
            DiaChi = diaChi,
            Email = email
        };
        db.KhachHangs.Add(kh);
        await db.SaveChangesAsync();

        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, kh.MaKH.ToString()), new(ClaimTypes.Name, kh.TenKH), new(ClaimTypes.Role, "KhachHang") };
        var id = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(id));

        return RedirectToAction("Dashboard", "KhachHang");
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("UserLogin", "Account");
    }

    public IActionResult AccessDenied() => View();
}
