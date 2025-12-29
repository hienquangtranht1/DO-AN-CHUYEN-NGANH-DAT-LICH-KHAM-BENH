import 'dart:convert';
import 'dart:io';
import 'package:flutter/foundation.dart';
import 'package:flutter/material.dart';
import 'package:image_picker/image_picker.dart';
import '../services/auth_service.dart';
import '../services/api_service.dart';
import '../models/profile_model.dart';

class ProfileScreen extends StatefulWidget {
  const ProfileScreen({super.key});

  @override
  State<ProfileScreen> createState() => _ProfileScreenState();
}

class _ProfileScreenState extends State<ProfileScreen> {
  final AuthService _auth = AuthService();
  ProfileModel? _profile;
  bool _loading = false;
  File? _pickedImage;

  final TextEditingController _idController = TextEditingController();
  final TextEditingController _insuranceController = TextEditingController();
  final TextEditingController _fullnameController = TextEditingController();
  final TextEditingController _dobController = TextEditingController();
  final TextEditingController _genderController = TextEditingController();
  final TextEditingController _phoneController = TextEditingController();
  final TextEditingController _emailController = TextEditingController();
  final TextEditingController _addressController = TextEditingController();

  @override
  void initState() {
    super.initState();
    _loadProfile();
  }

  @override
  void dispose() {
    _idController.dispose();
    _insuranceController.dispose();
    _fullnameController.dispose();
    _dobController.dispose();
    _genderController.dispose();
    _phoneController.dispose();
    _emailController.dispose();
    _addressController.dispose();
    super.dispose();
  }

  Future<void> _loadProfile() async {
    setState(() => _loading = true);
    try {
      final res = await _auth.getProfileRaw();

      if (res == null) {
        _showMessage('Không tải được dữ liệu. Vui lòng thử lại.');
        return;
      }

      if (res is Map<String, dynamic>) {
        final model = ProfileModel.fromJson(res);
        if (mounted) {
          setState(() {
            _profile = model;
            _fillControllers(model);
          });
        }
      } else {
        _showMessage('Dữ liệu không đúng định dạng.');
      }
    } catch (e) {
      debugPrint('Load Profile Error: $e');
      _showMessage('Lỗi kết nối: $e');
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  void _fillControllers(ProfileModel model) {
    _idController.text = model.id.toString();
    _fullnameController.text = model.hoTen;
    _insuranceController.text = model.soBaoHiem;
    _genderController.text = model.gioiTinh;
    _phoneController.text = model.soDienThoai;
    _emailController.text = model.email;
    _addressController.text = model.diaChi;

    String dob = model.ngaySinh ?? '';
    if (dob.contains('T')) {
      dob = dob.split('T')[0];
    }
    _dobController.text = dob;
  }


  Future<void> _pickImage() async {
    final picker = ImagePicker();
    try {
      final picked = await picker.pickImage(
        source: ImageSource.gallery,
        maxWidth: 800,
        imageQuality: 80,
      );
      if (picked != null && mounted) {
        setState(() => _pickedImage = File(picked.path));
      }
    } catch (e) {
      debugPrint('Pick Image Error: $e');
    }
  }

  Future<void> _selectDate() async {
    DateTime initialDate = DateTime.now();
    if (_dobController.text.isNotEmpty) {
      try {
        initialDate = DateTime.parse(_dobController.text);
      } catch (_) {}
    }

    final DateTime? picked = await showDatePicker(
      context: context,
      initialDate: initialDate,
      firstDate: DateTime(1900),
      lastDate: DateTime.now(),
    );

    if (picked != null && mounted) {
      setState(() {
        _dobController.text = "${picked.year}-${picked.month.toString().padLeft(2, '0')}-${picked.day.toString().padLeft(2, '0')}";
      });
    }
  }


  Future<void> _save() async {
    if (!mounted) return;
    setState(() => _loading = true);

    try {
      final Map<String, String> fields = {
        'fullname': _fullnameController.text.trim(),
        'dob': _dobController.text.trim(),
        'gender': _genderController.text.trim(),
        'phone': _phoneController.text.trim(),
        'email': _emailController.text.trim(),
        'address': _addressController.text.trim(),
        'soBaoHiem': _insuranceController.text.trim(),
      };

      final res = await _auth.updateProfile(fields, _pickedImage);

      if (res['status'] == 200) {
        _showMessage('Cập nhật thành công!');
        await _loadProfile();
      } else {
        String msg = 'Cập nhật thất bại.';
        if (res['body'] != null) {
          try {
            final body = jsonDecode(res['body']);
            msg = body['message'] ?? msg;
          } catch (_) {}
        }
        _showMessage(msg);
      }
    } catch (e) {
      _showMessage('Lỗi khi lưu: $e');
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  void _showMessage(String message) {
    if (!mounted) return;
    ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(message)));
  }

  Widget _buildTextField(String label, TextEditingController controller,
      {bool isReadOnly = false,
        VoidCallback? onTap,
        TextInputType? type,
        IconData? icon}) {
    return Padding(
      padding: const EdgeInsets.only(bottom: 16.0),
      child: TextField(
        controller: controller,
        readOnly: isReadOnly,
        onTap: onTap,
        keyboardType: type,
        decoration: InputDecoration(
          labelText: label,
          prefixIcon: icon != null ? Icon(icon) : null,
          border: const OutlineInputBorder(),
          filled: isReadOnly,
          fillColor: isReadOnly ? Colors.grey[200] : null,
        ),
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    String? avatarUrl;
    if (_profile != null &&
        _profile!.hinhAnhBenhNhan.isNotEmpty &&
        _profile!.hinhAnhBenhNhan != 'default.jpg') {
      avatarUrl = '${ApiService.base}/uploads/${_profile!.hinhAnhBenhNhan}';
    }

    return Scaffold(
      appBar: AppBar(title: const Text('Hồ sơ cá nhân')),
      body: _loading && _profile == null
          ? const Center(child: CircularProgressIndicator())
          : SingleChildScrollView(
        padding: const EdgeInsets.all(16),
        child: Column(
          children: [
            Center(
              child: Stack(
                children: [
                  CircleAvatar(
                    radius: 60,
                    backgroundColor: Colors.grey[300],
                    backgroundImage: _pickedImage != null
                        ? FileImage(_pickedImage!)
                        : (avatarUrl != null
                        ? NetworkImage(avatarUrl) as ImageProvider
                        : null),
                    child: (_pickedImage == null && avatarUrl == null)
                        ? const Icon(Icons.person,
                        size: 60, color: Colors.grey)
                        : null,
                  ),
                  Positioned(
                    bottom: 0,
                    right: 0,
                    child: InkWell(
                      onTap: _pickImage,
                      child: Container(
                        padding: const EdgeInsets.all(8),
                        decoration: const BoxDecoration(
                          color: Colors.blue,
                          shape: BoxShape.circle,
                        ),
                        child: const Icon(Icons.camera_alt,
                            color: Colors.white, size: 20),
                      ),
                    ),
                  ),
                ],
              ),
            ),
            const SizedBox(height: 24),

            _buildTextField("Mã Bệnh Nhân", _idController,
                isReadOnly: true, icon: Icons.perm_identity),
            _buildTextField("Họ và tên", _fullnameController,
                icon: Icons.person),
            _buildTextField("Mã thẻ BHYT", _insuranceController,
                icon: Icons.health_and_safety),

            Row(
              children: [
                Expanded(
                  child: _buildTextField("Ngày sinh", _dobController,
                      isReadOnly: true,
                      onTap: _selectDate,
                      icon: Icons.calendar_today),
                ),
                const SizedBox(width: 12),
                Expanded(
                  child: _buildTextField("Giới tính", _genderController,
                      icon: Icons.people),
                ),
              ],
            ),

            _buildTextField("Số điện thoại", _phoneController,
                type: TextInputType.phone, icon: Icons.phone),
            _buildTextField("Email", _emailController,
                isReadOnly: true, icon: Icons.email),
            _buildTextField("Địa chỉ", _addressController,
                icon: Icons.home),

            const SizedBox(height: 20),

            SizedBox(
              width: double.infinity,
              height: 50,
              child: ElevatedButton(
                onPressed: _loading ? null : _save,
                style: ElevatedButton.styleFrom(
                  backgroundColor: Colors.blue,
                  foregroundColor: Colors.white,
                ),
                child: _loading
                    ? const SizedBox(
                  height: 20,
                  width: 20,
                  child: CircularProgressIndicator(
                      color: Colors.white, strokeWidth: 2),
                )
                    : const Text('Lưu thay đổi',
                    style: TextStyle(fontSize: 16)),
              ),
            ),
          ],
        ),
      ),
    );
  }
}