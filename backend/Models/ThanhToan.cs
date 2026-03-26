using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace DoAnCoSo.Models
{
    public class ThanhToan
    {
        [Key]
        public int MaThanhToan { get; set; }

        public int MaHoaDon { get; set; }

        [ForeignKey("MaHoaDon")]
        [JsonIgnore]
        public HoaDon? HoaDon { get; set; } 

        public int MaNguoiThue { get; set; }

        [ForeignKey("MaNguoiThue")]
        [JsonIgnore]
        public NguoiThue? NguoiThue { get; set; } 

        [Required]
        public DateTime NgayThanhToan { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TongTien { get; set; }

        [Required]
        [MaxLength(100)]
        public string HinhThucThanhToan { get; set; }

        [MaxLength(255)]
        public string? GhiChu { get; set; }

        [Required]
        public int TrangThai { get; set; }
    }
}
