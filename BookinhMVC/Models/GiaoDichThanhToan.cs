using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BookinhMVC.Models
{
    [Table("GiaoDichThanhToan")]
    public class GiaoDichThanhToan
    {
        [Key]
        public int MaGiaoDich { get; set; }

        [ForeignKey("BenhNhan")]
        public int MaBenhNhan { get; set; }

        // Navigation can be null when entity loaded partially
        public virtual BenhNhan? BenhNhan { get; set; }

        public int? MaLich { get; set; }
        public virtual LichHen? LichHen { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal SoTien { get; set; }

        public DateTime NgayGiaoDich { get; set; } = DateTime.Now;

        // Allow nullable strings to avoid required-in-constructor warnings
        [MaxLength(50)]
        public string? LoaiGiaoDich { get; set; }

        [MaxLength(255)]
        public string? MoTa { get; set; }

        // PaymentController uses NoiDung -> keep it nullable
        [MaxLength(255)]
        public string? NoiDung { get; set; }

        [MaxLength(50)]
        public string? MaThamChieu { get; set; }

        [MaxLength(50)]
        public string? TrangThai { get; set; }

        [MaxLength(100)]
        public string? PhuongThucThanhToan { get; set; }

        [MaxLength(500)]
        public string? GhiChu { get; set; }

        public DateTime? NgayCapNhat { get; set; }

        [MaxLength(50)]
        public string? NguoiXuLy { get; set; }
    }
}