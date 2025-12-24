using System.Diagnostics;
using System.Linq;
using BookinhMVC.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace BookinhMVC.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly BookingContext _context;

        // Cấu hình giới hạn và phí
        private const int QA_FREE_LIMIT = 3;
        private const long QA_FEE_AMOUNT = 10000; // 

        public HomeController(ILogger<HomeController> logger, BookingContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // Tải Bác sĩ nổi bật
            var doctors = await _context.BacSis
                .Include(b => b.Khoa)
                .OrderByDescending(b => b.MaBacSi)
                .Take(8)
                .ToListAsync();
            ViewBag.Doctors = doctors;

            // Tải Đánh giá
            var reviews = await _context.DanhGias
                .Include(r => r.BenhNhan)
                .Include(r => r.BacSi)
                .OrderByDescending(r => r.NgayDanhGia)
                .Take(3)
                .ToListAsync();
            ViewBag.Reviews = reviews;

            // Tải Blog
            var latestArticles = await _context.Articles
                .Where(a => a.IsPublished)
                .Include(a => a.Category)
                .Include(a => a.Author)
                .OrderByDescending(a => a.PublishDate)
                .Take(3)
                .ToListAsync();
            ViewBag.Articles = latestArticles;

            return View();
        }

        // Action xem chi tiết Blog
        public async Task<IActionResult> ArticleDetail(string slug)
        {
            if (string.IsNullOrEmpty(slug)) return RedirectToAction("HospitalBlog");

            var article = await _context.Articles
                .Include(a => a.Category)
                .Include(a => a.Author)
                .FirstOrDefaultAsync(a => a.Slug == slug && a.IsPublished);

            if (article == null) return RedirectToAction("NotFound", "Home");

            article.ViewsCount++;
            _context.Update(article);
            await _context.SaveChangesAsync();

            return View(article);
        }

        public async Task<IActionResult> HospitalBlog()
        {
            var allArticles = await _context.Articles
                .Where(a => a.IsPublished)
                .Include(a => a.Category)
                .Include(a => a.Author)
                .OrderByDescending(a => a.PublishDate)
                .ToListAsync();
            return View(allArticles);
        }

        public async Task<IActionResult> Reviews()
        {
            var allReviews = await _context.DanhGias
                .Include(r => r.BenhNhan)
                .Include(r => r.BacSi)
                .OrderByDescending(r => r.NgayDanhGia)
                .ToListAsync();
            return View(allReviews);
        }

        // ==========================================
        // ACTION QA (GET) - ĐÃ CẬP NHẬT LẤY SỐ DƯ
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> QA()
        {
            var userId = HttpContext.Session.GetInt32("UserId");

            decimal currentBalance = 0;
            bool hasPaid = false; // Biến kiểm tra: Đã trả tiền cho lượt này chưa?

            if (userId.HasValue)
            {
                // 1. Lấy số dư ví hiện tại
                var wallet = await _context.TaiKhoanBenhNhan.FirstOrDefaultAsync(x => x.MaBenhNhan == userId.Value);
                if (wallet != null) currentBalance = wallet.SoDuHienTai;

                // 2. Kiểm tra số lượng câu hỏi đã hỏi
                int totalQuestionsSent = await _context.Questions.CountAsync(q => q.UserId == userId.Value);
                int remainingFreeQuestions = QA_FREE_LIMIT - totalQuestionsSent;

                // 3. KIỂM TRA SESSION: User đã thanh toán ở PaymentController chưa?
                var paidSession = HttpContext.Session.GetString("HasPaidQA");
                if (paidSession == "true")
                {
                    hasPaid = true; // Đã trả tiền -> Cho phép hiện form
                }

                // 4. Truyền dữ liệu sang View
                ViewBag.QuestionsInQueue = totalQuestionsSent;
                ViewBag.CanAskFree = totalQuestionsSent < QA_FREE_LIMIT;
                ViewBag.RemainingFreeQuestions = remainingFreeQuestions > 0 ? remainingFreeQuestions : 0;
            }
            else
            {
                ViewBag.CanAskFree = false;
            }

            ViewBag.CurrentBalance = currentBalance;
            ViewBag.QaFeeAmount = QA_FEE_AMOUNT; // 10.000
            ViewBag.QaFreeLimit = QA_FREE_LIMIT;
            ViewBag.HasPaid = hasPaid; // <--- QUAN TRỌNG: View sẽ dùng biến này để quyết định hiển thị

            // Load danh sách bác sĩ và câu hỏi cũ
            var doctors = await _context.BacSis.Include(b => b.Khoa).ToListAsync();
            var answeredQuestions = await _context.Questions
                .Include(q => q.User).Include(q => q.Doctor)
                .Where(q => q.Status == "Đã trả lời")
                .OrderByDescending(q => q.AnsweredAt).ToListAsync();

            ViewBag.Doctors = doctors;
            return View(answeredQuestions);
        }

        // ==========================================
        // ACTION QA (POST) - XỬ LÝ GỬI CÂU HỎI
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> QA(int DoctorId, string Title, string Content)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                TempData["Error"] = "Vui lòng đăng nhập.";
                return RedirectToAction("QA");
            }

            int totalQuestionsSent = await _context.Questions.CountAsync(q => q.UserId == userId.Value);

            // --- KIỂM TRA ĐIỀU KIỆN ---
            if (totalQuestionsSent >= QA_FREE_LIMIT)
            {
                // Nếu hết lượt free, bắt buộc phải có session HasPaidQA
                var hasPaid = HttpContext.Session.GetString("HasPaidQA");
                if (hasPaid != "true")
                {
                    TempData["Error"] = "Bạn chưa thanh toán phí tư vấn.";
                    return RedirectToAction("QA");
                }
            }

            if (DoctorId == 0 || string.IsNullOrWhiteSpace(Title) || string.IsNullOrWhiteSpace(Content))
            {
                TempData["Error"] = "Vui lòng nhập đầy đủ thông tin.";
                return RedirectToAction("QA");
            }

            // --- LƯU CÂU HỎI ---
            var question = new Question
            {
                UserId = userId.Value,
                DoctorId = DoctorId,
                Title = Title,
                Content = Content,
                Status = "Chờ trả lời",
                CreatedAt = DateTime.Now,
                Answer = "",
                Category = ""
            };
            _context.Questions.Add(question);
            await _context.SaveChangesAsync();

            // --- XỬ LÝ SAU KHI LƯU ---
            // Nếu đây là câu hỏi trả phí, xóa Session xác nhận để lần sau user phải trả tiếp
            if (totalQuestionsSent >= QA_FREE_LIMIT)
            {
                HttpContext.Session.Remove("HasPaidQA");
            }

            TempData["Message"] = "Gửi câu hỏi thành công!";
            return RedirectToAction("QA");
        }

        public IActionResult HospitalAbout() => View();
        public IActionResult Testing() => View();
        public IActionResult GeneralCheckup() => View();
        public IActionResult Cardiology()
        {
            var doctors = _context.BacSis.Include(b => b.Khoa).ToList();
            return View(doctors);
        }
        public IActionResult Contact() => View();
        public IActionResult Privacy() => View();

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error() => View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });

        public IActionResult Doctors(string name, int departmentId = 0)
        {
            var doctorsQuery = _context.BacSis.Include(b => b.Khoa).AsQueryable();

            if (!string.IsNullOrWhiteSpace(name))
            {
                string searchName = name.ToLower().Trim();
                doctorsQuery = doctorsQuery.Where(d => d.HoTen.ToLower().Contains(searchName));
            }

            if (departmentId > 0)
            {
                doctorsQuery = doctorsQuery.Where(d => d.MaKhoa == departmentId);
            }

            var filteredDoctors = doctorsQuery.ToList();

            if (filteredDoctors == null || !filteredDoctors.Any())
            {
                return RedirectToAction("NotFound", "Home");
            }

            var departments = _context.Khoas.ToList();
            ViewBag.Departments = departments;
            ViewBag.SearchName = name;
            ViewBag.SelectedDepartment = departmentId;

            return View(filteredDoctors);
        }

        public IActionResult NotFound()
        {
            Response.StatusCode = 404;
            return View();
        }
    }
}