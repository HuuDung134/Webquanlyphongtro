# Phân Quyền Chức Năng - Hệ thống Quản lý Nhà trọ

## Tổng quan
Tài liệu này mô tả chi tiết các chức năng dành cho **Admin** và **User (Người thuê)** trong hệ thống.

---

## 🔐 CHỨC NĂNG DÀNH CHO ADMIN

### 1. Quản lý Tài khoản (UserController)
- ✅ **Xem danh sách tất cả tài khoản** - `GET /api/User`
- ✅ **Xem chi tiết tài khoản** - `GET /api/User/{id}`
- ✅ **Cập nhật thông tin tài khoản** - `PUT /api/User/{id}`
- ✅ **Xóa tài khoản** - `DELETE /api/User/{id}` (không thể xóa tài khoản Admin)

### 2. Quản lý Nhà trọ (NhaTroController)
- ✅ **Xem danh sách nhà trọ** - `GET /api/NhaTro`
- ✅ **Xem chi tiết nhà trọ** - `GET /api/NhaTro/{id}`
- ✅ **Thêm nhà trọ mới** - `POST /api/NhaTro`
- ✅ **Cập nhật thông tin nhà trọ** - `PUT /api/NhaTro/{id}`
- ✅ **Xóa nhà trọ** - `DELETE /api/NhaTro/{id}`

### 3. Quản lý Loại Phòng (LoaiPhongController) - **CHỈ ADMIN**
- ✅ **Xem danh sách loại phòng** - `GET /api/LoaiPhong`
- ✅ **Xem chi tiết loại phòng** - `GET /api/LoaiPhong/{id}`
- ✅ **Thêm loại phòng mới** - `POST /api/LoaiPhong`
- ✅ **Cập nhật loại phòng** - `PUT /api/LoaiPhong/{id}`
- ✅ **Xóa loại phòng** - `DELETE /api/LoaiPhong/{id}`

### 4. Quản lý Phòng (PhongController)
- ✅ **Xem danh sách tất cả phòng** - `GET /api/Phong`
- ✅ **Xem chi tiết phòng** - `GET /api/Phong/{id}`
- ✅ **Xem phòng theo nhà trọ** - `GET /api/Phong/NhaTro/{nhaTroId}`
- ✅ **Xem phòng theo trạng thái** - `GET /api/Phong/TrangThai/{trangThaiId}`
- ✅ **Xem phòng trống** - `GET /api/Phong/Trong`
- ✅ **Thêm phòng mới** - `POST /api/Phong`
- ✅ **Upload ảnh phòng** - `POST /api/Phong/UploadImage`
- ✅ **Cập nhật thông tin phòng** - `PUT /api/Phong/{id}`
- ✅ **Xóa phòng** - `DELETE /api/Phong/{id}`
- ✅ **Mở cửa từ xa** - `POST /api/Phong/mo-cua-tu-xa/{id}`
- ✅ **Đóng cửa từ xa** - `POST /api/Phong/dong-cua-tu-xa/{id}`
- ✅ **Xem lịch sử đóng/mở cửa** - `GET /api/Phong/{id}/lich-su-dong-mo`

### 5. Quản lý Người thuê (NguoiThueController)
- ✅ **Xem danh sách người thuê** - `GET /api/NguoiThue`
- ✅ **Tìm kiếm người thuê** - `GET /api/NguoiThue/Search?keyword=...`
- ✅ **Xem chi tiết người thuê** - `GET /api/NguoiThue/{id}`
- ✅ **Thêm người thuê mới** - `POST /api/NguoiThue`
- ✅ **Cập nhật thông tin người thuê** - `PUT /api/NguoiThue/{id}`
- ✅ **Xóa người thuê** - `DELETE /api/NguoiThue/{id}`

### 6. Quản lý Hợp đồng (HopDongController)
- ✅ **Xem danh sách tất cả hợp đồng** - `GET /api/HopDong`
- ✅ **Xem phòng chưa có hợp đồng** - `GET /api/HopDong/Phong/KhongCoHopDong`
- ✅ **Xem người thuê chưa có hợp đồng** - `GET /api/HopDong/NguoiThue/KhongCoHopDong`
- ✅ **Xem chi tiết hợp đồng** - `GET /api/HopDong/{id}`
- ✅ **Tạo hợp đồng mới** - `POST /api/HopDong`
- ✅ **Cập nhật hợp đồng** - `PUT /api/HopDong/{id}`
- ✅ **Xóa hợp đồng** - `DELETE /api/HopDong/{id}`
- ✅ **Kết thúc hợp đồng** - `PUT /api/HopDong/KetThuc/{id}`

### 7. Quản lý Hóa đơn (HoaDonController)
- ✅ **Xem danh sách tất cả hóa đơn** - `GET /api/HoaDon`
- ✅ **Xem hóa đơn theo người thuê** - `GET /api/HoaDon?maNguoiThue={id}`
- ✅ **Lấy thông tin phòng để tạo hóa đơn** - `GET /api/HoaDon/GetThongTinPhong/{phongId}`
- ✅ **Xem phòng chưa có hóa đơn trong tháng** - `GET /api/HoaDon/GetPhongChuaCoHoaDonTrongThang?thang={thang}&nam={nam}`
- ✅ **Tạo hóa đơn mới** - `POST /api/HoaDon`
- ✅ **Cập nhật hóa đơn** - `PUT /api/HoaDon/{id}`
- ✅ **Xóa hóa đơn** - `DELETE /api/HoaDon/{id}`
- ✅ **Kiểm tra hóa đơn đã tồn tại** - `GET /api/HoaDon/CheckHoaDon/{maPhong}/{kyHoaDon}`
- ✅ **In hóa đơn (HTML)** - `GET /api/HoaDon/Print/{id}`
- ✅ **In hóa đơn (PDF)** - `GET /api/HoaDon/PrintPdf/{id}`

### 8. Quản lý Thanh toán (ThanhToanController)
- ✅ **Xem danh sách tất cả thanh toán** - `GET /api/ThanhToan`
- ✅ **Xem chi tiết thanh toán** - `GET /api/ThanhToan/{id}`
- ✅ **Xem thanh toán theo hóa đơn** - `GET /api/ThanhToan/HoaDon/{hoaDonId}`
- ✅ **Tạo thanh toán mới** - `POST /api/ThanhToan`
- ✅ **Cập nhật thanh toán** - `PUT /api/ThanhToan/{id}`
- ✅ **Xác nhận thanh toán** - `POST /api/ThanhToan/{id}/xac-nhan`
- ✅ **Xóa thanh toán** - `DELETE /api/ThanhToan/{id}`
- ✅ **Tạo thanh toán MoMo** - `POST /api/ThanhToan/create`

### 9. Quản lý Chỉ số Điện (ChiSoDienController)
- ✅ **Xem danh sách tất cả chỉ số điện** - `GET /api/ChiSoDien`
- ✅ **Xem chi tiết chỉ số điện** - `GET /api/ChiSoDien/{id}`
- ✅ **Upload ảnh và OCR chỉ số điện** - `POST /api/ChiSoDien/upload?maPhong={id}`
- ✅ **Thêm chỉ số điện thủ công** - `POST /api/ChiSoDien`
- ✅ **Cập nhật chỉ số điện** - `PUT /api/ChiSoDien/{id}`

### 10. Quản lý Chỉ số Nước (ChiSoNuocController)
- ✅ **Xem danh sách tất cả chỉ số nước** - `GET /api/ChiSoNuoc`
- ✅ **Xem chi tiết chỉ số nước** - `GET /api/ChiSoNuoc/{id}`
- ✅ **Upload ảnh và OCR chỉ số nước** - `POST /api/ChiSoNuoc/upload?maPhong={id}`
- ✅ **Thêm chỉ số nước thủ công** - `POST /api/ChiSoNuoc`
- ✅ **Cập nhật chỉ số nước** - `PUT /api/ChiSoNuoc/{id}`
- ✅ **Xóa chỉ số nước** - `DELETE /api/ChiSoNuoc/{id}`

### 11. Quản lý Dịch vụ (DichVuController) - **CHỈ ADMIN**
- ✅ **Xem danh sách dịch vụ** - `GET /api/DichVu`
- ✅ **Xem chi tiết dịch vụ** - `GET /api/DichVu/{id}`
- ✅ **Xem tổng tiền dịch vụ theo phòng** - `GET /api/DichVu/TongTienDichVuTheoPhong`
- ✅ **Xem giá hiện tại của dịch vụ** - `GET /api/DichVu/{id}/GiaHienTai`
- ✅ **Xem lịch sử giá dịch vụ** - `GET /api/DichVu/{id}/LichSuGia`
- ✅ **Thêm dịch vụ mới** - `POST /api/DichVu`
- ✅ **Cập nhật dịch vụ** - `PUT /api/DichVu/{id}`
- ✅ **Xóa dịch vụ** - `DELETE /api/DichVu/{id}`
- ✅ **Cập nhật giá dịch vụ** - `POST /api/DichVu/{id}/CapNhatGia`

### 12. Quản lý Giá Điện (GiaDienController)
- ✅ **Xem danh sách bảng giá điện** - `GET /api/GiaDien`
- ✅ **Xem chi tiết bảng giá điện** - `GET /api/GiaDien/{id}`
- ✅ **Thêm bảng giá điện mới** - `POST /api/GiaDien`
- ✅ **Cập nhật bảng giá điện** - `PUT /api/GiaDien/{id}`
- ✅ **Xóa bảng giá điện** - `DELETE /api/GiaDien/{id}`

### 13. Quản lý Giá Nước (GiaNuocController)
- ✅ **Xem danh sách bảng giá nước** - `GET /api/GiaNuoc`
- ✅ **Xem chi tiết bảng giá nước** - `GET /api/GiaNuoc/{id}`
- ✅ **Thêm bảng giá nước mới** - `POST /api/GiaNuoc`
- ✅ **Cập nhật bảng giá nước** - `PUT /api/GiaNuoc/{id}`
- ✅ **Xóa bảng giá nước** - `DELETE /api/GiaNuoc/{id}`

### 14. Quản lý Thông báo (ThongBaoController)
- ✅ **Xem danh sách thông báo** - `GET /api/ThongBao`
- ✅ **Tạo thông báo mới** - `POST /api/ThongBao`
- ✅ **Gửi thông báo cho 1 người** - `POST /api/ThongBao/gui-cho-nguoi`
- ✅ **Gửi thông báo cho nhiều người** - `POST /api/ThongBao/gui-cho-nhieu-nguoi`
- ✅ **Cập nhật thông báo** - `PUT /api/ThongBao/{id}`
- ✅ **Xóa thông báo** - `DELETE /api/ThongBao/{id}`

### 15. Quản lý Tin nhắn (TinNhanController) - **CHỈ ADMIN**
- ✅ **Gửi tin nhắn cho khách hàng** - `POST /api/TinNhan/admin-gui-cho-khach`
- ✅ **Xem danh sách hội thoại** - `GET /api/TinNhan/admin/danh-sach-hoi-thoai`
- ✅ **Xem tin nhắn với khách hàng** - `GET /api/TinNhan/admin/khach-hang/{maNguoiThue}`
- ✅ **Đánh dấu đã đọc tất cả** - `POST /api/TinNhan/admin/khach-hang/{maNguoiThue}/da-doc-tat-ca`
- ✅ **Thu hồi tin nhắn** - `DELETE /api/TinNhan/thu-hoi/{id}`
- ✅ **Sửa tin nhắn** - `PUT /api/TinNhan/sua/{id}`

### 16. Dashboard & Báo cáo (DashboardController)
- ✅ **Xem thống kê tổng quan** - `GET /api/Dashboard/statistics`
- ✅ **Xem trạng thái phòng** - `GET /api/Dashboard/room-status`
- ✅ **Xem doanh thu theo tháng** - `GET /api/Dashboard/monthly-revenue`
- ✅ **Xem hóa đơn gần đây** - `GET /api/Dashboard/recent-bills`
- ✅ **Xem sử dụng điện nước** - `GET /api/Dashboard/utility-usage`
- ✅ **Xem trạng thái hợp đồng** - `GET /api/Dashboard/contract-status`
- ✅ **Xem doanh thu theo tháng/năm** - `GET /api/Dashboard/revenue-by-month-year?year={year}&month={month}`
- ✅ **Xem phòng chưa thanh toán** - `GET /api/Dashboard/unpaid-rooms`
- ✅ **Xem hợp đồng sắp hết hạn** - `GET /api/Dashboard/expiring-contracts?daysThreshold={days}`
- ✅ **Xem KPI** - `GET /api/Dashboard/kpi` (**CHỈ ADMIN**)

---

## 👤 CHỨC NĂNG DÀNH CHO USER (NGƯỜI THUÊ)

### 1. Xem thông tin cá nhân
- ✅ **Xem thông tin người thuê của mình** - `GET /api/NguoiThue/{id}` (chỉ xem được của mình)

### 2. Quản lý Hợp đồng
- ✅ **Xem hợp đồng của mình** - `GET /api/HopDong` (chỉ xem được hợp đồng của mình)
- ✅ **Xem chi tiết hợp đồng của mình** - `GET /api/HopDong/{id}` (chỉ xem được hợp đồng của mình)

### 3. Quản lý Hóa đơn
- ✅ **Xem hóa đơn của mình** - `GET /api/HoaDon?maNguoiThue={id}` (chỉ xem được hóa đơn của mình)
- ✅ **In hóa đơn của mình** - `GET /api/HoaDon/Print/{id}` (chỉ in được hóa đơn của mình)
- ✅ **In hóa đơn PDF của mình** - `GET /api/HoaDon/PrintPdf/{id}` (chỉ in được hóa đơn của mình)

### 4. Quản lý Thanh toán
- ✅ **Xem thanh toán của mình** - `GET /api/ThanhToan` (chỉ xem được thanh toán của mình)
- ✅ **Tạo thanh toán mới** - `POST /api/ThanhToan` (chỉ thanh toán hóa đơn của mình)
- ✅ **Tạo thanh toán MoMo** - `POST /api/ThanhToan/create` (chỉ thanh toán hóa đơn của mình)

### 5. Quản lý Chỉ số Điện
- ✅ **Xem chỉ số điện của phòng mình** - `GET /api/ChiSoDien` (chỉ xem được phòng mình đang thuê)
- ✅ **Upload ảnh chỉ số điện** - `POST /api/ChiSoDien/upload` (tự động lấy phòng từ hợp đồng đang hiệu lực)
- ✅ **Thêm chỉ số điện thủ công** - `POST /api/ChiSoDien` (chỉ cho phòng mình đang thuê)

### 6. Quản lý Chỉ số Nước
- ✅ **Xem chỉ số nước của phòng mình** - `GET /api/ChiSoNuoc` (chỉ xem được phòng mình đang thuê)
- ✅ **Upload ảnh chỉ số nước** - `POST /api/ChiSoNuoc/upload` (tự động lấy phòng từ hợp đồng đang hiệu lực)
- ✅ **Thêm chỉ số nước thủ công** - `POST /api/ChiSoNuoc` (chỉ cho phòng mình đang thuê)

### 7. Điều khiển Cửa (PhongController)
- ✅ **Mở/Đóng cửa phòng của mình** - `POST /api/Phong/nguoi-thue-dieu-khien` (chỉ điều khiển được phòng mình đang thuê)
  - Body: `{ "HanhDong": "OPEN" }` hoặc `{ "HanhDong": "CLOSE" }`
- ✅ **Xem lịch sử đóng/mở cửa phòng mình** - `GET /api/Phong/{id}/lich-su-dong-mo` (chỉ xem được phòng mình đang thuê)

### 8. Quản lý Tin nhắn (TinNhanController)
- ✅ **Gửi tin nhắn cho Admin** - `POST /api/TinNhan/khach-gui-cho-admin`
- ✅ **Xem tin nhắn với Admin** - `GET /api/TinNhan/khach/voi-admin`
- ✅ **Đánh dấu đã đọc** - `POST /api/TinNhan/khach/da-doc-tat-ca`
- ✅ **Thu hồi tin nhắn của mình** - `DELETE /api/TinNhan/thu-hoi/{id}` (chỉ thu hồi được tin nhắn mình gửi)
- ✅ **Sửa tin nhắn của mình** - `PUT /api/TinNhan/sua/{id}` (chỉ sửa được tin nhắn mình gửi, trong vòng 5 phút)

### 9. Xem Thông báo
- ✅ **Xem danh sách thông báo** - `GET /api/ThongBao` (xem tất cả thông báo còn hiệu lực)

### 10. Dashboard
- ✅ **Xem thống kê tổng quan** - `GET /api/Dashboard/statistics` (xem thống kê chung)

---

## 🔄 CHỨC NĂNG CHUNG (CẢ ADMIN VÀ USER)

### 1. Xác thực (AuthController)
- ✅ **Đăng ký tài khoản** - `POST /api/Auth/dang-ky`
- ✅ **Đăng nhập** - `POST /api/Auth/dang-nhap`
- ✅ **Đăng xuất** - (xử lý ở frontend)

### 2. Xem thông tin cơ bản
- ✅ **Xem danh sách phòng** - `GET /api/Phong` (cả Admin và User đều xem được)
- ✅ **Xem danh sách loại phòng** - `GET /api/LoaiPhong` (cả Admin và User đều xem được)
- ✅ **Xem danh sách dịch vụ** - `GET /api/DichVu` (cả Admin và User đều xem được)
- ✅ **Xem bảng giá điện** - `GET /api/GiaDien` (cả Admin và User đều xem được)
- ✅ **Xem bảng giá nước** - `GET /api/GiaNuoc` (cả Admin và User đều xem được)

---

## 📊 TÓM TẮT PHÂN QUYỀN

### Admin có quyền:
- ✅ Quản lý toàn bộ hệ thống
- ✅ CRUD tất cả các bảng: Nhà trọ, Phòng, Loại phòng, Người thuê, Hợp đồng, Hóa đơn, Thanh toán
- ✅ Quản lý dịch vụ, giá điện, giá nước
- ✅ Quản lý tài khoản người dùng
- ✅ Xem KPI và báo cáo chi tiết
- ✅ Điều khiển cửa từ xa cho bất kỳ phòng nào
- ✅ Gửi tin nhắn cho khách hàng
- ✅ Tạo và quản lý thông báo

### User (Người thuê) có quyền:
- ✅ Xem thông tin cá nhân của mình
- ✅ Xem hợp đồng, hóa đơn, thanh toán của mình
- ✅ Thanh toán hóa đơn (bao gồm thanh toán MoMo)
- ✅ Nhập chỉ số điện/nước cho phòng mình đang thuê
- ✅ Điều khiển cửa phòng mình đang thuê
- ✅ Gửi và nhận tin nhắn với Admin
- ✅ Xem thông báo hệ thống
- ✅ Xem thống kê tổng quan (không có KPI)

### Hạn chế của User:
- ❌ Không thể tạo/sửa/xóa: Nhà trọ, Phòng, Loại phòng, Dịch vụ, Giá điện/nước
- ❌ Không thể quản lý người thuê khác
- ❌ Không thể quản lý hợp đồng/hóa đơn của người khác
- ❌ Không thể điều khiển cửa phòng khác
- ❌ Không thể xem KPI và báo cáo chi tiết
- ❌ Không thể quản lý tài khoản

---

## 🔒 BẢO MẬT

### Kiểm tra quyền được thực hiện qua:
1. **Attribute `[Authorize(Roles = "Admin")]`** - Kiểm tra ở controller level
2. **Logic kiểm tra trong code** - Kiểm tra ở method level
3. **JWT Token** - Xác thực người dùng qua token
4. **Filter dữ liệu** - User chỉ xem được dữ liệu của mình

### Lưu ý:
- Tất cả các endpoint đều yêu cầu đăng nhập (trừ đăng ký/đăng nhập)
- User chỉ có thể thao tác với dữ liệu liên quan đến hợp đồng đang hiệu lực của mình
- Admin có toàn quyền truy cập và quản lý hệ thống

---

## 📝 GHI CHÚ

- Các endpoint có `[Authorize(Roles = "Admin")]` chỉ Admin mới truy cập được
- Các endpoint không có attribute này nhưng có logic kiểm tra trong code sẽ lọc dữ liệu theo người dùng
- User có thể upload chỉ số điện/nước mà không cần chỉ định phòng (hệ thống tự động lấy từ hợp đồng đang hiệu lực)


