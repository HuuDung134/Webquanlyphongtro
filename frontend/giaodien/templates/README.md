# Template System - Hướng dẫn sử dụng

## Tổng quan

Template system giúp chuẩn hóa giao diện và giảm code lặp lại trong các trang HTML. Tất cả các trang đều sử dụng sidebar và header chung được render tự động.

## Cấu trúc

```
frontend/giaodien/
├── templates/
│   ├── page-template.html  # Template mẫu
│   └── README.md           # File này
├── js/
│   └── template.js         # Logic render sidebar và utilities
├── css/
│   └── common.css          # Styles chung cho tất cả trang
└── [các trang HTML]
```

## Cách sử dụng

### 1. Tạo trang mới từ template

Copy `templates/page-template.html` và đổi tên thành file mới của bạn, sau đó:

1. **Cập nhật title và tiêu đề:**
```html
<title>Quản lý XXX - Hệ thống quản lý nhà trọ</title>
<h1 class="h2 text-primary">Quản lý XXX</h1>
```

2. **Thêm sidebar container:**
```html
<div id="sidebar-container"></div>
```

3. **Khởi tạo template trong script:**
```javascript
document.addEventListener('DOMContentLoaded', function() {
    initTemplate('ten-file-cua-ban.html');
    // Các function khác của trang
});
```

### 2. Cấu trúc HTML chuẩn

```html
<!DOCTYPE html>
<html lang="vi">
<head>
    <!-- Meta tags -->
    <title>...</title>
    
    <!-- CSS -->
    <link href="https://cdn.jsdelivr.net/npm/bootstrap@5.1.3/dist/css/bootstrap.min.css" rel="stylesheet">
    <link href="https://cdnjs.cloudflare.com/ajax/libs/font-awesome/5.15.4/css/all.min.css" rel="stylesheet">
    <link href="css/common.css" rel="stylesheet">
    
    <!-- Page-specific CSS -->
    <style>...</style>
</head>
<body>
    <div class="container-fluid">
        <div class="row">
            <div id="sidebar-container"></div>
            
            <main class="col-md-9 ms-sm-auto col-lg-10 px-md-4 main-content">
                <div class="page-header">
                    <h1 class="h2 text-primary">Tiêu đề</h1>
                    <button class="btn btn-success shadow-sm">Action</button>
                </div>
                
                <div class="fade-in">
                    <!-- Nội dung trang -->
                </div>
            </main>
        </div>
    </div>
    
    <!-- Scripts -->
    <script src="https://cdn.jsdelivr.net/npm/bootstrap@5.1.3/dist/js/bootstrap.bundle.min.js"></script>
    <script src="Config.js"></script>
    <script src="js/template.js"></script>
    <script src="your-page.js"></script>
    
    <script>
        document.addEventListener('DOMContentLoaded', function() {
            initTemplate('your-page.html');
        });
    </script>
</body>
</html>
```

## Utility Functions

Template system cung cấp các utility functions có sẵn:

### showAlert(message, type, duration)
Hiển thị thông báo toast.

```javascript
showAlert('Lưu thành công!', 'success');
showAlert('Có lỗi xảy ra!', 'danger');
showAlert('Cảnh báo!', 'warning');
showAlert('Thông tin', 'info');
```

### formatCurrency(amount)
Định dạng số tiền theo VND.

```javascript
formatCurrency(1000000); // "1.000.000 ₫"
```

### formatDate(date)
Định dạng ngày theo định dạng Việt Nam.

```javascript
formatDate(new Date()); // "09/12/2025"
```

### formatDateForInput(date)
Định dạng ngày cho input[type="date"].

```javascript
formatDateForInput(new Date()); // "2025-12-09"
```

### confirmDialog(message, title)
Hiển thị dialog xác nhận.

```javascript
const confirmed = await confirmDialog('Bạn có chắc muốn xóa?', 'Xác nhận');
if (confirmed) {
    // Xóa
}
```

### showLoading(show)
Hiển thị/ẩn loading overlay.

```javascript
showLoading(true);  // Hiển thị
// ... làm việc ...
showLoading(false); // Ẩn
```

## CSS Classes có sẵn

### Layout
- `.main-content` - Container chính
- `.page-header` - Header của trang
- `.sidebar` - Sidebar navigation
- `.fade-in` - Animation fade in

### Components
- `.card` - Card component với hover effect
- `.card-hover` - Card có hiệu ứng hover
- `.empty-state` - Trạng thái rỗng
- `.section-divider` - Phân cách section

### Utilities
- `.shadow-sm` - Shadow nhẹ
- `.shadow` - Shadow vừa
- `.text-primary` - Màu primary
- `.bg-primary` - Background primary

## Menu Items

Để thêm menu item mới, chỉnh sửa `MENU_ITEMS` trong `js/template.js`:

```javascript
const MENU_ITEMS = [
    { href: 'index.html', icon: 'fa-home', text: 'Trang chủ' },
    { href: 'new-page.html', icon: 'fa-icon', text: 'Trang mới' },
    // ...
];
```

## Responsive

Template tự động responsive:
- Desktop: Sidebar cố định bên trái
- Mobile: Sidebar chuyển thành static, main-content full width

## Authentication

Template tự động kiểm tra authentication:
- Nếu chưa đăng nhập → redirect về `login.html`
- Hiển thị tên user trong sidebar
- Function `logout()` có sẵn

## Ví dụ

Xem các file sau để tham khảo:
- `chisodien.html` - Trang sử dụng template đầy đủ
- `chisonuoc.html` - Trang đã được chuyển sang template
- `templates/page-template.html` - Template mẫu

## Lưu ý

1. **Luôn sử dụng `initTemplate()`** trong `DOMContentLoaded`
2. **Đặt tên file chính xác** khi gọi `initTemplate()`
3. **Sử dụng utility functions** thay vì tự viết lại
4. **Thêm styles riêng** trong `<style>` tag của từng trang
5. **Không hardcode sidebar** - luôn dùng `#sidebar-container`

## Troubleshooting

### Sidebar không hiển thị
- Kiểm tra có `<div id="sidebar-container"></div>` chưa
- Kiểm tra đã gọi `initTemplate()` chưa
- Kiểm tra console có lỗi JavaScript không

### User info không hiển thị
- Kiểm tra localStorage có `token` và `user` chưa
- Kiểm tra format của `user` object

### Styles không áp dụng
- Kiểm tra đã include `css/common.css` chưa
- Kiểm tra thứ tự load CSS (common.css phải sau Bootstrap)

