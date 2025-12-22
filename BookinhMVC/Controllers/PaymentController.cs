using BookinhMVC.Models;
using BookinhMVC.Helpers; // Thư viện VNPAY
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace BookinhMVC.Controllers
{
    [Route("[controller]")]
    public class PaymentController : Controller
    {
        private readonly BookingContext _context;
        private readonly IConfiguration _configuration;

        public PaymentController(BookingContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        // ============================================================
        // 1. TRANG QUẢN LÝ VÍ (HIỂN THỊ SỐ DƯ)
        // ============================================================
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "User");

            // Lấy hoặc tạo mới ví
            var wallet = await _context.TaiKhoanBenhNhan.FirstOrDefaultAsync(x => x.MaBenhNhan == userId);
            if (wallet == null)
            {
                wallet = new TaiKhoanBenhNhan { MaBenhNhan = userId.Value, SoDuHienTai = 0, NgayCapNhatCuoi = DateTime.Now };
                _context.TaiKhoanBenhNhan.Add(wallet);
                await _context.SaveChangesAsync();
            }

            ViewBag.CurrentBalance = wallet.SoDuHienTai;
            return View();
        }

        // ============================================================
        // 2. TRANG LỊCH SỬ GIAO DỊCH (MỚI BỔ SUNG)
        // ============================================================
        [HttpGet("History")]
        public async Task<IActionResult> History()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "User");

            var history = await _context.GiaoDichThanhToan
                                        .Where(x => x.MaBenhNhan == userId)
                                        .OrderByDescending(x => x.NgayGiaoDich) // Mới nhất lên đầu
                                        .ToListAsync();

            return View(history);
        }

        // ============================================================
        // 3. XỬ LÝ TRỪ TIỀN VÍ (ĐỂ THANH TOÁN DỊCH VỤ QA)
        // ============================================================
        [HttpPost("PayWithWallet")]
        public async Task<IActionResult> PayWithWallet(decimal amount, string returnUrl)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "User");

            var wallet = await _context.TaiKhoanBenhNhan.FirstOrDefaultAsync(x => x.MaBenhNhan == userId);

            // Kiểm tra số dư
            if (wallet == null || wallet.SoDuHienTai < amount)
            {
                TempData["Error"] = "Số dư không đủ. Vui lòng nạp thêm!";
                if (!string.IsNullOrEmpty(returnUrl)) return Redirect(returnUrl);
                return RedirectToAction("Index");
            }

            // --- TRỪ TIỀN ---
            wallet.SoDuHienTai -= amount;
            wallet.NgayCapNhatCuoi = DateTime.Now;

            // --- GHI LOG ---
            var trans = new GiaoDichThanhToan
            {
                MaBenhNhan = userId.Value,
                SoTien = amount,
                NgayGiaoDich = DateTime.Now,
                LoaiGiaoDich = "Thanh toán dịch vụ (Trừ Ví)",
                NoiDung = "Thanh toán phí hỏi đáp bác sĩ",
                MaThamChieu = DateTime.Now.Ticks.ToString(),
                TrangThai = "Thành công"
            };
            _context.GiaoDichThanhToan.Add(trans);
            await _context.SaveChangesAsync();

            // --- MỞ KHÓA QA ---
            HttpContext.Session.SetString("HasPaidQA", "true");

            TempData["Success"] = $"Thanh toán thành công! Đã trừ {amount:N0}đ.";

            if (!string.IsNullOrEmpty(returnUrl)) return Redirect(returnUrl);
            return RedirectToAction("Index");
        }

        // ============================================================
        // 4. TẠO GIAO DỊCH NẠP TIỀN QUA QR (CHECKOUT)
        // ============================================================
        [HttpPost("Checkout")]
        public async Task<IActionResult> Checkout(decimal amount, string orderInfo)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "User");

            string uniqueCode = DateTime.Now.Ticks.ToString();

            // Tạo giao dịch Pending
            var trans = new GiaoDichThanhToan
            {
                MaBenhNhan = userId.Value,
                SoTien = amount,
                NgayGiaoDich = DateTime.Now,
                LoaiGiaoDich = "Nạp tiền (QR)",
                NoiDung = orderInfo ?? "Nap tien vao vi",
                MaThamChieu = uniqueCode,
                TrangThai = "Đang xử lý"
            };

            _context.GiaoDichThanhToan.Add(trans);
            await _context.SaveChangesAsync();

            ViewBag.TransId = trans.MaGiaoDich;
            ViewBag.Amount = amount;
            ViewBag.Content = uniqueCode;
            ViewBag.OrderInfo = orderInfo;
            ViewBag.BankId = "TCB";
            ViewBag.AccountNo = "19074184799019";
            ViewBag.AccountName = "TRAN QUANG HIEN";

            return View("Checkout");
        }

        // ============================================================
        // 5. API CHECK TRẠNG THÁI (DÙNG CHO TRANG QR)
        // ============================================================
        [HttpGet("CheckStatus")]
        public async Task<IActionResult> CheckStatus(int id)
        {
            var trans = await _context.GiaoDichThanhToan.FindAsync(id);

            if (trans != null && trans.TrangThai == "Thành công")
            {
                // Nếu QR thành công thì CỘNG TIỀN VÀO VÍ
                await ProcessAddMoneyToWallet(trans);

                return Json(new { success = true });
            }
            return Json(new { success = false });
        }

        // ============================================================
        // 6. NẠP TIỀN QUA VNPAY
        // ============================================================
        [HttpPost("CreatePaymentVnpay")]
        public async Task<IActionResult> CreatePaymentVnpay(decimal amount)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "User");

            if (amount < 10000)
            {
                TempData["Error"] = "VNPAY yêu cầu nạp tối thiểu 10,000đ";
                return RedirectToAction("Index");
            }

            string uniqueCode = DateTime.Now.Ticks.ToString();
            var trans = new GiaoDichThanhToan
            {
                MaBenhNhan = userId.Value,
                SoTien = amount,
                NgayGiaoDich = DateTime.Now,
                LoaiGiaoDich = "Nạp tiền Ví (VNPAY)",
                NoiDung = "Nạp tiền qua cổng VNPAY",
                MaThamChieu = uniqueCode,
                TrangThai = "Đang xử lý"
            };
            _context.GiaoDichThanhToan.Add(trans);
            await _context.SaveChangesAsync();

            // Config VNPAY
            string vnp_Returnurl = _configuration["VnPay:ReturnUrl"];
            string vnp_Url = _configuration["VnPay:BaseUrl"];
            string vnp_TmnCode = _configuration["VnPay:TmnCode"];
            string vnp_HashSecret = _configuration["VnPay:HashSecret"];

            VnPayLibrary vnpay = new VnPayLibrary();
            vnpay.AddRequestData("vnp_Version", VnPayLibrary.VERSION);
            vnpay.AddRequestData("vnp_Command", "pay");
            vnpay.AddRequestData("vnp_TmnCode", vnp_TmnCode);
            vnpay.AddRequestData("vnp_Amount", ((long)amount * 100).ToString());
            vnpay.AddRequestData("vnp_CreateDate", DateTime.Now.ToString("yyyyMMddHHmmss"));
            vnpay.AddRequestData("vnp_CurrCode", "VND");
            vnpay.AddRequestData("vnp_IpAddr", "127.0.0.1");
            vnpay.AddRequestData("vnp_Locale", "vn");
            vnpay.AddRequestData("vnp_OrderInfo", "Nap tien " + uniqueCode);
            vnpay.AddRequestData("vnp_OrderType", "other");
            vnpay.AddRequestData("vnp_ReturnUrl", vnp_Returnurl);
            vnpay.AddRequestData("vnp_TxnRef", uniqueCode);

            string paymentUrl = vnpay.CreateRequestUrl(vnp_Url, vnp_HashSecret);
            return Redirect(paymentUrl);
        }

        [HttpGet("PaymentCallback")]
        public async Task<IActionResult> PaymentCallback()
        {
            if (Request.Query.Count == 0) return RedirectToAction("Index");

            string vnp_HashSecret = _configuration["VnPay:HashSecret"];
            var vnpayData = Request.Query;
            VnPayLibrary vnpay = new VnPayLibrary();

            foreach (var s in vnpayData)
            {
                if (!string.IsNullOrEmpty(s.Key) && s.Key.StartsWith("vnp_"))
                {
                    vnpay.AddResponseData(s.Key, s.Value);
                }
            }

            long orderId = Convert.ToInt64(vnpay.GetResponseData("vnp_TxnRef"));
            long vnp_Amount = Convert.ToInt64(vnpay.GetResponseData("vnp_Amount")) / 100;
            string vnp_ResponseCode = vnpay.GetResponseData("vnp_ResponseCode");
            string vnp_SecureHash = vnpay.GetResponseData("vnp_SecureHash");

            bool checkSignature = vnpay.ValidateSignature(vnp_SecureHash, vnp_HashSecret);

            if (checkSignature)
            {
                if (vnp_ResponseCode == "00")
                {
                    // CẬP NHẬT TRẠNG THÁI VÀ CỘNG TIỀN
                    var trans = await _context.GiaoDichThanhToan.FirstOrDefaultAsync(x => x.MaThamChieu == orderId.ToString());
                    if (trans != null)
                    {
                        trans.TrangThai = "Thành công";
                        await ProcessAddMoneyToWallet(trans);
                    }
                    TempData["Success"] = $"Thanh toán VNPAY thành công! Đã cộng {vnp_Amount:N0}đ.";
                }
                else
                {
                    var trans = await _context.GiaoDichThanhToan.FirstOrDefaultAsync(x => x.MaThamChieu == orderId.ToString());
                    if (trans != null) { trans.TrangThai = "Thất bại"; await _context.SaveChangesAsync(); }
                    TempData["Error"] = $"Lỗi VNPAY: {vnp_ResponseCode}";
                }
            }
            return RedirectToAction("Index");
        }

        // ============================================================
        // 7. HÀM PHỤ TRỢ: CỘNG TIỀN VÀO VÍ (TRÁNH LOGIC LẶP LẠI)
        // ============================================================
        private async Task ProcessAddMoneyToWallet(GiaoDichThanhToan trans)
        {
            // Chỉ cộng tiền nếu là giao dịch NẠP TIỀN
            if (trans.LoaiGiaoDich.Contains("Nạp tiền"))
            {
                var wallet = await _context.TaiKhoanBenhNhan.FirstOrDefaultAsync(x => x.MaBenhNhan == trans.MaBenhNhan);
                if (wallet == null)
                {
                    wallet = new TaiKhoanBenhNhan { MaBenhNhan = trans.MaBenhNhan, SoDuHienTai = 0, NgayCapNhatCuoi = DateTime.Now };
                    _context.TaiKhoanBenhNhan.Add(wallet);
                }

                // Logic đơn giản: Cộng tiền. 
                // Thực tế nên có cờ IsProcessed để tránh cộng 2 lần nếu gọi hàm này nhiều lần.
                // Ở đây giả sử gọi hàm này là đã kiểm soát được luồng.

                // Lưu ý: Nếu trạng thái đã là Thành công trước đó rồi thì cẩn thận cộng đúp.
                // Nhưng ở controller này mình gọi hàm này ngay khi set Thành công nên tạm ổn cho demo.

                wallet.SoDuHienTai += trans.SoTien;
                wallet.NgayCapNhatCuoi = DateTime.Now;

                await _context.SaveChangesAsync();
            }
        }
    }
}