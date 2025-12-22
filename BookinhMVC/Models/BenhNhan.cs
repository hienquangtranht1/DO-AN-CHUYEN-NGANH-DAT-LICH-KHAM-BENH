using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BookinhMVC.Models
{
    public class BenhNhan
    {
        [Key, ForeignKey("NguoiDung")]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int MaBenhNhan { get; set; }

        public string? HoTen { get; set; }
        public DateTime? NgaySinh { get; set; }
        public string? GioiTinh { get; set; }
        public string? SoDienThoai { get; set; }
        public string? Email { get; set; }
        public string? DiaChi { get; set; } = null;
        public string? SoBaoHiem { get; set; }
        public string? HinhAnhBenhNhan { get; set; }
        public DateTime NgayTao { get; set; } = DateTime.Now;

        public virtual NguoiDung? NguoiDung { get; set; }

        public virtual ICollection<DanhGia>? DanhGias { get; set; } = new List<DanhGia>();
        public virtual ICollection<HoSoBenhAn>? HoSoBenhAns { get; set; } = new List<HoSoBenhAn>();
        public virtual ICollection<LichHen>? LichHens { get; set; } = new List<LichHen>();
        public virtual ICollection<GiaoDichThanhToan>? GiaoDichThanhToans { get; set; } = new List<GiaoDichThanhToan>();
    }
}