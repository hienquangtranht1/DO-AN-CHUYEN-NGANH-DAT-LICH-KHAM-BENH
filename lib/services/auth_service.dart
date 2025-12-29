import 'dart:convert';
import 'dart:io';
import 'package:shared_preferences/shared_preferences.dart';
import '../models/doctor.dart';
import '../models/payment_res.dart'; // 👇 QUAN TRỌNG: Import model này để sửa lỗi
import 'api_service.dart';

class AuthService {
  final ApiService _api = ApiService();

  Future<void> saveUserSession(int userId, String fullName, String role) async {
    final prefs = await SharedPreferences.getInstance();
    await prefs.setInt('userId', userId);
    await prefs.setString('fullName', fullName);
    await prefs.setString('role', role);
    await prefs.setBool('isLoggedIn', true);
  }

  Future<Map<String, dynamic>?> getUserInfo() async {
    final prefs = await SharedPreferences.getInstance();
    if (prefs.getBool('isLoggedIn') == true) {
      return {
        'userId': prefs.getInt('userId') ?? 0,
        'fullName': prefs.getString('fullName') ?? '',
        'role': prefs.getString('role') ?? '',
      };
    }
    return null;
  }

  Future<int?> getCurrentUserId() async {
    final prefs = await SharedPreferences.getInstance();
    return prefs.getInt('userId');
  }

  Future<void> clearUserSession() async {
    final prefs = await SharedPreferences.getInstance();
    await prefs.clear();
  }

  Future<Map<String, dynamic>> login(String username, String password) async {
    final res = await _api.postJson('/api/user/login', {
      'Username': username,
      'Password': password
    });

    if (res['status'] == 200) {
      try {
        final body = jsonDecode(res['body']);
        if (body['success'] == true && body['data'] != null) {
          final data = body['data'];
          await saveUserSession(
              data['maBenhNhan'] ?? data['userId'] ?? 0,
              data['hoTen'] ?? data['fullName'] ?? 'Người dùng',
              'Bệnh nhân'
          );
        } else {
          int userId = body['userId'] ?? body['id'] ?? 0;
          await saveUserSession(userId, username, 'Bệnh nhân');
        }
      } catch (_) {}
    }
    return res;
  }

  Future<Map<String, dynamic>> register(Map<String, dynamic> form) async {
    return await _api.postJson('/api/user/register', {
      'Username': form['username'],
      'Password': form['password'],
      'Fullname': form['fullname'],
      'Dob': form['dob'],
      'Gender': form['gender'],
      'Phone': form['phone'],
      'Email': form['email'],
      'Address': form['address'],
      'SoBaoHiem': form['soBaoHiem'],
    });
  }

  Future<Map<String, dynamic>> verifyOtp(String otp) async {
    return await _api.postJson('/api/user/verify-otp', {'Otp': otp});
  }

  Future<void> logout() async {
    await _api.postJson('/Logout', {});
    await clearUserSession();
    _api.clearSession();
  }

  Future<Map<String, dynamic>> forgotPasswordSend(String email) async {
    return await _api.postJson('/api/user/forgot-password', {
      'Email': email, 'Step': 'request_otp', 'Otp': ''
    });
  }

  Future<Map<String, dynamic>> forgotPasswordVerify(String email, String otp) async {
    return await _api.postJson('/api/user/forgot-password', {
      'Email': email, 'Step': 'verify_otp', 'Otp': otp
    });
  }

  Future<Map<String, dynamic>> resetPassword(String email, String newPass, String confirmPass, String otp) async {
    return await _api.postJson('/api/user/reset-password', {
      'NewPassword': newPass, 'ConfirmPassword': confirmPass, 'Otp': otp
    });
  }

  Future<Map<String, dynamic>> sendChangePasswordCode() async {
    return await _api.postJson('/User/SendVerificationCode', {});
  }

  Future<Map<String, dynamic>> changePassword(String oldPass, String newPass, String confirmPass, String code) async {
    return await _api.postJson('/User/ChangePassword', {
      'oldPassword': oldPass, 'newPassword': newPass, 'confirmPassword': confirmPass, 'verificationCode': code
    });
  }

  Future<Map<String, dynamic>?> getProfileRaw() async {
    try {
      final res = await _api.get('/api/user/profile');
      if (res['status'] == 200) {
        final body = jsonDecode(res['body']);
        if (body['data'] != null) return body['data'];
      }
    } catch (_) {}
    return null;
  }

  Future<Map<String, dynamic>> updateProfile(Map<String, String> fields, File? imageFile) async {
    return await _api.multipartPost('/api/user/update-profile', fields, imageFile);
  }

  Future<List<Doctor>> getDoctors() async {
    try {
      final res = await _api.get('/api/doctors');
      if (res['status'] == 200) {
        final dynamic body = jsonDecode(res['body']);
        List<dynamic> list = [];
        if (body is List) list = body;
        else if (body is Map) {
          if (body.containsKey('data')) list = body['data'];
          else if (body.containsKey('doctors')) list = body['doctors'];
        }
        return list.map((e) => Doctor.fromJson(e)).toList();
      }
    } catch (_) {}
    return [];
  }

  Future<Map<String, dynamic>> getAppointments() async {
    return await _api.get('/api/user/appointments');
  }

  Future<Map<String, dynamic>> bookAppointment(Map<String, dynamic> data) async {
    return await _api.postJson('/api/Appointment/Book', data);
  }

  Future<List<String>> getDoctorWorkingDates(int doctorId) async {
    try {
      final res = await _api.get('/Appointment/GetAvailableDates?doctorId=$doctorId');
      if (res['status'] == 200) {
        final body = jsonDecode(res['body']);
        if (body is List) return List<String>.from(body);
        if (body['dates'] != null) return List<String>.from(body['dates']);
      }
    } catch (_) {}
    return [];
  }

  Future<List<String>> getAvailableTimes(int doctorId, String date) async {
    try {
      final res = await _api.get('/Appointment/GetAvailableTimes?doctorId=$doctorId&date=$date');
      if (res['status'] == 200) {
        final body = jsonDecode(res['body']);
        if (body is List) return List<String>.from(body);
        if (body['times'] != null) return List<String>.from(body['times']);
      }
    } catch (_) {}
    return [];
  }

  Future<int> getUnreadNotificationCount() async {
    try {
      final userId = await getCurrentUserId();
      if (userId == null) return 0;
      final res = await _api.get('/api/Notification/UnreadCount?userId=$userId');
      if (res['status'] == 200) {
        final body = jsonDecode(res['body']);
        if (body is Map && body.containsKey('count')) return body['count'];
        if (body is int) return body;
        return int.tryParse(res['body'].toString()) ?? 0;
      }
    } catch (_) {}
    return 0;
  }

  Future<List<dynamic>> getNotifications({int page = 1, int pageSize = 20}) async {
    try {
      final userId = await getCurrentUserId();
      if (userId == null) return [];
      final res = await _api.get('/api/Notification/GetUserNotifications?userId=$userId&page=$page&pageSize=$pageSize');
      if (res['status'] == 200) {
        final body = jsonDecode(res['body']);
        if (body is List) return body;
        if (body['data'] is List) return body['data'];
        if (body['notifications'] is List) return body['notifications'];
      }
    } catch (_) {}
    return [];
  }

  Future<void> markAsRead(int notificationId) async {
    await _api.postJson('/api/Notification/MarkAsRead?id=$notificationId', {});
  }

  Future<void> markAllRead() async {
    final userId = await getCurrentUserId();
    if (userId != null) {
      await _api.postJson('/api/Notification/MarkAllAsRead?userId=$userId', {});
    }
  }

  Future<void> deleteNotification(int notificationId) async {
    await _api.delete('/api/Notification/Delete?id=$notificationId');
  }

  Future<double> getWalletBalance() async {
    try {
      final res = await getProfileRaw();
      if (res != null && res['soDu'] != null) {
        return double.tryParse(res['soDu'].toString()) ?? 0.0;
      }
    } catch (_) {}
    return 0.0;
  }

  Future<Map<String, dynamic>> payAppointment(int appointmentId) async {
    return await _api.multipartPost(
        '/PayAppointment',
        {'appointmentId': appointmentId.toString()},
        null
    );
  }

  // 👇 HÀM MỚI: Trả về PaymentRes thay vì Map để sửa lỗi ở TopUpScreen
  Future<PaymentRes> createDepositUrl(double amount) async {
    try {
      final res = await _api.postJson('/Payment/api/create-deposit', {
        'Amount': amount
      });

      if (res['status'] == 200) {
        // Xử lý dữ liệu trả về từ API (Map hoặc String JSON)
        final dynamic body = (res['body'] is String)
            ? jsonDecode(res['body'])
            : res['body'];

        return PaymentRes.fromJson(body);
      } else {
        final dynamic body = (res['body'] is String)
            ? jsonDecode(res['body'])
            : res['body'];
        return PaymentRes(success: false, message: body['message'] ?? "Lỗi kết nối");
      }
    } catch (e) {
      return PaymentRes(success: false, message: "Lỗi ứng dụng: $e");
    }
  }
}