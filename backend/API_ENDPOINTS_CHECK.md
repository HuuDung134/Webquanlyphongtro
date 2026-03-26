# Kiểm tra API Endpoints - Mobile Controller

## Các endpoint Mobile cần có trong Swagger:

### 1. POST /api/Mobile/billing/generate
- **Mục đích**: Tạo hóa đơn cho cư dân
- **Request Body**: `{}` (rỗng hoặc `MobileBillingGenerateRequest`)
- **Response**: 
  ```json
  {
    "Message": "Tạo hóa đơn thành công",
    "MaHoaDon": int,
    "KyHoaDon": string,
    "NgayLap": datetime,
    "TenPhong": string,
    "TienPhong": decimal,
    "TienDien": decimal,
    "TienNuoc": decimal,
    "TienDichVu": decimal,
    "TongTien": decimal
  }
  ```
- **Flutter sử dụng**: `BillingService.generateBilling()`

### 2. GET /api/Mobile/billing
- **Mục đích**: Lấy danh sách hóa đơn của cư dân
- **Response**: Array of billing objects với:
  - `MaHoaDon`, `MaPhong`, `MaNguoiThue`
  - `TenPhong`, `TenNguoiThue`
  - `NgayLap`, `KyHoaDon`
  - `TienPhong`, `TienDien`, `TienNuoc`, `TienDichVu`
  - `TongTien`
  - `ChiTietDichVu` (array)
  - `TrangThai` (1: Chưa thanh toán, 2: Đã thanh toán)
- **Flutter sử dụng**: `BillingService.getBillingList()`

### 3. GET /api/Mobile/iot/meters
- **Mục đích**: Lấy chỉ số điện và nước từ IoT cho cư dân
- **Response**:
  ```json
  {
    "MaPhong": int,
    "TenPhong": string,
    "ChiSoDien": {
      "MaDien": int,
      "MaPhong": int,
      "TenPhong": string,
      "SoDienCu": int,
      "SoDienMoi": int,
      "SoDienTieuThu": int,
      "TienDien": decimal,
      "HinhAnhDien": string,
      "NgayThangDien": datetime,
      "BacDien": int?
    },
    "ChiSoNuoc": {
      "MaNuoc": int,
      "MaPhong": int,
      "TenPhong": string,
      "SoNuocCu": int,
      "SoNuocMoi": int,
      "SoNuocTieuThu": int,
      "TienNuoc": decimal,
      "HinhAnhNuoc": string,
      "NgayThangNuoc": datetime,
      "BacNuoc": int?
    }
  }
  ```
- **Flutter sử dụng**: `IoTService.getMeters()`

## Các endpoint khác cần kiểm tra:

### Auth
- POST /api/auth/dang-nhap
- POST /api/auth/dang-ky

### TinNhan (Message)
- POST /api/TinNhan/khach-gui-cho-admin
- GET /api/TinNhan/admin/danh-sach-hoi-thoai
- GET /api/TinNhan/admin/khach-hang/{maNguoiThue}
- POST /api/TinNhan/admin-gui-cho-khach

### HoaDon (Billing) - Admin
- GET /api/HoaDon (tất cả hóa đơn)

### ChiSoDien/ChiSoNuoc - Admin
- GET /api/ChiSoDien (tất cả chỉ số điện)
- GET /api/ChiSoNuoc (tất cả chỉ số nước)
- GET /api/ChiSoDien/Phong/{maPhong}
- GET /api/ChiSoNuoc/Phong/{maPhong}

## Lưu ý:
- Tất cả endpoint Mobile đều yêu cầu `[Authorize]` - cần token
- Base URL trong Flutter: `http://192.168.1.245:5253/api`
- Swagger đang chạy tại: `http://localhost:5253/swagger/index.html`

