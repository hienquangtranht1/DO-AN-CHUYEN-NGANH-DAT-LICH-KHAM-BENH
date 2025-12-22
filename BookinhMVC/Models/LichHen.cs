using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema; // Cần thêm nếu bạn muốn chi tiết hơn về kiểu dữ liệu SQL

namespace BookinhMVC.Models
{
    public class LichHen
    {
        [Key]
        public int MaLich { get; set; }

        public int MaBenhNhan { get; set; }

        public int MaBacSi { get; set; }

        public DateTime NgayGio { get; set; }

        public string TrieuChung { get; set; }

        public string TrangThai { get; set; } // Trạng thái xác nhận (Đã xác nhận, Chờ xác nhận, Đã hủy)
        public DateTime NgayTao { get; set; } = DateTime.Now; // Mặc định là giờ hiện tại

        // ============== THUỘC TÍNH MỚI CHO THANH TOÁN ==============

        // Sử dụng bool/bit để lưu trạng thái thanh toán. Mặc định là false (chưa thanh toán).
        // [Column(TypeName = "bit")] // Có thể thêm nếu dùng Data Annotations chi tiết


        // ==========================================================

        // Navigation Properties
        public BenhNhan BenhNhan { get; set; }
        public BacSi BacSi { get; set; }
        public bool DaThongBao { get; set; } = false; // Cờ đánh dấu đã gửi nhắc nhở
    }
}