using System.ComponentModel.DataAnnotations;

namespace BookinhMVC.Models
{
    // Mô hình dùng để truyền dữ liệu thanh toán từ View/Controller đến MomoService
    public class OrderInfoModel
    {
        [Required]
        public string FullName { get; set; }

        // Sử dụng long vì MoMo yêu cầu số tiền là số nguyên (không có thập phân)
        [Required]
        public long Amount { get; set; }

        [Required]
        public string OrderInfo { get; set; }

        public string OrderId { get; set; } // Sẽ được tạo trong Service
    }
}