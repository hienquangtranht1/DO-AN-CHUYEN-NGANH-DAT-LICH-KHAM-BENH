class Appointment {
  final int id;
  final int maBacSi;
  final String? bacSiHoTen;
  final DateTime ngayGio;
  final String? trangThai;
  final String? chuyenKhoa;
  final String? lyDoKham;
  final bool isPaid;

  Appointment({
    required this.id,
    required this.maBacSi,
    this.bacSiHoTen,
    required this.ngayGio,
    this.trangThai,
    this.chuyenKhoa,
    this.lyDoKham,
    this.isPaid = false,
  });

  factory Appointment.fromJson(Map<String, dynamic> json) {
    return Appointment(
      id: json['maLichHen'] ?? json['id'] ?? json['maLich'] ?? 0,
      maBacSi: json['maBacSi'] ?? 0,
      bacSiHoTen: json['bacSiHoTen'] ?? json['doctorName'] ?? 'Bác sĩ',
      ngayGio: json['ngayGio'] != null ? DateTime.parse(json['ngayGio']) : DateTime.now(),
      trangThai: json['trangThai'] ?? json['status'] ?? 'Chờ xác nhận',
      chuyenKhoa: json['chuyenKhoa'] ?? json['specialty'] ?? 'Đa khoa',
      lyDoKham: json['lyDoKham'] ?? json['reason'] ?? json['trieuChung'] ?? '',
      isPaid: json['isPaid'] ?? false,
    );
  }
}