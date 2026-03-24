-- ============================================
-- Script tạo Database cho Hệ thống Quản lý Nhà trọ
-- Dựa trên ApplicationDbContext và các Models
-- ============================================

-- Tạo Database
IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'QuanLyNhaTro')
BEGIN
    CREATE DATABASE QuanLyNhaTro;
END
GO

USE QuanLyNhaTro;
GO

-- ============================================
-- 1. Bảng NhaTro
-- ============================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'NhaTro')
BEGIN
    CREATE TABLE NhaTro (
        MaNhaTro INT IDENTITY(1,1) PRIMARY KEY,
        TenNhaTro NVARCHAR(100) NOT NULL,
        DiaChi NVARCHAR(255) NOT NULL,
        MoTa NVARCHAR(255) NULL
    );
END
GO

-- ============================================
-- 2. Bảng LoaiPhong
-- ============================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'LoaiPhong')
BEGIN
    CREATE TABLE LoaiPhong (
        MaLoaiPhong INT IDENTITY(1,1) PRIMARY KEY,
        TenLoaiPhong NVARCHAR(100) NOT NULL,
        MoTa NVARCHAR(255) NULL
    );
END
GO

-- ============================================
-- 3. Bảng User
-- ============================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Users')
BEGIN
    CREATE TABLE Users (
        MaNguoiDung INT IDENTITY(1,1) PRIMARY KEY,
        TenDangNhap NVARCHAR(50) NOT NULL,
        MatKhau NVARCHAR(100) NOT NULL,
        VaiTro NVARCHAR(50) NOT NULL DEFAULT 'NguoiDung',
        TrangThai BIT NOT NULL DEFAULT 1,
        NgayTao DATETIME NOT NULL DEFAULT GETDATE(),
        TelegramChatId BIGINT NULL
    );
    
    -- Tạo unique index cho TenDangNhap
    CREATE UNIQUE INDEX IX_Users_TenDangNhap ON Users(TenDangNhap);
END
GO

-- ============================================
-- 4. Bảng NguoiThue
-- ============================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'NguoiThue')
BEGIN
    CREATE TABLE NguoiThue (
        MaNguoiThue INT IDENTITY(1,1) PRIMARY KEY,
        HoTen NVARCHAR(100) NOT NULL,
        CCCD NVARCHAR(20) NULL,
        SDT NVARCHAR(15) NULL,
        NgaySinh DATETIME NULL,
        DiaChi NVARCHAR(255) NULL,
        Email NVARCHAR(100) NULL,
        GioiTinh NVARCHAR(10) NULL,
        QuocTich NVARCHAR(50) NULL,
        NoiCongTac NVARCHAR(100) NULL,
        MaNguoiDung INT NULL,
        CONSTRAINT FK_NguoiThue_User FOREIGN KEY (MaNguoiDung) 
            REFERENCES Users(MaNguoiDung) ON DELETE SET NULL
    );
END
GO

-- ============================================
-- 5. Bảng Phong
-- ============================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Phong')
BEGIN
    CREATE TABLE Phong (
        MaPhong INT IDENTITY(1,1) PRIMARY KEY,
        MaNhaTro INT NOT NULL,
        MaLoaiPhong INT NOT NULL,
        TenPhong NVARCHAR(100) NOT NULL,
        DienTich FLOAT NULL,
        GiaPhong DECIMAL(18,2) NOT NULL,
        SucChua INT NOT NULL,
        TrangThai INT NOT NULL, -- 0: Available, 1: Occupied, 2: Under Maintenance
        MoTa NVARCHAR(255) NULL,
        HinhAnh NVARCHAR(MAX) NULL, -- JSON array stored as string
        CONSTRAINT FK_Phong_NhaTro FOREIGN KEY (MaNhaTro) 
            REFERENCES NhaTro(MaNhaTro) ON DELETE NO ACTION,
        CONSTRAINT FK_Phong_LoaiPhong FOREIGN KEY (MaLoaiPhong) 
            REFERENCES LoaiPhong(MaLoaiPhong) ON DELETE NO ACTION
    );
END
GO

-- ============================================
-- 6. Bảng HopDong
-- ============================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'HopDong')
BEGIN
    CREATE TABLE HopDong (
        MaHopDong INT IDENTITY(1,1) PRIMARY KEY,
        MaNguoiThue INT NOT NULL,
        MaPhong INT NOT NULL,
        NgayBatDau DATETIME NOT NULL,
        NgayKetThuc DATETIME NULL,
        TienCoc DECIMAL(18,2) NOT NULL,
        NoiDung NVARCHAR(255) NULL,
        CONSTRAINT FK_HopDong_NguoiThue FOREIGN KEY (MaNguoiThue) 
            REFERENCES NguoiThue(MaNguoiThue) ON DELETE NO ACTION,
        CONSTRAINT FK_HopDong_Phong FOREIGN KEY (MaPhong) 
            REFERENCES Phong(MaPhong) ON DELETE NO ACTION
    );
END
GO

-- ============================================
-- 7. Bảng ChiTietHopDong
-- ============================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ChiTietHopDong')
BEGIN
    CREATE TABLE ChiTietHopDong (
        MaChiTietHopDong INT IDENTITY(1,1) PRIMARY KEY,
        MaHopDong INT NOT NULL,
        MaNguoiThue INT NOT NULL,
        CONSTRAINT FK_ChiTietHopDong_HopDong FOREIGN KEY (MaHopDong) 
            REFERENCES HopDong(MaHopDong) ON DELETE NO ACTION,
        CONSTRAINT FK_ChiTietHopDong_NguoiThue FOREIGN KEY (MaNguoiThue) 
            REFERENCES NguoiThue(MaNguoiThue) ON DELETE NO ACTION
    );
END
GO

-- ============================================
-- 8. Bảng GiaDien
-- ============================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'GiaDien')
BEGIN
    CREATE TABLE GiaDien (
        MaGiaDien INT IDENTITY(1,1) PRIMARY KEY,
        BacDien INT NOT NULL,
        GiaTienDien DECIMAL(18,2) NOT NULL,
        TuSoDien INT NOT NULL,
        DenSoDien INT NOT NULL
    );
    
    -- Tạo unique index cho BacDien
    CREATE UNIQUE INDEX IX_GiaDien_BacDien ON GiaDien(BacDien);
END
GO

-- ============================================
-- 9. Bảng GiaNuoc
-- ============================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'GiaNuoc')
BEGIN
    CREATE TABLE GiaNuoc (
        MaGiaNuoc INT IDENTITY(1,1) PRIMARY KEY,
        BacNuoc INT NOT NULL,
        GiaTienNuoc DECIMAL(18,2) NOT NULL,
        TuSoNuoc INT NOT NULL,
        DenSoNuoc INT NOT NULL
    );
    
    -- Tạo unique index cho BacNuoc
    CREATE UNIQUE INDEX IX_GiaNuoc_BacNuoc ON GiaNuoc(BacNuoc);
END
GO

-- ============================================
-- 10. Bảng ChiSoDien
-- ============================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ChiSoDien')
BEGIN
    CREATE TABLE ChiSoDien (
        MaDien INT IDENTITY(1,1) PRIMARY KEY,
        MaPhong INT NOT NULL,
        MaGiaDien INT NOT NULL,
        SoDienCu INT NOT NULL,
        SoDienMoi INT NOT NULL,
        SoDienTieuThu INT NOT NULL,
        TienDien DECIMAL(18,2) NOT NULL,
        HinhAnhDien NVARCHAR(MAX) NULL,
        NgayThangDien DATETIME NOT NULL,
        CONSTRAINT FK_ChiSoDien_Phong FOREIGN KEY (MaPhong) 
            REFERENCES Phong(MaPhong) ON DELETE NO ACTION,
        CONSTRAINT FK_ChiSoDien_GiaDien FOREIGN KEY (MaGiaDien) 
            REFERENCES GiaDien(MaGiaDien) ON DELETE NO ACTION
    );
END
GO

-- ============================================
-- 11. Bảng ChiSoNuoc
-- ============================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ChiSoNuoc')
BEGIN
    CREATE TABLE ChiSoNuoc (
        MaNuoc INT IDENTITY(1,1) PRIMARY KEY,
        MaPhong INT NOT NULL,
        MaGiaNuoc INT NOT NULL,
        SoNuocCu INT NOT NULL,
        SoNuocMoi INT NOT NULL,
        SoNuocTieuThu INT NOT NULL,
        TienNuoc DECIMAL(18,2) NOT NULL,
        HinhAnhNuoc NVARCHAR(MAX) NULL,
        NgayThangNuoc DATETIME NOT NULL,
        CONSTRAINT FK_ChiSoNuoc_Phong FOREIGN KEY (MaPhong) 
            REFERENCES Phong(MaPhong) ON DELETE NO ACTION,
        CONSTRAINT FK_ChiSoNuoc_GiaNuoc FOREIGN KEY (MaGiaNuoc) 
            REFERENCES GiaNuoc(MaGiaNuoc) ON DELETE NO ACTION
    );
END
GO

-- ============================================
-- 12. Bảng DichVu
-- ============================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'DichVu')
BEGIN
    CREATE TABLE DichVu (
        MaDichVu INT IDENTITY(1,1) PRIMARY KEY,
        TenDichVu NVARCHAR(100) NOT NULL,
        Tiendichvu FLOAT NOT NULL
    );
END
GO

-- ============================================
-- 13. Bảng LichSuGiaDichVu
-- ============================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'LichSuGiaDichVu')
BEGIN
    CREATE TABLE LichSuGiaDichVu (
        MaLichSu INT IDENTITY(1,1) PRIMARY KEY,
        MaDichVu INT NOT NULL,
        GiaDichVu DECIMAL(18,2) NOT NULL,
        NgayHieuLuc DATETIME NOT NULL,
        CONSTRAINT FK_LichSuGiaDichVu_DichVu FOREIGN KEY (MaDichVu) 
            REFERENCES DichVu(MaDichVu) ON DELETE NO ACTION
    );
END
GO

-- ============================================
-- 14. Bảng HoaDon
-- ============================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'HoaDon')
BEGIN
    CREATE TABLE HoaDon (
        MaHoaDon INT IDENTITY(1,1) PRIMARY KEY,
        MaNguoiThue INT NOT NULL,
        MaPhong INT NOT NULL,
        MaDien INT NOT NULL,
        MaNuoc INT NOT NULL,
        TienDichVu DECIMAL(18,2) NOT NULL DEFAULT 0,
        TongTien DECIMAL(18,2) NOT NULL,
        NgayLap DATETIME NOT NULL,
        KyHoaDon NVARCHAR(7) NOT NULL,
        CONSTRAINT FK_HoaDon_NguoiThue FOREIGN KEY (MaNguoiThue) 
            REFERENCES NguoiThue(MaNguoiThue) ON DELETE NO ACTION,
        CONSTRAINT FK_HoaDon_Phong FOREIGN KEY (MaPhong) 
            REFERENCES Phong(MaPhong) ON DELETE NO ACTION,
        CONSTRAINT FK_HoaDon_ChiSoDien FOREIGN KEY (MaDien) 
            REFERENCES ChiSoDien(MaDien) ON DELETE NO ACTION,
        CONSTRAINT FK_HoaDon_ChiSoNuoc FOREIGN KEY (MaNuoc) 
            REFERENCES ChiSoNuoc(MaNuoc) ON DELETE NO ACTION
    );
END
GO

-- ============================================
-- 15. Bảng ChiTietHoaDon
-- ============================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ChiTietHoaDon')
BEGIN
    CREATE TABLE ChiTietHoaDon (
        MaChiTiet INT IDENTITY(1,1) PRIMARY KEY,
        MaHoaDon INT NOT NULL,
        LoaiKhoan NVARCHAR(50) NOT NULL,
        SoTien DECIMAL(18,2) NOT NULL,
        MaDichVu INT NULL,
        SoLuong INT NULL,
        DonGia DECIMAL(18,2) NULL,
        CONSTRAINT FK_ChiTietHoaDon_HoaDon FOREIGN KEY (MaHoaDon) 
            REFERENCES HoaDon(MaHoaDon) ON DELETE CASCADE,
        CONSTRAINT FK_ChiTietHoaDon_DichVu FOREIGN KEY (MaDichVu) 
            REFERENCES DichVu(MaDichVu) ON DELETE NO ACTION
    );
END
GO

-- ============================================
-- 16. Bảng ThanhToan
-- ============================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ThanhToan')
BEGIN
    CREATE TABLE ThanhToan (
        MaThanhToan INT IDENTITY(1,1) PRIMARY KEY,
        MaHoaDon INT NOT NULL,
        MaNguoiThue INT NOT NULL,
        NgayThanhToan DATETIME NOT NULL,
        TongTien DECIMAL(18,2) NOT NULL,
        HinhThucThanhToan NVARCHAR(100) NOT NULL,
        GhiChu NVARCHAR(255) NULL,
        TrangThai INT NOT NULL,
        CONSTRAINT FK_ThanhToan_HoaDon FOREIGN KEY (MaHoaDon) 
            REFERENCES HoaDon(MaHoaDon) ON DELETE NO ACTION,
        CONSTRAINT FK_ThanhToan_NguoiThue FOREIGN KEY (MaNguoiThue) 
            REFERENCES NguoiThue(MaNguoiThue) ON DELETE NO ACTION
    );
END
GO

-- ============================================
-- 17. Bảng ThongBao
-- ============================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ThongBao')
BEGIN
    CREATE TABLE ThongBao (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Title NVARCHAR(255) NOT NULL,
        Content NVARCHAR(MAX) NOT NULL,
        CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),
        ExpireAt DATETIME NULL
    );
END
GO

-- ============================================
-- 18. Bảng TinNhan
-- ============================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'TinNhan')
BEGIN
    CREATE TABLE TinNhan (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        MaNguoiGui INT NOT NULL,
        MaNguoiNhan INT NOT NULL,
        LoaiNguoiGui NVARCHAR(20) NOT NULL,
        LoaiNguoiNhan NVARCHAR(20) NOT NULL,
        NoiDung NVARCHAR(MAX) NOT NULL,
        ThoiGianGui DATETIME NOT NULL DEFAULT GETDATE(),
        DaDocAt DATETIME NULL,
        DaThuHoi BIT NOT NULL DEFAULT 0,
        DaSua BIT NOT NULL DEFAULT 0,
        NoiDungGoc NVARCHAR(MAX) NULL
    );
END
GO

-- ============================================
-- 19. Bảng LichSuDongMo
-- ============================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'LichSuDongMo')
BEGIN
    CREATE TABLE LichSuDongMo (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        MaPhong INT NOT NULL,
        HanhDong NVARCHAR(50) NOT NULL,
        ThoiGian DATETIME NOT NULL DEFAULT GETDATE(),
        NguoiThucHien NVARCHAR(100) NOT NULL,
        GhiChu NVARCHAR(255) NULL,
        CONSTRAINT FK_LichSuDongMo_Phong FOREIGN KEY (MaPhong) 
            REFERENCES Phong(MaPhong) ON DELETE NO ACTION
    );
END
GO

-- ============================================
-- 20. Bảng SuCo
-- ============================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'SuCo')
BEGIN
    CREATE TABLE SuCo (
        MaSuCo INT IDENTITY(1,1) PRIMARY KEY,
        MaNguoiThue INT NOT NULL,
        MaPhong INT NOT NULL,
        TieuDe NVARCHAR(255) NOT NULL,
        MoTa NVARCHAR(MAX) NOT NULL,
        NgayBaoCao DATETIME NOT NULL DEFAULT GETDATE(),
        TrangThai NVARCHAR(50) NOT NULL DEFAULT 'Chờ xử lý',
        GhiChu NVARCHAR(500) NULL,
        NgayXuLy DATETIME NULL,
        HinhAnh NVARCHAR(255) NULL,
        CONSTRAINT FK_SuCo_NguoiThue FOREIGN KEY (MaNguoiThue) 
            REFERENCES NguoiThue(MaNguoiThue) ON DELETE NO ACTION,
        CONSTRAINT FK_SuCo_Phong FOREIGN KEY (MaPhong) 
            REFERENCES Phong(MaPhong) ON DELETE NO ACTION
    );
END
GO

-- ============================================
-- 21. Bảng ChatHistory
-- ============================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ChatHistory')
BEGIN
    CREATE TABLE ChatHistory (
        MaChatHistory INT IDENTITY(1,1) PRIMARY KEY,
        TelegramChatId BIGINT NOT NULL,
        MaNguoiDung INT NOT NULL,
        UserMessage NVARCHAR(2000) NOT NULL,
        BotResponse NVARCHAR(5000) NULL,
        Intent NVARCHAR(100) NULL,
        VaiTro NVARCHAR(50) NULL,
        ThoiGian DATETIME NOT NULL DEFAULT GETDATE(),
        MessageType NVARCHAR(50) NULL,
        ContextData NVARCHAR(500) NULL,
        CONSTRAINT FK_ChatHistory_User FOREIGN KEY (MaNguoiDung) 
            REFERENCES Users(MaNguoiDung) ON DELETE NO ACTION
    );
END
GO

-- ============================================
-- Tạo các Index để tối ưu hiệu suất
-- ============================================

-- Index cho HopDong
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_HopDong_MaNguoiThue')
    CREATE INDEX IX_HopDong_MaNguoiThue ON HopDong(MaNguoiThue);
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_HopDong_MaPhong')
    CREATE INDEX IX_HopDong_MaPhong ON HopDong(MaPhong);
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_HopDong_NgayBatDau')
    CREATE INDEX IX_HopDong_NgayBatDau ON HopDong(NgayBatDau);
GO

-- Index cho HoaDon
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_HoaDon_MaNguoiThue')
    CREATE INDEX IX_HoaDon_MaNguoiThue ON HoaDon(MaNguoiThue);
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_HoaDon_MaPhong')
    CREATE INDEX IX_HoaDon_MaPhong ON HoaDon(MaPhong);
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_HoaDon_KyHoaDon')
    CREATE INDEX IX_HoaDon_KyHoaDon ON HoaDon(KyHoaDon);
GO

-- Index cho ThanhToan
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ThanhToan_MaHoaDon')
    CREATE INDEX IX_ThanhToan_MaHoaDon ON ThanhToan(MaHoaDon);
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ThanhToan_TrangThai')
    CREATE INDEX IX_ThanhToan_TrangThai ON ThanhToan(TrangThai);
GO

-- Index cho ChiSoDien
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ChiSoDien_MaPhong')
    CREATE INDEX IX_ChiSoDien_MaPhong ON ChiSoDien(MaPhong);
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ChiSoDien_NgayThangDien')
    CREATE INDEX IX_ChiSoDien_NgayThangDien ON ChiSoDien(NgayThangDien);
GO

-- Index cho ChiSoNuoc
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ChiSoNuoc_MaPhong')
    CREATE INDEX IX_ChiSoNuoc_MaPhong ON ChiSoNuoc(MaPhong);
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ChiSoNuoc_NgayThangNuoc')
    CREATE INDEX IX_ChiSoNuoc_NgayThangNuoc ON ChiSoNuoc(NgayThangNuoc);
GO

-- Index cho TinNhan
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_TinNhan_MaNguoiGui')
    CREATE INDEX IX_TinNhan_MaNguoiGui ON TinNhan(MaNguoiGui);
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_TinNhan_MaNguoiNhan')
    CREATE INDEX IX_TinNhan_MaNguoiNhan ON TinNhan(MaNguoiNhan);
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_TinNhan_ThoiGianGui')
    CREATE INDEX IX_TinNhan_ThoiGianGui ON TinNhan(ThoiGianGui);
GO

-- Index cho LichSuDongMo
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_LichSuDongMo_MaPhong')
    CREATE INDEX IX_LichSuDongMo_MaPhong ON LichSuDongMo(MaPhong);
GO

-- Index cho SuCo
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_SuCo_MaNguoiThue')
    CREATE INDEX IX_SuCo_MaNguoiThue ON SuCo(MaNguoiThue);
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_SuCo_TrangThai')
    CREATE INDEX IX_SuCo_TrangThai ON SuCo(TrangThai);
GO

PRINT 'Database QuanLyNhaTro đã được tạo thành công!';
PRINT 'Tất cả các bảng, ràng buộc và index đã được thiết lập.';
GO


