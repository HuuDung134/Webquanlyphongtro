using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DoAnCoSo.Models
{
    /// <summary>
    /// Model lưu lịch sử chat từ Telegram Bot để AI có thể tham khảo context
    /// </summary>
    public class ChatHistory
    {
        [Key]
        public int MaChatHistory { get; set; }

        [Required]
        public long TelegramChatId { get; set; } // ID chat từ Telegram

        [Required]
        public int MaNguoiDung { get; set; }
        [ForeignKey("MaNguoiDung")]
        public User User { get; set; } = null!;

        [Required]
        [MaxLength(2000)]
        public string UserMessage { get; set; } = string.Empty; // Tin nhắn của user

        [MaxLength(5000)]
        public string? BotResponse { get; set; } // Phản hồi của bot

        [MaxLength(100)]
        public string? Intent { get; set; } // Intent được detect (bills, contract_info, etc.)

        [MaxLength(50)]
        public string? VaiTro { get; set; } // Tenant hoặc Admin

        public DateTime ThoiGian { get; set; } = DateTime.Now;

        [MaxLength(50)]
        public string? MessageType { get; set; } // text, photo, document

        [MaxLength(500)]
        public string? ContextData { get; set; } // Dữ liệu context đã sử dụng
    }
}

