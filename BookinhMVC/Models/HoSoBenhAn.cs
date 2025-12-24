using System.ComponentModel.DataAnnotations;

namespace BookinhMVC.Models
{
    public class HoSoBenhAn
    {
        [Key]
        public int MaHoSo { get; set; }
        public int MaBenhNhan { get; set; }
        public int MaBacSi { get; set; }
        public string ChanDoan { get; set; }
        public string PhuongAnDieuTri { get; set; }
        public DateTime NgayKham { get; set; }

        public BenhNhan BenhNhan { get; set; }
        public BacSi BacSi { get; set; }
    }
}
