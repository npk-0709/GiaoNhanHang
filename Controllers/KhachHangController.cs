using System.Security.Claims;
using GiaoNhanHang.Data;
using GiaoNhanHang.Helpers;
using GiaoNhanHang.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GiaoNhanHang.Controllers;

[Authorize]
public class KhachHangController(AppDbContext db) : Controller
{
    int MaKHHienTai => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // --- CUSTOMER AREA ---
    [Authorize(Roles = "KhachHang")]
    public async Task<IActionResult> Dashboard(string? q, string? trangThai)
    {
        var maKH = MaKHHienTai;
        var query = db.VanDons
            .Include(v => v.NguoiNhan)
            .Where(v => v.MaKH_Gui == maKH)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
        {
            q = q.Trim();
            query = query.Where(v => v.MaVanDon.Contains(q) || (v.NguoiNhan != null && (v.NguoiNhan.TenKH.Contains(q) || v.NguoiNhan.SoDienThoai.Contains(q))));
        }

        if (!string.IsNullOrWhiteSpace(trangThai))
        {
            query = query.Where(v => v.TrangThai == trangThai);
        }

        var donHang = await query
            .OrderByDescending(v => v.NgayTao)
            .Take(50)
            .ToListAsync();
            
        ViewBag.q = q;
        ViewBag.trangThai = trangThai;
        ViewBag.TrangThaiList = new[] { GiaoNhanHang.Helpers.TT.ChoLay, GiaoNhanHang.Helpers.TT.DaNhapKho, GiaoNhanHang.Helpers.TT.DangVanChuyen, GiaoNhanHang.Helpers.TT.DaGiao, GiaoNhanHang.Helpers.TT.Hoan, "Hủy", "Hư hỏng", "Thất lạc" };

        return View(donHang);
    }

    [Authorize(Roles = "KhachHang")]
    public async Task<IActionResult> TaoDon()
    {
        ViewBag.LoaiHang = await db.LoaiHangs.OrderBy(l => l.MaLoaiHang).ToListAsync();
        var me = await db.KhachHangs.FindAsync(MaKHHienTai);
        var vm = new VanDonCreateVM
        {
            TenNguoiGui = me?.TenKH,
            SdtGui = me?.SoDienThoai,
            DiaChiGui = me?.DiaChi
        };
        vm.Items.Add(new ItemVM());
        return View(vm);
    }

    [Authorize(Roles = "KhachHang")]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> TaoDon(VanDonCreateVM vm)
    {
        vm.MaKH_Gui = MaKHHienTai;
        vm.Items = vm.Items.Where(i => i.MaLoaiHang > 0).ToList();
        if (vm.Items.Count == 0) ModelState.AddModelError("", "Thêm ít nhất một mặt hàng.");

        if (!ModelState.IsValid)
        {
            ViewBag.LoaiHang = await db.LoaiHangs.OrderBy(l => l.MaLoaiHang).ToListAsync();
            if (vm.Items.Count == 0) vm.Items.Add(new ItemVM());
            return View(vm);
        }

        var khNhan = await db.KhachHangs.FirstOrDefaultAsync(k => k.SoDienThoai == vm.SdtNhan);
        if (khNhan == null)
        {
            khNhan = new KhachHang { TenKH = vm.TenNhan, SoDienThoai = vm.SdtNhan, DiaChi = vm.DiaChiNhan, Email = vm.EmailNhan };
            db.KhachHangs.Add(khNhan);
        }
        else 
        {
            khNhan.TenKH = vm.TenNhan;
            khNhan.DiaChi = vm.DiaChiNhan;
            khNhan.Email = vm.EmailNhan;
        }

        var khGui = await db.KhachHangs.FindAsync(MaKHHienTai);
        if (khGui != null)
        {
            khGui.TenKH = vm.TenNguoiGui ?? khGui.TenKH;
            khGui.SoDienThoai = vm.SdtGui ?? khGui.SoDienThoai;
            khGui.DiaChi = vm.DiaChiGui ?? khGui.DiaChi;
        }
        await db.SaveChangesAsync(); // Lưu cả người gửi lẫn người nhận (để sinh/cập nhật MaKH)

        var tongKg = vm.Items.Sum(i => i.TrongLuong);
        var (bac, tien) = await GiaoNhanHang.Data.CuocPhi.Tinh(db, tongKg, vm.Items.Select(i => i.MaLoaiHang));

        var prefix = "VD" + DateTime.Now.ToString("yyMMdd");
        var soDon = await db.VanDons.CountAsync(v => v.MaVanDon.StartsWith(prefix));
        var ma = prefix + (soDon + 1).ToString("0000");

        var nv = await db.NhanViens.FirstOrDefaultAsync(n => n.VaiTro == "TiepNhan") ?? await db.NhanViens.FirstAsync();

        var vd = new VanDon
        {
            MaVanDon = ma,
            MaKH_Gui = vm.MaKH_Gui,
            MaKH_Nhan = khNhan.MaKH,
            MaNV_Nhan = nv.MaNV,
            MaBangGia = bac?.MaBangGia,
            NgayTao = DateTime.Now,
            TrongLuongTong = tongKg,
            TongTien = tien,
            TrangThai = GiaoNhanHang.Helpers.TT.ChoLay,
            KichThuoc = vm.KichThuoc,
            GiaTriBuuGui = vm.GiaTriBuuGui,
            MaDonKhachHang = vm.MaDonKhachHang,
            ThuCOD = vm.ThuCOD,
            NguoiTraCuoc = vm.NguoiTraCuoc ?? "NguoiGui",
            HuongDanGiaoHang = vm.HuongDanGiaoHang,
            LoaiDichVu = vm.LoaiDichVu ?? "TieuChuan",
            ChiTiet = vm.Items.Select(i => new ChiTietVanDon { MaLoaiHang = i.MaLoaiHang, SoLuong = i.SoLuong, TrongLuong = i.TrongLuong, GhiChu = i.GhiChu }).ToList(),
            HanhTrinh = new List<HanhTrinhGiaoHang> { new() { MaNV_Giao = nv.MaNV, ThoiGianCapNhat = DateTime.Now, ViTri = "Hệ thống Online", TrangThai = GiaoNhanHang.Helpers.TT.ChoLay, TrangThaiChiTiet = "Đã tiếp nhận đơn hàng online" } }
        };
        db.VanDons.Add(vd);
        await db.SaveChangesAsync();

        TempData["ok"] = $"Đã tạo đơn hàng {ma} thành công. Cước phí dự kiến {GiaoNhanHang.Helpers.Fmt.Tien(tien)}.";
        return RedirectToAction(nameof(Dashboard));
    }

    [Authorize(Roles = "KhachHang")]
    public async Task<IActionResult> ThongKe()
    {
        var maKH = MaKHHienTai;
        var donHang = await db.VanDons.Where(v => v.MaKH_Gui == maKH).ToListAsync();
        
        var vm = new {
            DangXuLy = donHang.Count(v => v.TrangThai == TT.ChoLay || v.TrangThai == TT.DaNhapKho || v.TrangThai == TT.DangVanChuyen),
            DaGiao = donHang.Count(v => v.TrangThai == TT.DaGiao),
            NgoaiLe = donHang.Count(v => v.TrangThai == TT.Hoan || v.TrangThai == "Hư hỏng" || v.TrangThai == "Thất lạc"),
            TongSoDonThang = donHang.Count(v => v.NgayTao.Month == DateTime.Now.Month && v.NgayTao.Year == DateTime.Now.Year),
            DoanhThuThang = donHang.Where(v => v.NgayTao.Month == DateTime.Now.Month && v.NgayTao.Year == DateTime.Now.Year && v.TrangThai == TT.DaGiao).Sum(v => v.TongTien)
        };
        return View(vm);
    }

    [Authorize(Roles = "KhachHang")]
    public async Task<IActionResult> TaiKhoan()
    {
        var kh = await db.KhachHangs.FindAsync(MaKHHienTai);
        var giaoDich = await db.GiaoDichTaiKhoans.Where(g => g.MaKH == MaKHHienTai).OrderByDescending(g => g.ThoiGian).ToListAsync();
        ViewBag.SoDu = kh?.SoDu ?? 0;
        return View(giaoDich);
    }



    [Authorize(Roles = "KhachHang")]
    public async Task<IActionResult> HoSo()
    {
        var kh = await db.KhachHangs.FindAsync(MaKHHienTai);
        return View(kh);
    }

    [Authorize(Roles = "KhachHang"), HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> HoSo(string tenKH, string soDienThoai, string diaChi, string email)
    {
        var kh = await db.KhachHangs.FindAsync(MaKHHienTai);
        if (kh != null)
        {
            kh.TenKH = tenKH;
            kh.SoDienThoai = soDienThoai;
            kh.DiaChi = diaChi;
            kh.Email = email;
            await db.SaveChangesAsync();
            TempData["ok"] = "Đã cập nhật hồ sơ.";
        }
        return RedirectToAction(nameof(HoSo));
    }

    [Authorize(Roles = "KhachHang"), HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DoiMatKhau(string matKhauCu, string matKhauMoi, string nhapLai)
    {
        if (matKhauMoi != nhapLai)
        {
            TempData["loi"] = "Mật khẩu nhập lại không khớp.";
            return RedirectToAction(nameof(HoSo));
        }

        var kh = await db.KhachHangs.FindAsync(MaKHHienTai);
        if (kh != null)
        {
            if (kh.MatKhauHash != Mk.Hash(matKhauCu))
            {
                TempData["loi"] = "Mật khẩu cũ không chính xác.";
                return RedirectToAction(nameof(HoSo));
            }

            kh.MatKhauHash = Mk.Hash(matKhauMoi);
            await db.SaveChangesAsync();
            TempData["ok"] = "Đã đổi mật khẩu thành công.";
        }
        return RedirectToAction(nameof(HoSo));
    }

    [Authorize(Roles = "KhachHang")]
    public IActionResult TaoNhieuDon() => View();



    [Authorize(Roles = "KhachHang"), HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> TaoNhieuDon(Microsoft.AspNetCore.Http.IFormFile file)
    {
        if (file == null || file.Length == 0) { TempData["loi"] = "Vui lòng chọn file Excel."; return RedirectToAction(nameof(TaoNhieuDon)); }
        
        try
        {
            using var stream = new MemoryStream();
            await file.CopyToAsync(stream);
            using var package = new OfficeOpenXml.ExcelPackage(stream);
            var ws = package.Workbook.Worksheets[0];
            int rowCount = ws.Dimension.Rows;
            int count = 0;

            var nv = await db.NhanViens.FirstOrDefaultAsync(n => n.VaiTro == "TiepNhan") ?? await db.NhanViens.FirstAsync();
            
            for (int row = 2; row <= rowCount; row++)
            {
                var tenNhan = ws.Cells[row, 1].Value?.ToString();
                var sdtNhan = ws.Cells[row, 2].Value?.ToString();
                var diaChiNhan = ws.Cells[row, 3].Value?.ToString();
                if (string.IsNullOrEmpty(tenNhan)) continue;

                // Tự động thêm người nhận nếu chưa có
                var khNhan = await db.KhachHangs.FirstOrDefaultAsync(k => k.SoDienThoai == sdtNhan);
                if (khNhan == null)
                {
                    khNhan = new KhachHang { TenKH = tenNhan, SoDienThoai = sdtNhan ?? "", DiaChi = diaChiNhan };
                    db.KhachHangs.Add(khNhan);
                    await db.SaveChangesAsync();
                }

                decimal.TryParse(ws.Cells[row, 4].Value?.ToString(), out var kg);
                decimal.TryParse(ws.Cells[row, 5].Value?.ToString(), out var giaTri);
                decimal.TryParse(ws.Cells[row, 6].Value?.ToString(), out var cod);
                var dichVu = ws.Cells[row, 7].Value?.ToString() ?? "TieuChuan";
                
                var (bac, tien) = await CuocPhi.Tinh(db, kg, new[] { 1 });

                var prefix = "VD" + DateTime.Now.ToString("yyMMdd");
                var soDon = await db.VanDons.CountAsync(v => v.MaVanDon.StartsWith(prefix));
                var ma = prefix + (soDon + 1 + count).ToString("0000");

                var vd = new VanDon
                {
                    MaVanDon = ma, MaKH_Gui = MaKHHienTai, MaKH_Nhan = khNhan.MaKH, MaNV_Nhan = nv.MaNV,
                    MaBangGia = bac?.MaBangGia, NgayTao = DateTime.Now, TrongLuongTong = kg, TongTien = tien,
                    TrangThai = TT.ChoLay, GiaTriBuuGui = giaTri, ThuCOD = cod, LoaiDichVu = dichVu,
                    ChiTiet = new List<ChiTietVanDon> { new() { MaLoaiHang = 1, SoLuong = 1, TrongLuong = kg } },
                    HanhTrinh = new List<HanhTrinhGiaoHang> { new() { MaNV_Giao = nv.MaNV, ViTri = "Hệ thống Online", TrangThai = TT.ChoLay } }
                };
                db.VanDons.Add(vd);
                count++;
            }
            await db.SaveChangesAsync();
            TempData["ok"] = $"Đã tạo thành công {count} đơn hàng từ Excel.";
        }
        catch (Exception ex)
        {
            TempData["loi"] = "Lỗi đọc file Excel: " + ex.Message;
        }
        
        return RedirectToAction(nameof(Dashboard));
    }

    [Authorize(Roles = "KhachHang")]
    public async Task<IActionResult> ChiTiet(string id)
    {
        var vd = await db.VanDons
            .Include(v => v.NguoiGui).Include(v => v.NguoiNhan)
            .Include(v => v.ChiTiet).ThenInclude(c => c.LoaiHangHoa)
            .Include(v => v.HanhTrinh).ThenInclude(h => h.NhanVien)
            .FirstOrDefaultAsync(v => v.MaVanDon == id && v.MaKH_Gui == MaKHHienTai);
        if (vd == null) return NotFound();
        return View(vd);
    }

    [Authorize(Roles = "KhachHang")]
    public async Task<IActionResult> InDon(string id)
    {
        var vd = await db.VanDons
            .Include(v => v.NguoiGui).Include(v => v.NguoiNhan)
            .Include(v => v.ChiTiet).ThenInclude(c => c.LoaiHangHoa)
            .FirstOrDefaultAsync(v => v.MaVanDon == id && v.MaKH_Gui == MaKHHienTai);
        if (vd == null) return NotFound();
        return View(vd);
    }

    [Authorize(Roles = "KhachHang")]
    public async Task<IActionResult> XuatExcel()
    {
        var donHang = await db.VanDons.Include(v => v.NguoiNhan)
            .Where(v => v.MaKH_Gui == MaKHHienTai).OrderByDescending(v => v.NgayTao).ToListAsync();
        
        using var package = new OfficeOpenXml.ExcelPackage();
        var ws = package.Workbook.Worksheets.Add("DonHang");
        ws.Cells[1, 1].Value = "Mã VĐ"; ws.Cells[1, 2].Value = "Ngày Tạo";
        ws.Cells[1, 3].Value = "Người Nhận"; ws.Cells[1, 4].Value = "SĐT Nhận";
        ws.Cells[1, 5].Value = "Dịch Vụ"; ws.Cells[1, 6].Value = "Cước Phí";
        ws.Cells[1, 7].Value = "COD"; ws.Cells[1, 8].Value = "Trạng Thái";
        
        for (int i = 0; i < donHang.Count; i++)
        {
            var v = donHang[i];
            ws.Cells[i + 2, 1].Value = v.MaVanDon;
            ws.Cells[i + 2, 2].Value = v.NgayTao.ToString("dd/MM/yyyy");
            ws.Cells[i + 2, 3].Value = v.NguoiNhan?.TenKH;
            ws.Cells[i + 2, 4].Value = v.NguoiNhan?.SoDienThoai;
            ws.Cells[i + 2, 5].Value = v.LoaiDichVu;
            ws.Cells[i + 2, 6].Value = v.TongTien;
            ws.Cells[i + 2, 7].Value = v.ThuCOD;
            ws.Cells[i + 2, 8].Value = v.TrangThai;
        }
        ws.Cells.AutoFitColumns();
        return File(package.GetAsByteArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"DonHang_{DateTime.Now:yyyyMMdd}.xlsx");
    }

    [Authorize(Roles = "KhachHang")]
    public async Task<IActionResult> NgoaiLe()
    {
        var maKH = MaKHHienTai;
        var donNgoaiLe = await db.VanDons
            .Include(v => v.NguoiNhan)
            .Where(v => v.MaKH_Gui == maKH && (v.TrangThai == TT.Hoan || v.TrangThai == "Hủy" || v.TrangThai == "Hư hỏng" || v.TrangThai == "Thất lạc"))
            .OrderByDescending(v => v.NgayTao)
            .ToListAsync();
            
        ViewBag.Stats = new {
            TongNgoaiLe = donNgoaiLe.Count,
            HoanHang = donNgoaiLe.Count(v => v.TrangThai == TT.Hoan),
            Huy = donNgoaiLe.Count(v => v.TrangThai == "Hủy"),
            HuHongThatLac = donNgoaiLe.Count(v => v.TrangThai == "Hư hỏng" || v.TrangThai == "Thất lạc"),
            TongThietHai = donNgoaiLe.Sum(v => v.GiaTriBuuGui)
        };
        
        return View(donNgoaiLe);
    }

    // --- ADMIN AREA ---
    [Authorize(Roles = "QuanLy,TiepNhan")]
    public async Task<IActionResult> Index(string? q)
    {
        var qry = db.KhachHangs.AsQueryable();
        if (!string.IsNullOrWhiteSpace(q))
        {
            q = q.Trim();
            qry = qry.Where(k => k.TenKH.Contains(q) || k.SoDienThoai.Contains(q));
        }
        ViewBag.q = q;
        return View(await qry.OrderBy(k => k.TenKH).Take(100).ToListAsync());
    }

    [Authorize(Roles = "QuanLy,TiepNhan")]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Luu(KhachHang m)
    {
        if (!ModelState.IsValid) { TempData["loi"] = ModelState.Gop(); return RedirectToAction(nameof(Index)); }
        if (m.MaKH == 0) db.KhachHangs.Add(m); else db.KhachHangs.Update(m);
        await db.SaveChangesAsync();
        TempData["ok"] = "Đã lưu khách hàng.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "QuanLy,TiepNhan")]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Xoa(int id)
    {
        try
        {
            var m = await db.KhachHangs.FindAsync(id);
            if (m != null) { db.KhachHangs.Remove(m); await db.SaveChangesAsync(); TempData["ok"] = "Đã xóa khách hàng."; }
        }
        catch (DbUpdateException) { TempData["loi"] = "Không thể xóa: khách hàng đã có vận đơn."; }
        return RedirectToAction(nameof(Index));
    }
}
