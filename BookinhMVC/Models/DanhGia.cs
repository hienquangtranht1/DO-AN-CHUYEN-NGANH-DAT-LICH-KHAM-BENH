using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BookinhMVC.Models
{
    public class DanhGia
    {
        [Key]
        public int MaDanhGia { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn Bác sĩ.")]
        [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn Bác sĩ.")]
        [ForeignKey("BacSi")]
        public int MaBacSi { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn Bệnh nhân.")]
        [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn Bệnh nhân.")]
        [ForeignKey("BenhNhan")]
        public int MaBenhNhan { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn điểm đánh giá.")]
        [Range(1, 5, ErrorMessage = "Điểm đánh giá phải từ 1 đến 5.")]
        public int DiemDanhGia { get; set; }

        public string NhanXet { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn ngày đánh giá.")]
        public DateTime NgayDanhGia { get; set; }

        public virtual BacSi BacSi { get; set; }
        public virtual BenhNhan BenhNhan { get; set; }
    }
}