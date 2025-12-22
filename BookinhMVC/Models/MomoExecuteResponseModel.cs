namespace BookinhMVC.Models
{
    public class MomoExecuteResponseModel
    {
        public string OrderId { get; set; }
        public string Amount { get; set; }
        public string FullName { get; set; }
        public string OrderInfo { get; set; }
        // Có thể thêm ErrorCode nếu bạn muốn truyền từ PaymentCallBack
        public string ErrorCode { get; set; }
    }
}