using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BookinhMVC.Models
{
    public class LichLamViec
    {
        [Key]
        public int MaLich { get; set; }
        public int MaBacSi { get; set; }
        public DateTime NgayLamViec { get; set; }
        public string ThuTrongTuan { get; set; }
        public TimeSpan GioBatDau { get; set; }
        public TimeSpan GioKetThuc { get; set; }
        public string TrangThai { get; set; }
        public DateTime NgayTao { get; set; }

        [ForeignKey("MaBacSi")]
        public BacSi BacSi { get; set; }
    }

}
