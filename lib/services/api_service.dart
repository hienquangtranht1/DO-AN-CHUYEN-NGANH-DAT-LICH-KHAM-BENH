import 'dart:async';
import 'dart:convert';
import 'dart:io';
import 'dart:typed_data';
import 'package:flutter/foundation.dart';
import 'package:http/http.dart' as http;
import 'package:http/io_client.dart';
import '../models/doctor.dart'; 
//trước khi chạy check trước ip của mạng đang bắt sau đó tắt tường lửa để chạy
class ApiService {
  static final ApiService _instance = ApiService._internal();
  factory ApiService() => _instance;



  static const String base = "http://172.20.10.6:5062";

  static const Duration _timeout = Duration(seconds: 60);

  final http.Client _client;
  String? _cookie; 

  ApiService._internal() : _client = _createClient(true);

  static http.Client _createClient(bool allowBadCert) {
    if (!kIsWeb && allowBadCert) {
      final ioc = HttpClient()
        ..badCertificateCallback = (X509Certificate cert, String host, int port) => true;
      return IOClient(ioc);
    }
    return http.Client();
  }

  void dispose() {
    try {
      _client.close();
    } catch (_) {}
  }

  void _saveCookie(http.Response res) {
    String? rawCookie = res.headers['set-cookie'];
    if (rawCookie != null) {
      int index = rawCookie.indexOf(';');
      _cookie = (index == -1) ? rawCookie : rawCookie.substring(0, index);
      if (kDebugMode) debugPrint('[ApiService] Cookie Saved: $_cookie');
    }
  }

  void clearSession() {
    _cookie = null;
    if (kDebugMode) debugPrint('[ApiService] Session cleared');
  }

  Map<String, String> _buildHeaders({String? contentType}) {
    final h = <String, String>{};
    if (contentType != null) h['Content-Type'] = contentType;
    if (_cookie != null) h['Cookie'] = _cookie!;
    return h;
  }


  Future<Map<String, dynamic>> postJson(String path, Map<String, dynamic> body) async {
    final urlStr = '$base$path';
    final uri = Uri.parse(urlStr);

    try {
      if (kDebugMode) debugPrint('POST $uri -> $body');

      final res = await _client.post(
        uri,
        headers: _buildHeaders(contentType: 'application/json'),
        body: jsonEncode(body),
      ).timeout(_timeout);

      _saveCookie(res);

      if (kDebugMode) debugPrint('Response: ${res.statusCode} ${res.body}');
      return {'status': res.statusCode, 'body': res.body};
    } catch (e) {
      if (kDebugMode) debugPrint('POST Error: $e');
      return {'status': 500, 'body': '{"message":"Lỗi kết nối: $e"}'};
    }
  }

  Future<Map<String, dynamic>> get(String path) async {
    final urlStr = '$base$path';
    final uri = Uri.parse(urlStr);

    try {
      if (kDebugMode) debugPrint('GET $uri');

      final res = await _client.get(
        uri,
        headers: _buildHeaders(),
      ).timeout(_timeout);

      _saveCookie(res);

      if (kDebugMode && res.statusCode != 200) {
        debugPrint('GET Response Error: ${res.statusCode} ${res.body}');
      }

      return {'status': res.statusCode, 'body': res.body};
    } catch (e) {
      return {'status': 500, 'body': '{"message":"Lỗi kết nối: $e"}'};
    }
  }

  Future<Map<String, dynamic>> delete(String path) async {
    final uri = Uri.parse('$base$path');
    try {
      if (kDebugMode) debugPrint('DELETE $uri');
      final res = await _client.delete(
        uri,
        headers: _buildHeaders(),
      ).timeout(_timeout);

      _saveCookie(res);
      return {'status': res.statusCode, 'body': res.body};
    } catch (e) {
      return {'status': 500, 'body': '{"message":"Lỗi kết nối: $e"}'};
    }
  }

  Future<Map<String, dynamic>> multipartPost(String path, Map<String, String> fields, File? file) async {
    final uri = Uri.parse('$base$path');
    try {
      var request = http.MultipartRequest('POST', uri);
      request.headers.addAll(_buildHeaders());
      request.fields.addAll(fields);

      if (file != null) {
        request.files.add(await http.MultipartFile.fromPath('hinhAnhBenhNhan', file.path));
      }

      if (kDebugMode) debugPrint('MULTIPART POST $uri');

      final streamedRes = await _client.send(request).timeout(_timeout);
      final res = await http.Response.fromStream(streamedRes);

      _saveCookie(res);

      if (kDebugMode) debugPrint('Response: ${res.statusCode} ${res.body}');
      return {'status': res.statusCode, 'body': res.body};
    } catch (e) {
      return {'status': 500, 'body': '{"message":"Lỗi upload: $e"}'};
    }
  }


  Future<bool> health({String path = '/api/health'}) async {
    try {
      final res = await get(path);
      return res['status'] != 500 && res['status'] != 404;
    } catch (_) {
      return false;
    }
  }

  Future<List<Doctor>> fetchFeaturedDoctors() async {
    try {
      final res = await get('/api/doctors');
      if (res['status'] == 200) {
        final decoded = jsonDecode(res['body']);
        if (decoded is List) {
          return decoded.map((e) => Doctor.fromJson(Map<String, dynamic>.from(e))).toList();
        }
        else if (decoded is Map && decoded.containsKey('doctors')) {
          final List list = decoded['doctors'];
          return list.map((e) => Doctor.fromJson(Map<String, dynamic>.from(e))).toList();
        }
      }
    } catch (e) {
      debugPrint('Error fetching doctors: $e');
    }
    return [];
  }

  Future<Uint8List?> fetchImageBytes(String? url) async {
    if (url == null || url.isEmpty) return null;

    try {
      final fullUrl = url.startsWith('http') ? url : '$base$url';

      final res = await _client.get(Uri.parse(fullUrl)).timeout(const Duration(seconds: 10));
      if (res.statusCode == 200) {
        return res.bodyBytes;
      }
    } catch (_) {}
    return null;
  }
}