class NotificationModel {
  final int maThongBao;
  final int maNguoiDung;
  final String tieuDe;
  final String noiDung;
  final DateTime ngayTao;
  final bool daXem;
  final int? maLichHen;

  NotificationModel({
    required this.maThongBao,
    required this.maNguoiDung,
    required this.tieuDe,
    required this.noiDung,
    required this.ngayTao,
    required this.daXem,
    this.maLichHen,
  });

  factory NotificationModel.fromJson(Map<String, dynamic> json) {
    return NotificationModel(
      maThongBao: json['maThongBao'] ?? 0,
      maNguoiDung: json['maNguoiDung'] ?? 0,
      tieuDe: json['tieuDe'] ?? 'Thông báo',
      noiDung: json['noiDung'] ?? '',
      ngayTao: json['ngayTao'] != null
          ? DateTime.parse(json['ngayTao'])
          : DateTime.now(),
      daXem: json['daXem'] ?? false,
      maLichHen: json['maLichHen'],
    );
  }
}