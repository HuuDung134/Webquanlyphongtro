import 'dart:convert';
import 'package:http/http.dart' as http;
import 'package:shared_preferences/shared_preferences.dart';
import '../config/app_config.dart';
import '../models/kpi_model.dart';

class KPIService {
  static const String baseUrl = "${AppConfig.baseUrl}/Dashboard";

  // Lấy token từ SharedPreferences
  static Future<String?> _getToken() async {
    final prefs = await SharedPreferences.getInstance();
    return prefs.getString("token");
  }

  // Lấy KPI data từ endpoint mới
  static Future<Map<String, dynamic>> getKPIData() async {
    try {
      final token = await _getToken();
      if (token == null) {
        throw Exception("Chưa đăng nhập");
      }

      // Gọi endpoint KPI mới từ backend
      final kpiUrl = Uri.parse("$baseUrl/kpi");
      final response = await http.get(
        kpiUrl,
        headers: {
          "Content-Type": "application/json",
          "Authorization": "Bearer $token",
        },
      );

      if (response.statusCode != 200) {
        throw Exception("Lỗi khi lấy KPI: ${response.statusCode} - ${response.body}");
      }

      final kpiData = jsonDecode(response.body);
      
      // Map từ backend response sang model
      final mappedData = {
        "occupancyRate": kpiData["OccupancyRate"] ?? kpiData["occupancyRate"] ?? 0.0,
        "totalRooms": kpiData["TotalRooms"] ?? kpiData["totalRooms"] ?? 0,
        "occupiedRooms": kpiData["OccupiedRooms"] ?? kpiData["occupiedRooms"] ?? 0,
        "vacantRooms": kpiData["VacantRooms"] ?? kpiData["vacantRooms"] ?? 0,
        "dso": kpiData["DSO"] ?? kpiData["dso"] ?? 0.0,
        "unpaidInvoices": kpiData["UnpaidInvoices"] ?? kpiData["unpaidInvoices"] ?? 0,
        "totalUnpaidAmount": kpiData["TotalUnpaidAmount"] ?? kpiData["totalUnpaidAmount"] ?? 0.0,
        "averageUnpaidDays": kpiData["AverageUnpaidDays"] ?? kpiData["averageUnpaidDays"] ?? 0.0,
        "incidentsPer100Rooms": kpiData["IncidentsPer100Rooms"] ?? kpiData["incidentsPer100Rooms"] ?? 0.0,
        "maintenanceRooms": kpiData["MaintenanceRooms"] ?? kpiData["maintenanceRooms"] ?? 0,
        "totalIncidents": kpiData["TotalIncidents"] ?? kpiData["totalIncidents"] ?? 0,
      };

      return {
        "success": true,
        "data": KPIModel.fromJson(mappedData),
      };
    } catch (e) {
      return {
        "success": false,
        "message": e.toString().replaceAll("Exception: ", ""),
      };
    }
  }
}

