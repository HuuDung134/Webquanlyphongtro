using System.ComponentModel.DataAnnotations;

namespace DoAnCoSo.Models
{
    public class NhaTro
    {
        [Key]
        public int MaNhaTro { get; set; }
        [Required]
        [MaxLength(100)]
        public string TenNhaTro { get; set; }
        [Required]
        [MaxLength(255)]
        public string DiaChi { get; set; }
        [MaxLength(255)]
        public string? MoTa { get; set; }
    }
} 