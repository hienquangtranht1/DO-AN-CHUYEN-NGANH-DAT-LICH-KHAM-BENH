class PaymentRes {
  final bool success;
  final String? paymentUrl;
  final String? message;

  PaymentRes({
    required this.success,
    this.paymentUrl,
    this.message,
  });

  factory PaymentRes.fromJson(Map<String, dynamic> json) {
    return PaymentRes(
      success: json['success'] ?? false,
      paymentUrl: json['paymentUrl'],
      message: json['message'],
    );
  }
}