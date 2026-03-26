using Microsoft.AspNetCore.Mvc;
using DoAnCoSo.Services;

[Route("api/ocr")]
[ApiController]
public class OcrController : ControllerBase
{
    private readonly IOCRAPIService _ocrService;


public OcrController(IOCRAPIService ocrService)
    {
        _ocrService = ocrService;
    }

    [HttpPost("ExtractMeterReading")]
    public async Task<IActionResult> ExtractMeterReading(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("Chưa có file upload.");

        try
        {
            var result = await _ocrService.RecognizeMeterReadingFromStreamAsync(file.OpenReadStream(), file.FileName);

            if (!result.Success)
                return BadRequest(result.Message);

            return Ok(new
            {
                MeterReading = result.ExtractedNumber,
                RawText = result.RawText,
                Message = result.Message
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Lỗi khi gọi OCR service: {ex.Message}");
        }
    }

}
