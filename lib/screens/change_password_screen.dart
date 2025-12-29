import 'dart:convert';
import 'package:flutter/material.dart';
import '../services/auth_service.dart';

class ChangePasswordScreen extends StatefulWidget {
  const ChangePasswordScreen({super.key});
  @override
  State<ChangePasswordScreen> createState() => _ChangePasswordScreenState();
}

class _ChangePasswordScreenState extends State<ChangePasswordScreen> {
  final AuthService _auth = AuthService();
  final TextEditingController _old = TextEditingController();
  final TextEditingController _new = TextEditingController();
  final TextEditingController _confirm = TextEditingController();
  final TextEditingController _code = TextEditingController();
  bool _loading = false;

  Future<void> _sendCode() async {
    setState(() => _loading = true);
    final res = await _auth.sendChangePasswordCode();
    setState(() => _loading = false);
    if (res['status'] == 200) {
      ScaffoldMessenger.of(context).showSnackBar(const SnackBar(content: Text('Mã đã được gửi')));
    } else {
      String msg = 'Không thể gửi mã';
      try {
        final map = jsonDecode(res['body']);
        if (map is Map && map['message'] != null) msg = map['message'].toString();
      } catch (_) {}
      ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(msg)));
    }
  }

  Future<void> _change() async {
    if (_new.text != _confirm.text) {
      ScaffoldMessenger.of(context).showSnackBar(const SnackBar(content: Text('Mật khẩu mới không khớp')));
      return;
    }
    setState(() => _loading = true);
    final res = await _auth.changePassword(_old.text, _new.text, _confirm.text, _code.text);
    setState(() => _loading = false);
    if (res['status'] == 200) {
      ScaffoldMessenger.of(context).showSnackBar(const SnackBar(content: Text('Đổi mật khẩu thành công')));
      Navigator.of(context).pop();
    } else {
      String msg = 'Đổi mật khẩu thất bại';
      try {
        final map = jsonDecode(res['body']);
        if (map is Map && map['message'] != null) msg = map['message'].toString();
      } catch (_) {}
      ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(msg)));
    }
  }

  @override
  void dispose() {
    _old.dispose();
    _new.dispose();
    _confirm.dispose();
    _code.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Đổi mật khẩu')),
      body: Padding(
        padding: const EdgeInsets.all(12),
        child: Column(
          children: [
            TextField(controller: _old, decoration: const InputDecoration(labelText: 'Mật khẩu cũ'), obscureText: true),
            const SizedBox(height: 8),
            TextField(controller: _new, decoration: const InputDecoration(labelText: 'Mật khẩu mới'), obscureText: true),
            const SizedBox(height: 8),
            TextField(controller: _confirm, decoration: const InputDecoration(labelText: 'Xác nhận mật khẩu'), obscureText: true),
            const SizedBox(height: 8),
            TextField(controller: _code, decoration: const InputDecoration(labelText: 'Mã xác thực (nếu có)')),
            const SizedBox(height: 12),
            Row(
              children: [
                ElevatedButton(onPressed: _loading ? null : _sendCode, child: const Text('Gửi mã')),
                const SizedBox(width: 12),
                Expanded(child: ElevatedButton(onPressed: _loading ? null : _change, child: const Text('Đổi mật khẩu'))),
              ],
            )
          ],
        ),
      ),
    );
  }
}
