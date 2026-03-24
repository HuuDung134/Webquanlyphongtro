import { baseUrl } from './baseUrl.js';

document.addEventListener('DOMContentLoaded', function() {
    const loginForm = document.getElementById('loginForm');
    const errorAlert = document.getElementById('errorAlert');
    const submitButton = loginForm.querySelector('button[type="submit"]');

    loginForm.addEventListener('submit', async function(e) {
        e.preventDefault();

        const username = document.getElementById('username').value;
        const password = document.getElementById('password').value;

        try {
            // Disable nút đăng nhập và hiển thị loading
            submitButton.disabled = true;
            submitButton.innerHTML = '<span class="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span> Đang đăng nhập...';
            errorAlert.style.display = 'none';

            console.log('Đang gửi request đăng nhập...');
            const response = await fetch(`${baseUrl}/api/Auth/dang-nhap`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({
                    tenDangNhap: username,
                    matKhau: password
                })
            });

            const data = await response.json();
            console.log('Response từ server:', data);

            if (response.ok) {
                // Lưu token và thông tin user vào localStorage
                localStorage.setItem('token', data.token);
                const userData = {
                    id: data.maNguoiDung,
                    username: data.tenDangNhap,
                    fullname: data.hoTen,
                    email: data.email,
                    phone: data.soDienThoai,
                    role: data.vaiTro
                };
                localStorage.setItem('user', JSON.stringify(userData));
                
                console.log('Thông tin người dùng:', userData);
                console.log('Vai trò người dùng:', data.vaiTro);
                
                // Chuyển hướng dựa trên role
                if (data.vaiTro === 'Admin' || data.vaiTro === 'admin') {
                    console.log('Chuyển hướng đến trang admin');
                    window.location.href = 'index.html';
                } else {
                    console.log('Chuyển hướng đến trang user');
                    // Thử chuyển hướng trực tiếp
                    try {
                        window.location.replace('user-dashboard.html');
                    } catch (redirectError) {
                        console.error('Lỗi chuyển hướng:', redirectError);
                        // Nếu chuyển hướng thất bại, thử cách khác
                        window.location.href = 'user-dashboard.html';
                    }
                }
            } else {
                console.error('Đăng nhập thất bại:', data);
                errorAlert.style.display = 'block';
                errorAlert.textContent = data.thongBao || 'Đăng nhập thất bại!';
            }
        } catch (error) {
            console.error('Lỗi:', error);
            errorAlert.style.display = 'block';
            errorAlert.textContent = 'Có lỗi xảy ra. Vui lòng thử lại sau!';
        } finally {
            // Enable lại nút đăng nhập
            submitButton.disabled = false;
            submitButton.innerHTML = 'Đăng nhập';
        }
    });
}); 