// Lấy danh sách giá nước
async function loadGiaNuoc() {
    try {
        const token = localStorage.getItem('token');
        if (!token) {
            window.location.href = 'login.html';
            return;
        }

        const response = await fetch(`${Config.API_URL}${Config.ENDPOINTS.GIA_NUOC}`, {
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
            const errorData = await response.json();
            throw new Error(errorData.message || 'Không thể tải danh sách giá nước');
        }

        const data = await response.json();
        const tableBody = document.getElementById('giaNuocTableBody');
        if (!tableBody) {
            console.error('Không tìm thấy element giaNuocTableBody');
            return;
        }
        tableBody.innerHTML = '';

        data.forEach(giaNuoc => {
            const row = document.createElement('tr');
            row.innerHTML = `
                <td>${giaNuoc.maGiaNuoc}</td>
                <td>${giaNuoc.bacNuoc}</td>
                <td>${formatCurrency(giaNuoc.giaTienNuoc)}</td>
                <td>${giaNuoc.tuSoNuoc}</td>
                <td>${giaNuoc.denSoNuoc}</td>
                <td>
                    <button class="btn btn-sm btn-primary" onclick="editGiaNuoc(${giaNuoc.maGiaNuoc})">
                        <i class="fas fa-edit"></i>
                    </button>
                    <button class="btn btn-sm btn-danger" onclick="deleteGiaNuoc(${giaNuoc.maGiaNuoc})">
                        <i class="fas fa-trash"></i>
                    </button>
                </td>
            `;
            tableBody.appendChild(row);
        });
    } catch (error) {
        console.error('Error:', error);
        showAlert(error.message || 'Có lỗi xảy ra khi tải danh sách giá nước', 'danger');
    }
}

// Thêm giá nước mới
document.getElementById('saveGiaNuocBtn').addEventListener('click', async () => {
    try {
        const token = localStorage.getItem('token');
        if (!token) {
            window.location.href = 'login.html';
            return;
        }

        // Lấy và validate dữ liệu
        const bacNuoc = parseInt(document.getElementById('bacNuoc').value);
        const giaTienNuoc = parseFloat(document.getElementById('giaTienNuoc').value);
        const tuSoNuoc = parseInt(document.getElementById('tuSoNuoc').value);
        const denSoNuoc = parseInt(document.getElementById('denSoNuoc').value);

        // Kiểm tra dữ liệu đầu vào
        if (isNaN(bacNuoc) || bacNuoc < 1) {
            throw new Error('Bậc nước phải lớn hơn 0');
        }
        if (isNaN(giaTienNuoc) || giaTienNuoc < 0) {
            throw new Error('Giá tiền nước không được âm');
        }
        if (isNaN(tuSoNuoc) || tuSoNuoc < 0) {
            throw new Error('Số nước bắt đầu không được âm');
        }
        if (isNaN(denSoNuoc) || denSoNuoc <= tuSoNuoc) {
            throw new Error('Số nước kết thúc phải lớn hơn số nước bắt đầu');
        }

        const giaNuoc = {
            bacNuoc: bacNuoc,
            giaTienNuoc: giaTienNuoc,
            tuSoNuoc: tuSoNuoc,
            denSoNuoc: denSoNuoc
        };

        const response = await fetch(`${Config.API_URL}${Config.ENDPOINTS.GIA_NUOC}`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${token}`,
                'Accept': 'application/json'
            },
            body: JSON.stringify(giaNuoc)
        });

        if (response.status === 401) {
            window.location.href = 'login.html';
            return;
        }

        if (!response.ok) {
            const errorData = await response.json();
            throw new Error(errorData.message || 'Có lỗi xảy ra khi thêm giá nước');
        }

        showAlert('Thêm giá nước thành công', 'success');
        const modal = bootstrap.Modal.getInstance(document.getElementById('addGiaNuocModal'));
        modal.hide();
        document.getElementById('addGiaNuocForm').reset();
        loadGiaNuoc();
    } catch (error) {
        console.error('Error:', error);
        showAlert(error.message || 'Có lỗi xảy ra khi thêm giá nước', 'danger');
    }
});

// Sửa giá nước
async function editGiaNuoc(id) {
    try {
        const token = localStorage.getItem('token');
        if (!token) {
            window.location.href = 'login.html';
            return;
        }

        const response = await fetch(`${Config.API_URL}${Config.ENDPOINTS.GIA_NUOC}/${id}`, {
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
            const errorData = await response.json();
            throw new Error(errorData.message || 'Không thể tải thông tin giá nước');
        }

        const giaNuoc = await response.json();

        document.getElementById('editMaGiaNuoc').value = giaNuoc.maGiaNuoc;
        document.getElementById('editBacNuoc').value = giaNuoc.bacNuoc;
        document.getElementById('editGiaTienNuoc').value = giaNuoc.giaTienNuoc;
        document.getElementById('editTuSoNuoc').value = giaNuoc.tuSoNuoc;
        document.getElementById('editDenSoNuoc').value = giaNuoc.denSoNuoc;

        const modal = new bootstrap.Modal(document.getElementById('editGiaNuocModal'));
        modal.show();
    } catch (error) {
        console.error('Error:', error);
        showAlert(error.message || 'Có lỗi xảy ra khi tải thông tin giá nước', 'danger');
    }
}

// Cập nhật giá nước
document.getElementById('updateGiaNuocBtn').addEventListener('click', async () => {
    try {
        const token = localStorage.getItem('token');
        if (!token) {
            window.location.href = 'login.html';
            return;
        }

        // Lấy và validate dữ liệu
        const id = document.getElementById('editMaGiaNuoc').value;
        const bacNuoc = parseInt(document.getElementById('editBacNuoc').value);
        const giaTienNuoc = parseFloat(document.getElementById('editGiaTienNuoc').value);
        const tuSoNuoc = parseInt(document.getElementById('editTuSoNuoc').value);
        const denSoNuoc = parseInt(document.getElementById('editDenSoNuoc').value);

        // Kiểm tra dữ liệu đầu vào
        if (isNaN(bacNuoc) || bacNuoc < 1) {
            throw new Error('Bậc nước phải lớn hơn 0');
        }
        if (isNaN(giaTienNuoc) || giaTienNuoc < 0) {
            throw new Error('Giá tiền nước không được âm');
        }
        if (isNaN(tuSoNuoc) || tuSoNuoc < 0) {
            throw new Error('Số nước bắt đầu không được âm');
        }
        if (isNaN(denSoNuoc) || denSoNuoc <= tuSoNuoc) {
            throw new Error('Số nước kết thúc phải lớn hơn số nước bắt đầu');
        }

        const giaNuoc = {
            maGiaNuoc: parseInt(id),
            bacNuoc: bacNuoc,
            giaTienNuoc: giaTienNuoc,
            tuSoNuoc: tuSoNuoc,
            denSoNuoc: denSoNuoc
        };

        const response = await fetch(`${Config.API_URL}${Config.ENDPOINTS.GIA_NUOC}/${id}`, {
            method: 'PUT',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${token}`,
                'Accept': 'application/json'
            },
            body: JSON.stringify(giaNuoc)
        });

        if (response.status === 401) {
            window.location.href = 'login.html';
            return;
        }

        if (!response.ok) {
            const errorData = await response.json();
            throw new Error(errorData.message || 'Có lỗi xảy ra khi cập nhật giá nước');
        }

        showAlert('Cập nhật giá nước thành công', 'success');
        const modal = bootstrap.Modal.getInstance(document.getElementById('editGiaNuocModal'));
        modal.hide();
        loadGiaNuoc();
    } catch (error) {
        console.error('Error:', error);
        showAlert(error.message || 'Có lỗi xảy ra khi cập nhật giá nước', 'danger');
    }
});

// Xóa giá nước
async function deleteGiaNuoc(id) {
    if (!confirm('Bạn có chắc chắn muốn xóa giá nước này?')) {
        return;
    }

    try {
        const token = localStorage.getItem('token');
        if (!token) {
            window.location.href = 'login.html';
            return;
        }

        const response = await fetch(`${Config.API_URL}${Config.ENDPOINTS.GIA_NUOC}/${id}`, {
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
            const errorData = await response.json();
            throw new Error(errorData.message || 'Có lỗi xảy ra khi xóa giá nước');
        }

        showAlert('Xóa giá nước thành công', 'success');
        loadGiaNuoc();
    } catch (error) {
        console.error('Error:', error);
        showAlert(error.message || 'Có lỗi xảy ra khi xóa giá nước', 'danger');
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
    loadGiaNuoc();
}); 