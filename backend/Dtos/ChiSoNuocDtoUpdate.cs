using System.ComponentModel.DataAnnotations;

namespace DoAnCoSo.DTOs
{
    public class ChiSoNuocDtoUpdate
    {
        [Required]
        public int MaNuoc { get; set; }

        [Required]
        public int MaPhong { get; set; }

        [Required]
        public int SoNuocCu { get; set; }

        [Required]
        public int SoNuocMoi { get; set; }

        [Required]
        public DateTime NgayThangNuoc { get; set; }
    }
}
