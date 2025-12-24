using Microsoft.EntityFrameworkCore;
using BookinhMVC.Models;

namespace BookinhMVC.Models
{
    public class BookingContext : DbContext
    {
        public BookingContext(DbContextOptions<BookingContext> options) : base(options) { }

        // ============== CÁC BẢNG CHÍNH ==============
        public DbSet<NguoiDung> NguoiDungs { get; set; }
        public DbSet<Khoa> Khoas { get; set; }
        public DbSet<BacSi> BacSis { get; set; }
        public DbSet<BenhNhan> BenhNhans { get; set; }
        public DbSet<CsKh> CsKhs { get; set; }

        // ============== CÁC BẢNG CHỨC NĂNG ==============
        public DbSet<LichHen> LichHens { get; set; }
        public DbSet<LichLamViec> LichLamViecs { get; set; }
        public DbSet<HoSoBenhAn> HoSoBenhAns { get; set; }
        public DbSet<DanhGia> DanhGias { get; set; }

        // ============== HỆ THỐNG CHAT & CÂU HỎI ==============
        public DbSet<ChatMessage> ChatMessages { get; set; }
        public DbSet<Question> Questions { get; set; }
        public DbSet<ThongBao> ThongBaos { get; set; }

        // ============== THANH TOÁN & VÍ TIỀN (MỚI) ==============
        public DbSet<TaiKhoanBenhNhan> TaiKhoanBenhNhan { get; set; }
        public DbSet<GiaoDichThanhToan> GiaoDichThanhToan { get; set; }

        // ============== TIN TỨC & BÀI VIẾT ==============
        public DbSet<Category> Categories { get; set; }
        public DbSet<Article> Articles { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // --------------------------------------------------------
            // 1. CẤU HÌNH USER - BACSI - BENHNHAN (QUAN HỆ 1-1)
            // --------------------------------------------------------

            // Bác sĩ - Người dùng (1-1)
            modelBuilder.Entity<BacSi>()
                .HasOne(b => b.NguoiDung)
                .WithOne(n => n.BacSi)
                .HasForeignKey<BacSi>(b => b.MaNguoiDung)
                .OnDelete(DeleteBehavior.Cascade); // Xóa User -> Xóa Bác sĩ

            // Bệnh nhân - Người dùng (1-1, Shared Primary Key)
            modelBuilder.Entity<BenhNhan>()
                .HasKey(b => b.MaBenhNhan); // Khóa chính cũng là khóa ngoại

            modelBuilder.Entity<BenhNhan>()
                .Property(b => b.MaBenhNhan)
                .ValueGeneratedNever(); // ID lấy từ NguoiDung, không tự tăng

            modelBuilder.Entity<BenhNhan>()
                .HasOne(b => b.NguoiDung)
                .WithOne(u => u.BenhNhan)
                .HasForeignKey<BenhNhan>(b => b.MaBenhNhan)
                .OnDelete(DeleteBehavior.Cascade); // Xóa User -> Xóa Bệnh nhân

            // --------------------------------------------------------
            // 2. CẤU HÌNH CÁC BẢNG LIÊN KẾT (TRÁNH LỖI CIRCULAR DELETE)
            // --------------------------------------------------------

            // Đánh giá (Restrict để tránh xung đột khi xóa User/Bác sĩ)
            modelBuilder.Entity<DanhGia>()
                .HasOne(d => d.BacSi)
                .WithMany(b => b.DanhGias)
                .HasForeignKey(d => d.MaBacSi)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<DanhGia>()
                .HasOne(d => d.BenhNhan)
                .WithMany(b => b.DanhGias)
                .HasForeignKey(d => d.MaBenhNhan)
                .OnDelete(DeleteBehavior.Restrict);

            // Hồ sơ bệnh án
            modelBuilder.Entity<HoSoBenhAn>()
                .HasOne(h => h.BacSi)
                .WithMany(b => b.HoSoBenhAns)
                .HasForeignKey(h => h.MaBacSi)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<HoSoBenhAn>()
                .HasOne(h => h.BenhNhan)
                .WithMany(b => b.HoSoBenhAns)
                .HasForeignKey(h => h.MaBenhNhan)
                .OnDelete(DeleteBehavior.Restrict);

            // Lịch hẹn (Quan trọng: Không xóa lịch sử hẹn khi xóa user)
            modelBuilder.Entity<LichHen>()
                .HasOne(l => l.BacSi)
                .WithMany(b => b.LichHens)
                .HasForeignKey(l => l.MaBacSi)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<LichHen>()
                .HasOne(l => l.BenhNhan)
                .WithMany(b => b.LichHens)
                .HasForeignKey(l => l.MaBenhNhan)
                .OnDelete(DeleteBehavior.Restrict);

            // Add unique index to prevent double-booking (MaBacSi + NgayGio)
            modelBuilder.Entity<LichHen>()
                .HasIndex(l => new { l.MaBacSi, l.NgayGio })
                .IsUnique();

            // --------------------------------------------------------
            // 3. CẤU HÌNH VÍ TIỀN & GIAO DỊCH
            // --------------------------------------------------------

            // Tài khoản Bệnh nhân (Ví tiền) - 1:1 với Bệnh nhân
            modelBuilder.Entity<TaiKhoanBenhNhan>()
                .HasKey(t => t.MaBenhNhan);

            modelBuilder.Entity<TaiKhoanBenhNhan>()
                .Property(t => t.MaBenhNhan)
                .ValueGeneratedNever();

            modelBuilder.Entity<TaiKhoanBenhNhan>()
                .HasOne(t => t.BenhNhan)
                .WithOne() // Không cần nav property ngược lại
                .HasForeignKey<TaiKhoanBenhNhan>(t => t.MaBenhNhan)
                .OnDelete(DeleteBehavior.Cascade); // Xóa Bệnh nhân -> Xóa Ví

            // Giao dịch thanh toán
            modelBuilder.Entity<GiaoDichThanhToan>()
                .HasOne(g => g.BenhNhan)
                .WithMany(b => b.GiaoDichThanhToans)
                .HasForeignKey(g => g.MaBenhNhan)
                .OnDelete(DeleteBehavior.ClientSetNull); // Dùng ClientSetNull an toàn hơn Restrict cho SQL Server

            modelBuilder.Entity<GiaoDichThanhToan>()
                .HasOne(g => g.LichHen)
                .WithMany()
                .HasForeignKey(g => g.MaLich)
                .IsRequired(false) // Mã lịch có thể null (nếu là giao dịch nạp tiền)
                .OnDelete(DeleteBehavior.ClientSetNull);

            // --------------------------------------------------------
            // 4. CẤU HÌNH TIN TỨC (ARTICLE)
            // --------------------------------------------------------

            // Article -> Category
            modelBuilder.Entity<Article>()
                .HasOne(a => a.Category)
                .WithMany(c => c.Articles)
                .HasForeignKey(a => a.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            // Article -> Author (NguoiDung)
            modelBuilder.Entity<Article>()
                .HasOne(a => a.Author)
                .WithMany(u => u.Articles)
                .HasForeignKey(a => a.AuthorId)
                .OnDelete(DeleteBehavior.Restrict);

            base.OnModelCreating(modelBuilder);
        }
    }
}