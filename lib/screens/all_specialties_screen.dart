import 'package:flutter/material.dart';
import 'doctor_search_screen.dart';

class AllSpecialtiesScreen extends StatelessWidget {
  const AllSpecialtiesScreen({super.key});

  final List<Map<String, dynamic>> _allDepts = const [
    {'name': 'Tim mạch', 'icon': Icons.favorite, 'color': Colors.red},
    {'name': 'Thần kinh', 'icon': Icons.psychology, 'color': Colors.purple},
    {'name': 'Nha khoa', 'icon': Icons.medication, 'color': Colors.orange},
    {'name': 'Mắt', 'icon': Icons.visibility, 'color': Colors.blue},
    {'name': 'Xương khớp', 'icon': Icons.accessibility_new, 'color': Colors.green},
    {'name': 'Da liễu', 'icon': Icons.face, 'color': Colors.pink},
    {'name': 'Nhi khoa', 'icon': Icons.child_care, 'color': Colors.teal},
    {'name': 'Tai Mũi Họng', 'icon': Icons.hearing, 'color': Colors.indigo},
    {'name': 'Sản phụ khoa', 'icon': Icons.pregnant_woman, 'color': Colors.pinkAccent},
    {'name': 'Tiêu hóa', 'icon': Icons.fastfood, 'color': Colors.brown},
    {'name': 'Hô hấp', 'icon': Icons.air, 'color': Colors.cyan},
    {'name': 'Ung bướu', 'icon': Icons.health_and_safety, 'color': Colors.deepPurple},
    {'name': 'Phục hồi chức năng', 'icon': Icons.wheelchair_pickup, 'color': Colors.amber},
    {'name': 'Y học cổ truyền', 'icon': Icons.spa, 'color': Colors.greenAccent},
    {'name': 'Xét nghiệm', 'icon': Icons.science, 'color': Colors.lime},
  ];

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text("Tất cả Chuyên Khoa"), backgroundColor: Colors.white, foregroundColor: Colors.black, elevation: 0),
      body: GridView.builder(
        padding: const EdgeInsets.all(16),
        gridDelegate: const SliverGridDelegateWithFixedCrossAxisCount(
          crossAxisCount: 3, childAspectRatio: 0.85, crossAxisSpacing: 16, mainAxisSpacing: 16,
        ),
        itemCount: _allDepts.length,
        itemBuilder: (context, index) {
          final dept = _allDepts[index];
          return GestureDetector(
            onTap: () {
              Navigator.push(context, MaterialPageRoute(builder: (_) => DoctorSearchScreen(initialQuery: dept['name'])));
            },
            child: Container(
              decoration: BoxDecoration(
                color: Colors.white, borderRadius: BorderRadius.circular(12),
                boxShadow: [BoxShadow(color: Colors.grey.withOpacity(0.1), blurRadius: 5)], border: Border.all(color: Colors.grey.shade100),
              ),
              child: Column(
                mainAxisAlignment: MainAxisAlignment.center,
                children: [
                  Icon(dept['icon'], color: dept['color'], size: 35),
                  const SizedBox(height: 10),
                  Text(dept['name'], style: const TextStyle(fontWeight: FontWeight.bold, fontSize: 13), textAlign: TextAlign.center),
                ],
              ),
            ),
          );
        },
      ),
    );
  }
}