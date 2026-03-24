// Lấy danh sách giá điện
async function loadGiaDien() {
    try {
        const token = localStorage.getItem('token');
        if (!token) {
            window.location.href = 'login.html';
            return;
        }

        const response = await fetch(`${Config.API_URL}${Config.ENDPOINTS.GIA_DIEN}`, {
            headers: {
                'Authorization': `Bearer ${token}`,
                'Accept': 'application/json'
            }
        });

        if (response.status === 401) {
            window.location.href = 'login.html';
            return;
        }

        if (!response.ok) {
            throw new Error('Không thể tải danh sách giá điện');
        }

        const data = await response.json();
        const tableBody = document.getElementById('giaDienTableBody');
        tableBody.innerHTML = '';

        data.forEach(giaDien => {
            const row = document.createElement('tr');
            row.innerHTML = `
                <td>${giaDien.maGiaDien}</td>
                <td>${giaDien.bacDien}</td>
                <td>${formatCurrency(giaDien.giaTienDien)}</td>
                <td>${giaDien.tuSoDien}</td>
                <td>${giaDien.denSoDien}</td>
                <td>
                    <button class="btn btn-sm btn-primary" onclick="editGiaDien(${giaDien.maGiaDien})">
                        <i class="fas fa-edit"></i>
                    </button>
                    <button class="btn btn-sm btn-danger" onclick="deleteGiaDien(${giaDien.maGiaDien})">
                        <i class="fas fa-trash"></i>
                    </button>
                </td>
            `;
            tableBody.appendChild(row);
        });
    } catch (error) {
        console.error('Error:', error);
        showAlert('Có lỗi xảy ra khi tải danh sách giá điện', 'danger');
    }
}

// Thêm giá điện mới
document.getElementById('saveGiaDienBtn').addEventListener('click', async () => {
    try {
        const token = localStorage.getItem('token');
        if (!token) {
            window.location.href = 'login.html';
            return;
        }

        const bacDien = parseInt(document.getElementById('bacDien').value);
        const giaTienDien = parseFloat(document.getElementById('giaTienDien').value);
        const tuSoDien = parseInt(document.getElementById('tuSoDien').value);
        const denSoDien = parseInt(document.getElementById('denSoDien').value);

        if (!Number.isFinite(bacDien) || !Number.isFinite(giaTienDien) || !Number.isFinite(tuSoDien) || !Number.isFinite(denSoDien)) {
            throw new Error('Vui lòng nhập đầy đủ và hợp lệ các trường.');
        }
        if (tuSoDien > denSoDien) {
            throw new Error('"Từ số điện" phải nhỏ hơn hoặc bằng "Đến số điện".');
        }
        if (bacDien < 0 || giaTienDien < 0) {
            throw new Error('Giá trị không được âm.');
        }

        const giaDien = {
            bacDien,
            giaTienDien,
            tuSoDien,
            denSoDien
        };

        const response = await fetch(`${Config.API_URL}${Config.ENDPOINTS.GIA_DIEN}`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${token}`,
                'Accept': 'application/json'
            },
            body: JSON.stringify(giaDien)
        });

        if (response.status === 401) {
            window.location.href = 'login.html';
            return;
        }

        if (!response.ok) {
            const error = await response.json();
            throw new Error(error.message || 'Có lỗi xảy ra khi thêm giá điện');
        }

        showAlert('Thêm giá điện thành công', 'success');
        const modal = bootstrap.Modal.getInstance(document.getElementById('addGiaDienModal'));
        modal.hide();
        loadGiaDien();
    } catch (error) {
        console.error('Error:', error);
        showAlert(error.message || 'Có lỗi xảy ra khi thêm giá điện', 'danger');
    }
});

// Sửa giá điện
async function editGiaDien(id) {
    try {
        const token = localStorage.getItem('token');
        if (!token) {
            window.location.href = 'login.html';
            return;
        }

        const response = await fetch(`${Config.API_URL}${Config.ENDPOINTS.GIA_DIEN}/${id}`, {
            headers: {
                'Authorization': `Bearer ${token}`,
                'Accept': 'application/json'
            }
        });

        if (response.status === 401) {
            window.location.href = 'login.html';
            return;
        }

        if (!response.ok) {
            throw new Error('Không thể tải thông tin giá điện');
        }

        const giaDien = await response.json();

        document.getElementById('editMaGiaDien').value = giaDien.maGiaDien;
        document.getElementById('editBacDien').value = giaDien.bacDien;
        document.getElementById('editGiaTienDien').value = giaDien.giaTienDien;
        document.getElementById('editTuSoDien').value = giaDien.tuSoDien;
        document.getElementById('editDenSoDien').value = giaDien.denSoDien;

        const modal = new bootstrap.Modal(document.getElementById('editGiaDienModal'));
        modal.show();
    } catch (error) {
        console.error('Error:', error);
        showAlert('Có lỗi xảy ra khi tải thông tin giá điện', 'danger');
    }
}

// Cập nhật giá điện
document.getElementById('updateGiaDienBtn').addEventListener('click', async () => {
    try {
        const token = localStorage.getItem('token');
        if (!token) {
            window.location.href = 'login.html';
            return;
        }

        const id = document.getElementById('editMaGiaDien').value;
        const bacDien = parseInt(document.getElementById('editBacDien').value);
        const giaTienDien = parseFloat(document.getElementById('editGiaTienDien').value);
        const tuSoDien = parseInt(document.getElementById('editTuSoDien').value);
        const denSoDien = parseInt(document.getElementById('editDenSoDien').value);

        if (!Number.isFinite(bacDien) || !Number.isFinite(giaTienDien) || !Number.isFinite(tuSoDien) || !Number.isFinite(denSoDien)) {
            throw new Error('Vui lòng nhập đầy đủ và hợp lệ các trường.');
        }
        if (tuSoDien > denSoDien) {
            throw new Error('"Từ số điện" phải nhỏ hơn hoặc bằng "Đến số điện".');
        }
        if (bacDien < 0 || giaTienDien < 0) {
            throw new Error('Giá trị không được âm.');
        }

        const giaDien = {
            maGiaDien: parseInt(id),
            bacDien,
            giaTienDien,
            tuSoDien,
            denSoDien
        };

        const response = await fetch(`${Config.API_URL}${Config.ENDPOINTS.GIA_DIEN}/${id}`, {
            method: 'PUT',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${token}`,
                'Accept': 'application/json'
            },
            body: JSON.stringify(giaDien)
        });

        if (response.status === 401) {
            window.location.href = 'login.html';
            return;
        }

        if (!response.ok) {
            const error = await response.json();
            throw new Error(error.message || 'Có lỗi xảy ra khi cập nhật giá điện');
        }

        showAlert('Cập nhật giá điện thành công', 'success');
        const modal = bootstrap.Modal.getInstance(document.getElementById('editGiaDienModal'));
        modal.hide();
        loadGiaDien();
    } catch (error) {
        console.error('Error:', error);
        showAlert(error.message || 'Có lỗi xảy ra khi cập nhật giá điện', 'danger');
    }
});

// Xóa giá điện
async function deleteGiaDien(id) {
    if (!confirm('Bạn có chắc chắn muốn xóa giá điện này?')) {
        return;
    }

    try {
        const token = localStorage.getItem('token');
        if (!token) {
            window.location.href = 'login.html';
            return;
        }

        const response = await fetch(`${Config.API_URL}${Config.ENDPOINTS.GIA_DIEN}/${id}`, {
            method: 'DELETE',
            headers: {
                'Authorization': `Bearer ${token}`,
                'Accept': 'application/json'
            }
        });

        if (response.status === 401) {
            window.location.href = 'login.html';
            return;
        }

        if (!response.ok) {
            const error = await response.json();
            throw new Error(error.message || 'Có lỗi xảy ra khi xóa giá điện');
        }

        showAlert('Xóa giá điện thành công', 'success');
        loadGiaDien();
    } catch (error) {
        console.error('Error:', error);
        showAlert(error.message || 'Có lỗi xảy ra khi xóa giá điện', 'danger');
    }
}

// Format tiền
function formatCurrency(amount) {
    return new Intl.NumberFormat('vi-VN', {
        style: 'currency',
        currency: 'VND'
    }).format(amount);
}

// Hiển thị thông báo
function showAlert(message, type) {
    const alertDiv = document.createElement('div');
    alertDiv.className = `alert alert-${type} alert-dismissible fade show position-fixed top-0 end-0 m-3`;
    alertDiv.innerHTML = `
        ${message}
        <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
    `;
    document.body.appendChild(alertDiv);
    setTimeout(() => alertDiv.remove(), 3000);
}

// Load dữ liệu khi trang được tải
document.addEventListener('DOMContentLoaded', () => {
    loadGiaDien();
}); 