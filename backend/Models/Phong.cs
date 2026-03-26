using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DoAnCoSo.Models
{
    public class Phong
    {
        [Key]
        public int MaPhong { get; set; }
        
        [Required]
        public int MaNhaTro { get; set; }
        [ForeignKey("MaNhaTro")]
        public NhaTro NhaTro { get; set; }

        [Required]
        public int MaLoaiPhong { get; set; }
        [ForeignKey("MaLoaiPhong")]
        public LoaiPhong LoaiPhong { get; set; }

        [Required]
        [MaxLength(100)]
        public string TenPhong { get; set; }

        public float? DienTich { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal GiaPhong { get; set; }

        [Required]
        public int SucChua { get; set; }

        [Required]
        public int TrangThai { get; set; } // 0: Available, 1: Occupied, 2: Under Maintenance

        [MaxLength(255)]
        public string? MoTa { get; set; }

        public List<string>? HinhAnh { get; set; }

    
     
    }
} 