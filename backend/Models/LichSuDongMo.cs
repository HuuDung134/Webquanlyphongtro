using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace DoAnCoSo.Models
{
    public class LichSuDongMo
    {
        [Key]
        public int Id { get; set; }

        public int MaPhong { get; set; }

        [StringLength(50)]
        public string HanhDong { get; set; } // "Mở khóa", "Đóng khóa"

        public DateTime ThoiGian { get; set; } = DateTime.Now;

        [StringLength(100)]
        public string NguoiThucHien { get; set; } // Ví dụ: "Admin"

        [StringLength(255)]
        public string GhiChu { get; set; }

        [ForeignKey("MaPhong")]
        public virtual Phong Phong { get; set; }
    }
}
