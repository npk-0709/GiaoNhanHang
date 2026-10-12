using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace GiaoNhanHang.Helpers;

/// <summary>Các trạng thái của vận đơn.</summary>
public static class TT
{
    public const string ChoLay = "Chờ lấy hàng";
    public const string DaNhapKho = "Đã nhập kho";
    public const string DangVanChuyen = "Đang vận chuyển";
    public const string DangGiao = "Đang giao";
    public const string DaGiao = "Đã giao";
    public const string Hoan = "Hoàn hàng";

    public static readonly string[] Tat = { ChoLay, DaNhapKho, DangVanChuyen, DangGiao, DaGiao, Hoan };

    public static bool Cuoi(string t) => t == DaGiao || t == Hoan;

    public static string Css(string t) => t switch
    {
        ChoLay => "tt-cho",
        DaNhapKho => "tt-kho",
        DangVanChuyen => "tt-van",
        DangGiao => "tt-giao",
        DaGiao => "tt-ok",
        Hoan => "tt-hoan",
        _ => "tt-cho"
    };
}

public static class Fmt
{
    static readonly CultureInfo Vi = new("vi-VN");
    public static string Tien(decimal v) => v.ToString("N0", Vi) + " ₫";
    public static string Ngay(DateTime d) => d.ToString("dd/MM/yyyy HH:mm");
    public static string NgayNgan(DateTime d) => d.ToString("dd/MM/yyyy");
    public static string Kg(decimal v) => v.ToString("0.##", CultureInfo.InvariantCulture) + " kg";
    public static string So(decimal v) => v.ToString("0.##", CultureInfo.InvariantCulture);

    public static string AnSdt(string? s) =>
        string.IsNullOrEmpty(s) || s.Length < 7 ? "***" : s[..3] + new string('*', s.Length - 6) + s[^3..];

    public static string AnTen(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return "***";
        var p = s.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return "*** " + p[^1];
    }
}

public static class Mk
{
    // Chỉ dùng cho bài tập. Hệ thống thật nên dùng ASP.NET Core Identity / PBKDF2 có salt.
    public static string Hash(string s) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(s)));
}
//public static class Mk
//{
//    // Chỉ dùng cho bài tập. Hệ thống thật nên dùng ASP.NET Core Identity / PBKDF2 có salt.
//    public static string Hash(string s) => Convert.ToHexString(SHA1.HashData(Encoding.UTF8.GetBytes(s)));
//}

public static class ModelStateExt
{
    public static string Gop(this ModelStateDictionary ms) =>
        string.Join("; ", ms.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).Where(m => !string.IsNullOrWhiteSpace(m)));
}
