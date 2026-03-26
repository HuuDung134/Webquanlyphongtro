using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DoAnCoSo.Models
{
    public class SuCo
    {
        [Key]
        public int MaSuCo { get; set; }

        [Required]
        public int MaNguoiThue { get; set; }
        [ForeignKey("MaNguoiThue")]
        public NguoiThue NguoiThue { get; set; }

        [Required]
        public int MaPhong { get; set; }
        [ForeignKey("MaPhong")]
        public Phong Phong { get; set; }

        [Required]
        [MaxLength(255)]
        public string TieuDe { get; set; } // Ví dụ: "Vòi nước bị rò rỉ", "Bóng đèn hỏng"

        [Required]
        public string MoTa { get; set; } // Mô tả chi tiết sự cố

        [Required]
        public DateTime NgayBaoCao { get; set; } = DateTime.Now;

        [MaxLength(50)]
        public string TrangThai { get; set; } = "Chờ xử lý"; // Chờ xử lý, Đang xử lý, Đã xử lý, Đã hủy

        [MaxLength(500)]
        public string? GhiChu { get; set; } // Ghi chú từ chủ trọ

        public DateTime? NgayXuLy { get; set; } // Ngày chủ trọ xử lý xong

        [MaxLength(255)]
        public string? HinhAnh { get; set; } // Đường dẫn ảnh nếu khách gửi kèm
    }
}

