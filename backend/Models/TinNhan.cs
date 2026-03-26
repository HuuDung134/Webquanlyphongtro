using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DoAnCoSo.Models
{
    /// <summary>
    /// Model lưu tin nhắn giữa chủ trọ (Admin) và khách hàng (NguoiThue)
    /// </summary>
    public class TinNhan
    {
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// ID của người gửi (MaNguoiDung nếu Admin gửi, MaNguoiThue nếu NguoiThue gửi)
        /// </summary>
        [Required]
        public int MaNguoiGui { get; set; }

        /// <summary>
        /// ID của người nhận (MaNguoiDung nếu Admin nhận, MaNguoiThue nếu NguoiThue nhận)
        /// </summary>
        [Required]
        public int MaNguoiNhan { get; set; }

        /// <summary>
        /// Loại người gửi: "Admin" hoặc "NguoiThue"
        /// </summary>
        [Required]
        [MaxLength(20)]
        public string LoaiNguoiGui { get; set; } = string.Empty; // "Admin" hoặc "NguoiThue"

        /// <summary>
        /// Loại người nhận: "Admin" hoặc "NguoiThue"
        /// </summary>
        [Required]
        [MaxLength(20)]
        public string LoaiNguoiNhan { get; set; } = string.Empty; // "Admin" hoặc "NguoiThue"

        /// <summary>
        /// Nội dung tin nhắn
        /// </summary>
        [Required]
        public string NoiDung { get; set; } = string.Empty;

        /// <summary>
        /// Thời gian gửi tin nhắn
        /// </summary>
        public DateTime ThoiGianGui { get; set; } = DateTime.Now;

        /// <summary>
        /// Đã đọc chưa (null = chưa đọc, có giá trị = thời gian đọc)
        /// </summary>
        public DateTime? DaDocAt { get; set; }

        /// <summary>
        /// Đã thu hồi chưa
        /// </summary>
        public bool DaThuHoi { get; set; } = false;

        /// <summary>
        /// Đã sửa chưa
        /// </summary>
        public bool DaSua { get; set; } = false;

        /// <summary>
        /// Nội dung gốc (trước khi sửa)
        /// </summary>
        public string? NoiDungGoc { get; set; }
    }
}

