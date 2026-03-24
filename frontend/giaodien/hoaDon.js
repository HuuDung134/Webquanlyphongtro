const API_URL = Config.API_URL;

// Helper function to get token
function getToken() {
    const token = localStorage.getItem('token');
    if (!token) {
        window.location.href = 'login.html';
        return null;
    }
    return token;
}

// DOM Elements
const billList = document.getElementById('billList');
const addBillForm = document.getElementById('addBillForm');
const saveBillBtn = document.getElementById('saveBillBtn');
const phongSelect = document.querySelector('select[name="maPhong"]');

// Constants
const TRANG_THAI = {
    CHUA_THANH_TOAN: 1,
    DA_THANH_TOAN: 2
};

// Event Listeners
document.addEventListener('DOMContentLoaded', initializeApp);

// Phòng select change event
if (phongSelect) {
    phongSelect.addEventListener('change', async function() {
        if (this.value) {
            await loadPhongInfo(parseInt(this.value));
        }
    });
}

// Initialization
async function initializeApp() {
    try {
        await loadBills();
        if (saveBillBtn) {
            saveBillBtn.onclick = () => saveBill();
        }
        await loadPhong();
    } catch (error) {
        console.error('Error initializing app:', error);
        showToast('Không thể khởi tạo ứng dụng', 'danger');
    }
}

// Data Loading Functions
async function loadBills() {
    try {
        const response = await fetch(`${API_URL}${Config.ENDPOINTS.HOA_DON}`, {
            headers: {
                'Authorization': `Bearer ${localStorage.getItem('token')}`
            }
        });
        if (!response.ok) throw new Error('Không thể tải danh sách hóa đơn');
        const bills = await response.json();
        displayBills(bills);
    } catch (error) {
        console.error('Error loading bills:', error);
        showToast('Không thể tải danh sách hóa đơn', 'danger');
    }
}

async function loadPhong() {
    try {
        const response = await fetch(`${API_URL}${Config.ENDPOINTS.PHONG}`, {
            headers: {
                'Authorization': `Bearer ${localStorage.getItem('token')}`
            }
        });
        if (!response.ok) throw new Error('Không thể tải danh sách phòng');
        const phongData = await response.json();
        populatePhongSelect(phongData);
    } catch (error) {
        console.error('Error loading rooms:', error);
        showToast('Không thể tải danh sách phòng', 'danger');
    }
}

// Kiểm tra và tạo hóa đơn tự động khi có đủ thông tin
async function checkAndCreateBill(phongId) {
    try {
        // Lấy kỳ hóa đơn từ input nếu có, nếu không lấy kỳ hiện tại
        let kyHoaDon = '';
        const kyHoaDonInput = document.querySelector('input[name="kyHoaDon"]');
        if (kyHoaDonInput && kyHoaDonInput.value) {
            kyHoaDon = kyHoaDonInput.value;
        } else {
            kyHoaDon = getCurrentPeriod();
        }
        // Định dạng lại kỳ hóa đơn nếu là yyyy-MM
        if (/^\d{4}-\d{2}$/.test(kyHoaDon)) {
            // ok
        } else if (/^\d{4}-\d{2}-\d{2}$/.test(kyHoaDon)) {
            kyHoaDon = kyHoaDon.substring(0,7);
        } else {
            kyHoaDon = getCurrentPeriod();
        }
        // Log tham số truyền lên
        console.log('Kiểm tra hóa đơn:', phongId, kyHoaDon);
        // Kiểm tra xem phòng đã có hóa đơn cho kỳ này chưa
        const checkResponse = await fetch(`${API_URL}${Config.ENDPOINTS.HOA_DON}/CheckHoaDon/${phongId}/${kyHoaDon}`, {
            headers: {
                'Authorization': `Bearer ${getToken()}`
            }
        });
        if (!checkResponse.ok) {
            const errorText = await checkResponse.text();
            console.error('Lỗi khi kiểm tra hóa đơn:', errorText);
            showToast('Không thể kiểm tra hóa đơn: ' + errorText, 'danger');
            return;
        }
        const hasBill = await checkResponse.json();
        if (hasBill) {
            showToast('Phòng đã có hóa đơn cho kỳ này!', 'info');
            return;
        }
        // Lấy thông tin phòng
        const phongInfoResponse = await fetch(`${API_URL}${Config.ENDPOINTS.HOA_DON}/GetThongTinPhong/${phongId}`, {
            headers: {
                'Authorization': `Bearer ${getToken()}`
            }
        });
        if (!phongInfoResponse.ok) {
            const errorText = await phongInfoResponse.text();
            console.error('Lỗi khi lấy thông tin phòng:', errorText);
            showToast('Không thể lấy thông tin phòng: ' + errorText, 'danger');
            return;
        }
        const phongInfo = await phongInfoResponse.json();
        // Kiểm tra đủ thông tin
        if (!phongInfo.phong || !phongInfo.nguoiThue || !phongInfo.maDien || !phongInfo.maNuoc) {
            showToast('Chưa đủ thông tin để tạo hóa đơn (phòng, người thuê, điện, nước)', 'warning');
            return;
        }
        // Tạo hóa đơn mới
        const billData = {
            maPhong: phongId,
            maNguoiThue: phongInfo.nguoiThue.maNguoiThue,
            maDien: phongInfo.maDien,
            maNuoc: phongInfo.maNuoc,
            ngayLap: new Date().toISOString().split('T')[0],
            kyHoaDon: kyHoaDon,
            tienPhong: parseFloat(phongInfo.phong.giaPhong),
            tienDien: parseFloat(phongInfo.tienDien || 0),
            tienNuoc: parseFloat(phongInfo.tienNuoc || 0),
            tienDichVu: parseFloat(phongInfo.tongTienDichVu || 0),
            tongTien: parseFloat(phongInfo.phong.giaPhong) + 
                    parseFloat(phongInfo.tienDien || 0) + 
                    parseFloat(phongInfo.tienNuoc || 0) + 
                    parseFloat(phongInfo.tongTienDichVu || 0)
        };
        console.log('Tạo hóa đơn tự động:', billData);
        const createResponse = await fetch(`${API_URL}${Config.ENDPOINTS.HOA_DON}`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${getToken()}`
            },
            body: JSON.stringify(billData)
        });
        if (!createResponse.ok) {
            const errorText = await createResponse.text();
            console.error('Lỗi khi tạo hóa đơn:', errorText);
            showToast('Không thể tạo hóa đơn: ' + errorText, 'danger');
            return;
        }
        showToast('Đã tạo hóa đơn tự động cho phòng', 'success');
        await loadBills();
    } catch (error) {
        console.error('Lỗi khi tạo hóa đơn tự động:', error);
        showToast(error.message || 'Có lỗi xảy ra khi tạo hóa đơn tự động', 'danger');
    }
}

// Sửa lại hàm loadPhongInfo để chỉ gọi tạo hóa đơn khi thực sự đủ thông tin
async function loadPhongInfo(phongId) {
    try {
        const response = await fetch(`${API_URL}${Config.ENDPOINTS.HOA_DON}/GetThongTinPhong/${phongId}`, {
            headers: {
                'Authorization': `Bearer ${getToken()}`
            }
        });
        if (!response.ok) {
            const errorText = await response.text();
            throw new Error(errorText || 'Không thể tải thông tin phòng');
        }
        const phongInfo = await response.json();
        // Tự động điền thông tin vào form
        if (addBillForm) {
            const nguoiThueInput = addBillForm.querySelector('input[name="maNguoiThue"]') || 
                                  addBillForm.querySelector('select[name="maNguoiThue"]');
            const tienPhongInput = addBillForm.querySelector('input[name="tienPhong"]');
            const tienDienInput = addBillForm.querySelector('input[name="tienDien"]');
            const tienNuocInput = addBillForm.querySelector('input[name="tienNuoc"]');
            const tienDichVuInput = addBillForm.querySelector('input[name="tienDichVu"]');
            const tongTienInput = addBillForm.querySelector('input[name="tongTien"]');
            if (nguoiThueInput) nguoiThueInput.value = phongInfo.nguoiThue?.maNguoiThue || '';
            if (tienPhongInput) tienPhongInput.value = phongInfo.phong?.giaPhong || 0;
            if (tienDienInput) tienDienInput.value = phongInfo.tienDien || 0;
            if (tienNuocInput) tienNuocInput.value = phongInfo.tienNuoc || 0;
            if (tienDichVuInput) tienDichVuInput.value = phongInfo.tongTienDichVu || 0;
            // Tính tổng tiền
            const tongTien = (phongInfo.phong?.giaPhong || 0) + 
                           (phongInfo.tienDien || 0) + 
                           (phongInfo.tienNuoc || 0) + 
                           (phongInfo.tongTienDichVu || 0);
            if (tongTienInput) tongTienInput.value = tongTien;
            // Hiển thị thông tin phòng
            showPhongDetails(phongInfo);
            // Chỉ gọi tạo hóa đơn khi đủ thông tin
            if (phongInfo.phong && phongInfo.nguoiThue && phongInfo.maDien && phongInfo.maNuoc) {
                await checkAndCreateBill(phongId);
            }
        }
    } catch (error) {
        console.error('Error loading room info:', error);
        showToast(error.message, 'warning');
    }
}

function showPhongDetails(phongInfo) {
    // Tạo hoặc cập nhật div hiển thị thông tin
    let infoDiv = document.getElementById('phongInfoDisplay');
    if (!infoDiv) {
        infoDiv = document.createElement('div');
        infoDiv.id = 'phongInfoDisplay';
        infoDiv.className = 'alert alert-info mt-2';
        phongSelect.parentNode.appendChild(infoDiv);
    }
    
    infoDiv.innerHTML = `
        <h6>Thông tin phòng:</h6>
        <p><strong>Tên phòng:</strong> ${phongInfo.phong.tenPhong}</p>
        <p><strong>Người thuê:</strong> ${phongInfo.nguoiThue.hoTen}</p>
        <p><strong>Giá phòng:</strong> ${formatCurrency(phongInfo.phong.giaPhong)}</p>
        <p><strong>Tiền điện:</strong> ${formatCurrency(phongInfo.tienDien)}</p>
        <p><strong>Tiền nước:</strong> ${formatCurrency(phongInfo.tienNuoc)}</p>
        <p><strong>Tiền dịch vụ:</strong> ${formatCurrency(phongInfo.tongTienDichVu)}</p>
        <p><strong>Tổng tiền:</strong> ${formatCurrency(phongInfo.tongTienHoaDon)}</p>
    `;
}

// UI Functions
function displayBills(bills) {
    if (!billList) {
        console.error('billList element not found');
        return;
    }
    console.log(bills); // Debug log
    billList.innerHTML = '';
    bills.forEach(bill => {
        const card = createBillCard(bill);
        billList.appendChild(card);
    });
}

function populatePhongSelect(phongData) {
    if (!phongSelect) {
        console.error('phongSelect element not found');
        return;
    }
    
    phongSelect.innerHTML = '<option value="">Chọn phòng...</option>';
    phongData.forEach(phong => {
        const option = document.createElement('option');
        option.value = phong.maPhong;
        option.textContent = `${phong.tenPhong} - ${formatCurrency(phong.giaPhong)}`;
        phongSelect.appendChild(option);
    });
}

function createBillCard(bill) {
    const col = document.createElement('div');
    col.className = 'col-md-6';
    // Badge trạng thái
    let badge = '';
    if (bill.trangThai === 2 || bill.trangThai === 'Đã thanh toán') {
        badge = '<span class="badge bg-success ms-2"><i class="fas fa-check-circle me-1"></i>Đã thanh toán</span>';
    } else {
        badge = '<span class="badge bg-warning text-dark ms-2"><i class="fas fa-exclamation-circle me-1"></i>Chưa thanh toán</span>';
    }
    col.innerHTML = `
        <div class="card bill-card shadow rounded-4 border-0 mb-4">
            <div class="card-body">
                <div class="d-flex justify-content-between align-items-center mb-2">
                    <h5 class="card-title mb-0"><i class="fas fa-file-invoice-dollar text-primary me-2"></i>Hóa Đơn #${bill.maHoaDon}</h5>
                    ${badge}
                </div>
                <ul class="list-unstyled mb-3">
                    <li><i class="fas fa-door-open me-2 text-secondary"></i><strong>Phòng:</strong> ${bill.tenPhong || 'N/A'}</li>
                    <li><i class="fas fa-user me-2 text-secondary"></i><strong>Người thuê:</strong> ${bill.tenNguoiThue || 'N/A'}</li>
                    <li><i class="fas fa-calendar-alt me-2 text-secondary"></i><strong>Ngày lập:</strong> ${formatDate(bill.ngayLap)}</li>
                    <li><i class="fas fa-calendar me-2 text-secondary"></i><strong>Kỳ hóa đơn:</strong> ${bill.kyHoaDon}</li>
                    <li><i class="fas fa-coins me-2 text-secondary"></i><strong>Tiền phòng:</strong> ${formatCurrency(bill.tienPhong)}</li>
                    <li><i class="fas fa-bolt me-2 text-warning"></i><strong>Tiền điện:</strong> ${formatCurrency(bill.tienDien)}</li>
                    <li><i class="fas fa-tint me-2 text-info"></i><strong>Tiền nước:</strong> ${formatCurrency(bill.tienNuoc)}</li>
                    <li><i class="fas fa-concierge-bell me-2 text-success"></i><strong>Tiền dịch vụ:</strong> ${formatCurrency(bill.tienDichVu)}</li>
                    <li><i class="fas fa-money-bill-wave me-2 text-primary"></i><strong>Tổng tiền:</strong> ${formatCurrency(bill.tongTien)}</li>
                </ul>
                
                <div class="btn-group-vertical w-100">
                    <div class="btn-group mb-2">
                        <button class="btn btn-outline-danger" onclick="deleteBill(${bill.maHoaDon})">
                            <i class="fas fa-trash"></i> Xóa
                        </button>
                   
                    </div>
                    <div class="btn-group">
                     
                        <button class="btn btn-outline-success" onclick="downloadPdf(${bill.maHoaDon})">
                            <i class="fas fa-download"></i> Tải PDF
                        </button>
                    </div>
                </div>
            </div>
        </div>
    `;
    return col;
}

// Form Handling Functions
async function createBill() {
    // Hiển thị modal xác nhận
    const modal = new bootstrap.Modal(document.getElementById('confirmCreateBillModal'));
    modal.show();
    // Gắn sự kiện cho nút xác nhận (chỉ gắn 1 lần)
    const btn = document.getElementById('confirmCreateBillBtn');
    btn.onclick = async function() {
        modal.hide();
        await createBillConfirmed();
    };
}

// Hàm thực hiện tạo hóa đơn hàng loạt thật sự
async function createBillConfirmed() {
    try {
        const now = new Date();
        const thang = now.getMonth() + 1;
        const nam = now.getFullYear();
        // Lấy danh sách phòng chưa có hóa đơn
        const resPhong = await fetch(`${API_URL}${Config.ENDPOINTS.HOA_DON}/GetPhongChuaCoHoaDonTrongThang?thang=${thang}&nam=${nam}`, {
            headers: {
                'Authorization': `Bearer ${getToken()}`
            }
        });
        if (!resPhong.ok) {
            const errorText = await resPhong.text();
            showToast('Không thể lấy danh sách phòng: ' + errorText, 'danger');
            return;
        }
        const phongList = await resPhong.json();
        if (!phongList || phongList.length === 0) {
            showToast('Tất cả các phòng đều đã có hóa đơn kỳ này!', 'info');
            return;
        }
        let success = 0, fail = 0;
        for (const phong of phongList) {
            try {
                // Lấy thông tin phòng
                const resInfo = await fetch(`${API_URL}${Config.ENDPOINTS.HOA_DON}/GetThongTinPhong/${phong.maPhong}`, {
                    headers: {
                        'Authorization': `Bearer ${getToken()}`
                    }
                });
                if (!resInfo.ok) { fail++; continue; }
                const info = await resInfo.json();
                if (!info.phong || !info.nguoiThue || !info.maDien || !info.maNuoc) { fail++; continue; }
                // Tạo hóa đơn
                const billData = {
                    maPhong: phong.maPhong,
                    maNguoiThue: info.nguoiThue.maNguoiThue,
                    maDien: info.maDien,
                    maNuoc: info.maNuoc,
                    ngayLap: new Date().toISOString().split('T')[0],
                    kyHoaDon: `${nam}-${String(thang).padStart(2, '0')}`,
                    tienPhong: parseFloat(info.phong.giaPhong),
                    tienDien: parseFloat(info.tienDien || 0),
                    tienNuoc: parseFloat(info.tienNuoc || 0),
                    tienDichVu: parseFloat(info.tongTienDichVu || 0),
                    tongTien: parseFloat(info.phong.giaPhong) + 
                            parseFloat(info.tienDien || 0) + 
                            parseFloat(info.tienNuoc || 0) + 
                            parseFloat(info.tongTienDichVu || 0)
                };
                const resCreate = await fetch(`${API_URL}${Config.ENDPOINTS.HOA_DON}`, {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                        'Authorization': `Bearer ${getToken()}`
                    },
                    body: JSON.stringify(billData)
                });
                if (resCreate.ok) success++; else fail++;
            } catch (e) { fail++; }
        }
        showToast(`Đã tạo hóa đơn thành công cho ${success} phòng. Thất bại: ${fail}`, success > 0 ? 'success' : 'danger');
        await loadBills();
    } catch (error) {
        console.error('Lỗi khi tạo hóa đơn hàng loạt:', error);
        showToast(error.message || 'Lỗi khi tạo hóa đơn hàng loạt', 'danger');
    }
}

// CRUD Operations
async function saveBill() {
    try {
        const form = document.getElementById('addBillForm');
        const formData = new FormData(form);
        
        // Validate form data
        if (!formData.get('maPhong') || !formData.get('kyHoaDon') || !formData.get('ngayLap')) {
            showToast('Vui lòng điền đầy đủ thông tin bắt buộc', 'warning');
            return;
        }

        // Get room information first
        const roomResponse = await fetch(`${Config.API_URL}/api/HoaDon/GetThongTinPhong/${formData.get('maPhong')}`, {
            headers: {
                'Authorization': `Bearer ${getToken()}`
            }
        });

        if (!roomResponse.ok) {
            const errorText = await roomResponse.text();
            throw new Error(errorText || 'Không thể lấy thông tin phòng');
        }

        const roomData = await roomResponse.json();
        console.log('Room data:', roomData); // Debug log

        // Kiểm tra dữ liệu phòng
        if (!roomData || !roomData.phong || !roomData.nguoiThue) {
            throw new Error('Dữ liệu phòng không hợp lệ');
        }

        const billData = {
            maPhong: parseInt(formData.get('maPhong')),
            maNguoiThue: parseInt(roomData.nguoiThue.maNguoiThue),
            maDien: parseInt(roomData.maDien),
            maNuoc: parseInt(roomData.maNuoc),
            ngayLap: formData.get('ngayLap'),
            kyHoaDon: formData.get('kyHoaDon'),
            tienPhong: parseFloat(roomData.phong.giaPhong),
            tienDien: parseFloat(formData.get('tienDien')),
            tienNuoc: parseFloat(formData.get('tienNuoc')),
            tienDichVu: parseFloat(roomData.tongTienDichVu || 0),
            tongTien: parseFloat(roomData.phong.giaPhong) + 
                     parseFloat(formData.get('tienDien')) + 
                     parseFloat(formData.get('tienNuoc')) + 
                     parseFloat(roomData.tongTienDichVu || 0)
        };

        console.log('Sending bill data:', billData);

        const response = await fetch(`${Config.API_URL}/api/HoaDon`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${getToken()}`
            },
            body: JSON.stringify(billData)
        });

        if (!response.ok) {
            const errorText = await response.text();
            console.error('Server error:', errorText);
            throw new Error(errorText || 'Lỗi khi tạo hóa đơn');
        }

        const result = await response.json();
        showToast('Tạo hóa đơn thành công', 'success');
        bootstrap.Modal.getInstance(document.getElementById('addBillModal')).hide();
        addBillForm.reset();
        await loadBills();
    } catch (error) {
        console.error('Error saving bill:', error);
        showToast(error.message || 'Lỗi khi tạo hóa đơn', 'danger');
    }
}

async function editBill(maHoaDon) {
    try {
        const response = await fetch(`${API_URL}/HoaDon/${maHoaDon}`);
        if (!response.ok) throw new Error('Không thể tải thông tin hóa đơn');
        
        const bill = await response.json();
        if (!addBillForm) {
            console.error('addBillForm not found');
            return;
        }
        
        // Populate form với dữ liệu hóa đơn
        const maPhongInput = addBillForm.querySelector('[name="maPhong"]');
        const maNguoiThueInput = addBillForm.querySelector('[name="maNguoiThue"]');
        const tienPhongInput = addBillForm.querySelector('[name="tienPhong"]');
        const tienDienInput = addBillForm.querySelector('[name="tienDien"]');
        const tienNuocInput = addBillForm.querySelector('[name="tienNuoc"]');
        const tienDichVuInput = addBillForm.querySelector('[name="tienDichVu"]');
        const ngayLapInput = addBillForm.querySelector('[name="ngayLap"]');
        const kyHoaDonInput = addBillForm.querySelector('[name="kyHoaDon"]');

        if (maPhongInput) maPhongInput.value = bill.maPhong;
        if (maNguoiThueInput) maNguoiThueInput.value = bill.maNguoiThue;
        if (tienPhongInput) tienPhongInput.value = bill.tienPhong || 0;
        if (tienDienInput) tienDienInput.value = bill.tienDien || 0;
        if (tienNuocInput) tienNuocInput.value = bill.tienNuoc || 0;
        if (tienDichVuInput) tienDichVuInput.value = bill.tienDichVu || 0;
        if (ngayLapInput) ngayLapInput.value = bill.ngayLap ? bill.ngayLap.split('T')[0] : '';
        if (kyHoaDonInput) kyHoaDonInput.value = bill.kyHoaDon || '';
        
        document.querySelector('#addBillModal .modal-title').textContent = 'Sửa Hóa Đơn';
        const modal = new bootstrap.Modal(document.getElementById('addBillModal'));
        modal.show();
        
        if (saveBillBtn) {
            saveBillBtn.onclick = () => updateBill(maHoaDon);
        }
    } catch (error) {
        console.error('Error loading bill:', error);
        showToast('Không thể tải thông tin hóa đơn', 'danger');
    }
}

async function deleteBill(maHoaDon) {
    // Đảm bảo chỉ truyền id số nguyên
    const id = parseInt(maHoaDon);
    if (isNaN(id)) {
        showToast('ID hóa đơn không hợp lệ!', 'danger');
        console.error('ID hóa đơn không hợp lệ:', maHoaDon);
        return;
    }
    if (!confirm('Bạn có chắc chắn muốn xóa hóa đơn này?')) return;
    try {
        const response = await fetch(`${API_URL}${Config.ENDPOINTS.HOA_DON}/${id}`, {
            method: 'DELETE',
            headers: {
                'Authorization': `Bearer ${localStorage.getItem('token')}`
            }
        });
        if (!response.ok) {
            const errorText = await response.text();
            throw new Error(errorText || 'Lỗi khi xóa hóa đơn');
        }
        showToast('Xóa hóa đơn thành công', 'success');
        await loadBills();
    } catch (error) {
        console.error('Error deleting bill:', error);
        showToast(error.message || 'Không thể xóa hóa đơn', 'danger');
    }
}

function viewBillDetail(maHoaDon) {
    // Ưu tiên mở PDF nếu có, fallback về HTML nếu không có
    const pdfUrl = `${Config.API_URL}${Config.ENDPOINTS.HOA_DON}/PrintPdf/${maHoaDon}`;
    const htmlUrl = `${Config.API_URL}${Config.ENDPOINTS.HOA_DON}/Print/${maHoaDon}`;
    
    // Hiển thị loading
    showToast('Đang tạo PDF hóa đơn...', 'info');
    
    fetch(pdfUrl, { 
        method: 'HEAD',
        headers: {
            'Authorization': `Bearer ${getToken()}`
        }
    })
        .then(res => {
            if (res.ok) {
                window.open(pdfUrl, '_blank');
                showToast('Đã mở PDF hóa đơn', 'success');
            } else {
                window.open(htmlUrl, '_blank');
                showToast('Đã mở hóa đơn HTML', 'info');
            }
        })
        .catch((error) => {
            console.error('Lỗi khi mở hóa đơn:', error);
            window.open(htmlUrl, '_blank');
            showToast('Đã mở hóa đơn HTML (PDF không khả dụng)', 'warning');
        });
}

// Helper Functions
function formatDate(dateString) {
    return new Date(dateString).toLocaleDateString('vi-VN');
}

function formatCurrency(amount) {
    if (isNaN(amount) || amount === null || amount === undefined) return '0 ₫';
    return new Intl.NumberFormat('vi-VN', {
        style: 'currency',
        currency: 'VND'
    }).format(amount);
}

function getCurrentPeriod() {
    const now = new Date();
    const year = now.getFullYear();
    const month = String(now.getMonth() + 1).padStart(2, '0');
    return `${year}-${month}`;
}

function showToast(message, type = 'success') {
    const toastContainer = document.getElementById('toastContainer');
    const icon = type === 'success' ? 'fa-check-circle' : (type === 'danger' ? 'fa-times-circle' : 'fa-info-circle');
    const bg = type === 'success' ? 'bg-success' : (type === 'danger' ? 'bg-danger' : 'bg-info');
    const textColor = type === 'warning' ? 'text-dark' : 'text-white';
    const toastId = 'toast' + Date.now();
    const toast = document.createElement('div');
    toast.className = `toast align-items-center ${bg} ${textColor}`;
    toast.id = toastId;
    toast.setAttribute('role', 'alert');
    toast.setAttribute('aria-live', 'assertive');
    toast.setAttribute('aria-atomic', 'true');
    toast.innerHTML = `
        <div class="d-flex">
            <div class="toast-body">
                <i class="fas ${icon} me-2"></i>${message}
            </div>
            <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast" aria-label="Close"></button>
        </div>
    `;
    toastContainer.appendChild(toast);
    const bsToast = new bootstrap.Toast(toast, { delay: 3500 });
    bsToast.show();
    toast.addEventListener('hidden.bs.toast', () => toast.remove());
}

// Export functions for use in HTML
window.setupHoaDon = createBill;
window.editBill = editBill;
window.deleteBill = deleteBill;
window.viewBillDetail = viewBillDetail;
window.printPreviewBill = printPreviewBill;
window.printBillHtml = printBillHtml;
window.downloadPdf = downloadPdf;

async function updateBill(maHoaDon) {
    const formData = new FormData(addBillForm);
    
    const billData = {
        maHoaDon: maHoaDon,
        maNguoiThue: parseInt(formData.get('maNguoiThue')),
        maPhong: parseInt(formData.get('maPhong')),
        tienPhong: parseFloat(formData.get('tienPhong')) || 0,
        tienDien: parseFloat(formData.get('tienDien')) || 0,
        tienNuoc: parseFloat(formData.get('tienNuoc')) || 0,
        tienDichVu: parseFloat(formData.get('tienDichVu')) || 0,
        ngayLap: formData.get('ngayLap'),
        kyHoaDon: formData.get('kyHoaDon')
    };

    try {
        const response = await fetch(`${API_URL}${Config.ENDPOINTS.HOA_DON}/${maHoaDon}`, {
            method: 'PUT',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${localStorage.getItem('token')}`
            },
            body: JSON.stringify(billData)
        });

        if (!response.ok) {
            const errorText = await response.text();
            throw new Error(errorText || 'Lỗi khi cập nhật hóa đơn');
        }

        showToast('Cập nhật hóa đơn thành công', 'success');
        bootstrap.Modal.getInstance(document.getElementById('addBillModal')).hide();
        await loadBills();
    } catch (error) {
        console.error('Error updating bill:', error);
        showToast(error.message || 'Không thể cập nhật hóa đơn', 'danger');
    }
}

// Hàm in hóa đơn PDF (được gọi từ HTML)
function printPreviewBill(maHoaDon) {
    viewBillDetail(maHoaDon);
}

// Hàm in hóa đơn HTML
function printBillHtml(maHoaDon) {
    const htmlUrl = `${Config.API_URL}${Config.ENDPOINTS.HOA_DON}/Print/${maHoaDon}`;
    window.open(htmlUrl, '_blank');
    showToast('Đã mở hóa đơn HTML', 'info');
}

// Hàm tải PDF về máy
function downloadPdf(maHoaDon) {
    const pdfUrl = `${Config.API_URL}${Config.ENDPOINTS.HOA_DON}/PrintPdf/${maHoaDon}`;
    
    showToast('Đang tải PDF...', 'info');
    
    fetch(pdfUrl, {
        headers: {
            'Authorization': `Bearer ${getToken()}`
        }
    })
    .then(response => {
        if (response.ok) {
            return response.blob();
        }
        throw new Error('Không thể tải PDF');
    })
    .then(blob => {
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = `HoaDon_${maHoaDon}.pdf`;
        document.body.appendChild(a);
        a.click();
        window.URL.revokeObjectURL(url);
        document.body.removeChild(a);
        showToast('Đã tải PDF thành công', 'success');
    })
    .catch(error => {
        console.error('Lỗi khi tải PDF:', error);
        showToast('Không thể tải PDF', 'danger');
    });
}
