namespace DoAnCoSo.DTOs
{
    public class ChiSoDienDto
    {
        public int? MaDien { get; set; } // nullable để dùng chung cho Create & Update
        public int MaPhong { get; set; }
        public int MaGiaDien { get; set; }
        public int SoDienCu { get; set; }
        public int SoDienMoi { get; set; }
        public int SoDienTieuThu { get; set; }
        public decimal TienDien { get; set; }
        public string? AnhChiSoDien { get; set; }
        public DateTime NgayThangDien { get; set; }
        public string? TenPhong { get; set; }
    }
}
