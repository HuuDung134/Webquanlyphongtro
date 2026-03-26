namespace DoAnCoSo.DTOs
{
    public class ChiSoNuocDto
    {
        public int? MaNuoc { get; set; } // nullable để dùng chung cho Create & Update
        public int MaPhong { get; set; }
        public int MaGiaNuoc { get; set; }
        public int SoNuocCu { get; set; }
        public int SoNuocMoi { get; set; }
        public int SoNuocTieuThu { get; set; }
        public decimal TienNuoc { get; set; }
        public string? AnhChiSoNuoc { get; set; }
        public DateTime NgayThangNuoc { get; set; }
        public string? TenPhong { get; set; }
    }
}
