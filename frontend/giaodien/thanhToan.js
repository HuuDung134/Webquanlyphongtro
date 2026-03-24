// API endpoints
const API_URL = Config.API_URL;

// DOM Elements
const paymentList = document.getElementById('paymentList');
const addPaymentForm = document.getElementById('addPaymentForm');
const savePaymentBtn = document.getElementById('savePaymentBtn');
const statusFilter = document.getElementById('statusFilter');
const methodFilter = document.getElementById('methodFilter');
const startDate = document.getElementById('startDate');
const endDate = document.getElementById('endDate');

// Load payments when page loads
document.addEventListener('DOMContentLoaded', () => {
    loadPayments();
    loadBills();
    loadUnpaidBills();
});

// Load all payments
async function loadPayments() {
    try {
        const token = localStorage.getItem('token');
        if (!token) {
            throw new Error('Không có token xác thực');
        }

        const response = await fetch(`${Config.API_URL}${Config.ENDPOINTS.THANH_TOAN}`, {
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
            throw new Error('Không thể tải danh sách thanh toán');
        }

        const payments = await response.json();
        displayPayments(payments);
    } catch (error) {
        console.error('Error loading payments:', error);
        showAlert('Không thể tải danh sách thanh toán', 'danger');
    }
}

// Load bills for payment form
async function loadBills() {
    try {
        const token = localStorage.getItem('token');
        if (!token) {
            throw new Error('Không có token xác thực');
        }

        const response = await fetch(`${Config.API_URL}${Config.ENDPOINTS.HOA_DON}`, {
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
            throw new Error('Không thể tải danh sách hóa đơn');
        }

        const bills = await response.json();
        const billSelect = document.querySelector('select[name="maHoaDon"]');
        
        // Clear existing options except the first one
        while (billSelect.options.length > 1) {
            billSelect.remove(1);
        }
        
        // Add bill options
        bills.forEach(bill => {
            const option = document.createElement('option');
            option.value = bill.maHoaDon;
            option.textContent = `Hóa đơn #${bill.maHoaDon} - ${formatCurrency(bill.tongTien)}`;
            billSelect.appendChild(option);
        });
    } catch (error) {
        console.error('Error loading bills:', error);
        showAlert('Không thể tải danh sách hóa đơn', 'danger');
    }
}

// Load unpaid bills
async function loadUnpaidBills() {
    try {
        const token = localStorage.getItem('token');
        if (!token) {
            throw new Error('Không có token xác thực');
        }

        // Lấy danh sách hóa đơn
        const response = await fetch(`${Config.API_URL}${Config.ENDPOINTS.HOA_DON}`, {
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
            throw new Error('Không thể tải danh sách hóa đơn');
        }

        const bills = await response.json();
        console.log('Tất cả hóa đơn:', bills);

        // Phân loại hóa đơn đã thanh toán và chưa thanh toán
        const unpaidBills = [];
        const paidBills = [];
        
        for (const bill of bills) {
            // Kiểm tra thanh toán của hóa đơn
            const thanhToanResponse = await fetch(`${Config.API_URL}${Config.ENDPOINTS.THANH_TOAN}/HoaDon/${bill.maHoaDon}`, {
                headers: {
                    'Authorization': `Bearer ${token}`,
                    'Accept': 'application/json'
                }
            });

            if (thanhToanResponse.ok) {
                const thanhToanList = await thanhToanResponse.json();
                // Nếu không có thanh toán nào hoặc tất cả thanh toán đều chưa hoàn thành
                if (!thanhToanList || thanhToanList.length === 0) {
                    unpaidBills.push(bill);
                } else {
                    paidBills.push(bill);
                }
            }
        }

        console.log('Hóa đơn chưa thanh toán:', unpaidBills);
        console.log('Hóa đơn đã thanh toán:', paidBills);
        
        displayUnpaidBills(unpaidBills);
        displayPaidBills(paidBills);
    } catch (error) {
        console.error('Error loading bills:', error);
        showAlert('Không thể tải danh sách hóa đơn', 'danger');
    }
}

// Display payments in the UI
function displayPayments(payments) {
    paymentList.innerHTML = '';
    payments.forEach(payment => {
        const paymentCard = createPaymentCard(payment);
        paymentList.appendChild(paymentCard);
    });
}

// Display unpaid bills
function displayUnpaidBills(bills) {
    const unpaidBillsList = document.getElementById('unpaidBillsList');
    unpaidBillsList.innerHTML = '';

    if (!bills || bills.length === 0) {
        unpaidBillsList.innerHTML = '<div class="col-12"><p class="text-center">Không có hóa đơn chưa thanh toán</p></div>';
        return;
    }

    bills.forEach(bill => {
        const col = document.createElement('div');
        col.className = 'col-md-6 mb-4';
        col.innerHTML = `
            <div class="card h-100">
                <div class="card-body">
                    <div class="d-flex justify-content-between align-items-start mb-3">
                        <h5 class="card-title">Hóa Đơn #${bill.maHoaDon}</h5>
                        <span class="badge bg-danger">Chưa thanh toán</span>
                    </div>
                    <p class="card-text">
                        <strong>Phòng:</strong> ${bill.tenPhong || 'N/A'}<br>
                        <strong>Kỳ hóa đơn:</strong> ${bill.kyHoaDon || 'N/A'}<br>
                        <strong>Ngày lập:</strong> ${formatDate(bill.ngayLap)}<br>
                        <strong>Tiền phòng:</strong> ${formatCurrency(bill.tienPhong)}<br>
                        <strong>Tiền điện:</strong> ${formatCurrency(bill.tienDien)}<br>
                        <strong>Tiền nước:</strong> ${formatCurrency(bill.tienNuoc)}<br>
                        <strong>Tiền dịch vụ:</strong> ${formatCurrency(bill.tienDichVu)}<br>
                        <strong>Tổng tiền:</strong> ${formatCurrency(bill.tongTien)}
                    </p>
                    <div class="btn-group w-100">
                        <button class="btn btn-outline-primary" onclick="printBill(${bill.maHoaDon})">
                            <i class="fas fa-print"></i> In hóa đơn
                        </button>
                        <button class="btn btn-outline-success" onclick="thanhToanHoaDon(${bill.maHoaDon}, ${bill.tongTien}, '${bill.tenPhong}')">
                            <i class="fas fa-money-bill-wave"></i> Thanh toán
                        </button>
                    </div>
                </div>
            </div>
        `;
        unpaidBillsList.appendChild(col);
    });
}

// Display paid bills
function displayPaidBills(bills) {
    const paidBillsList = document.getElementById('paidBillsList');
    paidBillsList.innerHTML = '';

    if (!bills || bills.length === 0) {
        paidBillsList.innerHTML = '<div class="col-12"><p class="text-center">Không có hóa đơn đã thanh toán</p></div>';
        return;
    }

    bills.forEach(bill => {
        const col = document.createElement('div');
        col.className = 'col-md-6 mb-4';
        col.innerHTML = `
            <div class="card h-100">
                <div class="card-body">
                    <div class="d-flex justify-content-between align-items-start mb-3">
                        <h5 class="card-title">Hóa Đơn #${bill.maHoaDon}</h5>
                        <span class="badge bg-success">Đã thanh toán</span>
                    </div>
                    <p class="card-text">
                        <strong>Phòng:</strong> ${bill.tenPhong || 'N/A'}<br>
                        <strong>Kỳ hóa đơn:</strong> ${bill.kyHoaDon || 'N/A'}<br>
                        <strong>Ngày lập:</strong> ${formatDate(bill.ngayLap)}<br>
                        <strong>Tiền phòng:</strong> ${formatCurrency(bill.tienPhong)}<br>
                        <strong>Tiền điện:</strong> ${formatCurrency(bill.tienDien)}<br>
                        <strong>Tiền nước:</strong> ${formatCurrency(bill.tienNuoc)}<br>
                        <strong>Tiền dịch vụ:</strong> ${formatCurrency(bill.tienDichVu)}<br>
                        <strong>Tổng tiền:</strong> ${formatCurrency(bill.tongTien)}
                    </p>
                    <div class="btn-group w-100">
                        <button class="btn btn-outline-primary" onclick="printBill(${bill.maHoaDon})">
                            <i class="fas fa-print"></i> In hóa đơn
                        </button>
                    </div>
                </div>
            </div>
        `;
        paidBillsList.appendChild(col);
    });
}

// Format date
function formatDate(dateString) {
    const date = new Date(dateString);
    return date.toLocaleDateString('vi-VN');
}

// Format currency
function formatCurrency(amount) {
    // Kiểm tra và xử lý giá trị không hợp lệ
    if (amount === null || amount === undefined || isNaN(amount)) {
        return '0 ₫';
    }
    
    const numAmount = parseFloat(amount);
    if (isNaN(numAmount) || numAmount < 0) {
        return '0 ₫';
    }
    
    return new Intl.NumberFormat('vi-VN', {
        style: 'currency',
        currency: 'VND'
    }).format(numAmount);
}

// Print bill
function printBill(maHoaDon) {
    window.open(`${Config.API_URL}${Config.ENDPOINTS.HOA_DON}/${maHoaDon}/print`, '_blank');
}

// Thanh toán hóa đơn
async function thanhToanHoaDon(maHoaDon, tongTien, tenPhong) {
    try {
        const token = localStorage.getItem('token');
        if (!token) {
            throw new Error('Không có token xác thực');
        }

        // Lấy thông tin chi tiết hóa đơn để có maNguoiThue
        const billResponse = await fetch(`${Config.API_URL}${Config.ENDPOINTS.HOA_DON}/${maHoaDon}`, {
            headers: {
                'Authorization': `Bearer ${token}`,
                'Accept': 'application/json'
            }
        });

        if (!billResponse.ok) {
            throw new Error('Không thể lấy thông tin hóa đơn');
        }

        const bill = await billResponse.json();
        
        // Kiểm tra maNguoiThue có trong hóa đơn không
        if (!bill.maNguoiThue) {
            throw new Error('Hóa đơn không có thông tin người thuê');
        }

        // Chuẩn bị dữ liệu thanh toán
        // TrangThai = 2: Đang chờ xác nhận (không gửi email/SMS ngay)
        // Sau khi xác nhận, sẽ gửi email/SMS
        const paymentData = {
            maHoaDon: maHoaDon,
            maNguoiThue: bill.maNguoiThue,
            tongTien: tongTien,
            hinhThucThanhToan: "Tiền mặt", // Mặc định là tiền mặt
            ngayThanhToan: new Date().toISOString(),
            ghiChu: `Thanh toán hóa đơn phòng ${tenPhong}`,
            trangThai: 2 // Đang chờ xác nhận - sẽ gửi email/SMS khi được xác nhận
        };

        // Gọi API tạo thanh toán - sử dụng endpoint POST thông thường cho thanh toán tiền mặt
        const response = await fetch(`${Config.API_URL}${Config.ENDPOINTS.THANH_TOAN}`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${token}`,
                'Accept': 'application/json'
            },
            body: JSON.stringify(paymentData)
        });

        if (!response.ok) {
            const errorData = await response.json().catch(() => null);
            const errorMessage = errorData?.message || errorData?.title || 'Không thể tạo thanh toán';
            throw new Error(errorMessage);
        }

        const createdPayment = await response.json();
        console.log('Thanh toán đã được tạo:', createdPayment);
        
        // Tự động xác nhận thanh toán và gửi email/SMS
        // Vì người dùng đã bấm nút thanh toán, có nghĩa là họ muốn xác nhận
        try {
            console.log('Đang xác nhận thanh toán...');
            const confirmResponse = await fetch(`${Config.API_URL}${Config.ENDPOINTS.THANH_TOAN}/${createdPayment.maThanhToan}/xac-nhan`, {
                method: 'POST',
                headers: {
                    'Authorization': `Bearer ${token}`,
                    'Accept': 'application/json'
                }
            });

            if (confirmResponse.ok) {
                const confirmData = await confirmResponse.json().catch(() => null);
                console.log('Xác nhận thanh toán thành công:', confirmData);
                showAlert('Tạo và xác nhận thanh toán thành công. Email và SMS đã được gửi.', 'success');
            } else {
                const errorData = await confirmResponse.json().catch(() => null);
                console.error('Lỗi xác nhận thanh toán:', errorData);
                showAlert('Tạo thanh toán thành công nhưng chưa xác nhận. Vui lòng xác nhận sau.', 'warning');
            }
        } catch (confirmError) {
            console.error('Lỗi khi xác nhận thanh toán:', confirmError);
            showAlert('Tạo thanh toán thành công nhưng chưa xác nhận. Vui lòng xác nhận sau.', 'warning');
        }

        // Tải lại danh sách thanh toán và hóa đơn chưa thanh toán
        // Sử dụng setTimeout để đảm bảo alert hiển thị trước khi refresh dữ liệu
        setTimeout(async () => {
            try {
                await loadPayments();
                await loadUnpaidBills();
                console.log('Đã tải lại danh sách thanh toán và hóa đơn');
            } catch (reloadError) {
                console.error('Lỗi khi tải lại dữ liệu:', reloadError);
                // Nếu có lỗi khi load, reload trang để đảm bảo dữ liệu được cập nhật
                window.location.reload();
            }
        }, 1000);
    } catch (error) {
        console.error('Error creating payment:', error);
        showAlert(error.message || 'Không thể tạo thanh toán', 'danger');
    }
}

// Create a payment card element
function createPaymentCard(payment) {
    const col = document.createElement('div');
    col.className = 'col-md-4';
    
    const statusClass = getStatusClass(payment.trangThai);
    const statusText = getStatusText(payment.trangThai);
    
    // Xử lý số tiền - kiểm tra nhiều tên field có thể có
    const soTien = payment.soTien || payment.tongTien || payment.soTienThanhToan || 0;
    const formattedAmount = (soTien && !isNaN(soTien) && soTien > 0) 
        ? formatCurrency(soTien) 
        : formatCurrency(0);
    
    // Xử lý phương thức thanh toán - kiểm tra nhiều tên field có thể có
    const phuongThuc = payment.phuongThucThanhToan || payment.hinhThucThanhToan || payment.phuongThuc || 'Chưa xác định';
    
    // Xử lý ngày thanh toán
    const ngayThanhToan = payment.ngayThanhToan || payment.ngayTao || new Date().toISOString();
    
    col.innerHTML = `
        <div class="card payment-card">
            <div class="card-body">
                <div class="text-center mb-3">
                    <i class="fas fa-money-bill-wave fa-3x text-primary mb-2"></i>
                    <h5 class="card-title">Thanh Toán #${payment.maThanhToan || 'N/A'}</h5>
                </div>
                <p class="card-text">
                    <strong>Hóa đơn:</strong> #${payment.maHoaDon || 'N/A'}<br>
                    <strong>Số tiền:</strong> ${formattedAmount}<br>
                    <strong>Phương thức:</strong> ${phuongThuc}<br>
                    <strong>Ngày thanh toán:</strong> ${formatDateTime(ngayThanhToan)}<br>
                    <strong>Trạng thái:</strong> <span class="badge ${statusClass === 'payment-success' ? 'bg-success' : statusClass === 'payment-pending' ? 'bg-warning' : 'bg-danger'}">${statusText}</span><br>
                    <strong>Ghi chú:</strong> ${payment.ghiChu || 'Không có'}
                </p>
                <div class="btn-group w-100">
                    <button class="btn btn-outline-primary" onclick="editPayment(${payment.maThanhToan})">
                        <i class="fas fa-edit"></i> Sửa
                    </button>
                    <button class="btn btn-outline-danger" onclick="deletePayment(${payment.maThanhToan})">
                        <i class="fas fa-trash"></i> Xóa
                    </button>
                </div>
            </div>
        </div>
    `;
    
    return col;
}

// Get status class based on status ID
function getStatusClass(statusId) {
    switch (statusId) {
        case 1: return 'payment-success';
        case 2: return 'payment-pending';
        case 3: return 'payment-failed';
        default: return '';
    }
}

// Get status text based on status ID
function getStatusText(statusId) {
    switch (statusId) {
        case 1: return 'Đã thanh toán';
        case 2: return 'Đang chờ';
        case 3: return 'Thất bại';
        default: return 'Không xác định';
    }
}

// Format date and time
function formatDateTime(dateString) {
    if (!dateString) {
        return 'Chưa có';
    }
    
    try {
        const date = new Date(dateString);
        if (isNaN(date.getTime())) {
            return 'Ngày không hợp lệ';
        }
        return date.toLocaleString('vi-VN', {
            hour: '2-digit',
            minute: '2-digit',
            second: '2-digit',
            day: '2-digit',
            month: '2-digit',
            year: 'numeric'
        });
    } catch (error) {
        console.error('Lỗi format ngày:', error);
        return 'Ngày không hợp lệ';
    }
}

// Save new payment
savePaymentBtn.addEventListener('click', async () => {
    const formData = new FormData(addPaymentForm);
    const paymentData = {
        maHoaDon: parseInt(formData.get('maHoaDon')),
        soTien: parseFloat(formData.get('soTien')),
        phuongThucThanhToan: formData.get('phuongThucThanhToan'),
        ngayThanhToan: formData.get('ngayThanhToan'),
        ghiChu: formData.get('ghiChu') || null,
        trangThai: 1 // Default to successful payment
    };

    try {
        const token = localStorage.getItem('token');
        if (!token) {
            throw new Error('Không có token xác thực');
        }

        // Nếu chọn thanh toán MoMo
        if (paymentData.phuongThucThanhToan === 'MoMo') {
            const momoResponse = await createMomoPayment(paymentData);
            if (momoResponse && momoResponse.payUrl) {
                window.location.href = momoResponse.payUrl;
                return;
            }
        }

        const response = await fetch(`${Config.API_URL}${Config.ENDPOINTS.THANH_TOAN}`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${token}`,
                'Accept': 'application/json'
            },
            body: JSON.stringify(paymentData)
        });

        if (response.status === 401) {
            window.location.href = 'login.html';
            return;
        }

        if (!response.ok) {
            throw new Error('Lỗi khi thêm thanh toán');
        }

        showAlert('Thêm thanh toán thành công', 'success');
        const modal = bootstrap.Modal.getInstance(document.getElementById('addPaymentModal'));
        if (modal) {
            modal.hide();
        }
        
        // Reset form và đảm bảo title đúng
        addPaymentForm.reset();
        document.querySelector('#addPaymentModal .modal-title').textContent = 'Thêm thanh toán';
        
        loadPayments();
    } catch (error) {
        console.error('Error adding payment:', error);
        showAlert('Không thể thêm thanh toán', 'danger');
    }
});

// Create MoMo payment
async function createMomoPayment(paymentData) {
    try {
        const token = localStorage.getItem('token');
        if (!token) {
            throw new Error('Không có token xác thực');
        }

        const response = await fetch(`${Config.API_URL}${Config.ENDPOINTS.THANH_TOAN}/Momo`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${token}`,
                'Accept': 'application/json'
            },
            body: JSON.stringify(paymentData)
        });

        if (!response.ok) {
            throw new Error('Không thể tạo thanh toán MoMo');
        }

        return await response.json();
    } catch (error) {
        console.error('Error creating MoMo payment:', error);
        showAlert('Không thể tạo thanh toán MoMo', 'danger');
        return null;
    }
}

// Edit payment
async function editPayment(paymentId) {
    try {
        const token = localStorage.getItem('token');
        if (!token) {
            throw new Error('Không có token xác thực');
        }

        const response = await fetch(`${Config.API_URL}${Config.ENDPOINTS.THANH_TOAN}/${paymentId}`, {
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
            throw new Error('Không thể tải thông tin thanh toán');
        }

        const payment = await response.json();
        console.log('Payment data loaded:', payment);
        
        // Populate form with payment data
        const form = document.getElementById('addPaymentForm');
        
        // Xử lý số tiền - kiểm tra nhiều tên field có thể có
        const soTien = payment.soTien || payment.tongTien || payment.soTienThanhToan || 0;
        const soTienInput = form.querySelector('[name="soTien"]');
        if (soTienInput) {
            soTienInput.value = soTien;
        }
        
        // Xử lý phương thức thanh toán - kiểm tra nhiều tên field có thể có
        const phuongThuc = payment.phuongThucThanhToan || payment.hinhThucThanhToan || payment.phuongThuc || '';
        const phuongThucSelect = form.querySelector('[name="phuongThucThanhToan"]');
        if (phuongThucSelect) {
            phuongThucSelect.value = phuongThuc;
        }
        
        // Xử lý mã hóa đơn
        const maHoaDonSelect = form.querySelector('[name="maHoaDon"]');
        if (maHoaDonSelect && payment.maHoaDon) {
            maHoaDonSelect.value = payment.maHoaDon;
        }
        
        // Xử lý ngày thanh toán
        const ngayThanhToanInput = form.querySelector('[name="ngayThanhToan"]');
        if (ngayThanhToanInput && payment.ngayThanhToan) {
            // Format date for date input (YYYY-MM-DD)
            const dateStr = payment.ngayThanhToan;
            if (dateStr.includes('T')) {
                ngayThanhToanInput.value = dateStr.split('T')[0];
            } else {
                ngayThanhToanInput.value = dateStr.slice(0, 10);
            }
        }
        
        // Xử lý ghi chú
        const ghiChuTextarea = form.querySelector('[name="ghiChu"]');
        if (ghiChuTextarea) {
            ghiChuTextarea.value = payment.ghiChu || '';
        }
        
        // Update modal title
        document.querySelector('#addPaymentModal .modal-title').textContent = 'Sửa Thanh Toán';
        
        // Reset save button handler
        savePaymentBtn.onclick = () => updatePayment(paymentId);
        
        // Show modal
        const modal = new bootstrap.Modal(document.getElementById('addPaymentModal'));
        modal.show();
    } catch (error) {
        console.error('Error loading payment details:', error);
        showAlert('Không thể tải thông tin thanh toán', 'danger');
    }
}

// Update payment
async function updatePayment(paymentId) {
    const formData = new FormData(addPaymentForm);
    
    // Lấy số tiền và validate
    const soTienValue = formData.get('soTien');
    const soTien = soTienValue ? parseFloat(soTienValue) : 0;
    
    if (isNaN(soTien) || soTien <= 0) {
        showAlert('Vui lòng nhập số tiền hợp lệ', 'danger');
        return;
    }
    
    const paymentData = {
        maThanhToan: paymentId,
        maHoaDon: parseInt(formData.get('maHoaDon')) || null,
        soTien: soTien,
        phuongThucThanhToan: formData.get('phuongThucThanhToan') || 'Tiền mặt',
        ngayThanhToan: formData.get('ngayThanhToan') || new Date().toISOString().split('T')[0],
        ghiChu: formData.get('ghiChu') || null,
        trangThai: 1 // Default to successful payment
    };
    
    console.log('Updating payment with data:', paymentData);

    try {
        const token = localStorage.getItem('token');
        if (!token) {
            throw new Error('Không có token xác thực');
        }

        const response = await fetch(`${Config.API_URL}${Config.ENDPOINTS.THANH_TOAN}/${paymentId}`, {
            method: 'PUT',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${token}`,
                'Accept': 'application/json'
            },
            body: JSON.stringify(paymentData)
        });

        if (response.status === 401) {
            window.location.href = 'login.html';
            return;
        }

        if (!response.ok) {
            throw new Error('Lỗi khi cập nhật thanh toán');
        }

        showAlert('Cập nhật thanh toán thành công', 'success');
        const modal = bootstrap.Modal.getInstance(document.getElementById('addPaymentModal'));
        if (modal) {
            modal.hide();
        }
        
        // Reset form và button handler
        addPaymentForm.reset();
        savePaymentBtn.onclick = null; // Reset để quay về handler mặc định
        document.querySelector('#addPaymentModal .modal-title').textContent = 'Thêm thanh toán';
        
        loadPayments();
    } catch (error) {
        console.error('Error updating payment:', error);
        showAlert('Không thể cập nhật thanh toán', 'danger');
    }
}

// Delete payment
async function deletePayment(paymentId) {
    if (!confirm('Bạn có chắc chắn muốn xóa thanh toán này?')) return;

    try {
        const token = localStorage.getItem('token');
        if (!token) {
            throw new Error('Không có token xác thực');
        }

        const response = await fetch(`${Config.API_URL}${Config.ENDPOINTS.THANH_TOAN}/${paymentId}`, {
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
            throw new Error('Lỗi khi xóa thanh toán');
        }

        showAlert('Xóa thanh toán thành công', 'success');
        loadPayments();
    } catch (error) {
        console.error('Error deleting payment:', error);
        showAlert('Không thể xóa thanh toán', 'danger');
    }
}

// Apply filters
async function applyFilters() {
    try {
        const token = localStorage.getItem('token');
        if (!token) {
            throw new Error('Không có token xác thực');
        }

        let url = `${Config.API_URL}${Config.ENDPOINTS.THANH_TOAN}`;
        const params = new URLSearchParams();
        
        if (statusFilter.value) {
            params.append('trangThai', statusFilter.value);
        }
        if (methodFilter.value) {
            params.append('phuongThuc', methodFilter.value);
        }
        if (startDate.value) {
            params.append('startDate', startDate.value);
        }
        if (endDate.value) {
            params.append('endDate', endDate.value);
        }
        
        if (params.toString()) {
            url += `?${params.toString()}`;
        }
        
        const response = await fetch(url, {
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
            throw new Error('Không thể áp dụng bộ lọc');
        }

        const payments = await response.json();
        displayPayments(payments);
    } catch (error) {
        console.error('Error applying filters:', error);
        showAlert('Không thể áp dụng bộ lọc', 'danger');
    }
}

// Setup function to open add payment modal
window.setupThanhToan = function() {
    // Reset form
    addPaymentForm.reset();
    
    // Reset modal title
    document.querySelector('#addPaymentModal .modal-title').textContent = 'Thêm thanh toán';
    
    // Reset save button handler to default (add new payment)
    savePaymentBtn.onclick = null;
    savePaymentBtn.onclick = async () => {
        const formData = new FormData(addPaymentForm);
        const paymentData = {
            maHoaDon: parseInt(formData.get('maHoaDon')),
            soTien: parseFloat(formData.get('soTien')),
            phuongThucThanhToan: formData.get('phuongThucThanhToan'),
            ngayThanhToan: formData.get('ngayThanhToan'),
            ghiChu: formData.get('ghiChu') || null,
            trangThai: 1 // Default to successful payment
        };

        try {
            const token = localStorage.getItem('token');
            if (!token) {
                throw new Error('Không có token xác thực');
            }

            // Nếu chọn thanh toán MoMo
            if (paymentData.phuongThucThanhToan === 'MoMo') {
                const momoResponse = await createMomoPayment(paymentData);
                if (momoResponse && momoResponse.payUrl) {
                    window.location.href = momoResponse.payUrl;
                    return;
                }
            }

            const response = await fetch(`${Config.API_URL}${Config.ENDPOINTS.THANH_TOAN}`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'Authorization': `Bearer ${token}`,
                    'Accept': 'application/json'
                },
                body: JSON.stringify(paymentData)
            });

            if (response.status === 401) {
                window.location.href = 'login.html';
                return;
            }

            if (!response.ok) {
                throw new Error('Lỗi khi thêm thanh toán');
            }

            showAlert('Thêm thanh toán thành công', 'success');
            const modal = bootstrap.Modal.getInstance(document.getElementById('addPaymentModal'));
            if (modal) {
                modal.hide();
            }
            addPaymentForm.reset();
            loadPayments();
        } catch (error) {
            console.error('Error adding payment:', error);
            showAlert('Không thể thêm thanh toán', 'danger');
        }
    };
    
    // Show modal
    const modal = new bootstrap.Modal(document.getElementById('addPaymentModal'));
    modal.show();
};

// Show alert message
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