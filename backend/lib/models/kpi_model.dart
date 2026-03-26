class KPIModel {
  // Occupancy - Tỷ lệ lấp đầy
  final double occupancyRate; // %
  final int totalRooms;
  final int occupiedRooms;
  final int vacantRooms;

  // DSO - Days Sales Outstanding
  final double dso; // Số ngày
  final int unpaidInvoices;
  final double totalUnpaidAmount;
  final double averageUnpaidDays;

  // Số sự cố/100 phòng
  final double incidentsPer100Rooms; // Số sự cố trên 100 phòng
  final int maintenanceRooms; // Số phòng bảo trì (TrangThai = 2)
  final int totalIncidents; // Tổng số sự cố (có thể là phòng bảo trì hoặc thông báo sự cố)

  KPIModel({
    required this.occupancyRate,
    required this.totalRooms,
    required this.occupiedRooms,
    required this.vacantRooms,
    required this.dso,
    required this.unpaidInvoices,
    required this.totalUnpaidAmount,
    required this.averageUnpaidDays,
    required this.incidentsPer100Rooms,
    required this.maintenanceRooms,
    required this.totalIncidents,
  });

  factory KPIModel.fromJson(Map<String, dynamic> json) {
    return KPIModel(
      occupancyRate: ((json['occupancyRate'] ?? json['OccupancyRate'] ?? 0) as num).toDouble(),
      totalRooms: json['totalRooms'] ?? json['TotalRooms'] ?? 0,
      occupiedRooms: json['occupiedRooms'] ?? json['OccupiedRooms'] ?? 0,
      vacantRooms: json['vacantRooms'] ?? json['VacantRooms'] ?? 0,
      dso: ((json['dso'] ?? json['DSO'] ?? 0) as num).toDouble(),
      unpaidInvoices: json['unpaidInvoices'] ?? json['UnpaidInvoices'] ?? 0,
      totalUnpaidAmount: ((json['totalUnpaidAmount'] ?? json['TotalUnpaidAmount'] ?? 0) as num).toDouble(),
      averageUnpaidDays: ((json['averageUnpaidDays'] ?? json['AverageUnpaidDays'] ?? 0) as num).toDouble(),
      incidentsPer100Rooms: ((json['incidentsPer100Rooms'] ?? json['IncidentsPer100Rooms'] ?? 0) as num).toDouble(),
      maintenanceRooms: json['maintenanceRooms'] ?? json['MaintenanceRooms'] ?? 0,
      totalIncidents: json['totalIncidents'] ?? json['TotalIncidents'] ?? 0,
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'occupancyRate': occupancyRate,
      'totalRooms': totalRooms,
      'occupiedRooms': occupiedRooms,
      'vacantRooms': vacantRooms,
      'dso': dso,
      'unpaidInvoices': unpaidInvoices,
      'totalUnpaidAmount': totalUnpaidAmount,
      'averageUnpaidDays': averageUnpaidDays,
      'incidentsPer100Rooms': incidentsPer100Rooms,
      'maintenanceRooms': maintenanceRooms,
      'totalIncidents': totalIncidents,
    };
  }
}

