# Tích hợp eSMS - Gửi SMS thông báo

## Tổng quan

Hệ thống đã được tích hợp dịch vụ gửi SMS qua eSMS để:
1. **Gửi thông báo nhắc nhở** trước 3 ngày đến ngày đóng tiền
2. **Gửi SMS xác nhận** khi thanh toán thành công

## Cấu hình

### 1. Thêm vào file `.env`:

```env
# ESMS Configuration
ESMS_API_KEY=9B0310F2D4E5F3129D136C596C078E
ESMS_SECRET_KEY=8B571E6B09BFECAFAA3FB02E42A12B
ESMS_BRAND_NAME=QLNHA TRO
ESMS_API_URL=https://rest.esms.vn/MainService.svc/json/SendMultipleMessage_V4_post_json
```

### 2. Cấu hình trong `appsettings.json` (tùy chọn):

```json
{
  "ESms": {
    "ApiKey": "",
    "SecretKey": "",
    "BrandName": "QLNHA TRO",
    "ApiUrl": "https://rest.esms.vn/MainService.svc/json/SendMultipleMessage_V4_post_json"
  }
}
```

## Cách hoạt động

### 1. Nhắc nhở thanh toán (trước 3 ngày)

- **Service**: `BillReminderService`
- **Thời gian**: Chạy mỗi ngày, kiểm tra các hóa đơn chưa thanh toán
- **Điều kiện**: Gửi SMS từ ngày 7 đến ngày 10 hàng tháng (3 ngày trước ngày đóng tiền)
- **Nội dung SMS**: 
  ```
  Nhac nho: Hoa don phong [Tên phòng] ky [Kỳ hóa đơn] sap den han thanh toan vao ngay 10/[Tháng]/[Năm]. Tong tien: [Số tiền] VND. Vui long thanh toan dung han.
  ```

### 2. Xác nhận thanh toán thành công

- **Khi nào**: Sau khi thanh toán thành công (qua MoMo hoặc thanh toán trực tiếp)
- **Nơi gửi**:
  - `MomoPaymentService.PaymentCallbackAsync()` - Khi thanh toán MoMo thành công
  - `ThanhToanController.PostThanhToan()` - Khi thanh toán trực tiếp thành công
- **Nội dung SMS**:
  ```
  Thanh toan thanh cong hoa don [Kỳ hóa đơn] - Phong [Tên phòng]. So tien: [Số tiền] VND. Ngay thanh toan: [Ngày giờ]. Cam on ban!
  ```

## API eSMS

### Endpoint
```
POST https://rest.esms.vn/MainService.svc/json/SendMultipleMessage_V4_post_json
```

### Request Body
```json
{
  "ApiKey": "YOUR_API_KEY",
  "SecretKey": "YOUR_SECRET_KEY",
  "Phone": "0912345678",
  "Content": "Nội dung tin nhắn",
  "Brandname": "QLNHA TRO",
  "SmsType": 2,
  "Sandbox": false
}
```

### Response
```json
{
  "CodeResult": "100",
  "CountRegenerate": 0,
  "SMSID": "123456789"
}
```

- `CodeResult = "100"` hoặc `"200"`: Thành công
- Các mã khác: Thất bại

## Chuẩn hóa số điện thoại

Service tự động chuẩn hóa số điện thoại:
- Loại bỏ khoảng trắng, dấu gạch ngang, dấu ngoặc
- Chuyển đổi từ `+84` hoặc `84` sang `0`
- Kiểm tra định dạng số điện thoại Việt Nam (10 số, bắt đầu bằng 0)

Ví dụ:
- `+84912345678` → `0912345678`
- `84 912 345 678` → `0912345678`
- `0912-345-678` → `0912345678`

## Logging

Tất cả các hoạt động gửi SMS đều được log:
- `[ESmsService]` - Log từ ESmsService
- `[BillReminderService]` - Log khi gửi nhắc nhở
- `[MomoPaymentService]` - Log khi gửi xác nhận thanh toán MoMo
- `[ThanhToanController]` - Log khi gửi xác nhận thanh toán trực tiếp

## Xử lý lỗi

- Nếu gửi SMS thất bại, hệ thống **không chặn** luồng xử lý chính
- Lỗi được log và bỏ qua để đảm bảo thanh toán vẫn được xử lý
- Email và Zalo vẫn được gửi song song với SMS

## Kiểm tra

### Kiểm tra cấu hình
1. Kiểm tra file `.env` có đầy đủ thông tin ESMS
2. Kiểm tra logs khi khởi động ứng dụng:
   ```
   [ENV] Loaded .env from: ...
   ```

### Test gửi SMS
1. Tạo hóa đơn mới
2. Đợi đến ngày 7-10 hàng tháng để nhận SMS nhắc nhở
3. Thanh toán hóa đơn và kiểm tra SMS xác nhận

## Lưu ý

1. **Brandname**: Cần đăng ký Brandname với eSMS trước khi sử dụng
2. **Số dư tài khoản**: Đảm bảo tài khoản eSMS có đủ số dư
3. **Nội dung SMS**: Không được chứa ký tự đặc biệt, emoji
4. **Giới hạn**: Tuân thủ quy định gửi SMS của eSMS và pháp luật

## Troubleshooting

### SMS không được gửi
1. Kiểm tra API Key và Secret Key trong `.env`
2. Kiểm tra số dư tài khoản eSMS
3. Kiểm tra Brandname đã được đăng ký chưa
4. Kiểm tra logs để xem lỗi cụ thể

### Số điện thoại không hợp lệ
- Kiểm tra số điện thoại trong database có đúng định dạng không
- Service sẽ tự động chuẩn hóa, nhưng nếu không hợp lệ sẽ bỏ qua

### SMS bị chặn
- Kiểm tra nội dung SMS có vi phạm quy định không
- Liên hệ eSMS để kiểm tra Brandname và template

