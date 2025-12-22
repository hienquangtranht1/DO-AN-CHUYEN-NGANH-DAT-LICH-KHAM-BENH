using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using BookinhMVC.Models;
using Microsoft.Extensions.Configuration;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.IO;
using System.Globalization;

namespace BookinhMVC.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChatbotController : ControllerBase
    {
        private readonly BookingContext _context;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly string _geminiApiKey;
        private readonly string _geminiApiUrl;
        private static List<DiseaseInfo> _diseases;

        public ChatbotController(BookingContext context, IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;

            _geminiApiUrl = _configuration["Gemini:ApiUrl"];
            _geminiApiKey = _configuration["Gemini:ApiKey"];

            // Load danh sách bệnh từ file JSON
            if (_diseases == null)
            {
                LoadDiseasesFromJson();
            }
        }

        // Load diseases từ file JSON
        private void LoadDiseasesFromJson()
        {
            try
            {
                // Thử nhiều đường dẫn khác nhau
                var possiblePaths = new[]
                {
                    Path.Combine(Directory.GetCurrentDirectory(), "diseases.json"),
                    Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "diseases.json"),
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "diseases.json")
                };

                string jsonContent = null;
                foreach (var path in possiblePaths)
                {
                    if (System.IO.File.Exists(path))
                    {
                        jsonContent = System.IO.File.ReadAllText(path, Encoding.UTF8);
                        System.Diagnostics.Debug.WriteLine($"✓ Loaded diseases from: {path}");
                        break;
                    }
                }

                if (jsonContent != null)
                {
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                    };
                    var diseaseData = JsonSerializer.Deserialize<DiseaseData>(jsonContent, options);
                    _diseases = diseaseData?.diseases ?? new List<DiseaseInfo>();
                    System.Diagnostics.Debug.WriteLine($"✓ Loaded {_diseases.Count} diseases");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("✗ diseases.json not found in any location");
                    _diseases = new List<DiseaseInfo>();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"✗ Error loading diseases: {ex.Message}");
                _diseases = new List<DiseaseInfo>();
            }
        }

        // Test endpoint để kiểm tra xử lý bệnh
        [HttpGet("test-disease-query")]
        public IActionResult TestDiseaseQuery([FromQuery] string query = "toi bi dau dau")
        {
            var normalized = NormalizeMessage(query);
            var diseaseReply = ProcessDiseaseQuery(normalized);

            return Ok(new
            {
                OriginalQuery = query,
                NormalizedQuery = normalized,
                DiseasesLoaded = _diseases?.Count ?? 0,
                Reply = diseaseReply ?? "No disease found"
            });
        }

        // Test endpoint để kiểm tra diseases đã load chưa
        [HttpGet("test-diseases")]
        public IActionResult TestDiseases()
        {
            var result = new
            {
                DiseasesCount = _diseases?.Count ?? 0,
                DiseasesLoaded = _diseases != null,
                SampleDiseases = _diseases?.Take(5).Select(d => d.name).ToList() ?? new List<string>()
            };
            return Ok(result);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Post([FromForm] string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return Ok(new { reply = "Tin nhắn không được để trống." });

            var hospitalInfo = await GetHospitalInfo();

            // Chuẩn hóa và sửa lỗi chính tả
            var cleanMessage = NormalizeMessage(message);

            // Kiểm tra câu hỏi về bệnh tật trước
            var diseaseReply = ProcessDiseaseQuery(cleanMessage);
            if (!string.IsNullOrEmpty(diseaseReply))
                return Ok(new { reply = diseaseReply });

            // Xử lý local query
            var localReply = ProcessLocalQuery(cleanMessage, hospitalInfo);
            if (!string.IsNullOrEmpty(localReply))
                return Ok(new { reply = localReply });

            // Nếu không có, gọi Gemini AI
            var context = BuildContext(hospitalInfo);
            var fullMessage = context + "Câu hỏi: " + message;

            var geminiReply = await CallGeminiApi(fullMessage);
            return Ok(new { reply = CleanResponse(geminiReply) });
        }

        // Chuẩn hóa tin nhắn và sửa lỗi chính tả
        private string NormalizeMessage(string message)
        {
            var normalized = message.ToLower().Trim();

            // Từ điển sửa lỗi chính tả thông dụng
            var corrections = new Dictionary<string, string>
            {
                { "toi", "tôi" },
                { "bi", "bị" },
                { "dau", "đau" },
                { "benh", "bệnh" },
                { "lam", "làm" },
                { "sao", "sao" },
                { "nao", "nào" },
                { "nhu", "như" },
                { "the", "thế" },
                { "nhe", "nhẹ" },
                { "nang", "nặng" },
                { "uong", "uống" },
                { "thuoc", "thuốc" },
                { "gi", "gì" },
                { "ma", "mà" },
                { "co", "có" },
                { "khong", "không" },
                { "duoc", "được" },
                { "voi", "với" },
                { "cua", "của" },
                { "cho", "cho" },
                { "khi", "khi" },
                { "hay", "hay" },
                { "hoac", "hoặc" },
                { "va", "và" },
                { "neu", "nếu" },
                { "rat", "rất" },
                { "nhieu", "nhiều" },
                { "mot", "một" },
                { "nua", "nữa" },
                { "roi", "rồi" },
                { "chua", "chưa" },
                { "ban", "bạn" },
                { "minh", "mình" },
                { "den", "đến" },
                { "tu", "từ" },
                { "tren", "trên" },
                { "duoi", "dưới" },
                { "trong", "trong" },
                { "ngoai", "ngoài" }
            };

            // Tách từng từ và sửa lỗi
            var words = normalized.Split(' ');
            for (int i = 0; i < words.Length; i++)
            {
                var word = words[i];
                if (corrections.ContainsKey(word))
                {
                    words[i] = corrections[word];
                }
            }

            return string.Join(" ", words);
        }

        // Xử lý câu hỏi về bệnh tật
        private string ProcessDiseaseQuery(string message)
        {
            // Log để debug
            System.Diagnostics.Debug.WriteLine($"Processing disease query. Diseases count: {_diseases?.Count ?? 0}");
            System.Diagnostics.Debug.WriteLine($"Message: {message}");

            if (_diseases == null || _diseases.Count == 0)
            {
                System.Diagnostics.Debug.WriteLine("No diseases loaded");
                return null;
            }

            // Kiểm tra các từ khóa liên quan đến bệnh
            var diseaseKeywords = new[] { "bị", "đau", "bệnh", "điều trị", "chữa", "triệu chứng", "cách chữa", "viêm", "sốt", "cảm", "ho", "khỏi", "trị" };
            var hasDiseaseKeyword = diseaseKeywords.Any(k => message.Contains(k));

            System.Diagnostics.Debug.WriteLine($"Has disease keyword: {hasDiseaseKeyword}");

            if (!hasDiseaseKeyword)
                return null;

            // Tìm bệnh phù hợp nhất
            DiseaseInfo bestMatch = null;
            int bestScore = 0;

            foreach (var disease in _diseases)
            {
                var diseaseName = RemoveDiacritics(disease.name.ToLower());
                var normalizedMessage = RemoveDiacritics(message);

                // Khớp chính xác
                if (normalizedMessage.Contains(diseaseName))
                {
                    bestMatch = disease;
                    bestScore = 100;
                    System.Diagnostics.Debug.WriteLine($"Exact match found: {disease.name}");
                    break;
                }

                // Tính độ tương đồng
                var score = CalculateSimilarity(normalizedMessage, diseaseName);
                System.Diagnostics.Debug.WriteLine($"Checking '{disease.name}': score = {score}");

                if (score > bestScore && score >= 40)
                {
                    bestScore = score;
                    bestMatch = disease;
                }
            }

            System.Diagnostics.Debug.WriteLine($"Best match: {bestMatch?.name ?? "None"}, Score: {bestScore}");

            if (bestMatch != null)
            {
                var response = new StringBuilder();
                response.AppendLine($"**💊 Thông tin về {bestMatch.name}**\n");

                // Triệu chứng
                if (!string.IsNullOrEmpty(bestMatch.symptoms))
                {
                    response.AppendLine($"**🔍 Triệu chứng:**");
                    response.AppendLine($"{bestMatch.symptoms}\n");
                }

                // Nguyên nhân
                if (!string.IsNullOrEmpty(bestMatch.causes))
                {
                    response.AppendLine($"**⚠️ Nguyên nhân:**");
                    response.AppendLine($"{bestMatch.causes}\n");
                }

                // Điều trị
                if (!string.IsNullOrEmpty(bestMatch.treatment))
                {
                    response.AppendLine($"**💉 Cách điều trị:**");
                    response.AppendLine($"{bestMatch.treatment}\n");
                }

                // Phòng ngừa
                if (!string.IsNullOrEmpty(bestMatch.prevention))
                {
                    response.AppendLine($"**🛡️ Phòng ngừa:**");
                    response.AppendLine($"{bestMatch.prevention}\n");
                }

                response.AppendLine("\n⚠️ **LƯU Ý QUAN TRỌNG:**");
                response.AppendLine("Đây chỉ là thông tin tham khảo. Nếu triệu chứng nghiêm trọng hoặc không đỡ sau 2-3 ngày, vui lòng đến khám bác sĩ ngay.\n");

                response.AppendLine("[button:Đặt lịch khám ngay|đặt lịch]");
                response.AppendLine("[button:Xem danh sách bác sĩ|danh sách bác sĩ]");

                return response.ToString();
            }

            // Nếu có từ khóa bệnh nhưng không tìm thấy
            if (hasDiseaseKeyword)
            {
                return "Tôi hiểu bạn đang hỏi về vấn đề sức khỏe. Bạn có thể mô tả cụ thể hơn không?\n\n" +
                       "**Ví dụ:**\n" +
                       "• Tôi bị đau đầu\n" +
                       "• Bị cảm lạnh điều trị thế nào?\n" +
                       "• Cách chữa đau dạ dày\n" +
                       "• Triệu chứng viêm họng\n\n" +
                       "[button:Đặt lịch khám với bác sĩ|đặt lịch]";
            }

            return null;
        }

        // Loại bỏ dấu tiếng Việt để so sánh
        private string RemoveDiacritics(string text)
        {
            var normalized = text.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();
            foreach (var c in normalized)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                {
                    sb.Append(c);
                }
            }
            return sb.ToString().Normalize(NormalizationForm.FormC).ToLower();
        }

        // Tính độ tương đồng giữa message và tên bệnh (đơn giản)
        private int CalculateSimilarity(string text, string target)
        {
            // Tách thành các từ
            var textWords = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var targetWords = target.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            int matchCount = 0;
            foreach (var targetWord in targetWords)
            {
                if (textWords.Any(w => w.Contains(targetWord) || targetWord.Contains(w)))
                {
                    matchCount++;
                }
            }

            // Tính phần trăm khớp
            return (int)((matchCount * 100.0) / targetWords.Length);
        }

        private async Task<HospitalInfo> GetHospitalInfo()
        {
            var doctors = await _context.BacSis
                .Include(bs => bs.Khoa)
                .Select(bs => new DoctorInfo
                {
                    Id = bs.MaBacSi,
                    Name = bs.HoTen,
                    Department = bs.Khoa != null ? bs.Khoa.TenKhoa : "Chưa xác định",
                    Description = bs.MoTa ?? "Không có mô tả",
                    Phone = bs.SoDienThoai ?? "Chưa cập nhật",
                    Email = bs.Email ?? "Chưa cập nhật"
                }).ToListAsync();

            var departments = await _context.Khoas
                .Select(k => new DepartmentInfo
                {
                    Id = k.MaKhoa,
                    Name = k.TenKhoa,
                    Description = k.MoTa ?? "Không có mô tả"
                }).ToListAsync();

            return new HospitalInfo
            {
                Doctors = doctors,
                Departments = departments,
                Hospital = new HospitalBasicInfo
                {
                    Name = "Four Rock Hospital",
                    Address = "Khu E Hutech, Quận 9, TP. Hồ Chí Minh",
                    Phone = "(0123) 456-789",
                    Email = "info@fourrock.com",
                    WorkingHours = "24/7 - Hỗ trợ cả ngày lẫn đêm",
                    Emergency = "Cấp cứu 24/7"
                }
            };
        }

        private string ProcessLocalQuery(string message, HospitalInfo info)
        {
            // Quét tên bác sĩ
            foreach (var doctor in info.Doctors)
            {
                if (message.Contains(doctor.Name.ToLower()))
                {
                    return $"**Thông tin Bác sĩ {doctor.Name}**\n\n" +
                           $"* **Chuyên khoa:** {doctor.Department}\n" +
                           $"* **Mô tả:** {doctor.Description}\n" +
                           $"* **Điện thoại:** {doctor.Phone}\n" +
                           $"* **Email:** {doctor.Email}\n\n" +
                           "[button:Đặt lịch với bác sĩ này|đặt lịch]";
                }
            }

            // Quét tên khoa
            foreach (var dept in info.Departments)
            {
                if (message.Contains(dept.Name.ToLower()))
                {
                    var deptDoctors = info.Doctors.Where(d => d.Department == dept.Name).ToList();
                    var response = $"**Thông tin Khoa {dept.Name}**\n* Mô tả: {dept.Description}\n\n";
                    if (deptDoctors.Any())
                    {
                        response += "**Bác sĩ thuộc khoa:**\n";
                        foreach (var doc in deptDoctors)
                            response += $"* {doc.Name}\n";
                    }
                    else
                    {
                        response += "Hiện tại chưa có thông tin bác sĩ thuộc khoa này.\n";
                    }
                    return response;
                }
            }

            // Chào hỏi
            if (message == "xin chào" || message == "hi" || message == "hello" || message == "chào")
            {
                return "Xin chào! Tôi có thể giúp gì cho bạn?\n\n" +
                       "[button:Xem danh sách bác sĩ|danh sách bác sĩ]\n" +
                       "[button:Xem danh sách khoa|danh sách khoa]\n" +
                       "[button:Hỏi về bệnh|tôi bị đau đầu]\n" +
                       "[button:Hướng dẫn đặt lịch|đặt lịch]";
            }

            // Danh sách
            if (message.Contains("danh sách") || message.Contains("có những") || message.Contains("list"))
            {
                if (message.Contains("bác sĩ"))
                {
                    var doctorList = "**Danh sách bác sĩ tại Four Rock Hospital:**\n\n";
                    foreach (var doctor in info.Doctors)
                        doctorList += $"* {doctor.Name} - {doctor.Department}\n";
                    return doctorList;
                }
                if (message.Contains("khoa"))
                {
                    var deptList = "**Các khoa tại Four Rock Hospital:**\n\n";
                    foreach (var dept in info.Departments)
                        deptList += $"* {dept.Name}\n";
                    return deptList;
                }
                return "**Bạn muốn xem danh sách nào?**\n\n" +
                       "[button:Danh sách bác sĩ|danh sách bác sĩ]\n" +
                       "[button:Danh sách khoa|danh sách khoa]\n" +
                       "[button:Xem dịch vụ|dịch vụ]";
            }

            if (message.Contains("bác sĩ") || message.Contains("bác sí") || message.Contains("doctor"))
            {
                return "Bạn muốn hỏi về **bác sĩ** cụ thể nào?\n\n" +
                       "[button:Xem danh sách bác sĩ|danh sách bác sĩ]";
            }

            if (message.Contains("khoa") || message.Contains("chuyên khoa"))
            {
                return "Bạn muốn hỏi về **khoa** cụ thể nào?\n\n" +
                       "[button:Xem danh sách khoa|danh sách khoa]";
            }

            if (message.Contains("địa chỉ") || message.Contains("ở đâu"))
            {
                return $"**Thông tin liên hệ Four Rock Hospital:**\n\n" +
                       $"* **Địa chỉ:** {info.Hospital.Address}\n" +
                       $"* **Hotline:** {info.Hospital.Phone}\n" +
                       $"* **Email:** {info.Hospital.Email}";
            }

            if (message.Contains("thời gian") || message.Contains("giờ") || message.Contains("mở cửa"))
            {
                return $"**Thời gian hoạt động:**\n\n" +
                       $"* {info.Hospital.WorkingHours}\n" +
                       $"* {info.Hospital.Emergency}\n\n" +
                       "[button:Đặt lịch khám|đặt lịch]";
            }

            if (message.Contains("dịch vụ") || message.Contains("service") || message.Contains("khám"))
            {
                return "**Dịch vụ chính tại Four Rock Hospital:**\n" +
                       "* Khám tổng quát\n* Tim mạch\n* Xét nghiệm\n* Cấp cứu 24/7\n* Nội trú\n\n" +
                       "[button:Đặt lịch khám|đặt lịch]";
            }

            if (message.Contains("đặt lịch") || message.Contains("book"))
            {
                return $"**Hướng dẫn đặt lịch khám:**\n\n" +
                       "* **Cách 1:** Đặt lịch trực tuyến qua website.\n" +
                       $"* **Cách 2:** Gọi hotline {info.Hospital.Phone}.\n" +
                       $"* **Cách 3:** Đến trực tiếp tại {info.Hospital.Address}.\n\n" +
                       "[button:Xem danh sách bác sĩ|danh sách bác sĩ]";
            }

            return null;
        }

        private string BuildContext(HospitalInfo info)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Bạn là trợ lý AI của Four Rock Hospital. Thông tin bệnh viện:\n");
            sb.AppendLine($"Tên: {info.Hospital.Name}");
            sb.AppendLine($"Địa chỉ: {info.Hospital.Address}");
            sb.AppendLine($"Hotline: {info.Hospital.Phone}");
            sb.AppendLine($"Email: {info.Hospital.Email}");
            sb.AppendLine($"Thời gian làm việc: {info.Hospital.WorkingHours}\n");
            sb.AppendLine("Danh sách bác sĩ:");
            foreach (var doctor in info.Doctors)
                sb.AppendLine($"- {doctor.Name} (Chuyên khoa: {doctor.Department})");
            sb.AppendLine("\nDịch vụ chính: Khám tổng quát, Tim mạch, Xét nghiệm, Cấp cứu 24/7\n");
            sb.AppendLine("Hãy trả lời câu hỏi sau một cách thân thiện và chuyên nghiệp. Nếu câu hỏi không liên quan đến y tế hoặc bệnh viện, hãy lịch sự chuyển hướng về dịch vụ y tế:\n");
            return sb.ToString();
        }

        private async Task<string> CallGeminiApi(string fullMessage)
        {
            if (string.IsNullOrEmpty(_geminiApiKey) || string.IsNullOrEmpty(_geminiApiUrl))
            {
                return "Xin lỗi, chức năng AI chưa được cấu hình. Vui lòng liên hệ hotline (0123) 456-789.";
            }

            var client = _httpClientFactory.CreateClient();
            var url = _geminiApiUrl + _geminiApiKey;

            var requestBody = new
            {
                contents = new[] { new { parts = new[] { new { text = fullMessage } } } },
                generationConfig = new { temperature = 0.7, maxOutputTokens = 800 }
            };

            var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
            try
            {
                var response = await client.PostAsync(url, content);
                var responseString = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(responseString);

                if (doc.RootElement.TryGetProperty("candidates", out var candidates) &&
                    candidates.GetArrayLength() > 0 &&
                    candidates[0].TryGetProperty("content", out var contentElem) &&
                    contentElem.TryGetProperty("parts", out var parts) &&
                    parts.GetArrayLength() > 0 &&
                    parts[0].TryGetProperty("text", out var textElem))
                {
                    return textElem.GetString();
                }

                if (doc.RootElement.TryGetProperty("error", out var errorElem) &&
                    errorElem.TryGetProperty("message", out var errMsg))
                {
                    System.Diagnostics.Debug.WriteLine($"Gemini API Error: {errMsg.GetString()}");
                    return "Xin lỗi, tôi gặp vấn đề kỹ thuật. Vui lòng liên hệ hotline (0123) 456-789 để được hỗ trợ.";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Gemini Connection Error: {ex.Message}");
                return "Lỗi kết nối. Vui lòng thử lại sau.";
            }
            return "Xin lỗi, tôi không hiểu câu hỏi của bạn. Bạn có thể hỏi về thông tin bác sĩ, dịch vụ, địa chỉ hoặc đặt lịch khám.";
        }

        private string CleanResponse(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return "Xin lỗi, tôi không hiểu câu hỏi của bạn.";
            text = Regex.Replace(text, @"\n\s*\n", "\n\n");
            return text.Trim();
        }

        // Model classes
        private class HospitalInfo { public List<DoctorInfo> Doctors { get; set; } public List<DepartmentInfo> Departments { get; set; } public HospitalBasicInfo Hospital { get; set; } }
        private class DoctorInfo { public int Id { get; set; } public string Name { get; set; } public string Department { get; set; } public string Description { get; set; } public string Phone { get; set; } public string Email { get; set; } }
        private class DepartmentInfo { public int Id { get; set; } public string Name { get; set; } public string Description { get; set; } }
        private class HospitalBasicInfo { public string Name { get; set; } public string Address { get; set; } public string Phone { get; set; } public string Email { get; set; } public string WorkingHours { get; set; } public string Emergency { get; set; } }

        // Disease models
        private class DiseaseData { public List<DiseaseInfo> diseases { get; set; } }
        private class DiseaseInfo
        {
            public string name { get; set; }
            public string symptoms { get; set; }
            public string causes { get; set; }
            public string treatment { get; set; }
            public string prevention { get; set; }
        }
    }
}