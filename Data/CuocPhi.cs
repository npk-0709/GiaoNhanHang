using GiaoNhanHang.Models;
using Microsoft.EntityFrameworkCore;

namespace GiaoNhanHang.Data;

/// <summary>
/// Tính cước: tìm bậc giá theo tổng khối lượng (KhoiLuongTu &lt; kg &lt;= KhoiLuongDen),
/// sau đó nhân với hệ số phụ phí cao nhất trong các loại hàng của đơn.
/// Nếu vượt bậc cuối cùng thì dùng bậc cuối.
/// </summary>
public static class CuocPhi
{
    public static async Task<(BangGia? Bac, decimal Tien)> Tinh(AppDbContext db, decimal kg, IEnumerable<int> loaiIds)
    {
        if (kg <= 0) return (null, 0);

        var bac = await db.BangGias
            .Where(b => b.KhoiLuongTu < kg && kg <= b.KhoiLuongDen)
            .OrderBy(b => b.KhoiLuongTu)
            .FirstOrDefaultAsync()
            ?? await db.BangGias.OrderByDescending(b => b.KhoiLuongDen).FirstOrDefaultAsync();

        if (bac == null) return (null, 0);

        var ids = loaiIds.Distinct().ToList();
        decimal heSo = 1m;
        if (ids.Count > 0)
        {
            heSo = await db.LoaiHangs.Where(l => ids.Contains(l.MaLoaiHang))
                .Select(l => (decimal?)l.HeSoPhuPhi).MaxAsync() ?? 1m;
        }
        return (bac, Math.Round(bac.DonGia * heSo, 0));
    }
}
