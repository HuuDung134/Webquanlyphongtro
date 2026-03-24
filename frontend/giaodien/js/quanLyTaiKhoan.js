// Không cần import vì Config đã được gán vào window

// Kiểm tra quyền admin
function checkAdminAuth() {
    const user = JSON.parse(localStorage.getItem('user'));
    const token = localStorage.getItem('token');
    
    console.log('User info:', user);
    console.log('Token:', token);
    
    if (!user || !token) {
        console.log('Không tìm thấy user hoặc token');
        window.location.href = 'login.html';
        return;
    }

    // Kiểm tra vai trò
    if (user.role !== 'Admin') {
        console.log('Vai trò không phải Admin:', user.role);
        alert('Bạn không có quyền truy cập trang này!');
        window.location.href = 'index.html';
        return;
    }

    document.getElementById('userFullname').textContent = user.fullname;
}

// Khởi tạo DataTable
let usersTable;
$(document).ready(function() {
    checkAdminAuth();
    loadUsers();

    usersTable = $('#usersTable').DataTable({
        language: {
            url: '//cdn.datatables.net/plug-ins/1.11.5/i18n/vi.json'
        }
    });
});

// Load danh sách người dùng
async function loadUsers() {
    try {
        const token = localStorage.getItem('token');
        if (!token) {
            window.location.href = 'login.html';
            return;
        }
        const response = await fetch(`${Config.API_URL}${Config.ENDPOINTS.TAI_KHOAN}`, {
            method: 'GET',
            headers: {
                'Authorization': `Bearer ${token}`,
                'Content-Type': 'application/json'
            }
        });
        const users = await response.json();
        const tbody = $('#usersTable tbody');
        tbody.empty();
        if (Array.isArray(users)) {
            users.forEach(user => {
                const row = `
                    <tr>
                        <td>${user.maNguoiDung}</td>
                        <td>${user.tenDangNhap}</td>
                        <td>${getVaiTroText(user.vaiTro)}</td>
                        <td>${user.trangThai ? 'Hoạt động' : 'Khóa'}</td>
                        <td>
                            <button class="btn btn-sm btn-primary" onclick="editUser(${user.maNguoiDung})">
                                <i class="fas fa-edit"></i>
                            </button>
                            ${user.vaiTro !== 'Admin' ? `
                                <button class="btn btn-sm btn-danger" onclick="deleteUser(${user.maNguoiDung})">
                                    <i class="fas fa-trash"></i>
                                </button>
                            ` : ''}
                        </td>
                    </tr>
                `;
                tbody.append(row);
            });
        }
    } catch (error) {
        alert('Có lỗi xảy ra khi tải danh sách người dùng: ' + error.message);
    }
}

// Hàm chuyển đổi mã vai trò thành text
function getVaiTroText(vaiTro) {
    switch (vaiTro) {
        case 'Admin':
            return 'Quản trị viên';
        case 'ChuTro':
            return 'Chủ trọ';
        case 'NguoiDung':
            return 'Người dùng';
        default:
            return vaiTro;
    }
}

// Thêm người dùng mới
window.addUser = async function() {
    const token = localStorage.getItem('token');
    if (!token) {
        window.location.href = 'login.html';
        return;
    }
    const tenDangNhap = document.getElementById('tenDangNhap').value;
    const matKhau = document.getElementById('matKhau').value;
    const vaiTro = document.getElementById('vaiTro').value;
    try {
        const response = await fetch(`${Config.API_URL}/api/auth/dang-ky`, {
            method: 'POST',
            headers: {
                'Authorization': `Bearer ${token}`,
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({
                tenDangNhap,
                matKhau,
                vaiTro
            })
        });
        const data = await response.json();
        if (response.ok) {
            alert('Thêm người dùng thành công!');
            $('#addUserModal').modal('hide');
            document.getElementById('addUserForm').reset();
            await loadUsers();
        } else {
            alert(data.thongBao || 'Thêm người dùng thất bại!');
        }
    } catch (error) {
        alert('Có lỗi xảy ra khi thêm người dùng: ' + error.message);
    }
};

// Sửa thông tin người dùng
window.editUser = async function(maNguoiDung) {
    const token = localStorage.getItem('token');
    if (!token) {
        window.location.href = 'login.html';
        return;
    }
    try {
        const response = await fetch(`${Config.API_URL}${Config.ENDPOINTS.TAI_KHOAN}/${maNguoiDung}`, {
            method: 'GET',
            headers: {
                'Authorization': `Bearer ${token}`,
                'Content-Type': 'application/json'
            }
        });
        if (response.ok) {
            const user = await response.json();
            document.getElementById('editMaNguoiDung').value = user.maNguoiDung;
            document.getElementById('editTenDangNhap').value = user.tenDangNhap;
            document.getElementById('editVaiTro').value = user.vaiTro;
            document.getElementById('editTrangThai').value = user.trangThai.toString();
            if (user.vaiTro === 'Admin') {
                document.getElementById('editVaiTro').disabled = true;
            } else {
                document.getElementById('editVaiTro').disabled = false;
            }
            $('#editUserModal').modal('show');
        } else {
            alert('Không thể tải thông tin người dùng!');
        }
    } catch (error) {
        alert('Có lỗi xảy ra khi tải thông tin người dùng!');
    }
};

// Cập nhật thông tin người dùng
window.updateUser = async function() {
    const token = localStorage.getItem('token');
    if (!token) {
        window.location.href = 'login.html';
        return;
    }
    const maNguoiDung = document.getElementById('editMaNguoiDung').value;
    const tenDangNhap = document.getElementById('editTenDangNhap').value;
    const vaiTro = document.getElementById('editVaiTro').value;
    const trangThai = document.getElementById('editTrangThai').value === 'true';
    try {
        const response = await fetch(`${Config.API_URL}${Config.ENDPOINTS.TAI_KHOAN}/${maNguoiDung}`, {
            method: 'PUT',
            headers: {
                'Authorization': `Bearer ${token}`,
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({
                tenDangNhap,
                vaiTro,
                trangThai
            })
        });
        const data = await response.json();
        if (response.ok) {
            alert('Cập nhật thông tin thành công!');
            $('#editUserModal').modal('hide');
            loadUsers();
        } else {
            alert(data.thongBao || 'Cập nhật thông tin thất bại!');
        }
    } catch (error) {
        alert('Có lỗi xảy ra khi cập nhật thông tin!');
    }
};

// Xóa người dùng
window.deleteUser = async function(maNguoiDung) {
    const token = localStorage.getItem('token');
    if (!token) {
        window.location.href = 'login.html';
        return;
    }

    if (!confirm('Bạn có chắc chắn muốn xóa người dùng này?')) {
        return;
    }

    try {
        const response = await fetch(`${Config.API_URL}${Config.ENDPOINTS.TAI_KHOAN}/${maNguoiDung}`, {
            method: 'DELETE',
            headers: {
                'Authorization': `Bearer ${token}`,
                'Content-Type': 'application/json'
            }
        });

        const data = await response.json();
        if (response.ok) {
            alert('Xóa người dùng thành công!');
            loadUsers(); // Tải lại danh sách người dùng
        } else {
            alert(data.thongBao || 'Xóa người dùng thất bại!');
        }
    } catch (error) {
        console.error('Lỗi:', error);
        alert('Có lỗi xảy ra khi xóa người dùng!');
    }
};

// Đăng xuất
window.logout = function() {
    localStorage.removeItem('token');
    localStorage.removeItem('user');
    window.location.href = 'login.html';
}; 