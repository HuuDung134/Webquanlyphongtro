-- ============================================
-- Script chèn dữ liệu mẫu cho Database
-- ============================================

USE QuanLyNhaTro;
GO

-- ============================================
-- 1. Chèn dữ liệu NhaTro
-- ============================================
IF NOT EXISTS (SELECT * FROM NhaTro)
BEGIN
    INSERT INTO NhaTro (TenNhaTro, DiaChi, MoTa) VALUES
    ('Nhà trọ ABC', '123 Đường ABC, Quận 1, TP.HCM', 'Nhà trọ sạch sẽ, an ninh'),
    ('Nhà trọ XYZ', '456 Đường XYZ, Quận 2, TP.HCM', 'Nhà trọ gần trung tâm');
END
GO

-- ============================================
-- 2. Chèn dữ liệu LoaiPhong
-- ============================================
IF NOT EXISTS (SELECT * FROM LoaiPhong)
BEGIN
    INSERT INTO LoaiPhong (TenLoaiPhong, MoTa) VALUES
    ('Phòng đơn', 'Phòng dành cho 1 người'),
    ('Phòng đôi', 'Phòng dành cho 2 người'),
    ('Phòng ba', 'Phòng dành cho 3 người');
END
GO

-- ============================================
-- 3. Chèn dữ liệu User (Admin)
-- ============================================
IF NOT EXISTS (SELECT * FROM Users WHERE VaiTro = 'Admin')
BEGIN
    -- Mật khẩu: Admin123 (đã hash - cần hash lại trong ứng dụng thực tế)
    INSERT INTO Users (TenDangNhap, MatKhau, VaiTro, TrangThai, NgayTao) VALUES
    ('admin', 'Admin123', 'Admin', 1, GETDATE());
END
GO

-- ============================================
-- 4. Chèn dữ liệu GiaDien
-- ============================================
IF NOT EXISTS (SELECT * FROM GiaDien)
BEGIN
    INSERT INTO GiaDien (BacDien, GiaTienDien, TuSoDien, DenSoDien) VALUES
    (1, 1806.00, 0, 50),
    (2, 1866.00, 51, 100),
    (3, 2167.00, 101, 200),
    (4, 2729.00, 201, 300),
    (5, 3050.00, 301, 400),
    (6, 3151.00, 401, 999999);
END
GO

-- ============================================
-- 5. Chèn dữ liệu GiaNuoc
-- ============================================
IF NOT EXISTS (SELECT * FROM GiaNuoc)
BEGIN
    INSERT INTO GiaNuoc (BacNuoc, GiaTienNuoc, TuSoNuoc, DenSoNuoc) VALUES
    (1, 5973.00, 0, 10),
    (2, 7052.00, 11, 20),
    (3, 8669.00, 21, 30),
    (4, 15929.00, 31, 999999);
END
GO

-- ============================================
-- 6. Chèn dữ liệu DichVu
-- ============================================
IF NOT EXISTS (SELECT * FROM DichVu)
BEGIN
    INSERT INTO DichVu (TenDichVu, Tiendichvu) VALUES
    ('Internet', 100000),
    ('Giữ xe', 50000),
    ('Dọn dẹp', 200000),
    ('Giặt ủi', 50000);
END
GO

-- ============================================
-- 7. Chèn dữ liệu Phong
-- ============================================
IF NOT EXISTS (SELECT * FROM Phong)
BEGIN
    DECLARE @MaNhaTro1 INT = (SELECT TOP 1 MaNhaTro FROM NhaTro ORDER BY MaNhaTro);
    DECLARE @MaLoaiPhong1 INT = (SELECT TOP 1 MaLoaiPhong FROM LoaiPhong WHERE TenLoaiPhong = 'Phòng đơn');
    DECLARE @MaLoaiPhong2 INT = (SELECT TOP 1 MaLoaiPhong FROM LoaiPhong WHERE TenLoaiPhong = 'Phòng đôi');
    
    INSERT INTO Phong (MaNhaTro, MaLoaiPhong, TenPhong, DienTich, GiaPhong, SucChua, TrangThai, MoTa) VALUES
    (@MaNhaTro1, @MaLoaiPhong1, 'P101', 20, 2000000, 1, 0, 'Phòng đơn, có cửa sổ'),
    (@MaNhaTro1, @MaLoaiPhong1, 'P102', 20, 2000000, 1, 0, 'Phòng đơn, có cửa sổ'),
    (@MaNhaTro1, @MaLoaiPhong2, 'P201', 30, 3000000, 2, 0, 'Phòng đôi, rộng rãi'),
    (@MaNhaTro1, @MaLoaiPhong2, 'P202', 30, 3000000, 2, 0, 'Phòng đôi, rộng rãi');
END
GO

-- ============================================
-- 8. Chèn dữ liệu NguoiThue mẫu
-- ============================================
IF NOT EXISTS (SELECT * FROM NguoiThue WHERE HoTen LIKE '%Mẫu%')
BEGIN
    INSERT INTO NguoiThue (HoTen, CCCD, SDT, NgaySinh, DiaChi, Email, GioiTinh, QuocTich) VALUES
    ('Nguyễn Văn A', '001234567890', '0901234567', '1990-01-01', '123 Đường ABC', 'nguyenvana@email.com', 'Nam', 'Việt Nam'),
    ('Trần Thị B', '001234567891', '0901234568', '1992-05-15', '456 Đường XYZ', 'tranthib@email.com', 'Nữ', 'Việt Nam');
END
GO

PRINT 'Dữ liệu mẫu đã được chèn thành công!';
GO


