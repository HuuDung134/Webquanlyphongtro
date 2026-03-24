import { baseUrl } from './baseUrl.js';

// Kiểm tra đăng nhập
function checkAuth() {
    const token = localStorage.getItem('token');
    const user = JSON.parse(localStorage.getItem('user'));
    
    if (!token || !user) {
        window.location.href = 'login.html';
        return;
    }

    // Hiển thị thông tin người dùng
    document.getElementById('userFullname').textContent = user.fullname;
}

// Đăng xuất
window.logout = function() {
    localStorage.removeItem('token');
    localStorage.removeItem('user');
    window.location.href = 'login.html';
}

// Format số tiền
function formatCurrency(amount) {
    return new Intl.NumberFormat('vi-VN', {
        style: 'currency',
        currency: 'VND'
    }).format(amount);
}

// Format ngày tháng
function formatDate(dateString) {
    const date = new Date(dateString);
    return date.toLocaleDateString('vi-VN');
}

// Lấy thống kê tổng quan
async function loadDashboardStatistics() {
    try {
        const response = await fetch(`${baseUrl}/api/dashboard/statistics`, {
            headers: {
                'Authorization': `Bearer ${localStorage.getItem('token')}`
            }
        });
        const data = await response.json();

        // Hiển thị số phòng trống (vacantRooms)
        if (typeof data.vacantRooms !== 'undefined') {
            document.getElementById('rentedRooms').textContent = data.vacantRooms;
        } else if (typeof data.totalRooms !== 'undefined' && typeof data.occupiedRooms !== 'undefined') {
            // Tính số phòng trống = tổng phòng - phòng đang thuê
            const vacantRooms = data.totalRooms - data.occupiedRooms;
            document.getElementById('rentedRooms').textContent = vacantRooms;
        } else if (Array.isArray(data.roomStatuses)) {
            // Tính từ roomStatuses nếu có
            const totalRooms = data.roomStatuses.length;
            const occupiedRooms = data.roomStatuses.filter(r => r.status === 1).length;
            document.getElementById('rentedRooms').textContent = totalRooms - occupiedRooms;
        } else {
            document.getElementById('rentedRooms').textContent = '0';
        }
        
        document.getElementById('totalRooms').textContent = data.totalRooms || '0';
        document.getElementById('totalRevenue').textContent = formatCurrency(data.totalRevenue || 0);
        document.getElementById('totalTenants').textContent = data.totalTenants || '0';
    } catch (error) {
        console.error('Error loading dashboard statistics:', error);
        showError('Không thể tải thống kê tổng quan');
    }
}

// Lấy dữ liệu doanh thu theo tháng
window.loadMonthlyRevenue = async function() {
    const loadingDiv = document.getElementById('revenueChartLoading');
    if (loadingDiv) loadingDiv.style.display = 'flex';
    try {
        const yearSelect = document.getElementById('revenueYear');
        const selectedYear = yearSelect.value;
        const response = await fetch(`${baseUrl}/api/dashboard/revenue-by-month-year?year=${selectedYear}`, {
            headers: {
                'Authorization': `Bearer ${localStorage.getItem('token')}`
            }
        });
        const data = await response.json();
        const ctx = document.getElementById('revenueChart');
        if (window.revenueChart instanceof Chart) {
            window.revenueChart.destroy();
        }
        if (!data || data.length === 0) {
            // Hiển thị thông báo không có dữ liệu
            ctx.getContext('2d').clearRect(0, 0, ctx.width, ctx.height);
            ctx.getContext('2d').font = '16px Arial';
            ctx.getContext('2d').fillStyle = '#888';
            ctx.getContext('2d').textAlign = 'center';
            ctx.getContext('2d').fillText('Không có dữ liệu', ctx.width / 2, ctx.height / 2);
            return;
        }
        // Chuẩn bị dữ liệu cho biểu đồ
        const labels = data.map(item => `Tháng ${item.month}`);
        const values = data.map(item => item.totalRevenue);
        window.revenueChart = new Chart(ctx, {
            type: 'line',
            data: {
                labels: labels,
                datasets: [{
                    label: 'Doanh thu',
                    data: values,
                    borderColor: 'rgb(75, 192, 192)',
                    tension: 0.1,
                    fill: true,
                    backgroundColor: 'rgba(75, 192, 192, 0.1)'
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                scales: {
                    y: {
                        beginAtZero: true,
                        ticks: {
                            callback: function(value) {
                                return formatCurrency(value);
                            }
                        }
                    }
                },
                plugins: {
                    tooltip: {
                        callbacks: {
                            label: function(context) {
                                return formatCurrency(context.raw);
                            }
                        }
                    }
                }
            }
        });
    } catch (error) {
        showError('Không thể tải dữ liệu doanh thu');
        // Xóa biểu đồ nếu có lỗi
        const ctx = document.getElementById('revenueChart');
        if (window.revenueChart instanceof Chart) {
            window.revenueChart.destroy();
        }
        ctx.getContext('2d').clearRect(0, 0, ctx.width, ctx.height);
        ctx.getContext('2d').font = '16px Arial';
        ctx.getContext('2d').fillStyle = '#888';
        ctx.getContext('2d').textAlign = 'center';
        ctx.getContext('2d').fillText('Không có dữ liệu', ctx.width / 2, ctx.height / 2);
    } finally {
        if (loadingDiv) loadingDiv.style.display = 'none';
    }
}

// Khởi tạo năm cho select box
function initializeYearSelect() {
    const yearSelect = document.getElementById('revenueYear');
    const currentYear = new Date().getFullYear();
    
    // Thêm các năm từ 2020 đến năm hiện tại
    for (let year = currentYear; year >= 2020; year--) {
        const option = document.createElement('option');
        option.value = year;
        option.textContent = year;
        yearSelect.appendChild(option);
    }

    // Thêm event listener để cập nhật biểu đồ khi chọn năm
    yearSelect.addEventListener('change', loadMonthlyRevenue);
}

// Lấy trạng thái phòng
window.loadRoomStatus = async function() {
    const loadingDiv = document.getElementById('roomStatusChartLoading');
    if (loadingDiv) loadingDiv.style.display = 'flex';
    try {
        const response = await fetch(`${baseUrl}/api/dashboard/room-status`, {
            headers: {
                'Authorization': `Bearer ${localStorage.getItem('token')}`
            }
        });
        const data = await response.json();
        const ctx = document.getElementById('roomStatusChart');
        if (window.roomStatusChart instanceof Chart) {
            window.roomStatusChart.destroy();
        }
        if (!data || data.length === 0) {
            ctx.getContext('2d').clearRect(0, 0, ctx.width, ctx.height);
            ctx.getContext('2d').font = '16px Arial';
            ctx.getContext('2d').fillStyle = '#888';
            ctx.getContext('2d').textAlign = 'center';
            ctx.getContext('2d').fillText('Không có dữ liệu', ctx.width / 2, ctx.height / 2);
            return;
        }
        const labels = data.map(item => item.status);
        const values = data.map(item => item.count);
        window.roomStatusChart = new Chart(ctx, {
            type: 'doughnut',
            data: {
                labels: labels,
                datasets: [{
                    data: values,
                    backgroundColor: [
                        'rgb(54, 162, 235)',
                        'rgb(255, 99, 132)',
                        'rgb(255, 205, 86)'
                    ]
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false
            }
        });
    } catch (error) {
        showError('Không thể tải trạng thái phòng');
        const ctx = document.getElementById('roomStatusChart');
        if (window.roomStatusChart instanceof Chart) {
            window.roomStatusChart.destroy();
        }
        ctx.getContext('2d').clearRect(0, 0, ctx.width, ctx.height);
        ctx.getContext('2d').font = '16px Arial';
        ctx.getContext('2d').fillStyle = '#888';
        ctx.getContext('2d').textAlign = 'center';
        ctx.getContext('2d').fillText('Không có dữ liệu', ctx.width / 2, ctx.height / 2);
    } finally {
        if (loadingDiv) loadingDiv.style.display = 'none';
    }
}

// Lấy hóa đơn gần đây
async function loadRecentBills() {
    try {
        const response = await fetch(`${baseUrl}/api/dashboard/recent-bills`, {
            headers: {
                'Authorization': `Bearer ${localStorage.getItem('token')}`
            }
        });
        const data = await response.json();

        // Cập nhật bảng hóa đơn gần đây
        const tbody = document.querySelector('#recentBills tbody');
        tbody.innerHTML = '';

        data.forEach(bill => {
            const row = document.createElement('tr');
            row.innerHTML = `
                <td>${bill.maHoaDon}</td>
                <td>${formatDate(bill.ngayLap)}</td>
                <td>${bill.roomNumber}</td>
                <td>${bill.tenantName}</td>
                <td>${formatCurrency(bill.tongTien)}</td>
            `;
            tbody.appendChild(row);
        });
    } catch (error) {
        console.error('Error loading recent bills:', error);
    }
}

// Lấy danh sách phòng chưa thanh toán
window.loadUnpaidRooms = async function() {
    try {
        const tbody = document.querySelector('#unpaidRooms tbody');
        tbody.innerHTML = `
            <tr>
                <td colspan="4" class="text-center">
                    <div class="spinner-border text-primary" role="status">
                        <span class="visually-hidden">Loading...</span>
                    </div>
                </td>
            </tr>
        `;

        const response = await fetch(`${baseUrl}/api/dashboard/unpaid-rooms`, {
            headers: {
                'Authorization': `Bearer ${localStorage.getItem('token')}`
            }
        });
        const data = await response.json();

        tbody.innerHTML = '';

        if (data.length === 0) {
            const row = document.createElement('tr');
            row.innerHTML = '<td colspan="4" class="text-center">Không có phòng nào chưa thanh toán</td>';
            tbody.appendChild(row);
            return;
        }

        data.forEach(room => {
            const row = document.createElement('tr');
            row.innerHTML = `
                <td>${room.roomNumber}</td>
                <td>${room.tenantName}</td>
                <td>${formatCurrency(room.amount)}</td>
                <td>${formatDate(room.dueDate)}</td>
            `;
            tbody.appendChild(row);
        });
    } catch (error) {
        console.error('Error loading unpaid rooms:', error);
        showError('Không thể tải danh sách phòng chưa thanh toán');
    }
}

// Lấy danh sách hợp đồng sắp hết hạn
window.loadExpiringContracts = async function() {
    try {
        const tbody = document.querySelector('#expiringContracts tbody');
        tbody.innerHTML = `
            <tr>
                <td colspan="4" class="text-center">
                    <div class="spinner-border text-primary" role="status">
                        <span class="visually-hidden">Loading...</span>
                    </div>
                </td>
            </tr>
        `;

        const response = await fetch(`${baseUrl}/api/dashboard/expiring-contracts`, {
            headers: {
                'Authorization': `Bearer ${localStorage.getItem('token')}`
            }
        });
        const data = await response.json();

        tbody.innerHTML = '';

        if (data.length === 0) {
            const row = document.createElement('tr');
            row.innerHTML = '<td colspan="4" class="text-center">Không có hợp đồng nào sắp hết hạn</td>';
            tbody.appendChild(row);
            return;
        }

        data.forEach(contract => {
            const row = document.createElement('tr');
            const daysLeft = contract.daysUntilExpiration;
            const statusClass = daysLeft <= 7 ? 'text-danger' : daysLeft <= 15 ? 'text-warning' : '';
            
            row.innerHTML = `
                <td>${contract.roomNumber}</td>
                <td>${contract.tenantName}</td>
                <td>${formatDate(contract.endDate)}</td>
                <td class="${statusClass}">${daysLeft} ngày</td>
            `;
            tbody.appendChild(row);
        });
    } catch (error) {
        console.error('Error loading expiring contracts:', error);
        showError('Không thể tải danh sách hợp đồng sắp hết hạn');
    }
}

// Hiển thị thông báo lỗi
function showError(message) {
    // Xóa thông báo lỗi cũ nếu có
    const existingAlert = document.querySelector('.alert-danger');
    if (existingAlert) {
        existingAlert.remove();
    }

    const alertDiv = document.createElement('div');
    alertDiv.className = 'alert alert-danger alert-dismissible fade show';
    alertDiv.role = 'alert';
    alertDiv.innerHTML = `
        ${message}
        <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
    `;

    // Thêm thông báo vào đầu main content
    const mainContent = document.querySelector('.main-content');
    const firstRow = mainContent.querySelector('.row');
    if (firstRow) {
        mainContent.insertBefore(alertDiv, firstRow);
    } else {
        mainContent.appendChild(alertDiv);
    }

    // Tự động ẩn thông báo sau 5 giây
    setTimeout(() => {
        if (alertDiv.parentNode) {
            alertDiv.remove();
        }
    }, 5000);
}

async function loadThongBao() {
    try {
        const res = await fetch(`${baseUrl}/api/thongbao`);
        const data = await res.json();
        const thongBaoDiv = document.getElementById('thongBaoList');
        if (!thongBaoDiv) return;
        
        thongBaoDiv.innerHTML = '';
        if (!data || data.length === 0) {
            thongBaoDiv.innerHTML = '<div class="text-muted text-center py-3">Chưa có thông báo nào</div>';
            return;
        }
        data.forEach(tb => {
            // Escape HTML để tránh XSS
            const title = tb.title.replace(/'/g, "&#39;").replace(/"/g, "&quot;");
            const content = tb.content.replace(/'/g, "&#39;").replace(/"/g, "&quot;");
            const createdAt = new Date(tb.createdAt).toLocaleString('vi-VN');
            
            thongBaoDiv.innerHTML += `
                <div class="card mb-2">
                    <div class="card-body">
                        <div class="d-flex justify-content-between align-items-start">
                            <div class="flex-grow-1">
                                <h6 class="card-title mb-1">${escapeHtml(tb.title)}</h6>
                                <p class="card-text mb-1">${escapeHtml(tb.content)}</p>
                                <small class="text-muted">
                                    <i class="fas fa-clock"></i> ${createdAt}
                                </small>
                            </div>
                            <div class="ms-3">
                                <button class="btn btn-sm btn-outline-primary me-1" onclick="editThongBao(${tb.id}, '${title}', '${content}')" title="Sửa">
                                    <i class="fas fa-edit"></i>
                                </button>
                                <button class="btn btn-sm btn-outline-danger" onclick="deleteThongBao(${tb.id})" title="Xóa">
                                    <i class="fas fa-trash"></i>
                                </button>
                            </div>
                        </div>
                    </div>
                </div>
            `;
        });
    } catch (error) {
        console.error('Không thể tải thông báo hệ thống', error);
        const thongBaoDiv = document.getElementById('thongBaoList');
        if (thongBaoDiv) {
            thongBaoDiv.innerHTML = '<div class="alert alert-danger">Không thể tải danh sách thông báo</div>';
        }
    }
}

// Helper function để escape HTML
function escapeHtml(text) {
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}

// Xóa thông báo
window.deleteThongBao = async function(id) {
    if (!confirm('Bạn có chắc chắn muốn xóa thông báo này?')) return;
    try {
        const res = await fetch(`${baseUrl}/api/thongbao/${id}`, { method: 'DELETE' });
        if (res.ok) {
            await loadThongBao();
            // Hiển thị thông báo thành công
            const thongBaoDiv = document.getElementById('thongBaoList');
            if (thongBaoDiv && thongBaoDiv.children.length === 0) {
                thongBaoDiv.innerHTML = '<div class="text-muted text-center py-3">Chưa có thông báo nào</div>';
            }
        } else {
            alert('Xóa thông báo thất bại!');
        }
    } catch (error) {
        console.error('Error deleting notification:', error);
        alert('Xóa thông báo thất bại!');
    }
}

// Sửa thông báo (mở modal)
window.editThongBao = function(id, title, content) {
    document.getElementById('editThongBaoId').value = id;
    document.getElementById('editThongBaoTitle').value = title;
    document.getElementById('editThongBaoContent').value = content;
    const modal = new bootstrap.Modal(document.getElementById('editThongBaoModal'));
    modal.show();
}

// Đảm bảo function được gọi khi DOM sẵn sàng
document.addEventListener('DOMContentLoaded', () => {
    const saveBtn = document.getElementById('saveEditThongBaoBtn');
    if (saveBtn) {
        saveBtn.onclick = async function() {
            const id = document.getElementById('editThongBaoId').value;
            const title = document.getElementById('editThongBaoTitle').value.trim();
            const content = document.getElementById('editThongBaoContent').value.trim();
            if (!title || !content) {
                alert('Vui lòng nhập đầy đủ tiêu đề và nội dung!');
                return;
            }
            try {
                const res = await fetch(`${baseUrl}/api/thongbao/${id}`, {
                    method: 'PUT',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ title, content })
                });
                if (res.ok) {
                    const modalElement = document.getElementById('editThongBaoModal');
                    const modal = bootstrap.Modal.getInstance(modalElement);
                    if (modal) {
                        modal.hide();
                    }
                    await loadThongBao();
                } else {
                    alert('Cập nhật thông báo thất bại!');
                }
            } catch (error) {
                console.error('Error updating notification:', error);
                alert('Cập nhật thông báo thất bại!');
            }
        };
    }
});

// Toggle hiển thị danh sách thông báo
window.toggleThongBaoList = function() {
    const container = document.getElementById('thongBaoListContainer');
    const icon = document.getElementById('thongBaoToggleIcon');
    if (container.style.display === 'none') {
        container.style.display = 'block';
        icon.classList.remove('fa-chevron-down');
        icon.classList.add('fa-chevron-up');
        loadThongBao(); // Load khi mở
    } else {
        container.style.display = 'none';
        icon.classList.remove('fa-chevron-up');
        icon.classList.add('fa-chevron-down');
    }
}

// Khởi tạo trang
async function initializeDashboard() {
    checkAuth();
    initializeYearSelect();
    await loadDashboardStatistics();
    await loadMonthlyRevenue();
    await loadRoomStatus();
    await loadRecentBills();
    await loadUnpaidRooms();
    await loadExpiringContracts();
}

// Chạy khi trang được tải
document.addEventListener('DOMContentLoaded', () => {
    // ...
    const thongBaoForm = document.getElementById('thongBaoForm');
    if (thongBaoForm) {
        thongBaoForm.onsubmit = async function(e) {
            e.preventDefault();
            const title = document.getElementById('thongBaoTitle').value.trim();
            const content = document.getElementById('thongBaoContent').value.trim();
            if (!title || !content) {
                alert('Vui lòng nhập đầy đủ tiêu đề và nội dung!');
                return;
            }
            const res = await fetch(`${baseUrl}/api/thongbao`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({ title, content })
            });
            if (res.ok) {
                alert('Đã gửi thông báo!');
                document.getElementById('thongBaoTitle').value = '';
                document.getElementById('thongBaoContent').value = '';
                await loadThongBao();
            } else {
                alert('Gửi thông báo thất bại!');
            }
        }
    }
});

document.addEventListener('DOMContentLoaded', initializeDashboard); 