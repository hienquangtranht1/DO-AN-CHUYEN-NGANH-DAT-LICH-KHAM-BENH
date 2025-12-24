using System.ComponentModel.DataAnnotations;

namespace BookinhMVC.Models
{
    public class Khoa
    {
        [Key]
        public int MaKhoa { get; set; }
        public string TenKhoa { get; set; }
        public string MoTa { get; set; }
        public DateTime NgayTao { get; set; }

        public ICollection<BacSi> BacSis { get; set; }
    }

}
