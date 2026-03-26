namespace DoAnCoSo.Services
{
    public interface IHopDongService
    {
        string GetTrangThaiText(DateTime? ngayKetThuc);
        Task<bool> CheckNguoiThueCoHopDong(int maNguoiThue);
        Task<bool> CheckPhongCoHopDong(int maPhong);
    }

}
