// --- CONFIG & CONSTANTS ---
const API_URL = Config.API_URL; // Đảm bảo biến này đã được định nghĩa

// DOM Elements
const dienList = document.getElementById("dienList");
const addDienForm = document.getElementById("addDienForm");
const saveDienBtn = document.getElementById("saveDienBtn");
const phongSelect = document.querySelector('select[name="maPhong"]');

// State
let isEditing = false;
let editingDienId = null;

// --- INITIALIZATION ---
document.addEventListener("DOMContentLoaded", () => {
    loadChiSoDien();
    loadPhong();
    loadGiaDien(); // hiển thị bảng giá để tham khảo
    
    // Setup image upload và OCR
    const anhDienInput = document.getElementById('anhDienInput');
    if (anhDienInput) {
        anhDienInput.addEventListener('change', handleImageUploadDien);
    }
});

// --- HELPER FUNCTIONS (API & AUTH) ---

// Hàm fetch chung để tự động gắn Token và xử lý lỗi cơ bản
async function authFetch(endpoint, options = {}) {
    const token = localStorage.getItem('token');
    if (!token) {
        window.location.href = 'login.html';
        throw new Error('Chưa đăng nhập');
    }

    const defaultHeaders = {
        'Authorization': `Bearer ${token}`,
        'Accept': 'application/json'
        // Lưu ý: Không set 'Content-Type' nếu gửi FormData, trình duyệt sẽ tự set boundary
    };

    // Nếu body không phải FormData, set Content-Type là json
    if (options.body && !(options.body instanceof FormData)) {
        defaultHeaders['Content-Type'] = 'application/json';
    }

    const config = {
        ...options,
        headers: {
            ...defaultHeaders,
            ...options.headers
        }
    };

    const response = await fetch(`${API_URL}${endpoint}`, config);

    if (response.status === 401) {
        localStorage.removeItem('token');
        window.location.href = 'login.html';
        throw new Error('Phiên đăng nhập hết hạn');
    }

    if (!response.ok) {
        const fallback = 'Lỗi kết nối Server';
        let errorMessage = fallback;
        try {
            const text = await response.text(); // đọc một lần
            // Nếu server trả JSON, parse nhẹ
            try {
                const parsed = JSON.parse(text);
                errorMessage = parsed?.message || text || fallback;
            } catch {
                errorMessage = text || fallback;
            }
        } catch {
            errorMessage = fallback;
        }
        throw new Error(errorMessage);
    }

    // Với DELETE hoặc PUT không trả về content
    if (response.status === 204) return null;

    return response.json();
}

// --- MAIN FUNCTIONS (CHI SO DIEN) ---

// 1. Load danh sách chỉ số điện
async function loadChiSoDien() {
    try {
        const data = await authFetch('/api/ChiSoDien');
        displayChiSoDien(data);
    } catch (error) {
        console.error("Lỗi tải chỉ số điện:", error);
        showAlert(error.message, "danger");
    }
}

// 2. Load danh sách phòng cho thẻ Select
async function loadPhong() {
    try {
        const data = await authFetch('/api/Phong');
        phongSelect.innerHTML = '<option value="">Chọn phòng...</option>';
        data.forEach((phong) => {
            const option = document.createElement("option");
            option.value = phong.maPhong;
            option.textContent = phong.tenPhong;
            phongSelect.appendChild(option);
        });
    } catch (error) {
        console.error("Lỗi tải phòng:", error);
    }
}

// 3. Load và hiển thị bảng giá điện (chỉ để xem)
async function loadGiaDien() {
    try {
        const data = await authFetch('/api/GiaDien');
        displayGiaDien(data);
    } catch (error) {
        console.error("Lỗi tải giá điện:", error);
    }
}

// Xử lý upload ảnh và đọc chỉ số tự động
async function handleImageUploadDien(event) {
    const file = event.target.files[0];
    if (!file) return;

    const maPhong = document.querySelector('select[name="maPhong"]').value;
    if (!maPhong) {
        showAlert('Vui lòng chọn phòng trước', 'warning');
        event.target.value = '';
        return;
    }

    // Hiển thị preview ảnh
    const previewDiv = document.getElementById('imagePreviewDien');
    const previewImg = document.getElementById('previewImgDien');
    const ocrLoading = document.getElementById('ocrLoadingDien');
    const ocrResult = document.getElementById('ocrResultDien');
    const manualInput = document.getElementById('manualInputDien');

    previewImg.src = URL.createObjectURL(file);
    previewDiv.style.display = 'block';
    ocrLoading.style.display = 'block';
    ocrResult.style.display = 'none';
    manualInput.style.display = 'none';

    try {
        // Gọi API upload để đọc chỉ số tự động
        const formData = new FormData();
        formData.append('file', file);
        formData.append('maPhong', maPhong);

        const response = await fetch(`${API_URL}/api/ChiSoDien/upload`, {
            method: 'POST',
            headers: {
                'Authorization': `Bearer ${localStorage.getItem('token')}`
            },
            body: formData
        });

        ocrLoading.style.display = 'none';

        if (!response.ok) {
            const errorText = await response.text();
            throw new Error(errorText || 'Không thể đọc chỉ số từ ảnh');
        }

        const result = await response.json();
        
        // Hiển thị kết quả OCR
        if (result.soDienMoi !== undefined && result.soDienCu !== undefined) {
            // Tự động điền vào form
            document.getElementById('soDienCu').value = result.soDienCu || 0;
            document.getElementById('soDienMoi').value = result.soDienMoi || 0;
            
            // Hiển thị kết quả
            ocrResult.className = 'mt-2 alert alert-success';
            ocrResult.innerHTML = `
                <i class="fas fa-check-circle"></i> 
                Đã đọc thành công! Chỉ số cũ: <strong>${result.soDienCu}</strong> kWh, 
                Chỉ số mới: <strong>${result.soDienMoi}</strong> kWh
                ${result.tienDien ? `<br>Tiền điện: <strong>${formatCurrency(result.tienDien)}</strong>` : ''}
            `;
            ocrResult.style.display = 'block';
            manualInput.style.display = 'block';
            
            // Tự động set ngày hôm nay
            const ngayInput = document.querySelector('[name="ngayThangDien"]');
            if (ngayInput && !ngayInput.value) {
                ngayInput.value = new Date().toISOString().split('T')[0];
            }
        } else {
            throw new Error('Không đọc được chỉ số từ ảnh');
        }
    } catch (error) {
        console.error('OCR Error:', error);
        ocrResult.className = 'mt-2 alert alert-warning';
        ocrResult.innerHTML = `
            <i class="fas fa-exclamation-triangle"></i> 
            ${error.message || 'Không thể đọc chỉ số tự động. Vui lòng nhập thủ công.'}
        `;
        ocrResult.style.display = 'block';
        manualInput.style.display = 'block';
        
        // Lấy chỉ số cũ từ lần ghi trước
        try {
            const lastData = await authFetch('/api/ChiSoDien');
            const phongDien = lastData.filter(d => d.maPhong == maPhong);
            if (phongDien.length > 0) {
                const last = phongDien.sort((a, b) => new Date(b.ngayThangDien) - new Date(a.ngayThangDien))[0];
                document.getElementById('soDienCu').value = last.soDienMoi || 0;
            }
        } catch (e) {
            document.getElementById('soDienCu').value = 0;
        }
    }
}

// 4. Lưu chỉ số điện (Thêm mới / Cập nhật)
saveDienBtn.addEventListener("click", async () => {
    // Validate Form HTML5
    if (!addDienForm.checkValidity()) {
        addDienForm.classList.add('was-validated');
        return;
    }

    // Chỉ bắt buộc upload ảnh khi thêm mới, không bắt buộc khi edit
    const fileInput = document.getElementById('anhDienInput');
    if (!isEditing && (!fileInput || !fileInput.files[0])) {
        showAlert('Vui lòng upload ảnh đồng hồ điện', 'warning');
        return;
    }

    try {
        const maPhong = document.querySelector('select[name="maPhong"]').value;
        const soDienCu = parseInt(document.getElementById('soDienCu').value) || 0;
        const soDienMoi = parseInt(document.getElementById('soDienMoi').value) || 0;
        const ngayThang = document.querySelector('[name="ngayThangDien"]').value || new Date().toISOString().split('T')[0];

        // Validate logic nghiệp vụ Client
        if (soDienMoi <= soDienCu) {
            throw new Error("Chỉ số mới phải lớn hơn chỉ số cũ");
        }

        // Chuẩn bị FormData để gửi đi
        const apiFormData = new FormData();
        apiFormData.append('MaPhong', maPhong);
        apiFormData.append('SoDienCu', soDienCu);
        apiFormData.append('SoDienMoi', soDienMoi);
        apiFormData.append('NgayThangDien', ngayThang);

        // Nếu API cần MaDien khi update
        if (isEditing && editingDienId) {
            apiFormData.append('MaDien', editingDienId);
        }

        // Xử lý ảnh - convert sang base64
        if (fileInput.files[0]) {
            const base64String = await toBase64(fileInput.files[0]);
            apiFormData.append('AnhChiSoDien', base64String);
        }

        // Gọi API
        const url = isEditing
            ? `/api/ChiSoDien/${editingDienId}`
            : '/api/ChiSoDien';

        const method = isEditing ? 'PUT' : 'POST';

        await authFetch(url, {
            method: method,
            body: apiFormData
        });

        showAlert(isEditing ? "Cập nhật thành công" : "Thêm mới thành công", "success");

        // Reset form & reload
        const modal = bootstrap.Modal.getInstance(document.getElementById("addDienModal"));
        modal.hide();
        resetForm();
        loadChiSoDien();

    } catch (error) {
        console.error("Lỗi lưu chỉ số điện:", error);
        showAlert(error.message, "danger");
    }
});

// 5. Chuẩn bị dữ liệu để Edit
async function editChiSoDien(id) {
    try {
        const data = await authFetch(`/api/ChiSoDien/${id}`);

        // Fill dữ liệu vào form
        const form = document.getElementById("addDienForm");
        form.querySelector('[name="maPhong"]').value = data.maPhong;
        document.getElementById('soDienCu').value = data.soDienCu;
        document.getElementById('soDienMoi').value = data.soDienMoi;

        // Format ngày cho input datetime-local hoặc date
        if (data.ngayThangDien) {
            form.querySelector('[name="ngayThangDien"]').value = formatDateForInput(data.ngayThangDien);
        }

        // Hiển thị manual input khi edit
        const manualInput = document.getElementById('manualInputDien');
        if (manualInput) manualInput.style.display = 'block';

        // Ẩn preview và OCR result
        const previewDiv = document.getElementById('imagePreviewDien');
        const ocrResult = document.getElementById('ocrResultDien');
        if (previewDiv) previewDiv.style.display = 'none';
        if (ocrResult) ocrResult.style.display = 'none';

        // Ẩn required cho upload ảnh khi edit
        const anhDienRequired = document.getElementById('anhDienRequired');
        if (anhDienRequired) anhDienRequired.style.display = 'none';
        
        const anhDienInput = document.getElementById('anhDienInput');
        if (anhDienInput) {
            anhDienInput.required = false;
        }

        isEditing = true;
        editingDienId = id;

        // Mở modal
        const modal = new bootstrap.Modal(document.getElementById("addDienModal"));
        modal.show();

    } catch (error) {
        showAlert("Không thể tải chi tiết chỉ số điện", "danger");
    }
}

// 6. Xóa chỉ số điện
async function deleteChiSoDien(id) {
    if (!confirm("Bạn có chắc chắn muốn xóa?")) return;
    try {
        await authFetch(`/api/ChiSoDien/${id}`, { method: 'DELETE' });
        showAlert("Đã xóa thành công", "success");
        loadChiSoDien();
    } catch (error) {
        showAlert(error.message, "danger");
    }
}

// --- UI DISPLAY FUNCTIONS ---

function displayChiSoDien(data) {
    dienList.innerHTML = "";
    if (!data || data.length === 0) {
        dienList.innerHTML = '<div class="col-12 text-center">Chưa có dữ liệu</div>';
        return;
    }

    data.forEach(item => {
        const imageHtml = item.anhChiSoDien
            ? `<img src="${item.anhChiSoDien}" class="card-img-top" style="height: 150px; object-fit: cover;" onerror="this.remove();">`
            : '';
        const html = `
            <div class="col-md-4 mb-4">
                <div class="card h-100 shadow-sm">
                    ${imageHtml}
                    <div class="card-body">
                        <h5 class="card-title text-primary">${item.tenPhong || 'Phòng ?'}</h5>
                        <div class="card-text small">
                            <p class="mb-1"><i class="fas fa-calendar-alt"></i> ${formatDate(item.ngayThangDien)}</p>
                            <div class="d-flex justify-content-between border-bottom pb-2 mb-2">
                                <span>Mới: <strong>${item.soDienMoi}</strong></span>
                                <span>Cũ: <strong>${item.soDienCu}</strong></span>
                            </div>
                            <p class="mb-1">Tiêu thụ: <span class="badge bg-info text-dark">${item.soDienTieuThu} kWh</span></p>
                            <h6 class="mt-2 text-danger fw-bold">Tổng: ${formatCurrency(item.tienDien)}</h6>
                        </div>
                    </div>
                    <div class="card-footer bg-white border-top-0 d-flex justify-content-end gap-2">
                         <button class="btn btn-sm btn-outline-primary" onclick="editChiSoDien(${item.maDien})">
                            <i class="fas fa-edit"></i>
                        </button>
                        <button class="btn btn-sm btn-outline-danger" onclick="deleteChiSoDien(${item.maDien})">
                            <i class="fas fa-trash"></i>
                        </button>
                    </div>
                </div>
            </div>
        `;
        dienList.insertAdjacentHTML('beforeend', html);
    });
}

// Hàm hiển thị bảng giá điện với nút edit/delete
function displayGiaDien(data) {
    const tableBody = document.getElementById('giaDienTableBody');
    if (!tableBody) return; // Tránh lỗi nếu trang không có bảng này

    tableBody.innerHTML = '';
    if (!data || data.length === 0) {
        tableBody.innerHTML = '<tr><td colspan="4" class="text-center text-muted">Chưa có dữ liệu giá điện</td></tr>';
        return;
    }

    data.forEach(gd => {
        const bacDien = gd.bacDien || gd.BacDien;
        const giaTienDien = gd.giaTienDien || gd.GiaTienDien;
        const tuSoDien = gd.tuSoDien || gd.TuSoDien;
        const denSoDien = gd.denSoDien || gd.DenSoDien;
        const maGiaDien = gd.maGiaDien || gd.MaGiaDien;
        
        const row = `
            <tr>
                <td>${bacDien}</td>
                <td class="text-end">${formatCurrency(giaTienDien)}</td>
                <td class="text-center">${tuSoDien} - ${denSoDien}</td>
                <td>
                    <button class="btn btn-sm btn-outline-primary me-1" onclick="editGiaDien(${maGiaDien})" title="Sửa">
                        <i class="fas fa-edit"></i>
                    </button>
                    <button class="btn btn-sm btn-outline-danger" onclick="deleteGiaDien(${maGiaDien})" title="Xóa">
                        <i class="fas fa-trash"></i>
                    </button>
                </td>
            </tr>
        `;
        tableBody.insertAdjacentHTML('beforeend', row);
    });
}

// --- UTILS ---

function openAddDienModal() {
    resetForm();
    isEditing = false;
    editingDienId = null;
    
    // Hiển thị required cho upload ảnh khi thêm mới
    const anhDienRequired = document.getElementById('anhDienRequired');
    if (anhDienRequired) anhDienRequired.style.display = 'inline';
    
    const anhDienInput = document.getElementById('anhDienInput');
    if (anhDienInput) {
        anhDienInput.required = true;
    }
    
    const modal = new bootstrap.Modal(document.getElementById("addDienModal"));
    modal.show();
}

function resetForm() {
    addDienForm.reset();
    addDienForm.classList.remove('was-validated');
    isEditing = false;
    editingDienId = null;
    const fileInput = document.getElementById('anhDienInput');
    if (fileInput) fileInput.value = '';
    
    // Reset preview và OCR result
    const previewDiv = document.getElementById('imagePreviewDien');
    const ocrLoading = document.getElementById('ocrLoadingDien');
    const ocrResult = document.getElementById('ocrResultDien');
    const manualInput = document.getElementById('manualInputDien');
    
    if (previewDiv) previewDiv.style.display = 'none';
    if (ocrLoading) ocrLoading.style.display = 'none';
    if (ocrResult) {
        ocrResult.style.display = 'none';
        ocrResult.innerHTML = '';
    }
    if (manualInput) manualInput.style.display = 'none';
}

function formatDate(dateStr) {
    if (!dateStr) return '';
    return new Date(dateStr).toLocaleDateString('vi-VN', {
        day: '2-digit', month: '2-digit', year: 'numeric'
    });
}

function formatDateForInput(dateStr) {
    if (!dateStr) return '';
    return new Date(dateStr).toISOString().split('T')[0];
}

function formatCurrency(val) {
    return new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(val);
}

function showAlert(msg, type = 'success') {
    // Logic tạo alert Bootstrap
    const div = document.createElement('div');
    div.className = `alert alert-${type} alert-dismissible fade show fixed-top m-3`;
    div.style.zIndex = '9999';
    div.innerHTML = `
        ${msg}
        <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
    `;
    document.body.appendChild(div);
    setTimeout(() => div.remove(), 3000);
}

// Helper convert file to Base64 (Nếu bạn muốn upload ảnh)
const toBase64 = file => new Promise((resolve, reject) => {
    const reader = new FileReader();
    reader.readAsDataURL(file);
    reader.onload = () => resolve(reader.result); // Kết quả sẽ là string base64
    reader.onerror = error => reject(error);
});

// =====================================
// QUẢN LÝ GIÁ ĐIỆN
// =====================================

// Mở modal thêm giá điện
window.openAddGiaDienModal = function() {
    resetGiaDienForm();
    const modal = new bootstrap.Modal(document.getElementById('addGiaDienModal'));
    modal.show();
};

// Reset form giá điện
function resetGiaDienForm() {
    document.getElementById('bacDien').value = '';
    document.getElementById('giaTienDien').value = '';
    document.getElementById('tuSoDien').value = '';
    document.getElementById('denSoDien').value = '';
    document.getElementById('addGiaDienForm').classList.remove('was-validated');
}

// Thêm giá điện mới
document.addEventListener('DOMContentLoaded', () => {
    const saveGiaDienBtn = document.getElementById('saveGiaDienBtn');
    if (saveGiaDienBtn) {
        saveGiaDienBtn.addEventListener('click', async () => {
            const form = document.getElementById('addGiaDienForm');
            if (!form.checkValidity()) {
                form.classList.add('was-validated');
                return;
            }

            try {
                const bacDien = parseInt(document.getElementById('bacDien').value);
                const giaTienDien = parseFloat(document.getElementById('giaTienDien').value);
                const tuSoDien = parseInt(document.getElementById('tuSoDien').value);
                const denSoDien = parseInt(document.getElementById('denSoDien').value);

                if (tuSoDien >= denSoDien) {
                    showAlert('Số điện bắt đầu phải nhỏ hơn số điện kết thúc', 'danger');
                    return;
                }

                const giaDien = {
                    BacDien: bacDien,
                    GiaTienDien: giaTienDien,
                    TuSoDien: tuSoDien,
                    DenSoDien: denSoDien
                };

                await authFetch('/api/GiaDien', {
                    method: 'POST',
                    body: JSON.stringify(giaDien)
                });

                showAlert('Thêm giá điện thành công', 'success');
                const modal = bootstrap.Modal.getInstance(document.getElementById('addGiaDienModal'));
                modal.hide();
                resetGiaDienForm();
                loadGiaDien();
            } catch (error) {
                console.error('Lỗi thêm giá điện:', error);
                showAlert(error.message || 'Không thể thêm giá điện', 'danger');
            }
        });
    }

    // Cập nhật giá điện
    const updateGiaDienBtn = document.getElementById('updateGiaDienBtn');
    if (updateGiaDienBtn) {
        updateGiaDienBtn.addEventListener('click', async () => {
            const form = document.getElementById('editGiaDienForm');
            if (!form.checkValidity()) {
                form.classList.add('was-validated');
                return;
            }

            try {
                const maGiaDien = parseInt(document.getElementById('editMaGiaDien').value);
                const bacDien = parseInt(document.getElementById('editBacDien').value);
                const giaTienDien = parseFloat(document.getElementById('editGiaTienDien').value);
                const tuSoDien = parseInt(document.getElementById('editTuSoDien').value);
                const denSoDien = parseInt(document.getElementById('editDenSoDien').value);

                if (tuSoDien >= denSoDien) {
                    showAlert('Số điện bắt đầu phải nhỏ hơn số điện kết thúc', 'danger');
                    return;
                }

                const giaDien = {
                    MaGiaDien: maGiaDien,
                    BacDien: bacDien,
                    GiaTienDien: giaTienDien,
                    TuSoDien: tuSoDien,
                    DenSoDien: denSoDien
                };

                await authFetch(`/api/GiaDien/${maGiaDien}`, {
                    method: 'PUT',
                    body: JSON.stringify(giaDien)
                });

                showAlert('Cập nhật giá điện thành công', 'success');
                const modal = bootstrap.Modal.getInstance(document.getElementById('editGiaDienModal'));
                modal.hide();
                loadGiaDien();
            } catch (error) {
                console.error('Lỗi cập nhật giá điện:', error);
                showAlert(error.message || 'Không thể cập nhật giá điện', 'danger');
            }
        });
    }
});

// Sửa giá điện
window.editGiaDien = async function(id) {
    try {
        const data = await authFetch(`/api/GiaDien/${id}`);
        
        document.getElementById('editMaGiaDien').value = data.maGiaDien || data.MaGiaDien;
        document.getElementById('editBacDien').value = data.bacDien || data.BacDien;
        document.getElementById('editGiaTienDien').value = data.giaTienDien || data.GiaTienDien;
        document.getElementById('editTuSoDien').value = data.tuSoDien || data.TuSoDien;
        document.getElementById('editDenSoDien').value = data.denSoDien || data.DenSoDien;

        const modal = new bootstrap.Modal(document.getElementById('editGiaDienModal'));
        modal.show();
    } catch (error) {
        console.error('Lỗi tải thông tin giá điện:', error);
        showAlert('Không thể tải thông tin giá điện', 'danger');
    }
};

// Xóa giá điện
window.deleteGiaDien = async function(id) {
    if (!confirm('Bạn có chắc chắn muốn xóa giá điện này?')) {
        return;
    }

    try {
        await authFetch(`/api/GiaDien/${id}`, { method: 'DELETE' });
        showAlert('Xóa giá điện thành công', 'success');
        loadGiaDien();
    } catch (error) {
        console.error('Lỗi xóa giá điện:', error);
        showAlert(error.message || 'Không thể xóa giá điện', 'danger');
    }
};