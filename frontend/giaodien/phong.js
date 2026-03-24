// ============================================================
// KHAI BÁO BIẾN VÀ DOM ELEMENTS
// ============================================================
const phongList = document.getElementById('phongList');
const addPhongForm = document.getElementById('addPhongForm');
const savePhongBtn = document.getElementById('savePhongBtn');
const searchInput = document.getElementById('searchInput');
const loaiPhongFilter = document.getElementById('loaiPhongFilter');
const trangThaiFilter = document.getElementById('trangThaiFilter');

// Biến lưu ID phòng đang sửa (null nếu là thêm mới)
let currentEditingId = null;

// Map trạng thái theo backend: 0 trống, 1 thuê, 2 bảo trì
const STATUS_MAP = {
    0: { text: 'Còn trống', className: 'status-available' },
    1: { text: 'Đang thuê', className: 'status-occupied' },
    2: { text: 'Bảo trì', className: 'status-maintenance' }
};

function getStatusInfo(statusValue) {
    return STATUS_MAP[statusValue] || { text: 'Không xác định', className: 'status-available' };
}

// ============================================================
// CÁC HÀM KHỞI TẠO & AUTH
// ============================================================

// Kiểm tra token trước khi load trang
function checkAuth() {
    const token = localStorage.getItem('token');
    if (!token) {
        window.location.href = 'login.html';
        return false;
    }
    return true;
}

// Load rooms when page loads
document.addEventListener('DOMContentLoaded', () => {
    if (!checkAuth()) return;
    
    loadPhong();
    loadLoaiPhong();
    populateTrangThaiSelects();
    loadNhaTro();
    setupEventListeners();
});

// Setup event listeners
function setupEventListeners() {
    if (!savePhongBtn) return;
    
    // Save button event - chỉ gắn một lần
    savePhongBtn.onclick = (e) => {
        e.preventDefault(); // Ngăn submit form mặc định
        console.log('Save button clicked');
        savePhong();
    };

    // Search and filter events
    if (searchInput) searchInput.addEventListener('input', filterPhong);
    if (loaiPhongFilter) loaiPhongFilter.addEventListener('change', filterPhong);
    if (trangThaiFilter) trangThaiFilter.addEventListener('change', filterPhong);
}

// ============================================================
// CÁC HÀM LOAD DỮ LIỆU TỪ API
// ============================================================

// Load all rooms
async function loadPhong() {
    try {
        const token = localStorage.getItem('token');
        const response = await fetch(`${Config.API_URL}${Config.ENDPOINTS.PHONG}`, {
            headers: {
                'Authorization': `Bearer ${token}`,
                'Accept': 'application/json'
            }
        });

        if (response.status === 401) {
            window.location.href = 'login.html';
            return;
        }

        if (!response.ok) throw new Error('Không thể tải danh sách phòng');

        const phong = await response.json();
        displayPhong(phong);
    } catch (error) {
        console.error('Error loading rooms:', error);
        showAlert(error.message || 'Không thể tải danh sách phòng', 'danger');
    }
}

// Load room types
async function loadLoaiPhong() {
    try {
        const response = await fetch(`${Config.API_URL}${Config.ENDPOINTS.LOAI_PHONG}`, {
            headers: { 'Authorization': `Bearer ${localStorage.getItem('token')}` }
        });
        if (!response.ok) throw new Error('Không thể tải danh sách loại phòng');
        const loaiPhong = await response.json();
        displayLoaiPhong(loaiPhong);
    } catch (error) {
        console.error('Error:', error);
    }
}

// Đổ option trạng thái cố định theo backend (0/1/2)
function populateTrangThaiSelects() {
    const formSelect = document.getElementById('maTrangThai');
    if (formSelect) {
        formSelect.innerHTML = `
            <option value="">Chọn trạng thái</option>
            <option value="0">Còn trống</option>
            <option value="1">Đang thuê</option>
            <option value="2">Bảo trì</option>
        `;
    }

    if (trangThaiFilter) {
        trangThaiFilter.innerHTML = `
            <option value="">-- Tất cả trạng thái --</option>
            <option value="0">Còn trống</option>
            <option value="1">Đang thuê</option>
            <option value="2">Bảo trì</option>
        `;
    }
}

// Load boarding houses
async function loadNhaTro() {
    try {
        const response = await fetch(`${Config.API_URL}${Config.ENDPOINTS.NHA_TRO}`, {
            headers: { 'Authorization': `Bearer ${localStorage.getItem('token')}` }
        });
        if (!response.ok) throw new Error('Không thể tải danh sách nhà trọ');
        const nhaTro = await response.json();
        displayNhaTro(nhaTro);
    } catch (error) {
        console.error('Error:', error);
    }
}

// ============================================================
// CÁC HÀM HIỂN THỊ GIAO DIỆN (UI)
// ============================================================

function displayPhong(phong) {
    phongList.innerHTML = '';
    let countRented = 0;
    phong.forEach(room => {
        if (room.trangThai === 1) countRented++;
        const roomCard = createPhongCard(room);
        phongList.appendChild(roomCard);
    });
    
    const rentedCountEl = document.getElementById('rentedRoomCount');
    if (rentedCountEl) rentedCountEl.textContent = countRented;
}

function displayLoaiPhong(loaiPhong) {
    const maLoaiPhongSelect = document.getElementById('maLoaiPhong');
    if (!maLoaiPhongSelect) return;
    
    maLoaiPhongSelect.innerHTML = '<option value="">Chọn loại phòng</option>';
    loaiPhong.forEach(type => {
        maLoaiPhongSelect.innerHTML += `<option value="${type.maLoaiPhong}">${type.tenLoaiPhong}</option>`;
    });
}

function displayNhaTro(nhaTro) {
    const maNhaTroSelect = document.getElementById('maNhaTro');
    if (!maNhaTroSelect) return;
    
    maNhaTroSelect.innerHTML = '<option value="">Chọn nhà trọ</option>';
    nhaTro.forEach(nt => {
        maNhaTroSelect.innerHTML += `<option value="${nt.maNhaTro}" data-diachi="${nt.diaChi || ''}">${nt.tenNhaTro}</option>`;
    });
}

// Create a room card element
// FILE: phong.js

// Create a room card element
function createPhongCard(room) {
    const col = document.createElement('div');
    col.className = 'col-md-4';
    col.dataset.status = room.trangThai ?? '';
    col.dataset.loaiPhong = room.loaiPhong?.maLoaiPhong || '';

    const statusInfo = getStatusInfo(room.trangThai);

    // Xử lý ảnh đại diện
    let avatarImg = `<img src="default-room.jpg" alt="Default" class="room-image">`;
    if (Array.isArray(room.hinhAnh) && room.hinhAnh.length > 0) {
        avatarImg = `<img src="${room.hinhAnh[0]}" alt="${room.tenPhong}" class="room-image" onerror="this.src='default-room.jpg';">`;
    } else if (typeof room.hinhAnh === 'string' && room.hinhAnh) {
         avatarImg = `<img src="${room.hinhAnh}" alt="${room.tenPhong}" class="room-image" onerror="this.src='default-room.jpg';">`;
    }

    col.innerHTML = `
        <div class="card room-card">
            <div class="card-body">
                ${avatarImg}
                <div class="d-flex justify-content-between align-items-center mb-2">
                    <h5 class="card-title mb-0">Phòng: ${room.tenPhong}</h5>
                    <span class="status-badge ${statusInfo.className}">${statusInfo.text}</span>
                </div>
                
                <p class="card-text small">
                    <strong><i class="fas fa-home"></i></strong> ${room.nhaTro?.tenNhaTro || 'N/A'}<br>
                    <strong><i class="fas fa-layer-group"></i></strong> ${room.loaiPhong?.tenLoaiPhong || 'N/A'}<br>
                    <strong><i class="fas fa-money-bill"></i></strong> ${formatCurrency(room.giaPhong)}<br>
                    <strong><i class="fas fa-users"></i></strong> ${room.sucChua} người - 
                    <strong><i class="fas fa-ruler-combined"></i></strong> ${room.dienTich ? room.dienTich + ' m²' : 'N/A'}
                </p>
                
               <div class="d-flex gap-2 mb-2">
                    <button class="btn btn-warning btn-sm fw-bold text-dark flex-grow-1" onclick="moCuaTuXa(${room.maPhong}, '${room.tenPhong}')">
                        <i class="fas fa-unlock"></i> Mở
                    </button>
                    
                    <button class="btn btn-secondary btn-sm fw-bold flex-grow-1" onclick="dongCuaTuXa(${room.maPhong}, '${room.tenPhong}')">
                        <i class="fas fa-lock"></i> Đóng
                    </button>
                </div>
                <div class="btn-group w-100 btn-group-sm">
                    <button class="btn btn-outline-info" onclick="viewPhong(${room.maPhong})">
                        <i class="fas fa-eye"></i> Xem
                    </button>
                    <button class="btn btn-outline-primary" onclick="editPhong(${room.maPhong})">
                        <i class="fas fa-edit"></i> Sửa
                    </button>
                    <button class="btn btn-outline-danger" onclick="deletePhong(${room.maPhong})">
                        <i class="fas fa-trash"></i> Xóa
                    </button>
                </div>
            </div>
        </div>
    `;
    return col;
}

// Preview image before upload
function previewImage(input) {
    const file = input.files[0];
    if (file) {
        const reader = new FileReader();
        reader.onload = function (e) {
            const preview = document.getElementById('imagePreview');
            preview.style.display = 'block';
            // Xóa nội dung cũ để hiển thị ảnh mới chọn
            preview.innerHTML = `<img src="${e.target.result}" style="width:100px; height:100px; object-fit:cover; border-radius:4px;">`;
        };
        reader.readAsDataURL(file);
    }
}

// Format currency
function formatCurrency(amount) {
    return new Intl.NumberFormat('vi-VN', {
        style: 'currency',
        currency: 'VND'
    }).format(amount);
}

// ============================================================
// CÁC HÀM CRUD (CREATE, READ, UPDATE, DELETE)
// ============================================================

// Chuẩn bị form để tạo phòng mới
function createPhong() {
    currentEditingId = null;
    addPhongForm.reset();
    
    // Ẩn và xóa preview ảnh
    const imagePreview = document.getElementById('imagePreview');
    if (imagePreview) {
        imagePreview.style.display = 'none';
        imagePreview.innerHTML = '';
    }
    
    const modalTitle = document.getElementById('modalTitle');
    if (modalTitle) modalTitle.textContent = 'Thêm phòng mới';
    
    const modal = new bootstrap.Modal(document.getElementById('addPhongModal'));
    modal.show();
}

// Hàm SAVE PHONG (Quan trọng: Đã tích hợp logic Check Hợp Đồng)
async function savePhong() {
    console.log('savePhong function called');
    
    // 1. Validate Form
    if (!addPhongForm.checkValidity()) {
        console.log('Form validation failed');
        addPhongForm.reportValidity();
        return;
    }
    const formData = new FormData(addPhongForm);
    
    // 2. Lấy dữ liệu Text từ Select Box
    const nhaTroSelect = document.getElementById('maNhaTro');
    const selectedNhaTro = nhaTroSelect.options[nhaTroSelect.selectedIndex];
    
    const loaiPhongSelect = document.getElementById('maLoaiPhong');
    const selectedLoaiPhong = loaiPhongSelect.options[loaiPhongSelect.selectedIndex];
    
    const trangThaiSelect = document.getElementById('maTrangThai');
    const selectedTrangThai = trangThaiSelect.options[trangThaiSelect.selectedIndex];

    const STATUS_TRONG = 0;
    const STATUS_THUE = 1;

    let finalTrangThai = parseInt(formData.get('maTrangThai'));

    // 3. LOGIC TỰ ĐỘNG CẬP NHẬT TRẠNG THÁI THEO HỢP ĐỒNG (Chỉ chạy khi đang Sửa)
    if (currentEditingId) {
        try {
            console.log('Đang kiểm tra hạn hợp đồng...');
            
            // Gọi API lấy hợp đồng mới nhất của phòng
            // API: GET /api/HopDong/GetLatestByPhong/{id}
            const resHD = await fetch(`${Config.API_URL}/HopDong/Phong/${currentEditingId}`, {
                headers: { 'Authorization': `Bearer ${localStorage.getItem('token')}` }
            });

            if (resHD.ok) {
                const hdData = await resHD.json();
                console.log(resHD)
                console.log('Hợp đồng nhận được:', hdData);
                // Nếu có hợp đồng và có ngày kết thúc
                if (hdData && hdData.NgayKetThuc) {
                    const ngayKetThuc = new Date(hdData.NgayKetThuc);
                    const ngayHienTai = new Date();
                    
                    // Reset giờ về 0 để so sánh ngày
                    ngayKetThuc.setHours(0,0,0,0);
                    ngayHienTai.setHours(0,0,0,0);

                    console.log(`HĐ kết thúc: ${ngayKetThuc.toLocaleDateString()} vs Hôm nay: ${ngayHienTai.toLocaleDateString()}`);

                    if (ngayKetThuc >= ngayHienTai) {
                        finalTrangThai = STATUS_THUE;
                        console.log('=> Trạng thái: ĐANG THUÊ');
                    } else {
                        finalTrangThai = STATUS_TRONG;
                        console.log('=> Trạng thái: CÒN TRỐNG');
                    }
                } else {
                    // Không có hợp đồng -> Còn trống
                    finalTrangThai = STATUS_TRONG;
                }
            }
        } catch (err) {
            console.warn('Lỗi kiểm tra hợp đồng (có thể do API chưa sẵn sàng):', err);
            // Giữ nguyên trạng thái user chọn nếu API lỗi
        }
    }

    // 4. Tạo Object để lưu
    const roomData = {
        maNhaTro: parseInt(formData.get('maNhaTro')),
        maLoaiPhong: parseInt(formData.get('maLoaiPhong')),
        trangThai: finalTrangThai, 
        tenPhong: formData.get('tenPhong'),
        dienTich: formData.get('dienTich') ? parseFloat(formData.get('dienTich')) : null,
        giaPhong: parseFloat(formData.get('giaPhong')),
        sucChua: parseInt(formData.get('sucChua')),
        moTa: formData.get('moTa') || null,
        hinhAnh: [], 
        nhaTro: {
            maNhaTro: parseInt(formData.get('maNhaTro')),
            tenNhaTro: selectedNhaTro.text,
            diaChi: selectedNhaTro.getAttribute('data-diachi') || ''
        },
        loaiPhong: {
            maLoaiPhong: parseInt(formData.get('maLoaiPhong')),
            tenLoaiPhong: selectedLoaiPhong.text
        }
    };

    console.log('Data to save:', roomData);

    try {
        // 5. Upload hình ảnh (nếu có chọn file mới)
        const imageFiles = formData.getAll('hinhAnh');
        if (imageFiles && imageFiles.length > 0 && imageFiles[0].size > 0) {
            for (const file of imageFiles) {
                const imageFormData = new FormData();
                imageFormData.append('file', file);
                const uploadResponse = await fetch(`${Config.API_URL}${Config.ENDPOINTS.PHONG}/UploadImage`, {
                    method: 'POST',
                    headers: {
                        'Authorization': `Bearer ${localStorage.getItem('token')}`,
                        'Accept': 'application/json'
                    },
                    body: imageFormData
                });
                if (!uploadResponse.ok) {
                    const errorText = await uploadResponse.text();
                    throw new Error('Không thể upload ảnh: ' + errorText);
                }
                const uploadResult = await uploadResponse.json();
                roomData.hinhAnh.push(uploadResult.url);
            }
        }

        // 6. Gọi hàm lưu xuống DB
        await saveRoomData(roomData);
    } catch (error) {
        console.error('Error saving room:', error);
        showAlert(error.message || 'Không thể lưu thông tin phòng', 'danger');
    }
}

// Hàm gửi request API (POST hoặc PUT)
async function saveRoomData(roomData) {
    try {
        const url = currentEditingId 
            ? `${Config.API_URL}${Config.ENDPOINTS.PHONG}/${currentEditingId}`
            : `${Config.API_URL}${Config.ENDPOINTS.PHONG}`;
            
        if (currentEditingId) {
            roomData.maPhong = currentEditingId;
        }
        
        const response = await fetch(url, {
            method: currentEditingId ? 'PUT' : 'POST',
            headers: { 
                'Content-Type': 'application/json',
                'Accept': 'application/json',
                'Authorization': `Bearer ${localStorage.getItem('token')}`
            },
            body: JSON.stringify(roomData)
        });

        if (!response.ok) {
            const errorText = await response.text();
            throw new Error(errorText || 'Không thể lưu thông tin phòng');
        }

        showAlert(currentEditingId ? 'Cập nhật phòng thành công' : 'Thêm phòng thành công', 'success');
        
        // Đóng modal và reset form
        const modalEl = document.getElementById('addPhongModal');
        const modal = bootstrap.Modal.getInstance(modalEl);
        if (modal) modal.hide();
        
        addPhongForm.reset();
        document.getElementById('imagePreview').style.display = 'none';
        
        // Load lại danh sách
        loadPhong();
    } catch (error) {
        throw error; // Ném lỗi ra để hàm savePhong catch
    }
}

// Hàm mở form Sửa phòng
async function editPhong(roomId) {
    try {
        const response = await fetch(`${Config.API_URL}${Config.ENDPOINTS.PHONG}/${roomId}`, {
            headers: {
                'Authorization': `Bearer ${localStorage.getItem('token')}`,
                'Accept': 'application/json'
            }
        });
        if (!response.ok) throw new Error('Không thể tải thông tin phòng');
        
        const room = await response.json();
        
        // Set ID đang sửa
        currentEditingId = roomId;

        // Điền dữ liệu vào Form
        addPhongForm.maNhaTro.value = room.maNhaTro;
        addPhongForm.maLoaiPhong.value = room.maLoaiPhong;
        addPhongForm.maTrangThai.value = room.trangThai ?? '';
        addPhongForm.tenPhong.value = room.tenPhong;
        addPhongForm.dienTich.value = room.dienTich || '';
        addPhongForm.giaPhong.value = room.giaPhong || '';
        addPhongForm.sucChua.value = room.sucChua || '';
        addPhongForm.moTa.value = room.moTa || '';
        
        // Reset file input (Browser security không cho gán value)
        addPhongForm.hinhAnh.value = ''; 

        // Hiển thị ảnh cũ
        const imagePreview = document.getElementById('imagePreview');
        imagePreview.style.display = 'block';
        imagePreview.innerHTML = '<p class="text-muted small mb-2">Ảnh hiện tại:</p>';
        
        // Xử lý hiển thị mảng ảnh hoặc chuỗi ảnh
        const images = Array.isArray(room.hinhAnh) ? room.hinhAnh : (room.hinhAnh ? [room.hinhAnh] : []);
        
        if (images.length > 0) {
            images.forEach(url => {
                const img = document.createElement('img');
                img.src = url;
                img.style.width = '100px';
                img.style.height = '100px';
                img.style.objectFit = 'cover';
                img.style.borderRadius = '4px';
                img.style.marginRight = '5px';
                img.style.marginBottom = '5px';
                imagePreview.appendChild(img);
            });
        } else {
            imagePreview.innerHTML += '<span class="text-muted">Chưa có ảnh</span>';
        }

        // Đổi tên modal
        const modalTitle = document.getElementById('modalTitle');
        if (modalTitle) modalTitle.textContent = 'Sửa thông tin phòng';

        // Mở modal
        const modal = new bootstrap.Modal(document.getElementById('addPhongModal'));
        modal.show();
    } catch (error) {
        console.error('Error loading room details:', error);
        showAlert('Không thể tải thông tin phòng', 'danger');
    }
}

// Xóa phòng
async function deletePhong(phongId) {
    if (!confirm('Bạn có chắc chắn muốn xóa phòng này?')) return;

    try {
        const response = await fetch(`${Config.API_URL}${Config.ENDPOINTS.PHONG}/${phongId}`, {
            method: 'DELETE',
            headers: {
                'Authorization': `Bearer ${localStorage.getItem('token')}`,
                'Content-Type': 'application/json'
            }
        });

        if (!response.ok) {
            const errorText = await response.text();
            if (errorText.includes('FK_')) {
                throw new Error('Không thể xóa vì đang có dữ liệu liên quan (Hợp đồng, v.v)');
            }
            throw new Error(errorText || 'Lỗi khi xóa phòng');
        }

        showAlert('Xóa phòng thành công', 'success');
        loadPhong();
    } catch (error) {
        console.error('Error deleting room:', error);
        showAlert(error.message, 'danger');
    }
}

// ============================================================
// CÁC HÀM TIỆN ÍCH KHÁC (FILTER, VIEW, ALERT)
// ============================================================

function filterPhong() {
    const searchTerm = searchInput.value.toLowerCase();
    const selectedLoaiPhong = loaiPhongFilter.value;
    const selectedTrangThai = trangThaiFilter.value;

    const cards = phongList.getElementsByClassName('col-md-4');
    
    Array.from(cards).forEach(card => {
        const roomInfo = card.textContent.toLowerCase();
        const matchesSearch = searchTerm === '' || roomInfo.includes(searchTerm);

        const matchesLoaiPhong = selectedLoaiPhong === '' || card.dataset.loaiPhong === selectedLoaiPhong;
        const matchesTrangThai = selectedTrangThai === '' || card.dataset.status === selectedTrangThai;

        card.style.display = (matchesSearch && matchesLoaiPhong && matchesTrangThai) ? '' : 'none';
    });
}

function showAlert(message, type) {
    const alertDiv = document.createElement('div');
    alertDiv.className = `alert alert-${type} alert-dismissible fade show position-fixed top-0 end-0 m-3`;
    alertDiv.style.zIndex = '9999';
    alertDiv.innerHTML = `
        ${message}
        <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
    `;
    document.body.appendChild(alertDiv);
    setTimeout(() => alertDiv.remove(), 3000);
}

// Xem chi tiết phòng
// FILE: phong.js

// Xem chi tiết phòng
window.viewPhong = async function(maPhong) {
    try {
        const response = await fetch(`${Config.API_URL}${Config.ENDPOINTS.PHONG}/${maPhong}`, {
            headers: {
                'Authorization': `Bearer ${localStorage.getItem('token')}`,
                'Accept': 'application/json'
            }
        });
        // ===> ĐOẠN CODE MỚI: TẢI LỊCH SỬ <===
        const historyTableBody = document.getElementById('historyTableBody');
        if (historyTableBody) {
            historyTableBody.innerHTML = '<tr><td colspan="4" class="text-center">Đang tải lịch sử...</td></tr>';

            // Gọi API lấy lịch sử
            // URL: /api/Phong/LichSu/{id}
            fetch(`${Config.API_URL}${Config.ENDPOINTS.PHONG}/LichSu/${maPhong}`, {
                headers: { 'Authorization': `Bearer ${localStorage.getItem('token')}` }
            })
            .then(res => res.json())
            .then(logs => {
                historyTableBody.innerHTML = ''; // Xóa chữ loading
                
                if (!logs || logs.length === 0) {
                    historyTableBody.innerHTML = '<tr><td colspan="4" class="text-center text-muted">Chưa có lịch sử nào.</td></tr>';
                    return;
                }

        // Duyệt danh sách và tạo dòng
        logs.forEach(log => {
            // Format thời gian cho đẹp (VD: 09/12/2025 10:30:00)
            const timeStr = new Date(log.thoiGian).toLocaleString('vi-VN');
            
            // Tô màu hành động (Mở màu xanh, Đóng màu đỏ)
            const actionClass = log.hanhDong.includes('MỞ') ? 'text-success fw-bold' : 'text-danger fw-bold';

            const row = `
                <tr>
                    <td>${timeStr}</td>
                    <td class="${actionClass}">${log.hanhDong}</td>
                    <td>${log.nguoiThucHien || 'Hệ thống'}</td>
                    <td class="text-muted fst-italic">${log.ghiChu || ''}</td>
                </tr>
            `;
            historyTableBody.innerHTML += row;
        });
    })
    .catch(err => {
        console.error(err);
        historyTableBody.innerHTML = '<tr><td colspan="4" class="text-danger text-center">Lỗi tải lịch sử.</td></tr>';
    });
}
        
        if (!response.ok) throw new Error('Không thể tải thông tin phòng');
        const room = await response.json();

        // 1. Hiển thị hình ảnh Carousel (Giữ nguyên logic cũ)
        const roomImages = document.getElementById('roomImages');
        roomImages.innerHTML = '';
        const images = Array.isArray(room.hinhAnh) ? room.hinhAnh : (room.hinhAnh ? [room.hinhAnh] : []);
        if (images.length > 0) {
            images.forEach((url, index) => {
                const div = document.createElement('div');
                div.className = `carousel-item ${index === 0 ? 'active' : ''}`;
                div.innerHTML = `<img src="${url}" class="d-block w-100" style="height: 400px; object-fit: cover;">`;
                roomImages.appendChild(div);
            });
        } else {
            roomImages.innerHTML = `<div class="carousel-item active"><img src="default-room.jpg" class="d-block w-100" style="height: 400px; object-fit: cover;"></div>`;
        }

        // 2. Điền thông tin Text (Giữ nguyên logic cũ)
        document.getElementById('roomName').textContent = room.tenPhong;
        document.getElementById('boardingHouseName').textContent = room.nhaTro?.tenNhaTro || 'N/A';
        document.getElementById('roomType').textContent = room.loaiPhong?.tenLoaiPhong || 'N/A';
        document.getElementById('roomPrice').textContent = formatCurrency(room.giaPhong);
        document.getElementById('roomCapacity').textContent = `${room.sucChua} người`;
        document.getElementById('roomArea').textContent = room.dienTich ? `${room.dienTich} m²` : 'N/A';
        document.getElementById('roomAddress').textContent = room.nhaTro?.diaChi || 'N/A';
        document.getElementById('roomDescription').textContent = room.moTa || 'Không có mô tả';
        
        const statusInfo = getStatusInfo(room.trangThai);
        const roomStatusEl = document.getElementById('roomStatus');
        if (roomStatusEl) roomStatusEl.textContent = statusInfo.text;

        // 3. Xử lý nút Sửa
        const editRoomBtn = document.getElementById('editRoomBtn');
        if (editRoomBtn) {
            editRoomBtn.onclick = () => {
                bootstrap.Modal.getInstance(document.getElementById('viewPhongModal')).hide();
                editPhong(maPhong);
            };
        }

        // ===> 4. XỬ LÝ NÚT MỞ KHÓA TRONG MODAL (MỚI) <===
        const btnMoKhoaModal = document.getElementById('btnMoKhoaModal');
        if (btnMoKhoaModal) {
            // Clone nút để xóa sự kiện cũ tránh bị click đúp
            const newBtn = btnMoKhoaModal.cloneNode(true);
            btnMoKhoaModal.parentNode.replaceChild(newBtn, btnMoKhoaModal);
            
            newBtn.onclick = () => {
                moCuaTuXa(maPhong, room.tenPhong);
            };
        }
        const btnDongKhoaModal = document.getElementById('btnDongKhoaModal');
        if (btnDongKhoaModal) {
            const newBtn = btnDongKhoaModal.cloneNode(true);
            btnDongKhoaModal.parentNode.replaceChild(newBtn, btnDongKhoaModal);
            
            newBtn.onclick = () => {
                dongCuaTuXa(maPhong, room.tenPhong);
            };
        }
        // ===============================================

        const modal = new bootstrap.Modal(document.getElementById('viewPhongModal'));
        modal.show();
    } catch (error) {
        console.error('Error loading room details:', error);
        showAlert(error.message || 'Không thể xem chi tiết phòng', 'danger');
    }
};
window.moCuaTuXa = async function(maPhong, tenPhong = "") {
    // 1. Xác nhận hành động
    if (!confirm(`⚠️ CẢNH BÁO AN NINH:\n\nBạn có chắc chắn muốn kích hoạt mở khóa cửa phòng [${tenPhong}] không?`)) {
        return;
    }

    // 2. Tìm nút bấm để hiển thị loading (Tạo hiệu ứng xoay xoay)
    const btn = event.currentTarget; 
    const originalContent = btn.innerHTML;
    
    // Đổi trạng thái nút sang đang loading
    btn.innerHTML = `<span class="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span> Đang gửi lệnh...`;
    btn.disabled = true;

    try {
        // 3. Gọi API về Server
        // URL: /api/Phong/mo-cua-tu-xa/{id}
        // Lưu ý: Config.ENDPOINTS.PHONG thường là "/api/Phong"
        const response = await fetch(`${Config.API_URL}${Config.ENDPOINTS.PHONG}/mo-cua-tu-xa/${maPhong}`, {
            method: 'POST',
            headers: {
                'Authorization': `Bearer ${localStorage.getItem('token')}`,
                'Content-Type': 'application/json'
            }
        });

        // 4. Xử lý kết quả
        if (response.ok) {
            const data = await response.json();
            showAlert(`✅ THÀNH CÔNG: ${data.message || 'Cửa đang được mở!'}`, 'success');
        } else {
            const errorData = await response.json();
            throw new Error(errorData.message || 'Không thể kết nối đến thiết bị IoT');
        }

    } catch (error) {
        console.error('Lỗi mở cửa:', error);
        showAlert(`❌ THẤT BẠI: ${error.message}`, 'danger');
    } finally {
        // 5. Trả lại trạng thái nút sau 1 giây
        setTimeout(() => {
            if (btn) {
                btn.innerHTML = originalContent;
                btn.disabled = false;
            }
        }, 1000);
    }
};
// Dán vào cuối file phong.js

window.dongCuaTuXa = async function(maPhong, tenPhong = "") {
    if (!confirm(`Bạn có muốn ĐÓNG CHỐT cửa phòng [${tenPhong}] không?`)) return;

    const btn = event.currentTarget;
    const originalContent = btn.innerHTML;
    btn.innerHTML = `<span class="spinner-border spinner-border-sm"></span>...`;
    btn.disabled = true;

    try {
        // Gọi API Đóng
        const response = await fetch(`${Config.API_URL}${Config.ENDPOINTS.PHONG}/dong-cua-tu-xa/${maPhong}`, {
            method: 'POST',
            headers: {
                'Authorization': `Bearer ${localStorage.getItem('token')}`,
                'Content-Type': 'application/json'
            }
        });

        if (response.ok) {
            const data = await response.json();
            showAlert(`✅ ${data.message}`, 'success');
        } else {
            throw new Error('Lỗi kết nối');
        }
    } catch (error) {
        showAlert(`❌ Thất bại: ${error.message}`, 'danger');
    } finally {
        setTimeout(() => { btn.innerHTML = originalContent; btn.disabled = false; }, 1000);
    }
};