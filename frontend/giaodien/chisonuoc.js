// --- CONFIG & CONSTANTS ---
const API_URL = Config.API_URL;

// DOM Elements
const nuocList = document.getElementById("nuocList");
const addNuocForm = document.getElementById("addNuocForm");
const saveNuocBtn = document.getElementById("saveNuocBtn");
const phongSelect = document.querySelector('select[name="maPhong"]');

// State
let isEditing = false;
let editingNuocId = null;

// --- INITIALIZATION ---
document.addEventListener("DOMContentLoaded", () => {
    loadChiSoNuoc();
    loadPhong();
    loadGiaNuoc(); // hiển thị bảng giá để tham khảo
    
    // Setup image upload và OCR
    const anhNuocInput = document.getElementById('anhNuocInput');
    if (anhNuocInput) {
        anhNuocInput.addEventListener('change', handleImageUploadNuoc);
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

// --- MAIN FUNCTIONS (CHI SO NUOC) ---

// 1. Load danh sách chỉ số nước
async function loadChiSoNuoc() {
    try {
        const data = await authFetch('/api/ChiSoNuoc');
        displayChiSoNuoc(data);
    } catch (error) {
        console.error("Lỗi tải chỉ số nước:", error);
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
            option.value = phong.maPhong || phong.MaPhong;
            option.textContent = phong.tenPhong || phong.TenPhong;
            phongSelect.appendChild(option);
        });
    } catch (error) {
        console.error("Lỗi tải danh sách phòng:", error);
        showAlert(error.message, "danger");
    }
}

// 3. Load danh sách giá nước
async function loadGiaNuoc() {
    try {
        const data = await authFetch('/api/GiaNuoc');
        displayGiaNuoc(data);
    } catch (error) {
        console.error("Lỗi tải giá nước:", error);
        showAlert(error.message, "danger");
    }
}

// Xử lý upload ảnh và đọc chỉ số tự động
async function handleImageUploadNuoc(event) {
    const file = event.target.files[0];
    if (!file) return;

    const maPhong = document.querySelector('select[name="maPhong"]').value;
    if (!maPhong) {
        showAlert('Vui lòng chọn phòng trước', 'warning');
        event.target.value = '';
        return;
    }

    // Hiển thị preview ảnh
    const previewDiv = document.getElementById('imagePreviewNuoc');
    const previewImg = document.getElementById('previewImgNuoc');
    const ocrLoading = document.getElementById('ocrLoadingNuoc');
    const ocrResult = document.getElementById('ocrResultNuoc');
    const manualInput = document.getElementById('manualInputNuoc');

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

        const response = await fetch(`${API_URL}/api/ChiSoNuoc/upload`, {
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
        if (result.soNuocMoi !== undefined && result.soNuocCu !== undefined) {
            // Tự động điền vào form
            document.getElementById('soNuocCu').value = result.soNuocCu || 0;
            document.getElementById('soNuocMoi').value = result.soNuocMoi || 0;
            
            // Hiển thị kết quả
            ocrResult.className = 'mt-2 alert alert-success';
            ocrResult.innerHTML = `
                <i class="fas fa-check-circle"></i> 
                Đã đọc thành công! Chỉ số cũ: <strong>${result.soNuocCu}</strong> m³, 
                Chỉ số mới: <strong>${result.soNuocMoi}</strong> m³
                ${result.tienNuoc ? `<br>Tiền nước: <strong>${formatCurrency(result.tienNuoc)}</strong>` : ''}
            `;
            ocrResult.style.display = 'block';
            manualInput.style.display = 'block';
            
            // Tự động set ngày hôm nay
            const ngayInput = document.querySelector('[name="ngayThangNuoc"]');
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
            const lastData = await authFetch('/api/ChiSoNuoc');
            const phongNuoc = lastData.filter(d => d.maPhong == maPhong);
            if (phongNuoc.length > 0) {
                const last = phongNuoc.sort((a, b) => new Date(b.ngayThangNuoc) - new Date(a.ngayThangNuoc))[0];
                document.getElementById('soNuocCu').value = last.soNuocMoi || 0;
            }
        } catch (e) {
            document.getElementById('soNuocCu').value = 0;
        }
    }
}

// 4. Lưu chỉ số nước (Thêm mới / Cập nhật)
saveNuocBtn.addEventListener("click", async () => {
    // Validate Form HTML5
    if (!addNuocForm.checkValidity()) {
        addNuocForm.classList.add('was-validated');
        return;
    }

    // Chỉ bắt buộc upload ảnh khi thêm mới, không bắt buộc khi edit
    const fileInput = document.getElementById('anhNuocInput');
    if (!isEditing && (!fileInput || !fileInput.files[0])) {
        showAlert('Vui lòng upload ảnh đồng hồ nước', 'warning');
        return;
    }

    try {
        const maPhong = document.querySelector('select[name="maPhong"]').value;
        const soNuocCu = parseInt(document.getElementById('soNuocCu').value) || 0;
        const soNuocMoi = parseInt(document.getElementById('soNuocMoi').value) || 0;
        const ngayThang = document.querySelector('[name="ngayThangNuoc"]').value || new Date().toISOString().split('T')[0];

        // Validate logic nghiệp vụ Client
        if (soNuocMoi <= soNuocCu) {
            throw new Error("Chỉ số mới phải lớn hơn chỉ số cũ");
        }

        // Chuẩn bị FormData để gửi đi
        const apiFormData = new FormData();
        apiFormData.append('MaPhong', maPhong);
        apiFormData.append('SoNuocCu', soNuocCu);
        apiFormData.append('SoNuocMoi', soNuocMoi);
        apiFormData.append('NgayThangNuoc', ngayThang);

        // Nếu API cần MaNuoc khi update
        if (isEditing && editingNuocId) {
            apiFormData.append('MaNuoc', editingNuocId);
        }

        // Xử lý ảnh - convert sang base64
        if (fileInput.files[0]) {
            const base64String = await toBase64(fileInput.files[0]);
            apiFormData.append('AnhChiSoNuoc', base64String);
        }

        // Gọi API
        const url = isEditing
            ? `/api/ChiSoNuoc/${editingNuocId}`
            : '/api/ChiSoNuoc';

        const method = isEditing ? 'PUT' : 'POST';

        await authFetch(url, {
            method: method,
            body: apiFormData
        });

        showAlert(isEditing ? "Cập nhật thành công" : "Thêm mới thành công", "success");

        // Reset form & reload
        const modal = bootstrap.Modal.getInstance(document.getElementById("addNuocModal"));
        modal.hide();
        resetForm();
        loadChiSoNuoc();

    } catch (error) {
        console.error("Lỗi lưu chỉ số nước:", error);
        showAlert(error.message, "danger");
    }
});

// 5. Chuẩn bị dữ liệu để Edit
async function editChiSoNuoc(id) {
    try {
        const data = await authFetch(`/api/ChiSoNuoc/${id}`);

        // Fill dữ liệu vào form
        const form = document.getElementById("addNuocForm");
        form.querySelector('[name="maPhong"]').value = data.maPhong || data.MaPhong;
        document.getElementById('soNuocCu').value = data.soNuocCu || data.SoNuocCu;
        document.getElementById('soNuocMoi').value = data.soNuocMoi || data.SoNuocMoi;

        // Format ngày cho input datetime-local hoặc date
        if (data.ngayThangNuoc) {
            form.querySelector('[name="ngayThangNuoc"]').value = formatDateForInput(data.ngayThangNuoc);
        }

        // Hiển thị manual input khi edit
        const manualInput = document.getElementById('manualInputNuoc');
        if (manualInput) manualInput.style.display = 'block';

        // Ẩn preview và OCR result
        const previewDiv = document.getElementById('imagePreviewNuoc');
        const ocrResult = document.getElementById('ocrResultNuoc');
        if (previewDiv) previewDiv.style.display = 'none';
        if (ocrResult) ocrResult.style.display = 'none';

        // Ẩn required cho upload ảnh khi edit
        const anhNuocRequired = document.getElementById('anhNuocRequired');
        if (anhNuocRequired) anhNuocRequired.style.display = 'none';
        
        const anhNuocInput = document.getElementById('anhNuocInput');
        if (anhNuocInput) {
            anhNuocInput.required = false;
        }

        isEditing = true;
        editingNuocId = id;

        // Mở modal
        const modal = new bootstrap.Modal(document.getElementById("addNuocModal"));
        modal.show();

    } catch (error) {
        showAlert("Không thể tải chi tiết chỉ số nước", "danger");
    }
}

// 6. Xóa chỉ số nước
async function deleteChiSoNuoc(id) {
    if (!confirm("Bạn có chắc chắn muốn xóa?")) return;
    try {
        await authFetch(`/api/ChiSoNuoc/${id}`, { method: 'DELETE' });
        showAlert("Đã xóa thành công", "success");
        loadChiSoNuoc();
    } catch (error) {
        showAlert(error.message, "danger");
    }
}

// --- UI DISPLAY FUNCTIONS ---

function displayChiSoNuoc(data) {
    nuocList.innerHTML = "";
    if (!data || data.length === 0) {
        nuocList.innerHTML = '<div class="col-12 text-center">Chưa có dữ liệu</div>';
        return;
    }

    data.forEach(item => {
        const imageHtml = item.anhChiSoNuoc
            ? `<img src="${item.anhChiSoNuoc}" class="card-img-top" style="height: 150px; object-fit: cover;" onerror="this.remove();">`
            : '';
        const html = `
            <div class="col-md-4 mb-4">
                <div class="card h-100 shadow-sm">
                    ${imageHtml}
                    <div class="card-body">
                        <h5 class="card-title text-primary">${item.tenPhong || 'Phòng ?'}</h5>
                        <div class="card-text small">
                            <p class="mb-1"><i class="fas fa-calendar-alt"></i> ${formatDate(item.ngayThangNuoc)}</p>
                            <div class="d-flex justify-content-between border-bottom pb-2 mb-2">
                                <span>Mới: <strong>${item.soNuocMoi}</strong></span>
                                <span>Cũ: <strong>${item.soNuocCu}</strong></span>
                            </div>
                            <p class="mb-1">Tiêu thụ: <span class="badge bg-info text-dark">${item.soNuocTieuThu} m³</span></p>
                            <h6 class="mt-2 text-danger fw-bold">Tổng: ${formatCurrency(item.tienNuoc)}</h6>
                        </div>
                    </div>
                    <div class="card-footer bg-white border-top-0 d-flex justify-content-end gap-2">
                         <button class="btn btn-sm btn-outline-primary" onclick="editChiSoNuoc(${item.maNuoc})">
                            <i class="fas fa-edit"></i>
                        </button>
                        <button class="btn btn-sm btn-outline-danger" onclick="deleteChiSoNuoc(${item.maNuoc})">
                            <i class="fas fa-trash"></i>
                        </button>
                    </div>
                </div>
            </div>
        `;
        nuocList.insertAdjacentHTML('beforeend', html);
    });
}

// Hàm hiển thị bảng giá nước với nút edit/delete
function displayGiaNuoc(data) {
    const tableBody = document.getElementById('giaNuocTableBody');
    if (!tableBody) return; // Tránh lỗi nếu trang không có bảng này

    tableBody.innerHTML = '';
    if (!data || data.length === 0) {
        tableBody.innerHTML = '<tr><td colspan="4" class="text-center text-muted">Chưa có dữ liệu giá nước</td></tr>';
        return;
    }

    data.forEach(gd => {
        const bacNuoc = gd.bacNuoc || gd.BacNuoc;
        const giaTienNuoc = gd.giaTienNuoc || gd.GiaTienNuoc;
        const tuSoNuoc = gd.tuSoNuoc || gd.TuSoNuoc;
        const denSoNuoc = gd.denSoNuoc || gd.DenSoNuoc;
        const maGiaNuoc = gd.maGiaNuoc || gd.MaGiaNuoc;
        
        const row = `
            <tr>
                <td>${bacNuoc}</td>
                <td class="text-end">${formatCurrency(giaTienNuoc)}</td>
                <td class="text-center">${tuSoNuoc} - ${denSoNuoc}</td>
                <td>
                    <button class="btn btn-sm btn-outline-primary me-1" onclick="editGiaNuoc(${maGiaNuoc})" title="Sửa">
                        <i class="fas fa-edit"></i>
                    </button>
                    <button class="btn btn-sm btn-outline-danger" onclick="deleteGiaNuoc(${maGiaNuoc})" title="Xóa">
                        <i class="fas fa-trash"></i>
                    </button>
                </td>
            </tr>
        `;
        tableBody.insertAdjacentHTML('beforeend', row);
    });
}

// --- UTILS ---

function openAddNuocModal() {
    resetForm();
    isEditing = false;
    editingNuocId = null;
    
    // Hiển thị required cho upload ảnh khi thêm mới
    const anhNuocRequired = document.getElementById('anhNuocRequired');
    if (anhNuocRequired) anhNuocRequired.style.display = 'inline';
    
    const anhNuocInput = document.getElementById('anhNuocInput');
    if (anhNuocInput) {
        anhNuocInput.required = true;
    }
    
    const modal = new bootstrap.Modal(document.getElementById("addNuocModal"));
    modal.show();
}

function resetForm() {
    addNuocForm.reset();
    addNuocForm.classList.remove('was-validated');
    isEditing = false;
    editingNuocId = null;
    const fileInput = document.getElementById('anhNuocInput');
    if (fileInput) fileInput.value = '';
    
    // Reset preview và OCR result
    const previewDiv = document.getElementById('imagePreviewNuoc');
    const ocrLoading = document.getElementById('ocrLoadingNuoc');
    const ocrResult = document.getElementById('ocrResultNuoc');
    const manualInput = document.getElementById('manualInputNuoc');
    
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
// QUẢN LÝ GIÁ NƯỚC
// =====================================

// Mở modal thêm giá nước
window.openAddGiaNuocModal = function() {
    resetGiaNuocForm();
    const modal = new bootstrap.Modal(document.getElementById('addGiaNuocModal'));
    modal.show();
};

// Reset form giá nước
function resetGiaNuocForm() {
    document.getElementById('bacNuoc').value = '';
    document.getElementById('giaTienNuoc').value = '';
    document.getElementById('tuSoNuoc').value = '';
    document.getElementById('denSoNuoc').value = '';
    document.getElementById('addGiaNuocForm').classList.remove('was-validated');
}

// Thêm giá nước mới
document.addEventListener('DOMContentLoaded', () => {
    const saveGiaNuocBtn = document.getElementById('saveGiaNuocBtn');
    if (saveGiaNuocBtn) {
        saveGiaNuocBtn.addEventListener('click', async () => {
            const form = document.getElementById('addGiaNuocForm');
            if (!form.checkValidity()) {
                form.classList.add('was-validated');
                return;
            }

            try {
                const bacNuoc = parseInt(document.getElementById('bacNuoc').value);
                const giaTienNuoc = parseFloat(document.getElementById('giaTienNuoc').value);
                const tuSoNuoc = parseInt(document.getElementById('tuSoNuoc').value);
                const denSoNuoc = parseInt(document.getElementById('denSoNuoc').value);

                if (tuSoNuoc >= denSoNuoc) {
                    showAlert('Số nước bắt đầu phải nhỏ hơn số nước kết thúc', 'danger');
                    return;
                }

                const giaNuoc = {
                    BacNuoc: bacNuoc,
                    GiaTienNuoc: giaTienNuoc,
                    TuSoNuoc: tuSoNuoc,
                    DenSoNuoc: denSoNuoc
                };

                await authFetch('/api/GiaNuoc', {
                    method: 'POST',
                    body: JSON.stringify(giaNuoc)
                });

                showAlert('Thêm giá nước thành công', 'success');
                const modal = bootstrap.Modal.getInstance(document.getElementById('addGiaNuocModal'));
                modal.hide();
                resetGiaNuocForm();
                loadGiaNuoc();
            } catch (error) {
                console.error('Lỗi thêm giá nước:', error);
                showAlert(error.message || 'Không thể thêm giá nước', 'danger');
            }
        });
    }

    // Cập nhật giá nước
    const updateGiaNuocBtn = document.getElementById('updateGiaNuocBtn');
    if (updateGiaNuocBtn) {
        updateGiaNuocBtn.addEventListener('click', async () => {
            const form = document.getElementById('editGiaNuocForm');
            if (!form.checkValidity()) {
                form.classList.add('was-validated');
                return;
            }

            try {
                const maGiaNuoc = parseInt(document.getElementById('editMaGiaNuoc').value);
                const bacNuoc = parseInt(document.getElementById('editBacNuoc').value);
                const giaTienNuoc = parseFloat(document.getElementById('editGiaTienNuoc').value);
                const tuSoNuoc = parseInt(document.getElementById('editTuSoNuoc').value);
                const denSoNuoc = parseInt(document.getElementById('editDenSoNuoc').value);

                if (tuSoNuoc >= denSoNuoc) {
                    showAlert('Số nước bắt đầu phải nhỏ hơn số nước kết thúc', 'danger');
                    return;
                }

                const giaNuoc = {
                    MaGiaNuoc: maGiaNuoc,
                    BacNuoc: bacNuoc,
                    GiaTienNuoc: giaTienNuoc,
                    TuSoNuoc: tuSoNuoc,
                    DenSoNuoc: denSoNuoc
                };

                await authFetch(`/api/GiaNuoc/${maGiaNuoc}`, {
                    method: 'PUT',
                    body: JSON.stringify(giaNuoc)
                });

                showAlert('Cập nhật giá nước thành công', 'success');
                const modal = bootstrap.Modal.getInstance(document.getElementById('editGiaNuocModal'));
                modal.hide();
                loadGiaNuoc();
            } catch (error) {
                console.error('Lỗi cập nhật giá nước:', error);
                showAlert(error.message || 'Không thể cập nhật giá nước', 'danger');
            }
        });
    }
});

// Sửa giá nước
window.editGiaNuoc = async function(id) {
    try {
        const data = await authFetch(`/api/GiaNuoc/${id}`);
        
        document.getElementById('editMaGiaNuoc').value = data.maGiaNuoc || data.MaGiaNuoc;
        document.getElementById('editBacNuoc').value = data.bacNuoc || data.BacNuoc;
        document.getElementById('editGiaTienNuoc').value = data.giaTienNuoc || data.GiaTienNuoc;
        document.getElementById('editTuSoNuoc').value = data.tuSoNuoc || data.TuSoNuoc;
        document.getElementById('editDenSoNuoc').value = data.denSoNuoc || data.DenSoNuoc;

        const modal = new bootstrap.Modal(document.getElementById('editGiaNuocModal'));
        modal.show();
    } catch (error) {
        console.error('Lỗi tải thông tin giá nước:', error);
        showAlert('Không thể tải thông tin giá nước', 'danger');
    }
};

// Xóa giá nước
window.deleteGiaNuoc = async function(id) {
    if (!confirm('Bạn có chắc chắn muốn xóa giá nước này?')) {
        return;
    }

    try {
        await authFetch(`/api/GiaNuoc/${id}`, { method: 'DELETE' });
        showAlert('Xóa giá nước thành công', 'success');
        loadGiaNuoc();
    } catch (error) {
        console.error('Lỗi xóa giá nước:', error);
        showAlert(error.message || 'Không thể xóa giá nước', 'danger');
    }
};
