class Doctor {
  final int id;
  final String name;
  final String specialty;
  final String? imageUrl;
  final String? description;
  final String? phone;

  Doctor({
    required this.id,
    required this.name,
    required this.specialty,
    this.imageUrl,
    this.description,
    this.phone,
  });

  factory Doctor.fromJson(Map<String, dynamic> json) {
    return Doctor(
      id: json['id'] ?? 0,
      name: json['name'] ?? 'Bác sĩ',
      specialty: json['specialty'] ?? 'Đa khoa',
      imageUrl: json['imageUrl'],
      description: json['description'],
      phone: json['phone'],
    );
  }

  String? fullImageUrl(String baseUrl) {
    if (imageUrl == null || imageUrl!.isEmpty) return null;
    if (imageUrl!.startsWith('http')) return imageUrl;
    return "$baseUrl$imageUrl";
  }
}