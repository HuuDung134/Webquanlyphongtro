using DoAnCoSo.Data;
using DoAnCoSo.DTOs;
using DoAnCoSo.Models;
using Microsoft.EntityFrameworkCore;

public class ChiSoDienService
{
    private readonly ApplicationDbContext _context;

    public ChiSoDienService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ChiSoDien> CreateAsync(ChiSoDienDtoCreate dto)
    {
        // Lấy số điện cũ nhất
        int soDienCu = await _context.ChiSoDien
            .Where(x => x.MaPhong == dto.MaPhong)
            .OrderByDescending(x => x.NgayThangDien)
            .Select(x => x.SoDienMoi)
            .FirstOrDefaultAsync();

        // Tính tiêu thụ
        int soTieuThu = dto.SoDienMoi - soDienCu;
        if (soTieuThu < 0) soTieuThu = 0;

        // Lấy bảng giá điện theo bậc
        var dsGia = await _context.GiaDien
            .OrderBy(x => x.BacDien)
            .ToListAsync();

        decimal tienDien = TinhTienDienTheoBac(soTieuThu, dsGia);

        // Lưu vào DB
        var entity = new ChiSoDien
        {
            MaPhong = dto.MaPhong,
            MaGiaDien = dsGia.First().MaGiaDien, // bạn có thể đổi nếu nhiều bảng giá
            SoDienCu = soDienCu,
            SoDienMoi = dto.SoDienMoi,
            SoDienTieuThu = soTieuThu,
            TienDien = tienDien,
            NgayThangDien = dto.NgayThangDien,
            HinhAnhDien = dto.AnhChiSoDien
        };

        _context.ChiSoDien.Add(entity);
        await _context.SaveChangesAsync();

        return entity;
    }


    private decimal TinhTienDienTheoBac(int soTieuThu, List<GiaDien> dsGia)
    {
        decimal tong = 0;
        int conLai = soTieuThu;

        foreach (var bac in dsGia.OrderBy(x => x.BacDien))
        {
            if (conLai <= 0) break;

            int maxTrongBac = bac.DenSoDien - bac.TuSoDien + 1;
            int soTrongBac = Math.Min(maxTrongBac, conLai);

            tong += soTrongBac * bac.GiaTienDien;

            conLai -= soTrongBac;
        }

        return tong;
    }
}
