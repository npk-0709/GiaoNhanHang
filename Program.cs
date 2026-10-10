using GiaoNhanHang.Data;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<AppDbContext>(o =>
    o.UseSqlServer(builder.Configuration.GetConnectionString("Default")));
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(o =>
    {
        o.LoginPath = "/Account/UserLogin";
        o.AccessDeniedPath = "/Account/AccessDenied";
        o.ExpireTimeSpan = TimeSpan.FromHours(8);
    });

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Account/UserLogin");
}

app.UseStaticFiles();

// Cố định culture en-US để số thập phân (10.5) từ ô input type=number luôn đọc đúng.
// Hiển thị tiền tệ kiểu Việt Nam được xử lý riêng trong Helpers/Fmt.cs.
var en = new CultureInfo("en-US");
var loc = new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture(en),
    SupportedCultures = new List<CultureInfo> { en },
    SupportedUICultures = new List<CultureInfo> { en }
};
loc.RequestCultureProviders.Clear();
app.UseRequestLocalization(loc);

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(name: "default", pattern: "{controller=Account}/{action=UserLogin}/{id?}");

app.Run();
