// Kiểm tra xem Config đã tồn tại chưa để tránh lỗi khi load nhiều lần
if (typeof Config === 'undefined') {
    var Config = {
        API_URL: 'http://localhost:5253',
        CLOUDINARY: {
            CLOUD_NAME: 'dsdrcv24h',
            UPLOAD_PRESET: 'ml_default',
            FOLDER: 'phong_images'
        },
        ENDPOINTS: {
            PHONG: '/api/Phong',
            LOAI_PHONG: '/api/LoaiPhong',
            TRANG_THAI: '/api/TrangThai',
            NHA_TRO: '/api/NhaTro',
            HOP_DONG: '/api/HopDong',
            HOA_DON: '/api/HoaDon',
            NGUOI_THUE: '/api/NguoiThue',
            DICH_VU: '/api/DichVu',
            CHI_SO_DIEN: '/api/ChiSoDien',
            CHI_SO_NUOC: '/api/ChiSoNuoc',
            THANH_TOAN: '/api/ThanhToan',
            TAI_KHOAN: '/api/User',
            AUTH: '/api/Auth',
            GIA_DIEN: '/api/GiaDien',
            GIA_NUOC: '/api/GiaNuoc',
            TIN_NHAN: '/api/TinNhan'
        }
    };
}

// Export Config để các file khác có thể sử dụng
window.Config = Config; 