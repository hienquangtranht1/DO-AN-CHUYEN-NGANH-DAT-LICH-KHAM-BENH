import 'package:flutter/material.dart';
import 'package:intl/intl.dart';
import 'package:url_launcher/url_launcher.dart'; // Import url_launcher
import '../services/auth_service.dart';
import '../models/payment_res.dart';

class TopUpScreen extends StatefulWidget {
  const TopUpScreen({super.key});

  @override
  State<TopUpScreen> createState() => _TopUpScreenState();
}

class _TopUpScreenState extends State<TopUpScreen> {
  final AuthService _auth = AuthService();
  final TextEditingController _amountCtrl = TextEditingController();
  bool _isLoading = false;

  final List<double> _suggestions = [50000, 100000, 200000, 500000];

  Future<void> _processTopUp() async {
    final text = _amountCtrl.text.replaceAll(RegExp(r'[^0-9]'), '');
    final amount = double.tryParse(text) ?? 0;

    if (amount < 10000) {
      ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text("Số tiền nạp tối thiểu 10,000đ")));
      return;
    }

    setState(() => _isLoading = true);

    final PaymentRes result = await _auth.createDepositUrl(amount);

    setState(() => _isLoading = false);

    if (result.success && result.paymentUrl != null) {
      final uri = Uri.parse(result.paymentUrl!);

      if (await canLaunchUrl(uri)) {
        await launchUrl(uri, mode: LaunchMode.externalApplication);

        if (mounted) {
          Navigator.pop(context); // Đóng màn hình nạp để về Home chờ kết quả
          ScaffoldMessenger.of(context).showSnackBar(
              const SnackBar(content: Text("Đang mở cổng thanh toán VNPAY..."), backgroundColor: Colors.blue)
          );
        }
      } else {
        if (mounted) ScaffoldMessenger.of(context).showSnackBar(const SnackBar(content: Text("Không thể mở trình duyệt")));
      }
    } else {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
            SnackBar(content: Text(result.message ?? "Lỗi không xác định"), backgroundColor: Colors.red)
        );
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    final currencyFormat = NumberFormat("#,###", "vi_VN");

    return Scaffold(
      appBar: AppBar(title: const Text("Nạp tiền ví"), centerTitle: true, elevation: 0),
      body: Padding(
        padding: const EdgeInsets.all(20),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            const Text("Nhập số tiền (VNĐ)", style: TextStyle(fontSize: 16, fontWeight: FontWeight.bold)),
            const SizedBox(height: 10),
            TextField(
              controller: _amountCtrl,
              keyboardType: TextInputType.number,
              decoration: InputDecoration(
                suffixText: "đ",
                border: OutlineInputBorder(borderRadius: BorderRadius.circular(10)),
                hintText: "Tối thiểu 10,000",
              ),
            ),
            const SizedBox(height: 15),
            Wrap(
              spacing: 10,
              children: _suggestions.map((s) => ActionChip(
                label: Text("${currencyFormat.format(s)}đ"),
                onPressed: () => _amountCtrl.text = s.toInt().toString(),
                backgroundColor: Colors.blue.shade50,
              )).toList(),
            ),
            const Spacer(),
            SizedBox(
              width: double.infinity,
              height: 50,
              child: ElevatedButton(
                onPressed: _isLoading ? null : _processTopUp,
                style: ElevatedButton.styleFrom(backgroundColor: Colors.green),
                child: _isLoading
                    ? const SizedBox(width: 20, height: 20, child: CircularProgressIndicator(color: Colors.white, strokeWidth: 2))
                    : const Text("THANH TOÁN VNPAY", style: TextStyle(fontSize: 16, fontWeight: FontWeight.bold, color: Colors.white)),
              ),
            )
          ],
        ),
      ),
    );
  }
}