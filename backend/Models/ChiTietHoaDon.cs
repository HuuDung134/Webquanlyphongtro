using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DoAnCoSo.Models
{
    public class ChiTietHoaDon
    {
        [Key]
        public int MaChiTiet { get; set; }

        [Required]
        public int MaHoaDon { get; set; }
        public HoaDon HoaDon { get; set; }

        [Required]
        [MaxLength(50)]
        public string LoaiKhoan { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal SoTien { get; set; }

        // Chỉ giữ lại 1 trường khóa ngoại dịch vụ
        [ForeignKey("DichVu")]
        public int? MaDichVu { get; set; }
        public DichVu DichVu { get; set; }

        public int? SoLuong { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? DonGia { get; set; }
    }
}
