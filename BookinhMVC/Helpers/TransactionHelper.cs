using System;

namespace BookinhMVC.Helpers
{
    /// <summary>
    /// Helper class để tạo mã giao dịch duy nhất
    /// </summary>
    public static class TransactionHelper
    {
        /// <summary>
        /// Tạo mã giao dịch duy nhất với prefix
        /// Định dạng: NAP_20251214_123456789
        /// </summary>
        public static string GenerateCode(string prefix = "NAP")
        {
            // Loại bỏ ký tự không hợp lệ từ prefix
            prefix = System.Text.RegularExpressions.Regex.Replace(prefix, @"[^A-Z0-9]", "");
            
            // Tạo mã: PREFIX_YYYYMMDD_TICKS
            var timestamp = DateTime.Now;
            var dateStr = timestamp.ToString("yyyyMMdd");
            var ticks = timestamp.Ticks.ToString().Substring(8); // Lấy 8 ký tự cuối của Ticks
            
            return $"{prefix}_{dateStr}_{ticks}";
        }

        /// <summary>
        /// Tạo mã giao dịch với loại cụ thể
        /// </summary>
        public static string GenerateTransactionCode(string loaiGiaoDich)
        {
            return loaiGiaoDich switch
            {
                "Nạp tiền" => GenerateCode("NAP"),
                "Thanh toán lịch hẹn" => GenerateCode("CK"),
                "Hoàn tiền" => GenerateCode("HT"),
                _ => GenerateCode("GD")
            };
        }

        /// <summary>
        /// Kiểm tra mã giao dịch hợp lệ hay không
        /// </summary>
        public static bool IsValidTransactionCode(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
                return false;

            var parts = code.Split('_');
            return parts.Length == 3 && 
                   (parts[0] == "NAP" || parts[0] == "CK" || parts[0] == "HT" || parts[0] == "GD") &&
                   parts[1].Length == 8 && // YYYYMMDD
                   long.TryParse(parts[1], out _) &&
                   parts[2].Length >= 8; // Ticks
        }

        /// <summary>
        /// Kiểm tra mã giao dịch đã tồn tại hay không (trong cơ sở dữ liệu)
        /// Hàm này cần được gọi với DbContext
        /// </summary>
        public static string SanitizeTransactionCode(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
                return string.Empty;

            // Loại bỏ ký tự đặc biệt, chỉ giữ lại chữ cái, số, dấu gạch dưới
            return System.Text.RegularExpressions.Regex.Replace(code, @"[^A-Z0-9_]", "");
        }
    }
}