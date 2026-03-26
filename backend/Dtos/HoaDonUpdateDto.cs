using System.ComponentModel.DataAnnotations;

namespace DoAnCoSo.Models.Dtos
{
    public class HoaDonUpdateDto : HoaDonCreateDto
    {
        [Required]
        public int MaHoaDon { get; set; }
    }
}