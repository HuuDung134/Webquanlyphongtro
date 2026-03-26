using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DoAnCoSo.Models
{
    public class ChiTietHopDong
    {
        [Key]
        public int MaChiTietHopDong { get; set; }

        [Required]
        public int MaHopDong { get; set; }
        [ForeignKey("MaHopDong")]
        public HopDong HopDong { get; set; }

        [Required]
        public int MaNguoiThue { get; set; }
        [ForeignKey("MaNguoiThue")]
        public NguoiThue NguoiThue { get; set; }
    }
} 