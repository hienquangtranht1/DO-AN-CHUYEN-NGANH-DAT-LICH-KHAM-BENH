import 'package:flutter/material.dart';
import '../services/auth_service.dart';
import 'verify_otp_screen.dart';

class RegisterScreen extends StatefulWidget {
  const RegisterScreen({super.key});
  @override
  State<RegisterScreen> createState() => _RegisterScreenState();
}

class _RegisterScreenState extends State<RegisterScreen> {
  final AuthService _auth = AuthService();
  final _formKey = GlobalKey<FormState>();
  final TextEditingController _username = TextEditingController();
  final TextEditingController _password = TextEditingController();
  final TextEditingController _fullname = TextEditingController();
  DateTime? _dob;
  String _gender = 'Nam';
  final TextEditingController _phone = TextEditingController();
  final TextEditingController _email = TextEditingController();
  final TextEditingController _address = TextEditingController();
  final TextEditingController _soBaoHiem = TextEditingController();
  bool _loading = false;

  // Server-side rules mirrored:
  // username: alphanumeric 4-50, at least one letter, no spaces
  final RegExp _usernameRe = RegExp(r'^(?=.*[A-Za-z])[A-Za-z0-9]{4,50}$');
  // password: min6, at least one digit and one special char
  final RegExp _passwordRe = RegExp(r'^(?=.{6,100}$)(?=.*\d)(?=.*\W).*$');
  final RegExp _phoneRe = RegExp(r'^0\d{9}$');
  final RegExp _bhytRe = RegExp(r'^\d{10}$');

  String? _validateUsername(String? v) {
    if (v == null || v.trim().isEmpty) return 'Nhập tên đăng nhập';
    final s = v.trim();
    if (!_usernameRe.hasMatch(s)) {
      return 'Tên đăng nhập 4-50 ký tự, chữ/số, không khoảng trắng, có ít nhất 1 chữ cái';
    }
    return null;
  }

  String? _validatePassword(String? v) {
    if (v == null || v.isEmpty) return 'Nhập mật khẩu';
    if (!_passwordRe.hasMatch(v)) {
      return 'Mật khẩu >=6 ký tự, chứa chữ số và ký tự đặc biệt';
    }
    return null;
  }

  String? _validateFullname(String? v) {
    if (v == null || v.trim().isEmpty) return 'Nhập họ và tên';
    return null;
  }

  String? _validateDob(DateTime? d) {
    if (d == null) return 'Chọn ngày sinh';
    if (d.isAfter(DateTime.now())) return 'Ngày sinh không thể ở tương lai';
    return null;
  }

  String? _validatePhone(String? v) {
    if (v == null || v.trim().isEmpty) return 'Nhập số điện thoại';
    if (!_phoneRe.hasMatch(v.trim())) return 'Số điện thoại phải 10 chữ số và bắt đầu bằng 0';
    return null;
  }

  String? _validateEmail(String? v) {
    if (v == null || v.trim().isEmpty) return 'Nhập email';
    final email = v.trim();
    if (!RegExp(r'^[^@\s]+@[^@\s]+\.[^@\s]+$').hasMatch(email)) return 'Email không hợp lệ';
    return null;
  }

  String? _validateBHYT(String? v) {
    if (v == null || v.trim().isEmpty) return 'Nhập số BHYT';
    if (!_bhytRe.hasMatch(v.trim())) return 'Số BHYT phải đúng 10 chữ số';
    return null;
  }

  Future<void> _submit() async {
    // First validate form fields
    if (!_formKey.currentState!.validate()) return;

    final dobError = _validateDob(_dob);
    if (dobError != null) {
      ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(dobError)));
      return;
    }

    setState(() => _loading = true);

    final form = {
      'username': _username.text.trim(),
      'password': _password.text,
      'fullname': _fullname.text.trim(),
      'dob': _dob?.toIso8601String() ?? DateTime(1990).toIso8601String(),
      'gender': _gender,
      'phone': _phone.text.trim(),
      'email': _email.text.trim(),
      'address': _address.text.trim(),
      'soBaoHiem': _soBaoHiem.text.trim(),
    };

    try {
      final res = await _auth.register(form);

      // Accept both API shapes used in backend demos:
      final bool ok = (res is Map && (res['success'] == true || res['status'] == 200 || res['code'] == 200));
      final String message = (res is Map && (res['message'] ?? res['error'] ?? res['msg'] != null))
          ? (res['message'] ?? res['error'] ?? res['msg']).toString()
          : 'Đăng ký thất bại';

      if (ok) {
        if (mounted) {
          // navigate to OTP verify
          Navigator.of(context).push(MaterialPageRoute(builder: (_) => const VerifyOtpScreen()));
        }
      } else {
        ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(message)));
      }
    } catch (e) {
      ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text('Lỗi kết nối: $e')));
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  @override
  void dispose() {
    _username.dispose();
    _password.dispose();
    _fullname.dispose();
    _phone.dispose();
    _email.dispose();
    _address.dispose();
    _soBaoHiem.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Đăng ký')),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(12),
        child: Form(
          key: _formKey,
          autovalidateMode: AutovalidateMode.onUserInteraction,
          child: Column(children: [
            TextFormField(
              controller: _username,
              decoration: const InputDecoration(labelText: 'Tên đăng nhập'),
              validator: _validateUsername,
              textInputAction: TextInputAction.next,
            ),
            const SizedBox(height: 8),
            TextFormField(
              controller: _password,
              decoration: const InputDecoration(labelText: 'Mật khẩu'),
              obscureText: true,
              validator: _validatePassword,
              textInputAction: TextInputAction.next,
            ),
            const SizedBox(height: 8),
            TextFormField(
              controller: _fullname,
              decoration: const InputDecoration(labelText: 'Họ và tên'),
              validator: _validateFullname,
              textInputAction: TextInputAction.next,
            ),
            const SizedBox(height: 12),
            Row(children: [
              Expanded(
                child: InputDecorator(
                  decoration: const InputDecoration(labelText: 'Ngày sinh'),
                  child: GestureDetector(
                    onTap: () async {
                      final d = await showDatePicker(
                        context: context,
                        initialDate: DateTime(1990),
                        firstDate: DateTime(1900),
                        lastDate: DateTime.now(),
                      );
                      if (d != null) setState(() => _dob = d);
                    },
                    child: Text(_dob == null ? 'Chọn ngày' : _dob!.toLocal().toString().split(' ')[0]),
                  ),
                ),
              ),
              const SizedBox(width: 12),
              Expanded(
                child: DropdownButtonFormField<String>(
                  value: _gender,
                  items: const [
                    DropdownMenuItem(value: 'Nam', child: Text('Nam')),
                    DropdownMenuItem(value: 'Nữ', child: Text('Nữ')),
                    DropdownMenuItem(value: 'Khác', child: Text('Khác')),
                  ],
                  onChanged: (v) => setState(() => _gender = v ?? 'Nam'),
                  decoration: const InputDecoration(labelText: 'Giới tính'),
                ),
              ),
            ]),
            const SizedBox(height: 12),
            TextFormField(
              controller: _soBaoHiem,
              decoration: const InputDecoration(labelText: 'Số BHYT (10 chữ số)'),
              keyboardType: TextInputType.number,
              validator: _validateBHYT,
              textInputAction: TextInputAction.next,
            ),
            const SizedBox(height: 8),
            TextFormField(
              controller: _phone,
              decoration: const InputDecoration(labelText: 'Số điện thoại'),
              keyboardType: TextInputType.phone,
              validator: _validatePhone,
              textInputAction: TextInputAction.next,
            ),
            const SizedBox(height: 8),
            TextFormField(
              controller: _email,
              decoration: const InputDecoration(labelText: 'Email'),
              keyboardType: TextInputType.emailAddress,
              validator: _validateEmail,
              textInputAction: TextInputAction.next,
            ),
            const SizedBox(height: 8),
            TextFormField(
              controller: _address,
              decoration: const InputDecoration(labelText: 'Địa chỉ'),
              textInputAction: TextInputAction.done,
            ),
            const SizedBox(height: 16),
            SizedBox(
              width: double.infinity,
              child: ElevatedButton(
                onPressed: _loading ? null : _submit,
                child: _loading ? const SizedBox(width: 20, height: 20, child: CircularProgressIndicator(color: Colors.white, strokeWidth: 2)) : const Text('Gửi đăng ký'),
              ),
            ),
          ]),
        ),
      ),
    );
  }
}