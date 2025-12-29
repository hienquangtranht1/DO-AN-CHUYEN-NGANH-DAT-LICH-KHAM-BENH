import 'dart:convert';
import 'package:http/http.dart' as http;
import '../models/doctor.dart';
import 'api_service.dart';

class DoctorService {
  Future<Map<String, dynamic>> getDoctors({int page = 1, String search = ''}) async {
    try {
      final uri = Uri.parse('${ApiService.base}/api/doctors?page=$page&pageSize=10&search=$search');
      final response = await http.get(uri);

      if (response.statusCode == 200) {
        final body = jsonDecode(response.body);
        final List<dynamic> list = body['data'];
        final bool hasNext = body['hasNext'];

        List<Doctor> doctors = list.map((e) => Doctor.fromJson(e)).toList();

        return {
          'doctors': doctors,
          'hasNext': hasNext
        };
      }
      return {'doctors': [], 'hasNext': false};
    } catch (e) {
      print("Lỗi tải bác sĩ: $e");
      return {'doctors': [], 'hasNext': false};
    }
  }
}