namespace BookinhMVC.Models
{
    public class AppointmentViewModel
    {
        public int MaLich { get; set; }
        public string HoTenBenhNhan { get; set; }
        public string DiaChi { get; set; }
        public string GioiTinh { get; set; }
        public DateTime NgayGio { get; set; }
        public string TrieuChung { get; set; }
        public string TrangThai { get; set; }
        public List<string> AvailableTimes { get; set; } = new();
    }
} 