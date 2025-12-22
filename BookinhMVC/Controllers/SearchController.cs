using BookinhMVC.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions; // Thư viện Regex

namespace BookinhMVC.Controllers
{
    public class SearchController : Controller
    {
        private readonly BookingContext _context;

        // Danh sách các từ khóa chung chung cần loại bỏ khỏi chuỗi tìm kiếm
        private static readonly List<string> CommonDoctorKeywords = new List<string> {
            "bác sĩ", "doctor", "y tá", "khám bệnh", "phòng khám", "bệnh viện", "tìm kiếm"
        };

        public SearchController(BookingContext context)
        {
            _context = context;
        }

        // Action này xử lý tất cả các yêu cầu từ Navbar: /Search/Index?q=...
        public IActionResult Index(string q)
        {
            if (string.IsNullOrWhiteSpace(q))
            {
                return RedirectToAction("Index", "Home");
            }

            string originalQuery = q.Trim();
            string normalizedQuery = originalQuery.ToLower();

            // 1. KIỂM TRA VÀ ƯU TIÊN CHUYỂN HƯỚNG ĐẾN CÁC TRANG CỐ ĐỊNH (Dịch vụ/Giới thiệu)

            // Logic đã đúng: Ưu tiên chuyển hướng sang trang cố định nếu khớp
            if (normalizedQuery.Contains("tim mạch") || normalizedQuery.Contains("cardiology"))
            {
                return RedirectToAction("Cardiology", "Home");
            }

            if (normalizedQuery.Contains("khám tổng quát") || normalizedQuery.Contains("general checkup"))
            {
                return RedirectToAction("GeneralCheckup", "Home");
            }

            if (normalizedQuery.Contains("xét nghiệm") || normalizedQuery.Contains("testing"))
            {
                return RedirectToAction("Testing", "Home");
            }

            if (normalizedQuery.Contains("giới thiệu") || normalizedQuery.Contains("bệnh viện") || normalizedQuery.Contains("about"))
            {
                return RedirectToAction("HospitalAbout", "Home");
            }

            if (normalizedQuery.Contains("liên hệ") || normalizedQuery.Contains("contact"))
            {
                return RedirectToAction("Contact", "Home");
            }
            if (normalizedQuery.Contains("hỏi đáp") || normalizedQuery.Contains("QA"))
            {
                return RedirectToAction("QA", "Home");
            }

            // 2. XỬ LÝ TÌM KIẾM BÁC SĨ/CHUYÊN KHOA

            string cleanedQuery = originalQuery;

            // Loại bỏ các từ khóa chung chung (stopwords)
            foreach (var keyword in CommonDoctorKeywords)
            {
                // Sử dụng Regex để tìm kiếm từ khóa với các ranh giới từ (\b) và thay thế bằng chuỗi rỗng
                cleanedQuery = Regex.Replace(cleanedQuery, $@"\b{Regex.Escape(keyword)}\b", "", RegexOptions.IgnoreCase).Trim();
            }

            // Xóa khoảng trắng thừa
            cleanedQuery = Regex.Replace(cleanedQuery, @"\s+", " ").Trim();

            if (string.IsNullOrWhiteSpace(cleanedQuery))
            {
                // Nếu chuỗi bị làm sạch hoàn toàn (ví dụ: chỉ gõ "Bác sĩ" hoặc "Tìm kiếm")
                // -> Chuyển hướng đến Doctors KHÔNG lọc (hiển thị toàn bộ danh sách)
                return RedirectToAction("Doctors", "Home");
            }
            else
            {
                // Nếu vẫn còn từ khóa cụ thể (ví dụ: tên riêng, tên khoa cụ thể)
                // -> Chuyển hướng đến Doctors và truyền từ khóa đã làm sạch (cleanedQuery) để lọc
                return RedirectToAction("Doctors", "Home", new { name = cleanedQuery });
            }
        }
    }
}