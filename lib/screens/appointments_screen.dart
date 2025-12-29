import 'dart:async';
import 'dart:convert';
import 'package:flutter/material.dart';
import 'package:intl/intl.dart';
import '../services/auth_service.dart';
import '../services/signalr_service.dart';
import '../models/appointment.dart';

class AppointmentsScreen extends StatefulWidget {
  const AppointmentsScreen({super.key});

  @override
  State<AppointmentsScreen> createState() => _AppointmentsScreenState();
}

class _AppointmentsScreenState extends State<AppointmentsScreen> {
  final AuthService _auth = AuthService();
  List<Appointment> _items = [];
  bool _isLoading = true;
  String? _errorMessage;
  StreamSubscription? _signalRSubscription;

  @override
  void initState() {
    super.initState();
    _loadAppointments(showLoading: true);
    _signalRSubscription = SignalRService().onDataUpdated.listen((_) {
      if (mounted) _loadAppointments(showLoading: false);
    });
  }

  @override
  void dispose() {
    _signalRSubscription?.cancel();
    super.dispose();
  }

  Future<void> _loadAppointments({bool showLoading = false}) async {
    if (!mounted) return;
    if (showLoading) setState(() => _isLoading = true);

    try {
      final res = await _auth.getAppointments();
      if (res['status'] == 200) {
        final body = jsonDecode(res['body']);
        List<dynamic> listData = [];
        if (body is Map && body.containsKey('data')) {
          listData = body['data'];
        } else if (body is List) {
          listData = body;
        }

        if (mounted) {
          setState(() {
            _items = listData.map((e) => Appointment.fromJson(e)).toList();
            _items.sort((a, b) => b.ngayGio.compareTo(a.ngayGio));
            _errorMessage = null;
          });
        }
      } else {
        if (mounted) setState(() => _errorMessage = "Không thể tải dữ liệu.");
      }
    } catch (e) {
      if (mounted) setState(() => _errorMessage = "Lỗi kết nối mạng.");
    } finally {
      if (mounted) setState(() => _isLoading = false);
    }
  }

  Future<void> _processPayment(Appointment item) async {
    final confirm = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        title: const Text("Xác nhận thanh toán"),
        content: Text("Thanh toán 50.000 VNĐ cho lịch hẹn với ${item.bacSiHoTen}?"),
        actions: [
          TextButton(onPressed: () => Navigator.pop(ctx, false), child: const Text("Hủy")),
          ElevatedButton(
            onPressed: () => Navigator.pop(ctx, true),
            style: ElevatedButton.styleFrom(backgroundColor: Colors.blue),
            child: const Text("Thanh toán", style: TextStyle(color: Colors.white)),
          ),
        ],
      ),
    );

    if (confirm != true) return;

    if (!mounted) return;
    showDialog(context: context, barrierDismissible: false, builder: (_) => const Center(child: CircularProgressIndicator()));

    final res = await _auth.payAppointment(item.id);
    if (!mounted) return;
    Navigator.pop(context);

    if (res['status'] == 200) {
      final body = jsonDecode(res['body']);
      if (body['success'] == true) {
        ScaffoldMessenger.of(context).showSnackBar(const SnackBar(content: Text("Thanh toán thành công!"), backgroundColor: Colors.green));
        _loadAppointments();
      } else {
        String msg = body['message'] ?? "Lỗi thanh toán";
        if (body['insufficient'] == true) {
          _showTopUpDialog();
        } else {
          ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(msg), backgroundColor: Colors.red));
        }
      }
    } else {
      ScaffoldMessenger.of(context).showSnackBar(const SnackBar(content: Text("Lỗi kết nối Server"), backgroundColor: Colors.red));
    }
  }

  void _showTopUpDialog() {
    showDialog(
      context: context,
      builder: (ctx) => AlertDialog(
        title: const Text("Số dư không đủ"),
        content: const Text("Vui lòng nạp thêm tiền vào ví."),
        actions: [
          TextButton(onPressed: () => Navigator.pop(ctx), child: const Text("Đóng")),
        ],
      ),
    );
  }

  Color _getStatusColor(String? status) {
    switch (status) {
      case 'Đã hủy':
      case 'Từ chối':
        return Colors.red;
      case 'Đã xác nhận':
        return Colors.green;
      case 'Đã hoàn thành':
        return Colors.blue;
      default:
        return Colors.orange;
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: const Color(0xFFF5F7FA),
      appBar: AppBar(
        title: const Text('Lịch hẹn của tôi'),
        centerTitle: true,
        elevation: 0,
        backgroundColor: Colors.white,
        foregroundColor: Colors.black,
      ),
      body: _buildBody(),
    );
  }

  Widget _buildBody() {
    if (_isLoading && _items.isEmpty) {
      return const Center(child: CircularProgressIndicator());
    }

    if (_errorMessage != null && _items.isEmpty) {
      return Center(
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            const Icon(Icons.error_outline, size: 60, color: Colors.grey),
            const SizedBox(height: 10),
            Text(_errorMessage!, style: const TextStyle(color: Colors.grey)),
            TextButton(onPressed: () => _loadAppointments(showLoading: true), child: const Text("Thử lại"))
          ],
        ),
      );
    }

    if (_items.isEmpty) {
      return Center(
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Icon(Icons.calendar_today_outlined, size: 80, color: Colors.grey.shade300),
            const SizedBox(height: 16),
            Text("Bạn chưa có lịch hẹn nào", style: TextStyle(color: Colors.grey.shade600)),
          ],
        ),
      );
    }

    return RefreshIndicator(
      onRefresh: () async => await _loadAppointments(showLoading: false),
      child: ListView.separated(
        padding: const EdgeInsets.all(16),
        itemCount: _items.length,
        separatorBuilder: (_, __) => const SizedBox(height: 12),
        itemBuilder: (context, i) {
          final item = _items[i];
          return _buildAppointmentCard(item);
        },
      ),
    );
  }

  Widget _buildAppointmentCard(Appointment item) {
    final dateStr = DateFormat('dd/MM/yyyy').format(item.ngayGio);
    final timeStr = DateFormat('HH:mm').format(item.ngayGio);
    final isCancelled = item.trangThai == 'Đã hủy' || item.trangThai == 'Từ chối';
    final isConfirmed = item.trangThai == 'Đã xác nhận';

    return Container(
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(16),
        boxShadow: [
          BoxShadow(
            color: Colors.black.withOpacity(0.05),
            blurRadius: 10,
            offset: const Offset(0, 4),
          )
        ],
        border: isCancelled ? Border.all(color: Colors.red.shade100) : null,
      ),
      child: Material(
        color: Colors.transparent,
        child: Padding(
          padding: const EdgeInsets.all(16),
          child: Column(
            children: [
              Row(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Container(
                    width: 50, height: 50,
                    decoration: BoxDecoration(
                      color: isCancelled ? Colors.grey.shade100 : Colors.blue.shade50,
                      borderRadius: BorderRadius.circular(12),
                    ),
                    child: Icon(
                        Icons.medical_services_rounded,
                        color: isCancelled ? Colors.grey : Colors.blue
                    ),
                  ),
                  const SizedBox(width: 16),
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(
                          item.bacSiHoTen ?? "Bác sĩ",
                          style: TextStyle(
                            fontWeight: FontWeight.bold,
                            fontSize: 16,
                            color: isCancelled ? Colors.grey : Colors.black87,
                            decoration: isCancelled ? TextDecoration.lineThrough : null,
                          ),
                        ),
                        const SizedBox(height: 4),
                        Text(
                          item.chuyenKhoa ?? "Chuyên khoa",
                          style: TextStyle(fontSize: 13, color: Colors.grey.shade600),
                        ),
                      ],
                    ),
                  ),
                  Container(
                    padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 5),
                    decoration: BoxDecoration(
                      color: _getStatusColor(item.trangThai).withOpacity(0.1),
                      borderRadius: BorderRadius.circular(20),
                    ),
                    child: Text(
                      item.trangThai ?? "Chờ xử lý",
                      style: TextStyle(
                        color: _getStatusColor(item.trangThai),
                        fontWeight: FontWeight.bold,
                        fontSize: 12,
                      ),
                    ),
                  )
                ],
              ),
              const Padding(
                padding: EdgeInsets.symmetric(vertical: 12),
                child: Divider(height: 1),
              ),
              Row(
                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                children: [
                  Row(
                    children: [
                      Icon(Icons.access_time_filled, size: 16, color: Colors.grey.shade500),
                      const SizedBox(width: 6),
                      Text("$timeStr - $dateStr", style: const TextStyle(fontWeight: FontWeight.w600)),
                    ],
                  ),
                  if (item.isPaid)
                    const Row(children: [Icon(Icons.check_circle, size: 18, color: Colors.green), SizedBox(width: 4), Text("Đã thanh toán", style: TextStyle(color: Colors.green, fontWeight: FontWeight.bold))])
                  else if (isConfirmed)
                    SizedBox(
                      height: 32,
                      child: ElevatedButton(
                        onPressed: () => _processPayment(item),
                        style: ElevatedButton.styleFrom(backgroundColor: Colors.blue, padding: const EdgeInsets.symmetric(horizontal: 12), shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(20))),
                        child: const Text("Thanh toán 50k", style: TextStyle(fontSize: 12, color: Colors.white)),
                      ),
                    )
                  else if (!isCancelled)
                      const Text("Chờ xác nhận", style: TextStyle(color: Colors.grey, fontSize: 12, fontStyle: FontStyle.italic))
                ],
              )
            ],
          ),
        ),
      ),
    );
  }
}