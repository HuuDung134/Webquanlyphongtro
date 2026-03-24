# Bảng So Sánh Phân Quyền - Admin vs User

## 📋 Bảng Tổng Hợp

| Chức năng | Admin | User (Người thuê) | Ghi chú |
|-----------|-------|-------------------|---------|
| **QUẢN LÝ TÀI KHOẢN** |
| Xem danh sách tài khoản | ✅ Tất cả | ❌ | Admin only |
| Quản lý tài khoản (CRUD) | ✅ | ❌ | Admin only |
| **QUẢN LÝ NHÀ TRỌ** |
| Xem danh sách nhà trọ | ✅ | ✅ | Cả hai |
| Quản lý nhà trọ (CRUD) | ✅ | ❌ | Admin only |
| **QUẢN LÝ LOẠI PHÒNG** |
| Xem danh sách loại phòng | ✅ | ✅ | Cả hai |
| Quản lý loại phòng (CRUD) | ✅ | ❌ | Admin only |
| **QUẢN LÝ PHÒNG** |
| Xem danh sách phòng | ✅ Tất cả | ✅ Tất cả | Cả hai |
| Quản lý phòng (CRUD) | ✅ | ❌ | Admin only |
| Điều khiển cửa | ✅ Bất kỳ phòng nào | ✅ Chỉ phòng mình | User chỉ điều khiển phòng đang thuê |
| Xem lịch sử đóng/mở cửa | ✅ Tất cả | ✅ Chỉ phòng mình | |
| **QUẢN LÝ NGƯỜI THUÊ** |
| Xem danh sách người thuê | ✅ Tất cả | ❌ | Admin only |
| Quản lý người thuê (CRUD) | ✅ | ❌ | Admin only |
| Xem thông tin cá nhân | ✅ Tất cả | ✅ Chỉ của mình | |
| **QUẢN LÝ HỢP ĐỒNG** |
| Xem danh sách hợp đồng | ✅ Tất cả | ✅ Chỉ của mình | |
| Quản lý hợp đồng (CRUD) | ✅ | ❌ | Admin only |
| Kết thúc hợp đồng | ✅ | ❌ | Admin only |
| **QUẢN LÝ HÓA ĐƠN** |
| Xem danh sách hóa đơn | ✅ Tất cả | ✅ Chỉ của mình | |
| Tạo hóa đơn | ✅ | ❌ | Admin only |
| Cập nhật/Xóa hóa đơn | ✅ | ❌ | Admin only |
| In hóa đơn | ✅ Tất cả | ✅ Chỉ của mình | |
| **QUẢN LÝ THANH TOÁN** |
| Xem danh sách thanh toán | ✅ Tất cả | ✅ Chỉ của mình | |
| Tạo thanh toán | ✅ | ✅ Chỉ hóa đơn của mình | |
| Xác nhận thanh toán | ✅ | ❌ | Admin only |
| Thanh toán MoMo | ✅ | ✅ Chỉ hóa đơn của mình | |
| **QUẢN LÝ CHỈ SỐ ĐIỆN** |
| Xem chỉ số điện | ✅ Tất cả | ✅ Chỉ phòng mình | |
| Upload ảnh + OCR | ✅ Bất kỳ phòng | ✅ Chỉ phòng mình | User tự động lấy phòng từ hợp đồng |
| Thêm/Sửa chỉ số điện | ✅ | ✅ Chỉ phòng mình | |
| **QUẢN LÝ CHỈ SỐ NƯỚC** |
| Xem chỉ số nước | ✅ Tất cả | ✅ Chỉ phòng mình | |
| Upload ảnh + OCR | ✅ Bất kỳ phòng | ✅ Chỉ phòng mình | User tự động lấy phòng từ hợp đồng |
| Thêm/Sửa/Xóa chỉ số nước | ✅ | ✅ Chỉ phòng mình | |
| **QUẢN LÝ DỊCH VỤ** |
| Xem danh sách dịch vụ | ✅ | ✅ | Cả hai |
| Quản lý dịch vụ (CRUD) | ✅ | ❌ | Admin only |
| Cập nhật giá dịch vụ | ✅ | ❌ | Admin only |
| **QUẢN LÝ GIÁ ĐIỆN/NƯỚC** |
| Xem bảng giá | ✅ | ✅ | Cả hai |
| Quản lý bảng giá (CRUD) | ✅ | ❌ | Admin only |
| **QUẢN LÝ THÔNG BÁO** |
| Xem thông báo | ✅ | ✅ | Cả hai |
| Tạo/Sửa/Xóa thông báo | ✅ | ❌ | Admin only |
| **QUẢN LÝ TIN NHẮN** |
| Gửi tin nhắn cho Admin | ❌ | ✅ | User gửi cho Admin |
| Gửi tin nhắn cho khách | ✅ | ❌ | Admin gửi cho User |
| Xem danh sách hội thoại | ✅ | ❌ | Admin only |
| Xem tin nhắn | ✅ Tất cả | ✅ Với Admin | |
| Thu hồi/Sửa tin nhắn | ✅ Tin của mình | ✅ Tin của mình | Chỉ sửa được trong 5 phút |
| **DASHBOARD & BÁO CÁO** |
| Xem thống kê tổng quan | ✅ | ✅ | Cả hai |
| Xem KPI | ✅ | ❌ | Admin only |
| Xem báo cáo chi tiết | ✅ | ❌ | Admin only |

---

## 🔑 Ký hiệu

- ✅ = Có quyền
- ❌ = Không có quyền
- **Tất cả** = Có thể xem/truy cập tất cả dữ liệu
- **Chỉ của mình** = Chỉ xem/truy cập được dữ liệu liên quan đến bản thân
- **Chỉ phòng mình** = Chỉ thao tác được với phòng đang thuê (có hợp đồng hiệu lực)

---

## 📌 Lưu ý quan trọng

1. **User chỉ có thể thao tác với dữ liệu liên quan đến hợp đồng đang hiệu lực của mình**
2. **Khi User upload chỉ số điện/nước mà không chỉ định phòng, hệ thống tự động lấy phòng từ hợp đồng đang hiệu lực**
3. **User chỉ có thể điều khiển cửa phòng mình đang thuê**
4. **Tất cả các endpoint đều yêu cầu đăng nhập (trừ đăng ký/đăng nhập)**
5. **Admin có toàn quyền quản lý hệ thống**


