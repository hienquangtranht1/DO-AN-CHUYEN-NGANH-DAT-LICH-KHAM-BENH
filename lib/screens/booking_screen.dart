import 'dart:convert';
import 'package:flutter/material.dart';
import 'package:intl/intl.dart';
import '../services/auth_service.dart';
import '../services/api_service.dart';
import '../models/doctor.dart';

class BookingScreen extends StatefulWidget {
  final Doctor? preSelectedDoctor;

  const BookingScreen({super.key, this.preSelectedDoctor});

  @override
  State<BookingScreen> createState() => _BookingScreenState();
}

class _BookingScreenState extends State<BookingScreen> {
  final AuthService _auth = AuthService();
  final _formKey = GlobalKey<FormState>();

  List<Doctor> _doctors = [];
  Doctor? _selectedDoctor;

  final TextEditingController _nameCtrl = TextEditingController();
  final TextEditingController _phoneCtrl = TextEditingController();
  final TextEditingController _emailCtrl = TextEditingController();
  final TextEditingController _symptomsCtrl = TextEditingController();

  List<String> _workingDates = [];
  String? _selectedDate;
  bool _loadingDates = false;

  List<String> _availableTimes = [];
  String? _selectedTime;
  bool _loadingTimes = false;

  final List<String> _commonSymptoms = [
    'Đau đầu, chóng mặt',
    'Ho, sốt, sổ mũi',
    'Đau bụng, khó tiêu',
    'Đau mỏi cơ khớp',
    'Khám sức khỏe tổng quát',
    'Tái khám'
  ];
  String? _selectedCommonSymptom;
  bool _isSubmitting = false;

  @override
  void initState() {
    super.initState();
    _selectedDoctor = widget.preSelectedDoctor;

    _loadUserProfile();

    _loadDoctors();
  }


  Future<void> _loadUserProfile() async {
    final profile = await _auth.getProfileRaw();
    if (profile != null && mounted) {
      setState(() {
        _nameCtrl.text = profile['hoTen'] ?? '';
        _phoneCtrl.text = profile['soDienThoai'] ?? '';
        _emailCtrl.text = profile['email'] ?? '';
      });
    }
  }

  Future<void> _loadDoctors() async {
    final docs = await _auth.getDoctors();
    if (!mounted) return;
    setState(() {
      _doctors = docs;
      if (_selectedDoctor != null && docs.isNotEmpty) {
        try {
          _selectedDoctor = docs.firstWhere((d) => d.id == _selectedDoctor!.id);
        } catch (_) {}
      }
    });
    if (_selectedDoctor != null) _fetchWorkingDates();
  }

  Future<void> _fetchWorkingDates() async {
    if (_selectedDoctor == null) return;

    setState(() {
      _loadingDates = true;
      _workingDates = [];
      _selectedDate = null;
      _availableTimes = [];
      _selectedTime = null;
    });

    final dates = await _auth.getDoctorWorkingDates(_selectedDoctor!.id);

    if (mounted) {
      setState(() {
        _workingDates = dates;
        _loadingDates = false;
      });
    }
  }

  Future<void> _fetchTimeSlots() async {
    if (_selectedDoctor == null || _selectedDate == null) return;

    setState(() {
      _loadingTimes = true;
      _availableTimes = [];
      _selectedTime = null;
    });

    final times = await _auth.getAvailableTimes(_selectedDoctor!.id, _selectedDate!);

    if (mounted) {
      setState(() {
        _availableTimes = times;
        _loadingTimes = false;
      });
    }
  }

  String _formatDateDisplay(String isoDate) {
    try {
      final date = DateTime.parse(isoDate);
      return DateFormat('dd/MM/yyyy (EEEE)').format(date);
    } catch (_) {
      return isoDate;
    }
  }

  Future<void> _submit() async {
    if (_selectedDoctor == null || _selectedDate == null || _selectedTime == null) {
      ScaffoldMessenger.of(context).showSnackBar(const SnackBar(content: Text('Vui lòng chọn đầy đủ Bác sĩ, Ngày và Giờ khám')));
      return;
    }
    if (_symptomsCtrl.text.trim().isEmpty) {
      ScaffoldMessenger.of(context).showSnackBar(const SnackBar(content: Text('Vui lòng nhập triệu chứng hoặc chọn mẫu')));
      return;
    }

    setState(() => _isSubmitting = true);

    final data = {
      'SelectedDoctorId': _selectedDoctor!.id.toString(),
      'SelectedDate': _selectedDate,
      'SelectedTime': _selectedTime,
      'Symptoms': _symptomsCtrl.text,
    };

    try {
      final res = await _auth.bookAppointment(data);

      if (!mounted) return;
      setState(() => _isSubmitting = false);

      if (res['status'] == 200) {
        showDialog(
          context: context,
          barrierDismissible: false,
          builder: (ctx) => AlertDialog(
            shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(15)),
            title: const Column(
              children: [
                Icon(Icons.check_circle, color: Colors.green, size: 60),
                SizedBox(height: 10),
                Text("Thành công", style: TextStyle(color: Colors.green, fontWeight: FontWeight.bold))
              ],
            ),
            content: const Text("Đặt lịch thành công!\nVui lòng chờ bác sĩ xác nhận.", textAlign: TextAlign.center),
            actions: [
              TextButton(
                  onPressed: () {
                    Navigator.pop(ctx);
                    Navigator.pop(context, true);
                  },
                  child: const Text("Về trang chủ", style: TextStyle(fontSize: 16))
              )
            ],
          ),
        );
      } else {
        String msg = 'Lỗi đặt lịch';
        try {
          final body = jsonDecode(res['body']);
          if (body['message'] != null) msg = body['message'];
        } catch (_) {}
        ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text("❌ $msg"), backgroundColor: Colors.red));
      }
    } catch (e) {
      setState(() => _isSubmitting = false);
      ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text("Lỗi kết nối: $e")));
    }
  }


  @override
  Widget build(BuildContext context) {
    Widget buildDocInfo() {
      if (_selectedDoctor == null) return const SizedBox.shrink();

      final imageUrl = _selectedDoctor!.imageUrl != null
          ? (ApiService.base + _selectedDoctor!.imageUrl!)
          : null;

      return Container(
        padding: const EdgeInsets.all(16),
        decoration: BoxDecoration(
            color: Colors.blue.shade600,
            borderRadius: BorderRadius.circular(12),
            boxShadow: [BoxShadow(color: Colors.blue.withOpacity(0.3), blurRadius: 10, offset: const Offset(0, 5))]
        ),
        child: Row(children: [
          CircleAvatar(
              radius: 35,
              backgroundColor: Colors.white,
              backgroundImage: imageUrl != null ? NetworkImage(imageUrl) : null,
              child: imageUrl == null ? const Icon(Icons.person, size: 35, color: Colors.grey) : null
          ),
          const SizedBox(width: 16),
          Expanded(child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
            const Text("BÁC SĨ PHỤ TRÁCH", style: TextStyle(color: Colors.white70, fontSize: 11, fontWeight: FontWeight.bold, letterSpacing: 1)),
            const SizedBox(height: 4),
            Text(_selectedDoctor!.name, style: const TextStyle(color: Colors.white, fontSize: 18, fontWeight: FontWeight.bold)),
            Text(_selectedDoctor!.specialty, style: const TextStyle(color: Colors.white, fontSize: 14)),
          ]))
        ]),
      );
    }

    return Scaffold(
      appBar: AppBar(title: const Text('Đặt Lịch Khám Bệnh'), centerTitle: true, elevation: 0),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(16),
        child: Form(
          key: _formKey,
          child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [

            if (widget.preSelectedDoctor == null) ...[
              DropdownButtonFormField<Doctor>(
                decoration: InputDecoration(
                    labelText: 'Chọn Bác sĩ',
                    border: OutlineInputBorder(borderRadius: BorderRadius.circular(10)),
                    prefixIcon: const Icon(Icons.person_search)
                ),
                value: _selectedDoctor,
                items: _doctors.map((d) => DropdownMenuItem(value: d, child: Text(d.name))).toList(),
                onChanged: (v) {
                  setState(() => _selectedDoctor = v);
                  _fetchWorkingDates();
                },
              ),
              const SizedBox(height: 16),
            ],

            buildDocInfo(),
            const SizedBox(height: 24),

            const Text("Thông tin bệnh nhân", style: TextStyle(fontSize: 16, fontWeight: FontWeight.bold, color: Colors.blue)),
            const SizedBox(height: 12),
            Row(children: [
              Expanded(child: TextFormField(
                  controller: _nameCtrl,
                  readOnly: true,
                  decoration: InputDecoration(labelText: 'Họ tên', border: OutlineInputBorder(borderRadius: BorderRadius.circular(10)), filled: true, fillColor: Colors.grey.shade100, contentPadding: const EdgeInsets.symmetric(horizontal: 12, vertical: 12))
              )),
              const SizedBox(width: 12),
              Expanded(child: TextFormField(
                  controller: _phoneCtrl,
                  readOnly: true,
                  decoration: InputDecoration(labelText: 'SĐT', border: OutlineInputBorder(borderRadius: BorderRadius.circular(10)), filled: true, fillColor: Colors.grey.shade100, contentPadding: const EdgeInsets.symmetric(horizontal: 12, vertical: 12))
              )),
            ]),
            const SizedBox(height: 12),
            TextFormField(
                controller: _emailCtrl,
                readOnly: true,
                decoration: InputDecoration(labelText: 'Email', border: OutlineInputBorder(borderRadius: BorderRadius.circular(10)), filled: true, fillColor: Colors.grey.shade100, contentPadding: const EdgeInsets.symmetric(horizontal: 12, vertical: 12))
            ),

            const SizedBox(height: 24), const Divider(thickness: 1), const SizedBox(height: 16),

            const Text("Chọn Ngày Hẹn", style: TextStyle(fontWeight: FontWeight.bold, fontSize: 16)),
            const SizedBox(height: 8),
            if (_loadingDates)
              const Center(child: Padding(padding: EdgeInsets.all(10), child: CircularProgressIndicator()))
            else
              DropdownButtonFormField<String>(
                decoration: InputDecoration(
                    hintText: 'Vui lòng chọn ngày...',
                    border: OutlineInputBorder(borderRadius: BorderRadius.circular(10)),
                    prefixIcon: const Icon(Icons.calendar_month, color: Colors.blue),
                    contentPadding: const EdgeInsets.symmetric(horizontal: 12, vertical: 15)
                ),
                value: _selectedDate,
                items: _workingDates.map((d) => DropdownMenuItem(value: d, child: Text(_formatDateDisplay(d)))).toList(),
                onChanged: (v) {
                  setState(() => _selectedDate = v);
                  _fetchTimeSlots();
                },
              ),

            const SizedBox(height: 24),

            const Text("Chọn Khung Giờ", style: TextStyle(fontWeight: FontWeight.bold, fontSize: 16)),
            const SizedBox(height: 8),
            if (_loadingTimes)
              const Center(child: Padding(padding: EdgeInsets.all(10), child: CircularProgressIndicator()))
            else if (_selectedDate != null && _availableTimes.isEmpty)
              Container(padding: const EdgeInsets.all(12), width: double.infinity, decoration: BoxDecoration(color: Colors.red.shade50, borderRadius: BorderRadius.circular(8)), child: const Text("Hết lịch trống hôm nay", style: TextStyle(color: Colors.red), textAlign: TextAlign.center))
            else if (_availableTimes.isNotEmpty)
                Wrap(
                    spacing: 10,
                    runSpacing: 10,
                    children: _availableTimes.map((t) => ChoiceChip(
                      label: Text(t),
                      selected: _selectedTime == t,
                      onSelected: (s) => setState(() => _selectedTime = s ? t : null),
                      selectedColor: Colors.blue,
                      backgroundColor: Colors.grey.shade100,
                      labelStyle: TextStyle(color: _selectedTime == t ? Colors.white : Colors.black, fontWeight: FontWeight.bold),
                      padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
                    )).toList()
                )
              else
                const Text("Vui lòng chọn ngày trước", style: TextStyle(color: Colors.grey, fontStyle: FontStyle.italic)),

            const SizedBox(height: 24), const Divider(thickness: 1), const SizedBox(height: 16),

            const Text("Triệu chứng bệnh / Ghi chú", style: TextStyle(fontWeight: FontWeight.bold, fontSize: 16)),
            const SizedBox(height: 8),
            DropdownButtonFormField<String>(
              decoration: InputDecoration(border: OutlineInputBorder(borderRadius: BorderRadius.circular(10)), contentPadding: const EdgeInsets.symmetric(horizontal: 12, vertical: 15)),
              hint: const Text("-- Chọn triệu chứng mẫu --"),
              value: _selectedCommonSymptom,
              items: _commonSymptoms.map((s) => DropdownMenuItem(value: s, child: Text(s))).toList(),
              onChanged: (val) {
                setState(() {
                  _selectedCommonSymptom = val;
                  if (val != null) _symptomsCtrl.text = val;
                });
              },
            ),
            const SizedBox(height: 12),
            TextFormField(
                controller: _symptomsCtrl,
                maxLines: 3,
                decoration: InputDecoration(
                    hintText: 'Mô tả chi tiết tình trạng sức khỏe...',
                    border: OutlineInputBorder(borderRadius: BorderRadius.circular(10)),
                    fillColor: Colors.white,
                    filled: true
                )
            ),

            const SizedBox(height: 30),


            SizedBox(
              width: double.infinity, height: 55,
              child: ElevatedButton(
                onPressed: _isSubmitting ? null : _submit,
                style: ElevatedButton.styleFrom(
                    backgroundColor: Colors.blue.shade700,
                    foregroundColor: Colors.white,
                    shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
                    elevation: 5
                ),
                child: _isSubmitting
                    ? const CircularProgressIndicator(color: Colors.white)
                    : const Text('XÁC NHẬN ĐẶT LỊCH', style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold)),
              ),
            ),
            const SizedBox(height: 40),
          ]),
        ),
      ),
    );
  }
}