using Microsoft.AspNetCore.Mvc;
using DoAnCoSo.Models.Dtos;
using DoAnCoSo.Services;

namespace DoAnCoSo.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("dang-ky")]
        public async Task<ActionResult<NguoiDungResponseDto>> DangKy(DangKyDto dangKyDto)
        {
            try
            {
                var ketQua = await _authService.DangKy(dangKyDto);
                return Ok(ketQua);
            }
            catch (Exception ex)
            {
                return BadRequest(new { thongBao = ex.Message });
            }
        }

        [HttpPost("dang-nhap")]
        public async Task<ActionResult<NguoiDungResponseDto>> DangNhap(DangNhapDto dangNhapDto)
        {
            try
            {
                var ketQua = await _authService.DangNhap(dangNhapDto);
                return Ok(ketQua);
            }
            catch (Exception ex)
            {
                return BadRequest(new { thongBao = ex.Message });
            }
        }
    }
} 