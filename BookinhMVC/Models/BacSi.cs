using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BookinhMVC.Models
{
    public class BacSi
    {
        [Key]
        public int MaBacSi { get; set; }

        [ForeignKey("NguoiDung")]
        public int MaNguoiDung { get; set; }

        [Required]
        public string HoTen { get; set; }

        [ForeignKey("Khoa")]
        public int MaKhoa { get; set; }

        public string SoDienThoai { get; set; }
        public string Email { get; set; }
        public string HinhAnhBacSi { get; set; }
        public string MoTa { get; set; }
        public DateTime NgayTao { get; set; }

        public virtual NguoiDung NguoiDung { get; set; }
        public virtual Khoa Khoa { get; set; }
        public virtual ICollection<DanhGia> DanhGias { get; set; }
        public virtual ICollection<HoSoBenhAn> HoSoBenhAns { get; set; }
        public virtual ICollection<LichHen> LichHens { get; set; }
        public virtual ICollection<LichLamViec> LichLamViecs { get; set; }
    }
}
