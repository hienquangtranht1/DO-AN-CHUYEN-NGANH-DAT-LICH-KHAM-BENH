using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BookinhMVC.Models
{
    [Table("TaiKhoanBenhNhan")]
    public class TaiKhoanBenhNhan
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int MaBenhNhan { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal SoDuHienTai { get; set; } = 0;

        [Column(TypeName = "decimal(18, 2)")]
        public decimal TongTienNap { get; set; } = 0;

        [Column(TypeName = "decimal(18, 2)")]
        public decimal TongTienChi { get; set; } = 0;

        public DateTime NgayCapNhatCuoi { get; set; } = DateTime.Now;

        public DateTime NgayTao { get; set; } = DateTime.Now;

        [MaxLength(50)]
        public string? TrangThai { get; set; }

        [MaxLength(500)]
        public string? GhiChu { get; set; }

        // navigation may be null until explicitly loaded
        [ForeignKey("MaBenhNhan")]
        public virtual BenhNhan? BenhNhan { get; set; }

        public void CongTien(decimal soTien)
        {
            if (soTien <= 0) throw new InvalidOperationException("Số tiền phải lớn hơn 0.");
            SoDuHienTai += soTien;
            TongTienNap += soTien;
            NgayCapNhatCuoi = DateTime.Now;
        }

        public bool TruTien(decimal soTien)
        {
            if (soTien <= 0) throw new InvalidOperationException("Số tiền phải lớn hơn 0.");
            if (SoDuHienTai < soTien) return false;
            SoDuHienTai -= soTien;
            TongTienChi += soTien;
            NgayCapNhatCuoi = DateTime.Now;
            return true;
        }
    }
}