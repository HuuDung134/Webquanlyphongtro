using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using DoAnCoSo.Models;
using DoAnCoSo.Models.Dtos;
using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore;
using DoAnCoSo.Data;

namespace DoAnCoSo.Services
{
    public interface IAuthService
    {
        Task<NguoiDungResponseDto> DangKy(DangKyDto dangKyDto);
        Task<NguoiDungResponseDto> DangNhap(DangNhapDto dangNhapDto);
        string TaoJwtToken(User nguoiDung);
    }

    public class AuthService : IAuthService
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public AuthService(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        public async Task<NguoiDungResponseDto> DangKy(DangKyDto dangKyDto)
        {
            if (await _context.Users.AnyAsync(u => u.TenDangNhap == dangKyDto.TenDangNhap))
            {
                throw new Exception("Tên đăng nhập đã tồn tại");
            }

            var nguoiDung = new User
            {
                TenDangNhap = dangKyDto.TenDangNhap,
                MatKhau = BCrypt.Net.BCrypt.HashPassword(dangKyDto.MatKhau),
                VaiTro = dangKyDto.VaiTro
            };

            _context.Users.Add(nguoiDung);
            await _context.SaveChangesAsync();

            var token = TaoJwtToken(nguoiDung);

            return new NguoiDungResponseDto
            {
                MaNguoiDung = nguoiDung.MaNguoiDung,
                TenDangNhap = nguoiDung.TenDangNhap,
                VaiTro = nguoiDung.VaiTro,
                Token = token
            };
        }

        public async Task<NguoiDungResponseDto> DangNhap(DangNhapDto dangNhapDto)
        {
            var nguoiDung = await _context.Users.FirstOrDefaultAsync(u => u.TenDangNhap == dangNhapDto.TenDangNhap);

            if (nguoiDung == null || !BCrypt.Net.BCrypt.Verify(dangNhapDto.MatKhau, nguoiDung.MatKhau))
            {
                throw new Exception("Tên đăng nhập hoặc mật khẩu không đúng");
            }

            if (!nguoiDung.TrangThai)
            {
                throw new Exception("Tài khoản đã bị khóa");
            }

            var token = TaoJwtToken(nguoiDung);

            return new NguoiDungResponseDto
            {
                MaNguoiDung = nguoiDung.MaNguoiDung,
                TenDangNhap = nguoiDung.TenDangNhap,
                VaiTro = nguoiDung.VaiTro,
                Token = token
            };
        }

        public string TaoJwtToken(User nguoiDung)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, nguoiDung.MaNguoiDung.ToString()),
                new Claim(ClaimTypes.Name, nguoiDung.TenDangNhap),
                new Claim(ClaimTypes.Role, nguoiDung.VaiTro)
            };

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddDays(1),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
} 