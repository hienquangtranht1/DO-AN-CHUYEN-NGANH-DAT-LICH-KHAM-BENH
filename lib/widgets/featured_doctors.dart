import 'package:flutter/material.dart';
import '../services/auth_service.dart';
import '../models/doctor.dart';
import 'doctor_card.dart';

class FeaturedDoctors extends StatefulWidget {
  const FeaturedDoctors({super.key});
  @override
  State<FeaturedDoctors> createState() => _FeaturedDoctorsState();
}

class _FeaturedDoctorsState extends State<FeaturedDoctors> {
  late Future<List<Doctor>> _future;

  @override
  void initState() {
    super.initState();
    _future = AuthService().getDoctors();
  }

  @override
  Widget build(BuildContext context) {
    return FutureBuilder<List<Doctor>>(
      future: _future,
      builder: (context, snapshot) {
        if (snapshot.connectionState == ConnectionState.waiting) return const Center(child: CircularProgressIndicator());
        if (snapshot.hasError || !snapshot.hasData || snapshot.data!.isEmpty) return const Text('Không có dữ liệu bác sĩ');

        final list = snapshot.data!;
        return SizedBox(
          height: 160,
          child: ListView.builder(
            scrollDirection: Axis.horizontal,
            itemCount: list.length,
            itemBuilder: (ctx, i) => DoctorCard(doctor: list[i]),
          ),
        );
      },
    );
  }
}