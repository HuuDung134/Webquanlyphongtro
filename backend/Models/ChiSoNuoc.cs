using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DoAnCoSo.Models
{
    public class ChiSoNuoc
    {
        [Key]
        public int MaNuoc { get; set; }

        [Required]
        public int MaPhong { get; set; }
        [ForeignKey("MaPhong")]
        public Phong Phong { get; set; }

        [Required]
        public int MaGiaNuoc { get; set; }
        [ForeignKey("MaGiaNuoc")]
        public GiaNuoc GiaNuoc { get; set; }

        [Required]
        public int SoNuocCu { get; set; }

        [Required]
        public int SoNuocMoi { get; set; }

        [Required]
        public int SoNuocTieuThu { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TienNuoc { get; set; }
        public string HinhAnhNuoc { get; set; }


        [Required]
        public DateTime NgayThangNuoc { get; set; }
    }
} 