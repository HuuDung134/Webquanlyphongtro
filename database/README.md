# Database Scripts cho Hệ thống Quản lý Nhà trọ

## Mô tả
Thư mục này chứa các script SQL để tạo và khởi tạo database cho hệ thống quản lý nhà trọ.

## Cấu trúc Database

Database bao gồm các bảng chính:

### Bảng cơ bản
- **NhaTro**: Thông tin nhà trọ
- **LoaiPhong**: Loại phòng (đơn, đôi, ba...)
- **Phong**: Thông tin các phòng
- **Users**: Tài khoản người dùng
- **NguoiThue**: Thông tin người thuê

### Bảng hợp đồng
- **HopDong**: Hợp đồng thuê phòng
- **ChiTietHopDong**: Chi tiết hợp đồng

### Bảng hóa đơn và thanh toán
- **HoaDon**: Hóa đơn hàng tháng
- **ChiTietHoaDon**: Chi tiết các khoản trong hóa đơn
- **ThanhToan**: Lịch sử thanh toán

### Bảng điện nước
- **GiaDien**: Bảng giá điện theo bậc
- **GiaNuoc**: Bảng giá nước theo bậc
- **ChiSoDien**: Chỉ số điện theo tháng
- **ChiSoNuoc**: Chỉ số nước theo tháng

### Bảng dịch vụ
- **DichVu**: Danh sách dịch vụ
- **LichSuGiaDichVu**: Lịch sử thay đổi giá dịch vụ

### Bảng thông báo và tin nhắn
- **ThongBao**: Thông báo hệ thống
- **TinNhan**: Tin nhắn giữa admin và người thuê
- **ChatHistory**: Lịch sử chat từ Telegram Bot

### Bảng khác
- **LichSuDongMo**: Lịch sử đóng/mở khóa phòng
- **SuCo**: Báo cáo sự cố từ người thuê

## Cách sử dụng

### 1. Tạo Database

Chạy script `create_database.sql` để tạo database và tất cả các bảng:

```sql
-- Mở SQL Server Management Studio (SSMS) hoặc Azure Data Studio
-- Kết nối đến SQL Server instance
-- Mở file create_database.sql và chạy
```

Hoặc sử dụng command line:

```bash
sqlcmd -S localhost -i create_database.sql
```

### 2. Chèn dữ liệu mẫu (Tùy chọn)

Nếu muốn có dữ liệu mẫu để test, chạy script `seed_data.sql`:

```sql
-- Mở file seed_data.sql và chạy
```

Hoặc:

```bash
sqlcmd -S localhost -d QuanLyNhaTro -i seed_data.sql
```

## Lưu ý

1. **Mật khẩu**: Mật khẩu trong `seed_data.sql` là mật khẩu thô. Trong ứng dụng thực tế, bạn cần hash mật khẩu trước khi lưu vào database.

2. **Connection String**: Đảm bảo connection string trong `appsettings.json` của backend trỏ đúng đến database:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=localhost;Database=QuanLyNhaTro;Trusted_Connection=True;TrustServerCertificate=True;"
     }
   }
   ```

3. **Backup**: Nên backup database trước khi chạy các script này trên môi trường production.

4. **Migration**: Nếu bạn đang sử dụng Entity Framework Core migrations, có thể không cần chạy script này. Thay vào đó, sử dụng:
   ```bash
   dotnet ef database update
   ```

## Kiểm tra

Sau khi chạy script, kiểm tra:

1. Database `QuanLyNhaTro` đã được tạo
2. Tất cả các bảng đã được tạo
3. Các foreign key constraints đã được thiết lập
4. Các index đã được tạo
5. Dữ liệu mẫu (nếu có) đã được chèn

## Troubleshooting

### Lỗi: Database đã tồn tại
- Script đã kiểm tra và chỉ tạo database nếu chưa tồn tại
- Nếu muốn tạo lại, xóa database cũ trước:
  ```sql
  DROP DATABASE QuanLyNhaTro;
  ```

### Lỗi: Foreign key constraint
- Đảm bảo chạy script theo thứ tự: tạo bảng cha trước, bảng con sau
- Script đã được sắp xếp đúng thứ tự

### Lỗi: Index đã tồn tại
- Script đã kiểm tra và chỉ tạo index nếu chưa tồn tại

## Cấu trúc File

```
database/
├── create_database.sql    # Script tạo database và tất cả bảng
├── seed_data.sql          # Script chèn dữ liệu mẫu
└── README.md              # File hướng dẫn này
```

## Liên hệ

Nếu có vấn đề hoặc câu hỏi, vui lòng liên hệ team phát triển.


