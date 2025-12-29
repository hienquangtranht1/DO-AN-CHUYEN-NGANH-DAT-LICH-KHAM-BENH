class ProfileModel {
  final int id;
  final String hoTen;
  final String email;
  final String soDienThoai;
  final String? ngaySinh;
  final String gioiTinh;
  final String diaChi;
  final String soBaoHiem;
  final String hinhAnhBenhNhan;

  ProfileModel({
    required this.id,
    required this.hoTen,
    required this.email,
    required this.soDienThoai,
    this.ngaySinh,
    required this.gioiTinh,
    required this.diaChi,
    required this.soBaoHiem,
    required this.hinhAnhBenhNhan,
  });

  factory ProfileModel.fromJson(Map<String, dynamic> json) {
    return ProfileModel(
      id: json['maBenhNhan'] ?? json['MaBenhNhan'] ?? 0,
      hoTen: json['hoTen'] ?? json['HoTen'] ?? '',
      email: json['email'] ?? json['Email'] ?? '',
      soDienThoai: json['soDienThoai'] ?? json['SoDienThoai'] ?? '',
      ngaySinh: json['ngaySinh'] ?? json['NgaySinh'],
      gioiTinh: json['gioiTinh'] ?? json['GioiTinh'] ?? 'Khác',
      diaChi: json['diaChi'] ?? json['DiaChi'] ?? '',
      soBaoHiem: json['soBaoHiem'] ?? json['SoBaoHiem'] ?? '',
      hinhAnhBenhNhan: json['hinhAnhBenhNhan'] ?? json['HinhAnhBenhNhan'] ?? 'default.jpg',
    );
  }
}