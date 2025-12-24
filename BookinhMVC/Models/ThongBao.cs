using System;
using System.ComponentModel.DataAnnotations;

namespace BookinhMVC.Models
{
    public class ThongBao
    {
        [Key]
        public int MaThongBao { get; set; }
        public int MaNguoiDung { get; set; } // Người nhận
        public string TieuDe { get; set; }
        public string NoiDung { get; set; }
        public DateTime NgayTao { get; set; } = DateTime.Now;
        public bool DaXem { get; set; } = false;
        public int? MaLichHen { get; set; } // Link tới lịch hẹn (nếu có)
    }
}