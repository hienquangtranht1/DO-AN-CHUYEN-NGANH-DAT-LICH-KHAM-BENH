import 'dart:io';
import 'package:flutter/foundation.dart';
import 'package:flutter/material.dart';

import 'screens/login_screen.dart';
import 'services/notification_service.dart';

class _DevHttpOverrides extends HttpOverrides {
  final List<String> allowedHosts;
  _DevHttpOverrides({required this.allowedHosts});

  @override
  HttpClient createHttpClient(SecurityContext? context) {
    final client = super.createHttpClient(context);
    client.badCertificateCallback = (X509Certificate cert, String host, int port) {
      return kDebugMode && allowedHosts.contains(host);
    };
    return client;
  }
}

void main() async {
  WidgetsFlutterBinding.ensureInitialized();
  await NotificationService().init();
  if (kDebugMode) {
    HttpOverrides.global = _DevHttpOverrides(allowedHosts: ['10.0.2.2', 'localhost']);
  }

  runApp(const BookingApp());
}

class BookingApp extends StatelessWidget {
  const BookingApp({super.key});

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      title: 'Four Rock Mobile',
      theme: ThemeData(
        primarySwatch: Colors.blue,
        visualDensity: VisualDensity.adaptivePlatformDensity,
      ),
      home: const LoginScreen(),
      debugShowCheckedModeBanner: false,
    );
  }
}