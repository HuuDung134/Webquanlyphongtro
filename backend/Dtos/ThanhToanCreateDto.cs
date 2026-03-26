using System.ComponentModel.DataAnnotations;

public class ThanhToanCreateDto
{
    public int MaHoaDon { get; set; }
    public int MaNguoiThue { get; set; }
    public decimal TongTien { get; set; }
    public string HinhThucThanhToan { get; set; }
    public string? GhiChu { get; set; }
    public int TrangThai { get; set; }
}
