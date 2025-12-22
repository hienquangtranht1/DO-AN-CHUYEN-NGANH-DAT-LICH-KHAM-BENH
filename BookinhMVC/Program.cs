using BookinhMVC.Models;
using BookinhMVC.Services; // Nơi chứa AppointmentReminderService, MomoService...
using BookinhMVC.Hubs;     // 👇 QUAN TRỌNG: Import Hubs để dùng BookingHub
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Text.Json.Serialization; // Quan trọng để fix lỗi JSON Cycle

var builder = WebApplication.CreateBuilder(args);

// ==================================================================
// 1. ADD SERVICES (CẤU HÌNH DỊCH VỤ)
// ==================================================================

// 1.1 Đăng ký SignalR (Real-time)
builder.Services.AddSignalR();

// 1.2 Cấu hình Controller & JSON (Tránh lỗi vòng lặp dữ liệu khi query EF Core)
builder.Services.AddControllersWithViews()
    .AddJsonOptions(x => x.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles);

builder.Services.AddRazorPages();
builder.Services.AddHttpClient();
builder.Services.AddHttpContextAccessor(); // Cho phép truy cập Session/User ở mọi nơi

// 1.3 Đăng ký Background Service (Nhắc lịch hẹn tự động)
builder.Services.AddHostedService<AppointmentReminderService>();

// 1.4 Kết nối Database SQL Server
builder.Services.AddDbContext<BookingContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
);

// 1.5 Cấu hình Cookie Authentication (Giữ đăng nhập cho Web & API qua Session)
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "FourRockAuth";
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.LoginPath = "/User/Login";
        options.SlidingExpiration = true;
    });

// 1.6 Cấu hình Session (Lưu OTP, Giỏ hàng, User Info tạm)
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(60); // Tăng lên 60p cho thoải mái
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// 1.7 Cấu hình Thanh toán MoMo
builder.Services.Configure<MomoOptionModel>(builder.Configuration.GetSection("MomoAPI"));
builder.Services.AddScoped<IMomoService, MomoService>();

// 1.8 Cấu hình CORS (Cho phép Mobile App/Emulator gọi API và SignalR)
builder.Services.AddCors(options =>
{
    options.AddPolicy("LocalFlutter", policy =>
    {
        policy.WithOrigins(
            "http://10.0.2.2:5062",    // Android Emulator (Quan trọng nhất)
            "http://localhost:5062",   // Web Browser
            "http://127.0.0.1:5062"    // Local IP
        )
        .AllowAnyMethod()
        .AllowAnyHeader()
        .AllowCredentials(); // 👈 BẮT BUỘC để SignalR và Session hoạt động qua CORS
    });
});

var app = builder.Build();

// ==================================================================
// 2. SEED DATA (TẠO DỮ LIỆU MẪU TỰ ĐỘNG)
// ==================================================================
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var db = services.GetRequiredService<BookingContext>();
        var passwordHasher = new PasswordHasher<NguoiDung>();

        // db.Database.EnsureCreated(); // Mở nếu muốn tạo DB mới hoàn toàn

        // Tạo Admin
        if (!db.NguoiDungs.Any(u => u.TenDangNhap == "admin"))
        {
            var admin = new NguoiDung { TenDangNhap = "admin", VaiTro = "Admin", NgayTao = DateTime.Now };
            admin.MatKhau = passwordHasher.HashPassword(admin, "admin123456");
            db.NguoiDungs.Add(admin);
            db.SaveChanges();
            Console.WriteLine("✅ Admin created");
        }

        // Tạo CSKH
        if (!db.CsKhs.Any(u => u.Username == "cskh1"))
        {
            db.CsKhs.Add(new CsKh
            {
                Username = "cskh1",
                Password = "cskh123456",
                FullName = "CSKH",
                Email = "cskh1@fourrock.com",
                Phone = "0123456789",
                CreatedAt = DateTime.Now
            });
            db.SaveChanges();
            Console.WriteLine("✅ CSKH created");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Error seeding data: {ex.Message}");
    }
}

// ==================================================================
// 3. CONFIGURE PIPELINE (CẤU HÌNH HTTP REQUEST)
// ==================================================================

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}

// ⚠️ Tắt HTTPS Redirection để Emulator kết nối dễ dàng hơn (tránh lỗi SSL)
// app.UseHttpsRedirection(); 

app.UseStaticFiles();

app.UseRouting();

// ⚠️ CORS phải đặt GIỮA Routing và Auth
app.UseCors("LocalFlutter");

app.UseAuthentication();
app.UseAuthorization();
app.UseSession(); // Session đặt sau Auth

// 👇 3.1 ĐĂNG KÝ ĐƯỜNG DẪN SIGNALR HUB (QUAN TRỌNG NHẤT CHO MOBILE)
app.MapHub<BookingHub>("/bookingHub");

// Định tuyến Controller
app.MapControllers();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

// API Health Check (Để Mobile kiểm tra Server sống hay chết)
app.MapGet("/api/health", async (BookingContext db) =>
{
    try
    {
        var canConnect = await db.Database.CanConnectAsync();
        return Results.Ok(new { status = "ok", db_connected = canConnect, time = DateTime.Now });
    }
    catch (Exception ex)
    {
        return Results.Problem(detail: ex.Message);
    }
});

Console.WriteLine("🚀 Server starting on port 5062...");
app.Run();