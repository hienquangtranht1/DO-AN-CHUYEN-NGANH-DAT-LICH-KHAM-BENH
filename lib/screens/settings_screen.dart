import 'package:flutter/material.dart';
import 'package:package_info_plus/package_info_plus.dart';
import '../services/auth_service.dart';
import 'login_screen.dart';

class SettingsScreen extends StatefulWidget {
  const SettingsScreen({super.key});

  @override
  State<SettingsScreen> createState() => _SettingsScreenState();
}

class _SettingsScreenState extends State<SettingsScreen> {
  final AuthService _auth = AuthService();

  bool _notificationsEnabled = true;
  bool _darkModeEnabled = false;
  bool _biometricEnabled = false;
  String _version = '1.0.0';

  @override
  void initState() {
    super.initState();
    _loadVersion();
  }

  Future<void> _loadVersion() async {
  }

  Future<void> _showLogoutDialog(BuildContext context) async {
    return showDialog<void>(
      context: context,
      builder: (BuildContext ctx) {
        return AlertDialog(
          title: const Text('Xác nhận'),
          content: const Text('Bạn có chắc chắn muốn đăng xuất không?'),
          actions: [
            TextButton(
              child: const Text('Hủy'),
              onPressed: () => Navigator.of(ctx).pop(),
            ),
            TextButton(
              child: const Text('Đăng xuất', style: TextStyle(color: Colors.red)),
              onPressed: () {
                Navigator.of(ctx).pop();
                _performLogout();
              },
            ),
          ],
        );
      },
    );
  }

  void _performLogout() {
    _auth.logout();
    Navigator.of(context).pushAndRemoveUntil(
      MaterialPageRoute(builder: (_) => const LoginScreen()),
          (route) => false,
    );
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Cài đặt')),
      body: ListView(
        children: [
          _buildHeader('Chung'),
          SwitchListTile(
            secondary: const Icon(Icons.notifications_active, color: Colors.blue),
            title: const Text('Thông báo'),
            subtitle: const Text('Nhận tin tức và nhắc hẹn'),
            value: _notificationsEnabled,
            onChanged: (val) => setState(() => _notificationsEnabled = val),
          ),
          SwitchListTile(
            secondary: const Icon(Icons.dark_mode, color: Colors.purple),
            title: const Text('Chế độ tối'),
            value: _darkModeEnabled,
            onChanged: (val) => setState(() => _darkModeEnabled = val),
          ),
          ListTile(
            leading: const Icon(Icons.language, color: Colors.orange),
            title: const Text('Ngôn ngữ'),
            subtitle: const Text('Tiếng Việt'),
            trailing: const Icon(Icons.arrow_forward_ios, size: 16),
            onTap: () {
            },
          ),

          const Divider(),

          _buildHeader('Bảo mật'),
          SwitchListTile(
            secondary: const Icon(Icons.fingerprint, color: Colors.green),
            title: const Text('Đăng nhập vân tay'),
            value: _biometricEnabled,
            onChanged: (val) => setState(() => _biometricEnabled = val),
          ),
          ListTile(
            leading: const Icon(Icons.lock_reset, color: Colors.redAccent),
            title: const Text('Đổi mật khẩu'),
            trailing: const Icon(Icons.arrow_forward_ios, size: 16),
            onTap: () {
            },
          ),

          const Divider(),

          _buildHeader('Thông tin'),
          ListTile(
            leading: const Icon(Icons.info_outline, color: Colors.teal),
            title: const Text('Về ứng dụng'),
            onTap: () {},
          ),
          ListTile(
            leading: const Icon(Icons.policy, color: Colors.teal),
            title: const Text('Chính sách quyền riêng tư'),
            onTap: () {},
          ),
          ListTile(
            leading: const Icon(Icons.verified, color: Colors.grey),
            title: const Text('Phiên bản'),
            subtitle: Text(_version),
          ),

          const SizedBox(height: 20),

          Padding(
            padding: const EdgeInsets.symmetric(horizontal: 16),
            child: ElevatedButton.icon(
              onPressed: () => _showLogoutDialog(context),
              style: ElevatedButton.styleFrom(
                backgroundColor: Colors.red[50],
                foregroundColor: Colors.red,
                elevation: 0,
                padding: const EdgeInsets.symmetric(vertical: 12),
              ),
              icon: const Icon(Icons.logout),
              label: const Text('Đăng xuất tài khoản', style: TextStyle(fontSize: 16, fontWeight: FontWeight.bold)),
            ),
          ),
          const SizedBox(height: 30),
        ],
      ),
    );
  }

  Widget _buildHeader(String title) {
    return Padding(
      padding: const EdgeInsets.fromLTRB(16, 16, 16, 8),
      child: Text(
        title.toUpperCase(),
        style: TextStyle(
          color: Colors.grey[600],
          fontSize: 13,
          fontWeight: FontWeight.bold,
          letterSpacing: 1.2,
        ),
      ),
    );
  }
}