class CskhModel {
  final int id;
  final String fullName;

  CskhModel({required this.id, required this.fullName});

  factory CskhModel.fromJson(Map<String, dynamic> json) {
    return CskhModel(
      id: json['id'] ?? 0,
      fullName: json['fullName'] ?? 'Nhân viên hỗ trợ',
    );
  }
}