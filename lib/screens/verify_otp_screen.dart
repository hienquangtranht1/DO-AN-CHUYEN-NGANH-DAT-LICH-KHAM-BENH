import 'dart:convert';
import 'package:flutter/material.dart';
import '../services/auth_service.dart';
import 'login_screen.dart';

class VerifyOtpScreen extends StatefulWidget {
  const VerifyOtpScreen({super.key});
  @override
  State<VerifyOtpScreen> createState() => _VerifyOtpScreenState();
}

class _VerifyOtpScreenState extends State<VerifyOtpScreen> {
  final TextEditingController _otp = TextEditingController();
  final AuthService _auth = AuthService();
  bool _loading = false;

  Future<void> _submit() async {
    if (_otp.text.trim().isEmpty) return;
    setState(() => _loading = true);
    final res = await _auth.verifyOtp(_otp.text.trim());
    setState(() => _loading = false);
    debugPrint('VERIFY status: ${res['status']}');
    debugPrint('VERIFY body: ${res['body']}');
    if (res['status'] == 200) {
      if (mounted) Navigator.of(context).pushAndRemoveUntil(MaterialPageRoute(builder: (_) => const LoginScreen()), (r) => false);
    } else {
      String msg = 'Xác thực thất bại: ${res['status']}';
      try {
        final map = jsonDecode(res['body']);
        if (map is Map && map['message'] != null) msg = map['message'].toString();
      } catch (_) {}
      ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(msg)));
    }
  }

  @override
  void dispose() {
    _otp.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Xác nhận OTP')),
      body: Padding(
        padding: const EdgeInsets.all(12),
        child: Column(children: [
          const Text('Nhập mã OTP đã gửi tới email của bạn.'),
          TextField(controller: _otp, keyboardType: TextInputType.number, decoration: const InputDecoration(labelText: 'Mã OTP')),
          const SizedBox(height: 12),
          ElevatedButton(onPressed: _loading ? null : _submit, child: _loading ? const CircularProgressIndicator(color: Colors.white) : const Text('Xác nhận')),
        ]),
      ),
    );
  }
}
