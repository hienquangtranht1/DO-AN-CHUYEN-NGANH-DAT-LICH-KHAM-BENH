
import 'dart:convert';
import 'package:flutter/foundation.dart';
import 'package:flutter/material.dart';
import '../services/auth_service.dart';

class ForgotPasswordScreen extends StatefulWidget {
  const ForgotPasswordScreen({super.key});
  @override
  State<ForgotPasswordScreen> createState() => _ForgotPasswordScreenState();
}

class _ForgotPasswordScreenState extends State<ForgotPasswordScreen> {
  final AuthService _auth = AuthService();

  final TextEditingController _emailCtrl = TextEditingController();
  final TextEditingController _otpCtrl = TextEditingController();
  final TextEditingController _newCtrl = TextEditingController();
  final TextEditingController _confirmCtrl = TextEditingController();

  bool _loading = false;
  bool _sent = false;
  bool _verified = false;

  Future<void> _send() async {
    final email = _emailCtrl.text.trim();
    if (email.isEmpty) {
      ScaffoldMessenger.of(context).showSnackBar(const SnackBar(content: Text('Nhập email')));
      return;
    }
    setState(() => _loading = true);
    final res = await _auth.forgotPasswordSend(email);
    setState(() => _loading = false);
    debugPrint('FORGOT SEND status: ${res['status']} body: ${res['body']}');
    if (res['status'] == 200) {
      setState(() => _sent = true);
      ScaffoldMessenger.of(context).showSnackBar(const SnackBar(content: Text('Mã đã được gửi tới email')));
    } else {
      String msg = 'Gửi mã thất bại';
      try {
        final map = jsonDecode(res['body']);
        if (map is Map && map['message'] != null) msg = map['message'].toString();
      } catch (_) {}
      ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(msg)));
    }
  }

  Future<void> _verify() async {
    final email = _emailCtrl.text.trim();
    final otp = _otpCtrl.text.trim();
    if (email.isEmpty || otp.isEmpty) {
      ScaffoldMessenger.of(context).showSnackBar(const SnackBar(content: Text('Nhập email và mã OTP')));
      return;
    }
    setState(() => _loading = true);
    final res = await _auth.forgotPasswordVerify(email, otp);
    setState(() => _loading = false);
    debugPrint('FORGOT VERIFY status: ${res['status']} body: ${res['body']}');
    if (res['status'] == 200) {
      setState(() => _verified = true);
      ScaffoldMessenger.of(context).showSnackBar(const SnackBar(content: Text('Xác thực thành công. Nhập mật khẩu mới.')));
    } else {
      String msg = 'Xác thực thất bại';
      try {
        final map = jsonDecode(res['body']);
        if (map is Map && map['message'] != null) msg = map['message'].toString();
      } catch (_) {}
      ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(msg)));
    }
  }

  Future<void> _reset() async {
    final email = _emailCtrl.text.trim();
    final otp = _otpCtrl.text.trim();
    final np = _newCtrl.text;
    final cp = _confirmCtrl.text;
    if (np.isEmpty || cp.isEmpty) {
      ScaffoldMessenger.of(context).showSnackBar(const SnackBar(content: Text('Nhập mật khẩu mới và xác nhận')));
      return;
    }
    if (np != cp) {
      ScaffoldMessenger.of(context).showSnackBar(const SnackBar(content: Text('Mật khẩu mới không khớp')));
      return;
    }
    setState(() => _loading = true);
    final res = await _auth.resetPassword(email, np, cp, otp);
    setState(() => _loading = false);
    debugPrint('FORGOT RESET status: ${res['status']} body: ${res['body']}');
    if (res['status'] == 200) {
      ScaffoldMessenger.of(context).showSnackBar(const SnackBar(content: Text('Đặt lại mật khẩu thành công')));
      Navigator.of(context).pop();
    } else {
      String msg = 'Đặt lại mật khẩu thất bại';
      try {
        final map = jsonDecode(res['body']);
        if (map is Map && map['message'] != null) msg = map['message'].toString();
      } catch (_) {}
      ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(msg)));
    }
  }

  @override
  void dispose() {
    _emailCtrl.dispose();
    _otpCtrl.dispose();
    _newCtrl.dispose();
    _confirmCtrl.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Quên mật khẩu')),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(12),
        child: Column(
          children: [
            TextField(controller: _emailCtrl, decoration: const InputDecoration(labelText: 'Email'), keyboardType: TextInputType.emailAddress),
            const SizedBox(height: 12),
            SizedBox(
              width: double.infinity,
              child: ElevatedButton(
                onPressed: _loading ? null : _send,
                child: _loading ? const SizedBox(width: 16, height: 16, child: CircularProgressIndicator(color: Colors.white, strokeWidth: 2)) : const Text('Gửi mã xác thực'),
              ),
            ),
            const SizedBox(height: 16),
            if (_sent) ...[
              TextField(controller: _otpCtrl, decoration: const InputDecoration(labelText: 'Mã OTP'), keyboardType: TextInputType.number),
              const SizedBox(height: 8),
              SizedBox(
                width: double.infinity,
                child: ElevatedButton(
                  onPressed: _loading ? null : _verify,
                  child: _loading ? const SizedBox(width: 16, height: 16, child: CircularProgressIndicator(color: Colors.white, strokeWidth: 2)) : const Text('Xác thực mã'),
                ),
              ),
            ],
            const SizedBox(height: 16),
            if (_verified) ...[
              TextField(controller: _newCtrl, decoration: const InputDecoration(labelText: 'Mật khẩu mới'), obscureText: true),
              const SizedBox(height: 8),
              TextField(controller: _confirmCtrl, decoration: const InputDecoration(labelText: 'Xác nhận mật khẩu'), obscureText: true),
              const SizedBox(height: 12),
              SizedBox(
                width: double.infinity,
                child: ElevatedButton(
                  onPressed: _loading ? null : _reset,
                  child: _loading ? const SizedBox(width: 16, height: 16, child: CircularProgressIndicator(color: Colors.white, strokeWidth: 2)) : const Text('Đặt lại mật khẩu'),
                ),
              ),
            ],
          ],
        ),
      ),
    );
  }
}
