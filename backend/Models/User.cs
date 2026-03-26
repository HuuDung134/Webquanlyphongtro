using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DoAnCoSo.Models
{
    public class User
    {
        [Key]
        public int MaNguoiDung { get; set; }

        [Required]
        [StringLength(50)]
        public string TenDangNhap { get; set; }

        [Required]
        [StringLength(100)]
        public string MatKhau { get; set; }

        public string VaiTro { get; set; } = "NguoiDung"; // Admin hoặc NguoiDung

        public bool TrangThai { get; set; } = true;

        public DateTime NgayTao { get; set; } = DateTime.Now;
        public long? TelegramChatId { get; set; }

        // Quan hệ 1-1 với NguoiThue
        public virtual NguoiThue? NguoiThue { get; set; }
    }
} 