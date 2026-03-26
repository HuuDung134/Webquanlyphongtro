using System;
using System.ComponentModel.DataAnnotations;

namespace DoAnCoSo.Models
{
    public class ThongBao
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(255)]
        public string Title { get; set; } // Tiêu đề thông báo (VD: Cúp điện, Cúp nước)

        [Required]
        public string Content { get; set; } // Nội dung chi tiết

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? ExpireAt { get; set; } // Ngày hết hiệu lực (nếu có)
    }
} 