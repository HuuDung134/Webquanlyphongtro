using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace DoAnCoSo.Services
{
    public interface IOCRAPIService
    {
        Task<OCRResult> RecognizeMeterReadingFromStreamAsync(Stream imageStream, string fileName);
        Task<OCRResult> RecognizeMeterReadingFromBase64Async(string base64Image, string fileName);
        Task<OCRResult> RecognizeElectricityMeterFromStreamAsync(Stream imageStream, string fileName);
        Task<OCRResult> RecognizeWaterMeterFromStreamAsync(Stream imageStream, string fileName);
        Task<OCRResult> RecognizeMeterAutoDetectFromStreamAsync(Stream imageStream, string fileName);
        Task<ReceiptOCRResult> RecognizeReceiptFromStreamAsync(Stream imageStream, string fileName);
    }


public class OCRAPIService : IOCRAPIService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly string _apiKey;

        public OCRAPIService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _apiKey = _configuration["OCRSettings:ApiKey"] ?? "";
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
        }

        public async Task<OCRResult> RecognizeMeterReadingFromStreamAsync(Stream imageStream, string fileName)
        {
            try
            {
                byte[] imageBytes;
                using (var ms = new MemoryStream())
                {
                    await imageStream.CopyToAsync(ms);
                    imageBytes = ms.ToArray();
                }
                string base64 = Convert.ToBase64String(imageBytes);
                return await RecognizeMeterReadingFromBase64Async(base64, fileName);
            }
            catch (Exception ex)
            {
                return new OCRResult { Success = false, Message = $"Lỗi xử lý ảnh: {ex.Message}" };
            }
        }

        public async Task<ReceiptOCRResult> RecognizeReceiptFromStreamAsync(Stream imageStream, string fileName)
        {
            try
            {
                if (string.IsNullOrEmpty(_apiKey))
                    return new ReceiptOCRResult { Success = false, Message = "Chưa cấu hình OpenAI API key." };

                byte[] imageBytes;
                using (var ms = new MemoryStream())
                {
                    await imageStream.CopyToAsync(ms);
                    imageBytes = ms.ToArray();
                }

                var base64Image = Convert.ToBase64String(imageBytes);

                var prompt = """
Bạn là hệ thống OCR cho BIÊN LAI/ẢNH CHUYỂN KHOẢN ngân hàng.
Hãy trích xuất các trường sau (nếu không thấy thì để null hoặc chuỗi rỗng):
- soTien: số tiền (VNĐ) dạng số (ví dụ 1500000). Không có dấu chấm/phẩy.
- soTaiKhoan: số tài khoản người nhận (nếu có).
- tenNguoiNhan: tên người nhận (nếu có).
- noiDungChuyenKhoan: nội dung chuyển khoản (nếu có).
- ngayGiaoDich: ngày giao dịch dạng ISO yyyy-MM-dd (nếu có).

CHỈ TRẢ VỀ JSON thuần theo đúng schema sau, không thêm chữ nào khác:
{
  "soTien": 0,
  "soTaiKhoan": "",
  "tenNguoiNhan": "",
  "noiDungChuyenKhoan": "",
  "ngayGiaoDich": ""
}
""";

                var requestBody = new
                {
                    model = "gpt-4.1",
                    input = new[]
                    {
                        new
                        {
                            role = "user",
                            content = new object[]
                            {
                                new { type = "input_text", text = prompt },
                                new { type = "input_image", image_url = $"data:image/jpeg;base64,{base64Image}" }
                            }
                        }
                    }
                };

                var json = JsonSerializer.Serialize(requestBody);
                var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/responses")
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(responseContent);

                var rawText = doc.RootElement
                    .GetProperty("output")[0]
                    .GetProperty("content")[0]
                    .GetProperty("text")
                    .GetString() ?? "";

                // Parse JSON from model output (robust: allow extra whitespace/newlines)
                var jsonText = rawText.Trim();
                if (!jsonText.StartsWith("{"))
                {
                    var match = Regex.Match(jsonText, "\\{[\\s\\S]*\\}");
                    if (match.Success) jsonText = match.Value;
                }

                decimal? soTien = null;
                string soTaiKhoan = "";
                string tenNguoiNhan = "";
                string noiDung = "";
                DateTime? ngayGiaoDich = null;

                try
                {
                    using var dataDoc = JsonDocument.Parse(jsonText);
                    var root = dataDoc.RootElement;

                    if (root.TryGetProperty("soTien", out var soTienEl))
                    {
                        if (soTienEl.ValueKind == JsonValueKind.Number && soTienEl.TryGetDecimal(out var d))
                            soTien = d;
                        else if (soTienEl.ValueKind == JsonValueKind.String)
                        {
                            var s = (soTienEl.GetString() ?? "").Trim();
                            s = Regex.Replace(s, "[^0-9.]", "");
                            if (decimal.TryParse(s, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out var parsed))
                                soTien = parsed;
                        }
                    }

                    if (root.TryGetProperty("soTaiKhoan", out var stkEl))
                        soTaiKhoan = (stkEl.GetString() ?? "").Trim();
                    if (root.TryGetProperty("tenNguoiNhan", out var tenEl))
                        tenNguoiNhan = (tenEl.GetString() ?? "").Trim();
                    if (root.TryGetProperty("noiDungChuyenKhoan", out var ndEl))
                        noiDung = (ndEl.GetString() ?? "").Trim();
                    if (root.TryGetProperty("ngayGiaoDich", out var ngayEl))
                    {
                        var s = (ngayEl.GetString() ?? "").Trim();
                        if (DateTime.TryParse(s, out var dt))
                            ngayGiaoDich = dt.Date;
                    }
                }
                catch
                {
                    // Fallback: if JSON parse fails, just return raw text for debugging
                    return new ReceiptOCRResult
                    {
                        Success = false,
                        RawText = rawText,
                        Message = "Không parse được JSON từ OCR output."
                    };
                }

                var success = soTien.HasValue || !string.IsNullOrWhiteSpace(soTaiKhoan) || !string.IsNullOrWhiteSpace(noiDung);

                return new ReceiptOCRResult
                {
                    Success = success,
                    RawText = rawText,
                    SoTien = soTien,
                    SoTaiKhoan = soTaiKhoan,
                    TenNguoiNhan = tenNguoiNhan,
                    NoiDungChuyenKhoan = noiDung,
                    NgayGiaoDich = ngayGiaoDich,
                    Message = success ? "OCR biên lai thành công" : "Không nhận dạng được thông tin biên lai"
                };
            }
            catch (Exception ex)
            {
                return new ReceiptOCRResult { Success = false, Message = $"Lỗi gọi OpenAI: {ex.Message}" };
            }
        }

        public async Task<OCRResult> RecognizeMeterReadingFromBase64Async(string base64Image, string fileName)
        {
            try
            {
                if (string.IsNullOrEmpty(_apiKey))
                    return new OCRResult { Success = false, Message = "Chưa cấu hình OpenAI API key." };

                var requestBody = new
                {
                    model = "gpt-4.1",
                    input = new[]
                    {
                    new
                    {
                        role = "user",
                        content = new object[]
                        {
                            new { type = "input_text", text = "Extract electricity meter reading from this image" },
                            new { type = "input_image", image_url = $"data:image/jpeg;base64,{base64Image}" }
                        }
                    }
                }
                };

                var json = JsonSerializer.Serialize(requestBody);
                var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/responses")
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(responseContent);

                string rawText = doc.RootElement
                    .GetProperty("output")[0]
                    .GetProperty("content")[0]
                    .GetProperty("text")
                    .GetString() ?? "";

                string extractedNumber = ExtractMeterNumber(rawText);

                return new OCRResult
                {
                    Success = !string.IsNullOrEmpty(extractedNumber),
                    RawText = rawText,
                    ExtractedNumber = extractedNumber,
                    Message = string.IsNullOrEmpty(extractedNumber) ? "Không nhận dạng được số" : "OCR thành công"
                };
            }
            catch (Exception ex)
            {
                return new OCRResult { Success = false, Message = $"Lỗi gọi OpenAI: {ex.Message}" };
            }
        }

        public async Task<OCRResult> RecognizeElectricityMeterFromStreamAsync(Stream imageStream, string fileName)
        {
            try
            {
                byte[] imageBytes;
                using (var ms = new MemoryStream())
                {
                    await imageStream.CopyToAsync(ms);
                    imageBytes = ms.ToArray();
                }
                string base64 = Convert.ToBase64String(imageBytes);
                return await RecognizeElectricityMeterFromBase64Async(base64, fileName);
            }
            catch (Exception ex)
            {
                return new OCRResult { Success = false, Message = $"Lỗi xử lý ảnh: {ex.Message}" };
            }
        }

        public async Task<OCRResult> RecognizeWaterMeterFromStreamAsync(Stream imageStream, string fileName)
        {
            try
            {
                byte[] imageBytes;
                using (var ms = new MemoryStream())
                {
                    await imageStream.CopyToAsync(ms);
                    imageBytes = ms.ToArray();
                }
                string base64 = Convert.ToBase64String(imageBytes);
                return await RecognizeWaterMeterFromBase64Async(base64, fileName);
            }
            catch (Exception ex)
            {
                return new OCRResult { Success = false, Message = $"Lỗi xử lý ảnh: {ex.Message}" };
            }
        }

        /// <summary>
        /// Tự động phát hiện loại đồng hồ (điện hoặc nước) và trích xuất số đọc
        /// </summary>
        public async Task<OCRResult> RecognizeMeterAutoDetectFromStreamAsync(Stream imageStream, string fileName)
        {
            try
            {
                byte[] imageBytes;
                using (var ms = new MemoryStream())
                {
                    await imageStream.CopyToAsync(ms);
                    imageBytes = ms.ToArray();
                }
                string base64 = Convert.ToBase64String(imageBytes);
                return await RecognizeMeterAutoDetectFromBase64Async(base64, fileName);
            }
            catch (Exception ex)
            {
                return new OCRResult { Success = false, Message = $"Lỗi xử lý ảnh: {ex.Message}" };
            }
        }

        private async Task<OCRResult> RecognizeElectricityMeterFromBase64Async(string base64Image, string fileName)
        {
            try
            {
                if (string.IsNullOrEmpty(_apiKey))
                    return new OCRResult { Success = false, Message = "Chưa cấu hình OpenAI API key." };

                var requestBody = new
                {
                    model = "gpt-4.1",
                    input = new[]
                    {
                    new
                    {
                        role = "user",
                        content = new object[]
                        {
                            new { type = "input_text", text = "Extract electricity meter reading from this image. Return only the main number (black digits), ignore red digits or numbers after | symbol. Example: 12345 | 6 should return 12345." },
                            new { type = "input_image", image_url = $"data:image/jpeg;base64,{base64Image}" }
                        }
                    }
                }
                };

                var json = JsonSerializer.Serialize(requestBody);
                var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/responses")
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(responseContent);

                string rawText = doc.RootElement
                    .GetProperty("output")[0]
                    .GetProperty("content")[0]
                    .GetProperty("text")
                    .GetString() ?? "";

                string extractedNumber = ExtractElectricityMeterNumber(rawText);

                return new OCRResult
                {
                    Success = !string.IsNullOrEmpty(extractedNumber),
                    RawText = rawText,
                    ExtractedNumber = extractedNumber,
                    Message = string.IsNullOrEmpty(extractedNumber) ? "Không nhận dạng được số điện" : "OCR thành công"
                };
            }
            catch (Exception ex)
            {
                return new OCRResult { Success = false, Message = $"Lỗi gọi OpenAI: {ex.Message}" };
            }
        }

        private async Task<OCRResult> RecognizeWaterMeterFromBase64Async(string base64Image, string fileName)
        {
            try
            {
                if (string.IsNullOrEmpty(_apiKey))
                    return new OCRResult { Success = false, Message = "Chưa cấu hình OpenAI API key." };

                var requestBody = new
                {
                    model = "gpt-4.1",
                    input = new[]
                    {
                    new
                    {
                        role = "user",
                        content = new object[]
                        {
                            new { type = "input_text", text = @"Extract water meter reading from this image. 
- If the meter shows 4 digits (like 0001), return as integer (1)
- If the meter shows 5 digits (like 00001), return as decimal (0.1) - the last digit is decimal
- If the meter shows 6 digits (like 000002), return as decimal (0.2) - the last digit is decimal
Return only the main number (black digits), ignore leading zeros and red digits or numbers after - symbol. 
Example: 00001 should return 0.1, 000002 should return 0.2, 00154 should return 154." },
                            new { type = "input_image", image_url = $"data:image/jpeg;base64,{base64Image}" }
                        }
                    }
                }
                };

                var json = JsonSerializer.Serialize(requestBody);
                var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/responses")
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(responseContent);

                string rawText = doc.RootElement
                    .GetProperty("output")[0]
                    .GetProperty("content")[0]
                    .GetProperty("text")
                    .GetString() ?? "";

                string extractedNumber = ExtractWaterMeterNumber(rawText);

                return new OCRResult
                {
                    Success = !string.IsNullOrEmpty(extractedNumber),
                    RawText = rawText,
                    ExtractedNumber = extractedNumber,
                    Message = string.IsNullOrEmpty(extractedNumber) ? "Không nhận dạng được số nước" : "OCR thành công"
                };
            }
            catch (Exception ex)
            {
                return new OCRResult { Success = false, Message = $"Lỗi gọi OpenAI: {ex.Message}" };
            }
        }

        private string ExtractMeterNumber(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return "";

            var match = Regex.Match(text, @"\d{4,}");
            return match.Success ? match.Value : "";
        }

        /// <summary>
        /// Trích xuất số điện: 
        /// - 4 chữ số: số nguyên (ví dụ: 0002 -> 2)
        /// - 5 chữ số: số thập phân (ví dụ: 00002 -> 0.2)
        /// - 6 chữ số: số thập phân (ví dụ: 000002 -> 0.2)
        /// Bỏ số đỏ sau dấu | hoặc -
        /// </summary>
        private string ExtractElectricityMeterNumber(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return "";

            // Loại bỏ số sau dấu | hoặc dấu phân cách (số đỏ)
            text = Regex.Replace(text, @"\s*\|\s*\d+", "", RegexOptions.IgnoreCase);
            text = Regex.Replace(text, @"\s*-\s*\d+", "", RegexOptions.IgnoreCase);

            // Tìm tất cả số (ưu tiên số có 4-6 chữ số)
            var matches = Regex.Matches(text, @"\d{4,6}");
            if (matches.Count == 0)
            {
                // Nếu không tìm thấy số 4-6 chữ số, thử tìm số ít nhất 3 chữ số
                matches = Regex.Matches(text, @"\d{3,}");
            }
            
            // Nếu vẫn không tìm thấy, thử tìm số có ít nhất 1 chữ số (fallback)
            if (matches.Count == 0)
            {
                matches = Regex.Matches(text, @"\d+");
            }
            
            if (matches.Count == 0) return "";

            // Ưu tiên số có 4-6 chữ số (số chính)
            string bestNumber = "";
            int maxLength = 0;

            foreach (Match match in matches)
            {
                string originalNumber = match.Value;
                int length = originalNumber.Length;

                // Ưu tiên số có 4-6 chữ số
                if (length >= 4 && length <= 6)
                {
                    if (length > maxLength || (length == maxLength && string.Compare(originalNumber, bestNumber) > 0))
                    {
                        maxLength = length;
                        bestNumber = originalNumber;
                    }
                }
            }

            // Nếu không tìm thấy số 4-6 chữ số, lấy số dài nhất
            if (string.IsNullOrEmpty(bestNumber))
            {
                foreach (Match match in matches)
                {
                    if (match.Value.Length > maxLength)
                    {
                        maxLength = match.Value.Length;
                        bestNumber = match.Value;
                    }
                }
            }

            if (string.IsNullOrEmpty(bestNumber))
            {
                // Fallback: lấy số đầu tiên tìm được (nếu có)
                if (matches.Count > 0)
                {
                    bestNumber = matches[0].Value;
                }
                else
                {
                    return "";
                }
            }

            // Xử lý theo độ dài:
            // - 4 chữ số: số nguyên (0002 -> 2)
            // - 5 chữ số: số thập phân (00002 -> 0.2)
            // - 6 chữ số: số thập phân (000002 -> 0.2)
            if (bestNumber.Length == 4)
            {
                // Số nguyên: loại bỏ số 0 đầu
                string cleaned = bestNumber.TrimStart('0');
                return string.IsNullOrEmpty(cleaned) ? "0" : cleaned;
            }
            else if (bestNumber.Length == 5)
            {
                // Số thập phân: 00002 -> 0.2
                // Lấy 4 chữ số đầu làm số nguyên, chữ số cuối làm thập phân
                string soNguyen = bestNumber.Substring(0, 4).TrimStart('0');
                string soThapPhan = bestNumber.Substring(4, 1);
                
                if (string.IsNullOrEmpty(soNguyen)) soNguyen = "0";
                return $"{soNguyen}.{soThapPhan}";
            }
            else if (bestNumber.Length == 6)
            {
                // Số thập phân: 000002 -> 0.2
                // Lấy 4 chữ số đầu làm số nguyên, 2 chữ số cuối làm thập phân
                string soNguyen = bestNumber.Substring(0, 4).TrimStart('0');
                string soThapPhan = bestNumber.Substring(4, 2).TrimStart('0');
                
                if (string.IsNullOrEmpty(soNguyen)) soNguyen = "0";
                if (string.IsNullOrEmpty(soThapPhan)) soThapPhan = "0";
                return $"{soNguyen}.{soThapPhan}";
            }
            else
            {
                // Các trường hợp khác: loại bỏ số 0 đầu
                string cleaned = bestNumber.TrimStart('0');
                return string.IsNullOrEmpty(cleaned) ? "0" : cleaned;
            }
        }

        /// <summary>
        /// Trích xuất số nước: 
        /// - 4 chữ số: số nguyên (ví dụ: 0001 -> 1)
        /// - 5 chữ số: số thập phân (ví dụ: 00001 -> 0.1)
        /// - 6 chữ số: số thập phân (ví dụ: 000002 -> 0.2)
        /// </summary>
        private string ExtractWaterMeterNumber(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return "";

            // Loại bỏ số sau dấu - (số đỏ)
            text = Regex.Replace(text, @"\s*-\s*\d+", "", RegexOptions.IgnoreCase);
            text = Regex.Replace(text, @"\s*\|\s*\d+", "", RegexOptions.IgnoreCase);

            // Tìm tất cả số (ưu tiên số có 4-6 chữ số)
            var matches = Regex.Matches(text, @"\d{4,6}");
            if (matches.Count == 0)
            {
                // Nếu không tìm thấy số 4-6 chữ số, thử tìm số ít nhất 3 chữ số
                matches = Regex.Matches(text, @"\d{3,}");
            }
            
            // Nếu vẫn không tìm thấy, thử tìm số có ít nhất 1 chữ số (fallback)
            if (matches.Count == 0)
            {
                matches = Regex.Matches(text, @"\d+");
            }
            
            if (matches.Count == 0) return "";

            // Ưu tiên số có 4-6 chữ số (số chính)
            string bestNumber = "";
            int maxLength = 0;

            foreach (Match match in matches)
            {
                string originalNumber = match.Value;
                int length = originalNumber.Length;

                // Ưu tiên số có 4-6 chữ số
                if (length >= 4 && length <= 6)
                {
                    if (length > maxLength || (length == maxLength && string.Compare(originalNumber, bestNumber) > 0))
                    {
                        maxLength = length;
                        bestNumber = originalNumber;
                    }
                }
            }

            // Nếu không tìm thấy số 4-6 chữ số, lấy số dài nhất
            if (string.IsNullOrEmpty(bestNumber))
            {
                foreach (Match match in matches)
                {
                    if (match.Value.Length > maxLength)
                    {
                        maxLength = match.Value.Length;
                        bestNumber = match.Value;
                    }
                }
            }

            if (string.IsNullOrEmpty(bestNumber))
            {
                // Fallback: lấy số đầu tiên tìm được (nếu có)
                if (matches.Count > 0)
                {
                    bestNumber = matches[0].Value;
                }
                else
                {
                    return "";
                }
            }

            // Xử lý theo độ dài:
            // - 4 chữ số: số nguyên (0001 -> 1)
            // - 5 chữ số: số thập phân (00001 -> 0.1)
            // - 6 chữ số: số thập phân (000002 -> 0.2)
            if (bestNumber.Length == 4)
            {
                // Số nguyên: loại bỏ số 0 đầu
                string cleaned = bestNumber.TrimStart('0');
                return string.IsNullOrEmpty(cleaned) ? "0" : cleaned;
            }
            else if (bestNumber.Length == 5)
            {
                // Số thập phân: 00001 -> 0.1
                // Lấy 4 chữ số đầu làm số nguyên, chữ số cuối làm thập phân
                string soNguyen = bestNumber.Substring(0, 4).TrimStart('0');
                string soThapPhan = bestNumber.Substring(4, 1);
                
                if (string.IsNullOrEmpty(soNguyen)) soNguyen = "0";
                return $"{soNguyen}.{soThapPhan}";
            }
            else if (bestNumber.Length == 6)
            {
                // Số thập phân: 000002 -> 0.2
                // Lấy 4 chữ số đầu làm số nguyên, 2 chữ số cuối làm thập phân
                string soNguyen = bestNumber.Substring(0, 4).TrimStart('0');
                string soThapPhan = bestNumber.Substring(4, 2).TrimStart('0');
                
                if (string.IsNullOrEmpty(soNguyen)) soNguyen = "0";
                if (string.IsNullOrEmpty(soThapPhan)) soThapPhan = "0";
                return $"{soNguyen}.{soThapPhan}";
            }
            else
            {
                // Các trường hợp khác: loại bỏ số 0 đầu
                string cleaned = bestNumber.TrimStart('0');
                return string.IsNullOrEmpty(cleaned) ? "0" : cleaned;
            }
        }

        /// <summary>
        /// Tự động phát hiện loại đồng hồ và trích xuất số đọc
        /// </summary>
        private async Task<OCRResult> RecognizeMeterAutoDetectFromBase64Async(string base64Image, string fileName)
        {
            try
            {
                if (string.IsNullOrEmpty(_apiKey))
                    return new OCRResult { Success = false, Message = "Chưa cấu hình OpenAI API key." };

                // Prompt để AI tự phát hiện loại đồng hồ và trích xuất số
                var requestBody = new
                {
                    model = "gpt-4.1",
                    input = new[]
                    {
                    new
                    {
                        role = "user",
                        content = new object[]
                        {
                            new { type = "input_text", text = @"Phân tích ảnh này và xác định:
1. Đây là đồng hồ ĐIỆN hay đồng hồ NƯỚC?
2. Trích xuất số đọc chính (màu đen):
   - Nếu là đồng hồ ĐIỆN: lấy số chính, bỏ số đỏ sau dấu | (ví dụ: 12345 | 6 -> 12345)
   - Nếu là đồng hồ NƯỚC: lấy số chính, bỏ số 0 đầu và số đỏ sau dấu - (ví dụ: 00154 - 321 -> 154)
Trả về định dạng JSON: {""type"": ""dien"" hoặc ""nuoc"", ""reading"": ""số đọc""}" },
                            new { type = "input_image", image_url = $"data:image/jpeg;base64,{base64Image}" }
                        }
                    }
                }
                };

                var json = JsonSerializer.Serialize(requestBody);
                var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/responses")
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(responseContent);

                string rawText = doc.RootElement
                    .GetProperty("output")[0]
                    .GetProperty("content")[0]
                    .GetProperty("text")
                    .GetString() ?? "";

                // Parse kết quả từ AI
                string meterType = "";
                string extractedNumber = "";

                // Thử parse JSON từ response
                try
                {
                    using var jsonDoc = JsonDocument.Parse(rawText);
                    var root = jsonDoc.RootElement;
                    if (root.TryGetProperty("type", out var typeElement))
                        meterType = typeElement.GetString() ?? "";
                    if (root.TryGetProperty("reading", out var readingElement))
                        extractedNumber = readingElement.GetString() ?? "";
                }
                catch
                {
                    // Nếu không parse được JSON, thử extract từ text
                    // Tìm pattern "type": "dien" hoặc "nuoc"
                    var typeMatch = Regex.Match(rawText, @"""type""\s*:\s*""(\w+)""", RegexOptions.IgnoreCase);
                    if (typeMatch.Success)
                        meterType = typeMatch.Groups[1].Value.ToLower();

                    var readingMatch = Regex.Match(rawText, @"""reading""\s*:\s*""?(\d+)""?", RegexOptions.IgnoreCase);
                    if (readingMatch.Success)
                        extractedNumber = readingMatch.Groups[1].Value;
                }

                // Nếu không parse được, thử dùng logic cũ để extract
                if (string.IsNullOrEmpty(extractedNumber))
                {
                    // Thử extract như đồng hồ điện trước
                    extractedNumber = ExtractElectricityMeterNumber(rawText);
                    if (string.IsNullOrEmpty(extractedNumber))
                    {
                        // Nếu không được, thử như đồng hồ nước
                        extractedNumber = ExtractWaterMeterNumber(rawText);
                    }
                }

                // Xác định loại đồng hồ nếu chưa có
                if (string.IsNullOrEmpty(meterType))
                {
                    // Dựa vào pattern trong text để đoán
                    if (rawText.Contains("điện", StringComparison.OrdinalIgnoreCase) || 
                        rawText.Contains("electricity", StringComparison.OrdinalIgnoreCase))
                        meterType = "dien";
                    else if (rawText.Contains("nước", StringComparison.OrdinalIgnoreCase) || 
                             rawText.Contains("water", StringComparison.OrdinalIgnoreCase))
                        meterType = "nuoc";
                    else
                        meterType = "unknown";
                }

                return new OCRResult
                {
                    Success = !string.IsNullOrEmpty(extractedNumber),
                    RawText = rawText,
                    ExtractedNumber = extractedNumber,
                    Message = string.IsNullOrEmpty(extractedNumber) 
                        ? "Không nhận dạng được số đọc" 
                        : $"OCR thành công - Loại: {(meterType == "dien" ? "Điện" : meterType == "nuoc" ? "Nước" : "Không xác định")}"
                };
            }
            catch (Exception ex)
            {
                return new OCRResult { Success = false, Message = $"Lỗi gọi OpenAI: {ex.Message}" };
            }
        }
    }

    public class OCRResult
    {
        public bool Success { get; set; }
        public string RawText { get; set; } = "";
        public string ExtractedNumber { get; set; } = "";
        public string Message { get; set; } = "";
        public string MeterType { get; set; } = ""; // "dien", "nuoc", hoặc "unknown"
    }

    public class ReceiptOCRResult
    {
        public bool Success { get; set; }
        public string RawText { get; set; } = "";
        public string Message { get; set; } = "";
        public decimal? SoTien { get; set; }
        public string SoTaiKhoan { get; set; } = "";
        public string TenNguoiNhan { get; set; } = "";
        public string NoiDungChuyenKhoan { get; set; } = "";
        public DateTime? NgayGiaoDich { get; set; }
    }


}
