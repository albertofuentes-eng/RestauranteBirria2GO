using System.Globalization;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;
using RestauranteControl.Data;
using RestauranteControl.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews(opciones =>
{
    // Todo requiere sesión salvo lo marcado con [AllowAnonymous]; todos los POST validan antiforgery
    opciones.Filters.Add(new AuthorizeFilter());
    opciones.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
});

builder.Services.AddDbContext<AppDbContext>(o =>
    o.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddHttpContextAccessor();
builder.Services.AddMemoryCache();
builder.Services.AddScoped<ConfigService>();
builder.Services.AddScoped<PermisoService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<ComboService>();
builder.Services.AddScoped<CuentasMesaService>();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(o =>
    {
        o.LoginPath = "/Account/Login";
        o.AccessDeniedPath = "/Account/AccesoDenegado";
        o.ExpireTimeSpan = TimeSpan.FromHours(8);
        o.SlidingExpiration = true;
        o.Cookie.HttpOnly = true;
        o.Cookie.SameSite = SameSiteMode.Lax;
    });
builder.Services.AddAuthorization();

// Cultura invariante: evita problemas con separadores decimales al enlazar montos
var cultura = CultureInfo.InvariantCulture;
CultureInfo.DefaultThreadCurrentCulture = cultura;
CultureInfo.DefaultThreadCurrentUICulture = cultura;

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture(cultura),
    SupportedCultures = new List<CultureInfo> { cultura },
    SupportedUICultures = new List<CultureInfo> { cultura }
});
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");

await DbInitializer.AsegurarParametrosAsync(app.Services);
await DbInitializer.SembrarAdministradorAsync(app.Services);

app.Run();
