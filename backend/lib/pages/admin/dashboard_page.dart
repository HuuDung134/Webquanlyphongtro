import 'package:flutter/material.dart';
import '../../services/kpi_service.dart';
import '../../models/kpi_model.dart';

class DashboardPage extends StatefulWidget {
  const DashboardPage({super.key});

  @override
  State<DashboardPage> createState() => _DashboardPageState();
}

class _DashboardPageState extends State<DashboardPage> {
  bool _isLoading = false;
  KPIModel? _kpiData;
  String? _errorMessage;

  @override
  void initState() {
    super.initState();
    _loadKPIData();
  }

  Future<void> _loadKPIData() async {
    setState(() {
      _isLoading = true;
      _errorMessage = null;
    });

    final result = await KPIService.getKPIData();

    setState(() {
      _isLoading = false;
      if (result["success"] == true) {
        _kpiData = result["data"] as KPIModel;
      } else {
        _errorMessage = result["message"] ?? "Lỗi khi tải dữ liệu KPI";
      }
    });
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Dashboard KPI'),
        centerTitle: true,
        actions: [
          IconButton(
            icon: const Icon(Icons.refresh),
            onPressed: _isLoading ? null : _loadKPIData,
          ),
        ],
      ),
      body: _isLoading
          ? const Center(child: CircularProgressIndicator())
          : _errorMessage != null
              ? Center(
                  child: Column(
                    mainAxisAlignment: MainAxisAlignment.center,
                    children: [
                      Icon(
                        Icons.error_outline,
                        size: 64,
                        color: Colors.red.shade300,
                      ),
                      const SizedBox(height: 16),
                      Text(
                        _errorMessage!,
                        style: TextStyle(
                          fontSize: 16,
                          color: Colors.red.shade700,
                        ),
                        textAlign: TextAlign.center,
                      ),
                      const SizedBox(height: 24),
                      ElevatedButton.icon(
                        onPressed: _loadKPIData,
                        icon: const Icon(Icons.refresh),
                        label: const Text('Thử lại'),
                      ),
                    ],
                  ),
                )
              : _kpiData == null
                  ? const Center(child: Text('Không có dữ liệu'))
                  : RefreshIndicator(
                      onRefresh: _loadKPIData,
                      child: SingleChildScrollView(
                        padding: const EdgeInsets.all(16.0),
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            const Text(
                              'Chỉ số hiệu suất (KPI)',
                              style: TextStyle(
                                fontSize: 24,
                                fontWeight: FontWeight.bold,
                              ),
                            ),
                            const SizedBox(height: 24),
                            
                            // Occupancy Rate Card
                            _buildKPICard(
                              title: 'Tỷ lệ lấp đầy (Occupancy)',
                              value: '${_kpiData!.occupancyRate.toStringAsFixed(1)}%',
                              subtitle: '${_kpiData!.occupiedRooms}/${_kpiData!.totalRooms} phòng đang thuê',
                              icon: Icons.home,
                              color: Colors.blue,
                              trend: _kpiData!.occupancyRate >= 80 
                                  ? 'Tốt' 
                                  : _kpiData!.occupancyRate >= 60 
                                      ? 'Khá' 
                                      : 'Cần cải thiện',
                            ),
                            
                            const SizedBox(height: 16),
                            
                            // DSO Card
                            _buildKPICard(
                              title: 'DSO (Days Sales Outstanding)',
                              value: '${_kpiData!.dso.toStringAsFixed(1)} ngày',
                              subtitle: '${_kpiData!.unpaidInvoices} hóa đơn chưa thanh toán\n${_formatCurrency(_kpiData!.totalUnpaidAmount)} VNĐ',
                              icon: Icons.pending_actions,
                              color: Colors.orange,
                              trend: _kpiData!.dso <= 30 
                                  ? 'Tốt' 
                                  : _kpiData!.dso <= 60 
                                      ? 'Khá' 
                                      : 'Cần cải thiện',
                            ),
                            
                            const SizedBox(height: 16),
                            
                            // Incidents per 100 Rooms Card
                            _buildKPICard(
                              title: 'Số sự cố / 100 phòng',
                              value: '${_kpiData!.incidentsPer100Rooms.toStringAsFixed(1)}',
                              subtitle: '${_kpiData!.maintenanceRooms} phòng đang bảo trì\nTổng: ${_kpiData!.totalRooms} phòng',
                              icon: Icons.warning,
                              color: Colors.red,
                              trend: _kpiData!.incidentsPer100Rooms <= 5 
                                  ? 'Tốt' 
                                  : _kpiData!.incidentsPer100Rooms <= 10 
                                      ? 'Khá' 
                                      : 'Cần cải thiện',
                            ),
                            
                            const SizedBox(height: 32),
                            
                            // Chi tiết bổ sung
                            Card(
                              elevation: 4,
                              shape: RoundedRectangleBorder(
                                borderRadius: BorderRadius.circular(14),
                              ),
                              child: Padding(
                                padding: const EdgeInsets.all(16.0),
                                child: Column(
                                  crossAxisAlignment: CrossAxisAlignment.start,
                                  children: [
                                    const Text(
                                      'Chi tiết',
                                      style: TextStyle(
                                        fontSize: 18,
                                        fontWeight: FontWeight.bold,
                                      ),
                                    ),
                                    const SizedBox(height: 16),
                                    _buildDetailRow('Tổng số phòng', '${_kpiData!.totalRooms}'),
                                    _buildDetailRow('Phòng đang thuê', '${_kpiData!.occupiedRooms}'),
                                    _buildDetailRow('Phòng trống', '${_kpiData!.vacantRooms}'),
                                    _buildDetailRow('Phòng bảo trì', '${_kpiData!.maintenanceRooms}'),
                                    const Divider(height: 24),
                                    _buildDetailRow('Hóa đơn chưa thanh toán', '${_kpiData!.unpaidInvoices}'),
                                    _buildDetailRow('Tổng tiền chưa thu', '${_formatCurrency(_kpiData!.totalUnpaidAmount)} VNĐ'),
                                    _buildDetailRow('Số ngày trung bình chưa thu', '${_kpiData!.dso.toStringAsFixed(1)} ngày'),
                                  ],
                                ),
                              ),
                            ),
                          ],
                        ),
                      ),
                    ),
    );
  }

  Widget _buildKPICard({
    required String title,
    required String value,
    required String subtitle,
    required IconData icon,
    required Color color,
    required String trend,
  }) {
    Color trendColor;
    IconData trendIcon;
    
    if (trend == 'Tốt') {
      trendColor = Colors.green;
      trendIcon = Icons.trending_up;
    } else if (trend == 'Khá') {
      trendColor = Colors.orange;
      trendIcon = Icons.trending_flat;
    } else {
      trendColor = Colors.red;
      trendIcon = Icons.trending_down;
    }

    return Card(
      elevation: 4,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(14),
      ),
      child: Container(
        decoration: BoxDecoration(
          gradient: LinearGradient(
            begin: Alignment.topLeft,
            end: Alignment.bottomRight,
            colors: [
              color.withValues(alpha: 0.1),
              color.withValues(alpha: 0.05),
            ],
          ),
          borderRadius: BorderRadius.circular(14),
        ),
        child: Padding(
          padding: const EdgeInsets.all(20.0),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Row(
                children: [
                  Container(
                    padding: const EdgeInsets.all(12),
                    decoration: BoxDecoration(
                      color: color.withValues(alpha: 0.2),
                      borderRadius: BorderRadius.circular(12),
                    ),
                    child: Icon(icon, color: color, size: 28),
                  ),
                  const SizedBox(width: 16),
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(
                          title,
                          style: const TextStyle(
                            fontSize: 16,
                            fontWeight: FontWeight.w600,
                            color: Colors.black87,
                          ),
                        ),
                        const SizedBox(height: 4),
                        Row(
                          children: [
                            Icon(trendIcon, size: 16, color: trendColor),
                            const SizedBox(width: 4),
                            Text(
                              trend,
                              style: TextStyle(
                                fontSize: 12,
                                color: trendColor,
                                fontWeight: FontWeight.w500,
                              ),
                            ),
                          ],
                        ),
                      ],
                    ),
                  ),
                ],
              ),
              const SizedBox(height: 20),
              Text(
                value,
                style: TextStyle(
                  fontSize: 36,
                  fontWeight: FontWeight.bold,
                  color: color,
                ),
              ),
              const SizedBox(height: 8),
              Text(
                subtitle,
                style: TextStyle(
                  fontSize: 14,
                  color: Colors.grey.shade700,
                  height: 1.5,
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }

  Widget _buildDetailRow(String label, String value) {
    return Padding(
      padding: const EdgeInsets.only(bottom: 12.0),
      child: Row(
        mainAxisAlignment: MainAxisAlignment.spaceBetween,
        children: [
          Text(
            label,
            style: TextStyle(
              fontSize: 14,
              color: Colors.grey.shade700,
            ),
          ),
          Text(
            value,
            style: const TextStyle(
              fontSize: 14,
              fontWeight: FontWeight.w600,
            ),
          ),
        ],
      ),
    );
  }

  String _formatCurrency(dynamic value) {
    if (value == null) return '0';
    final numValue = (value is int || value is double)
        ? value.toDouble()
        : (double.tryParse(value.toString()) ?? 0);
    return numValue.toStringAsFixed(0).replaceAllMapped(
          RegExp(r'(\d{1,3})(?=(\d{3})+(?!\d))'),
          (Match m) => '${m[1]},',
        );
  }
}

