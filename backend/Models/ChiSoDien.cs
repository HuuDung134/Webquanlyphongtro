using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DoAnCoSo.Models
{
    public class ChiSoDien
    {
        [Key]
        public int MaDien { get; set; }

        [Required]
        public int MaPhong { get; set; }
        [ForeignKey("MaPhong")]
        public Phong Phong { get; set; }

        [Required]
        public int MaGiaDien { get; set; }
        [ForeignKey("MaGiaDien")]
        public GiaDien GiaDien { get; set; }

        [Required]
        public int SoDienCu { get; set; }

        [Required]
        public int SoDienMoi { get; set; }

        [Required]
        public int SoDienTieuThu { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TienDien { get; set; }

        public string HinhAnhDien { get; set; }

        [Required]
        public DateTime NgayThangDien { get; set; }
    }
} 