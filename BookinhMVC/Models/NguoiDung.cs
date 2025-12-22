using System.ComponentModel.DataAnnotations;

namespace BookinhMVC.Models
{
    public class NguoiDung
    {
        [Key]
        public int MaNguoiDung { get; set; }
        public string TenDangNhap { get; set; }
        public string MatKhau { get; set; }
        public string VaiTro { get; set; }
        public DateTime NgayTao { get; set; }

        public BacSi BacSi { get; set; }
        public BenhNhan BenhNhan { get; set; }
        public ICollection<Question> Questions { get; set; }
        public ICollection<Article> Articles { get; set; }
    }
}
