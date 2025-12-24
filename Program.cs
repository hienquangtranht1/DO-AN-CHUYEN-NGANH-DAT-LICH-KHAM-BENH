using BookinhMVC.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using VNPAY.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
builder.Services.AddHttpClient();

// Add DbContext with SQL Server
builder.Services.AddDbContext<BookingContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
);

// Add session support
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Add HttpContextAccessor for Session access
builder.Services.AddHttpContextAccessor();

// =========================================================================
// ✅ VNPAY Configuration
// =========================================================================

var vnpayConfig = builder.Configuration.GetSection("VNPAY");
builder.Services.AddVnpayClient(config =>
{
    config.TmnCode = vnpayConfig["TmnCode"]!;
    config.HashSecret = vnpayConfig["HashSecret"]!;

    // Lấy CallbackUrl từ VnpayConfig:vnp_ReturnUrl
    config.CallbackUrl = builder.Configuration["VnpayConfig:vnp_ReturnUrl"]
                        ?? vnpayConfig["CallbackUrl"]!;
});
// =========================================================================

var app = builder.Build();

// Seed default admin and CSKH accounts
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<BookingContext>();
    var passwordHasher = new PasswordHasher<NguoiDung>();

    try
    {
        // Ensure database is created
        db.Database.EnsureCreated();

        // Seed admin account if not exists
        if (!db.NguoiDungs.Any(u => u.TenDangNhap == "admin"))
        {
            var admin = new NguoiDung
            {
                TenDangNhap = "admin",
                VaiTro = "Admin",
                NgayTao = DateTime.Now
            };
            admin.MatKhau = passwordHasher.HashPassword(admin, "admin123456");
            db.NguoiDungs.Add(admin);
            db.SaveChanges();
            Console.WriteLine("✅ Admin account created successfully!");
        }

        // Seed CSKH account if not exists
        if (!db.CsKhs.Any(u => u.Username == "cskh1"))
        {
            db.CsKhs.Add(new CsKh
            {
                Username = "cskh1",
                Password = "cskh123456",
                FullName = "Chăm sóc khách hàng",
                Email = "cskh1@fourrock.com",
                Phone = "0123456789",
                CreatedAt = DateTime.Now
            });
            db.SaveChanges();
            Console.WriteLine("✅ CSKH account created successfully!");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Error during seeding: {ex.Message}");
    }
}

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
else
{
    // Enhanced error handling for development
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

// IMPORTANT: UseSession must come before UseRouting
app.UseSession();
app.UseRouting();
app.UseAuthorization();

// Map routes
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapControllers(); // For API routes (e.g., /api/Payment/ProceedAfterPayment)
app.MapRazorPages();

Console.WriteLine("🚀 Application started successfully!");
app.Run();