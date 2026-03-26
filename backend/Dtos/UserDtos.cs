using System.ComponentModel.DataAnnotations;

namespace DoAnCoSo.Models.Dtos
{
    public class DangKyDto
    {
        [Required]
        [StringLength(50)]
        public string TenDangNhap { get; set; }

        [Required]
        [StringLength(100)]
        [MinLength(6)]
        public string MatKhau { get; set; }

        public string VaiTro { get; set; } = "NguoiDung"; // Admin hoặc NguoiDung
    }

    public class DangNhapDto
    {
        [Required]
        public string TenDangNhap { get; set; }

        [Required]
        public string MatKhau { get; set; }
    }

    public class NguoiDungResponseDto
    {
        public int MaNguoiDung { get; set; }
        public string TenDangNhap { get; set; }
        public string VaiTro { get; set; }
        public string Token { get; set; }
    }
} 