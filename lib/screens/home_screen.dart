// lib/screens/home_screen.dart
import 'dart:async';
import 'package:flutter/material.dart';
import 'package:intl/intl.dart';

import '../services/api_service.dart';
import '../services/auth_service.dart';
import '../services/signalr_service.dart';
import '../services/chat_service.dart';

import '../models/doctor.dart';
import '../models/cskh_model.dart';

import '../widgets/doctor_card.dart';

import 'doctor_search_screen.dart';
import 'all_specialties_screen.dart';
import 'profile_screen.dart';
import 'appointments_screen.dart';
import 'change_password_screen.dart';
import 'settings_screen.dart';
import 'booking_screen.dart';
import 'login_screen.dart';
import 'notification_screen.dart';
import 'chat_screen.dart';
import 'top_up_screen.dart';

class HomeScreen extends StatefulWidget {
  const HomeScreen({super.key});
  @override
  State<HomeScreen> createState() => _HomeScreenState();
}

class _HomeScreenState extends State<HomeScreen> {
  final ApiService _api = ApiService();
  final AuthService _auth = AuthService();

  List<Doctor> _allDoctors = [];
  bool _loadingDocs = true;
  String _health = '...';
  int _unreadNotifCount = 0;
  int? _currentUserId;
  double _walletBalance = 0.0;
  StreamSubscription? _signalRSubscription;

  final List<Map<String, dynamic>> _departments = [
    {'name': 'Tim mạch', 'icon': Icons.favorite, 'color': Colors.red},
    {'name': 'Thần kinh', 'icon': Icons.psychology, 'color': Colors.deepPurple},
    {'name': 'Nha khoa', 'icon': Icons.medication, 'color': Colors.orange},
    {'name': 'Mắt', 'icon': Icons.visibility, 'color': Colors.blue},
    {'name': 'Xương khớp', 'icon': Icons.accessibility_new, 'color': Colors.green},
    {'name': 'Da liễu', 'icon': Icons.face, 'color': Colors.pink},
    {'name': 'Nhi khoa', 'icon': Icons.child_care, 'color': Colors.teal},
    {'name': 'Tai Mũi Họng', 'icon': Icons.hearing, 'color': Colors.indigo},
  ];

  @override
  void initState() {
    super.initState();
    _initData();

    // Lắng nghe Real-time: Khi Server báo "ReceiveStatusChange" (Nạp tiền xong)
    _signalRSubscription = SignalRService().onDataUpdated.listen((_) {
      if (mounted) {
        _fetchUnreadCount();
        _fetchWallet();
      }
    });
  }

  @override
  void dispose() {
    _signalRSubscription?.cancel();
    super.dispose();
  }

  Future<void> _initData() async {
    final userInfo = await _auth.getUserInfo();
    if (userInfo != null) {
      _currentUserId = userInfo['userId'];
      await SignalRService().initialize();
      await ChatService().initialize();
    }
    _checkHealth();
    await _loadDoctors();
    await _fetchUnreadCount();
    await _fetchWallet();
  }

  Future<void> _fetchWallet() async {
    try {
      final bal = await _auth.getWalletBalance();
      if (mounted) setState(() => _walletBalance = bal);
    } catch (_) {}
  }

  Future<void> _checkHealth() async {
    try {
      final ok = await _api.health();
      if (mounted) setState(() => _health = ok ? 'Online' : 'Offline');
    } catch (_) {
      if (mounted) setState(() => _health = 'Error');
    }
  }

  Future<void> _loadDoctors() async {
    if (!mounted) return;
    setState(() => _loadingDocs = true);
    try {
      final docs = await _auth.getDoctors();
      if (mounted) {
        setState(() {
          _allDoctors = docs;
          _loadingDocs = false;
        });
      }
    } catch (e) {
      if (mounted) setState(() => _loadingDocs = false);
    }
  }

  Future<void> _fetchUnreadCount() async {
    final count = await _auth.getUnreadNotificationCount();
    if (mounted) setState(() => _unreadNotifCount = count);
  }

  void _handleLogout() {
    SignalRService().stop();
    _auth.logout();
    Navigator.pushAndRemoveUntil(
      context,
      MaterialPageRoute(builder: (_) => const LoginScreen()),
          (r) => false,
    );
  }

  void _bookDoctor(Doctor doctor) async {
    final result = await Navigator.push(
      context,
      MaterialPageRoute(builder: (_) => BookingScreen(preSelectedDoctor: doctor)),
    );
    if (result == true) {
      _fetchUnreadCount();
    }
  }

  void _showOnlineCSKHList(BuildContext context) {
    showModalBottomSheet(
      context: context,
      shape: const RoundedRectangleBorder(borderRadius: BorderRadius.vertical(top: Radius.circular(20))),
      backgroundColor: Colors.white,
      builder: (ctx) {
        return Container(
          padding: const EdgeInsets.all(16),
          height: 450,
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Center(child: Container(width: 40, height: 4, decoration: BoxDecoration(color: Colors.grey[300], borderRadius: BorderRadius.circular(2)))),
              const SizedBox(height: 15),
              const Text("Nhân viên hỗ trợ trực tuyến", style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold)),
              const SizedBox(height: 5),
              const Text("Chọn một nhân viên để bắt đầu chat", style: TextStyle(color: Colors.grey, fontSize: 13)),
              const SizedBox(height: 15),
              Expanded(
                child: StreamBuilder<List<CskhModel>>(
                  stream: ChatService().onOnlineListUpdated,
                  builder: (context, snapshot) {
                    if (!snapshot.hasData) {
                      ChatService().fetchOnlineCSKH();
                      return const Center(child: CircularProgressIndicator());
                    }
                    final list = snapshot.data ?? [];
                    if (list.isEmpty) {
                      return Center(
                        child: Column(
                          mainAxisAlignment: MainAxisAlignment.center,
                          children: [
                            Icon(Icons.nightlight_round, size: 50, color: Colors.grey[300]),
                            const SizedBox(height: 10),
                            const Text("Hiện không có nhân viên nào online.", style: TextStyle(color: Colors.grey)),
                          ],
                        ),
                      );
                    }
                    return ListView.builder(
                      itemCount: list.length,
                      itemBuilder: (ctx, i) {
                        final agent = list[i];
                        return Card(
                          elevation: 0,
                          color: Colors.blue.shade50,
                          margin: const EdgeInsets.only(bottom: 10),
                          shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
                          child: ListTile(
                            contentPadding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
                            leading: Stack(
                              children: [
                                const CircleAvatar(radius: 24, backgroundColor: Colors.white, child: Icon(Icons.support_agent, color: Colors.blue)),
                                Positioned(
                                  right: 0,
                                  bottom: 0,
                                  child: Container(
                                    width: 12,
                                    height: 12,
                                    decoration: BoxDecoration(color: Colors.green, shape: BoxShape.circle, border: Border.all(color: Colors.white, width: 2)),
                                  ),
                                )
                              ],
                            ),
                            title: Text(agent.fullName, style: const TextStyle(fontWeight: FontWeight.bold, fontSize: 16)),
                            subtitle: const Text("Sẵn sàng hỗ trợ", style: TextStyle(color: Colors.green, fontSize: 12, fontWeight: FontWeight.w500)),
                            trailing: const Icon(Icons.chat_bubble, color: Colors.blue),
                            onTap: () {
                              Navigator.pop(context);
                              Navigator.push(context, MaterialPageRoute(builder: (_) => ChatScreen(targetCskhId: agent.id, targetName: agent.fullName)));
                            },
                          ),
                        );
                      },
                    );
                  },
                ),
              ),
            ],
          ),
        );
      },
    );
  }

  // ---------- Drawer header tùy chỉnh (fix pixel & overflow) ----------
  Widget _buildDrawerHeader(BuildContext context) {
    final currencyFormat = NumberFormat('#,###', 'vi_VN');

    return SafeArea(
      bottom: true,
      child: Material(
        color: Colors.transparent,
        clipBehavior: Clip.hardEdge,
        child: Container(
          width: double.infinity,
          padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 14),
          decoration: BoxDecoration(
            color: Colors.blue,
            image: const DecorationImage(
              image: AssetImage('assets/background.png'),
              fit: BoxFit.cover,
              opacity: 0.9,
              alignment: Alignment.center,
            ),
            boxShadow: [BoxShadow(color: Colors.black.withOpacity(0.06), blurRadius: 6, offset: Offset(0, 2))],
          ),
          child: Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              // Avatar
              CircleAvatar(
                radius: 30,
                backgroundColor: Colors.white,
                child: Image.asset(
                  'assets/hospital.png',
                  width: 40,
                  height: 40,
                  fit: BoxFit.contain,
                  errorBuilder: (_, __, ___) => const Icon(Icons.local_hospital, size: 40, color: Colors.blue),
                ),
              ),
              const SizedBox(width: 12),
              // Info + wallet
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    const Text('Four Rock Hospital', style: TextStyle(color: Colors.white, fontWeight: FontWeight.bold, fontSize: 16)),
                    const SizedBox(height: 4),
                    const Text('Chăm sóc sức khỏe toàn diện', style: TextStyle(color: Colors.white70, fontSize: 12)),
                    const SizedBox(height: 10),
                    Row(
                      children: [
                        // Wallet box: constrained and flexible to avoid overflow
                        ConstrainedBox(
                          constraints: const BoxConstraints(maxWidth: 120),
                          child: Container(
                            padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
                            decoration: BoxDecoration(
                              color: Colors.white.withOpacity(0.18),
                              borderRadius: BorderRadius.circular(8),
                              boxShadow: [BoxShadow(color: Colors.black.withOpacity(0.03), blurRadius: 3, offset: Offset(0, 1))],
                            ),
                            child: Row(
                              mainAxisSize: MainAxisSize.min,
                              children: [
                                const Icon(Icons.account_balance_wallet, color: Colors.white, size: 14),
                                const SizedBox(width: 6),
                                Flexible(
                                  child: Text(
                                    "${currencyFormat.format(_walletBalance)} đ",
                                    style: const TextStyle(color: Colors.white, fontWeight: FontWeight.bold, fontSize: 13),
                                    overflow: TextOverflow.ellipsis,
                                    maxLines: 1,
                                  ),
                                ),
                              ],
                            ),
                          ),
                        ),
                        const SizedBox(width: 10),
                        // Nạp button: fixed height to avoid layout shifts
                        SizedBox(
                          height: 32,
                          child: ElevatedButton(
                            style: ElevatedButton.styleFrom(
                              padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
                              backgroundColor: Colors.green,
                              shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
                              elevation: 0,
                            ),
                            onPressed: () {
                              Navigator.pop(context);
                              Navigator.push(context, MaterialPageRoute(builder: (_) => const TopUpScreen()));
                            },
                            child: const Row(
                              mainAxisSize: MainAxisSize.min,
                              children: [
                                Icon(Icons.add, color: Colors.white, size: 14),
                                SizedBox(width: 4),
                                Text("Nạp", style: TextStyle(color: Colors.white, fontWeight: FontWeight.bold, fontSize: 12)),
                              ],
                            ),
                          ),
                        ),
                      ],
                    ),
                  ],
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }

  // ---------- Drawer ----------
  Widget _buildDrawer(BuildContext context) {
    return Drawer(
      elevation: 0,
      child: Column(
        children: [
          _buildDrawerHeader(context),
          Expanded(
            child: ListView(
              padding: EdgeInsets.zero,
              children: [
                ListTile(leading: const Icon(Icons.person, color: Colors.blue), title: const Text('Hồ sơ cá nhân'), onTap: () { Navigator.pop(context); Navigator.push(context, MaterialPageRoute(builder: (_) => const ProfileScreen())); }),
                ListTile(leading: const Icon(Icons.calendar_month, color: Colors.green), title: const Text('Lịch hẹn của tôi'), onTap: () { Navigator.pop(context); Navigator.push(context, MaterialPageRoute(builder: (_) => const AppointmentsScreen())); }),
                ListTile(leading: const Icon(Icons.monetization_on, color: Colors.purple), title: const Text('Nạp tiền vào ví'), onTap: () { Navigator.pop(context); Navigator.push(context, MaterialPageRoute(builder: (_) => const TopUpScreen())); }),
                ListTile(leading: const Icon(Icons.lock, color: Colors.orange), title: const Text('Đổi mật khẩu'), onTap: () { Navigator.pop(context); Navigator.push(context, MaterialPageRoute(builder: (_) => const ChangePasswordScreen())); }),
                const Divider(height: 1),
                ListTile(leading: const Icon(Icons.settings, color: Colors.grey), title: const Text('Cài đặt'), onTap: () { Navigator.pop(context); Navigator.push(context, MaterialPageRoute(builder: (_) => const SettingsScreen())); }),
              ],
            ),
          ),
          const Divider(height: 1),
          ListTile(leading: const Icon(Icons.logout, color: Colors.red), title: const Text('Đăng xuất'), onTap: () { Navigator.pop(context); _handleLogout(); }),
          const SizedBox(height: 10),
        ],
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    final currencyFormat = NumberFormat('#,###', 'vi_VN');

    return Scaffold(
      backgroundColor: const Color(0xFFF5F7FA),
      appBar: AppBar(
        title: const Text('Trang chủ'),
        centerTitle: true,
        actions: [
          Padding(
            padding: const EdgeInsets.only(right: 8.0),
            child: Stack(
              clipBehavior: Clip.none,
              children: [
                IconButton(
                  icon: const Icon(Icons.notifications_outlined),
                  onPressed: () async {
                    if (_currentUserId == null) return;
                    await Navigator.push(context, MaterialPageRoute(builder: (_) => NotificationScreen(userId: _currentUserId!)));
                    _fetchUnreadCount();
                  },
                ),
                if (_unreadNotifCount > 0)
                  Positioned(
                    right: 4,
                    top: 6,
                    child: Container(
                      padding: const EdgeInsets.all(4),
                      decoration: const BoxDecoration(color: Colors.red, shape: BoxShape.circle),
                      constraints: const BoxConstraints(minWidth: 16, minHeight: 16),
                      child: Text('$_unreadNotifCount', style: const TextStyle(color: Colors.white, fontSize: 10, fontWeight: FontWeight.bold), textAlign: TextAlign.center),
                    ),
                  ),
              ],
            ),
          ),
          Padding(
              padding: const EdgeInsets.only(right: 16, left: 8),
              child: Center(child: Tooltip(message: "Server: $_health", child: Icon(Icons.circle, size: 10, color: _health == 'Online' ? Colors.greenAccent : Colors.red)))),
        ],
      ),
      drawer: _buildDrawer(context),
      floatingActionButton: FloatingActionButton.extended(
        onPressed: () => _showOnlineCSKHList(context),
        backgroundColor: Colors.blue,
        foregroundColor: Colors.white,
        icon: const Icon(Icons.support_agent),
        label: const Text("Hỗ trợ"),
      ),
      body: RefreshIndicator(
        onRefresh: () async {
          await _loadDoctors();
          await _fetchUnreadCount();
          await _fetchWallet();
          await ChatService().fetchOnlineCSKH();
        },
        child: SingleChildScrollView(
          physics: const AlwaysScrollableScrollPhysics(),
          padding: const EdgeInsets.all(16),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Container(
                height: 160,
                width: double.infinity,
                decoration: BoxDecoration(
                  borderRadius: BorderRadius.circular(16),
                  image: const DecorationImage(image: AssetImage('assets/slide1.png'), fit: BoxFit.cover),
                  boxShadow: [BoxShadow(color: Colors.black26, blurRadius: 8, offset: Offset(0, 4))],
                ),
                child: Container(
                  decoration: BoxDecoration(
                    borderRadius: BorderRadius.circular(16),
                    gradient: LinearGradient(colors: [Colors.transparent, Colors.black.withOpacity(0.7)], begin: Alignment.topCenter, end: Alignment.bottomCenter),
                  ),
                  padding: const EdgeInsets.all(16),
                  alignment: Alignment.bottomLeft,
                  child: const Text("Chăm sóc sức khỏe\ntoàn diện", style: TextStyle(color: Colors.white, fontWeight: FontWeight.bold, fontSize: 20, height: 1.2)),
                ),
              ),
              const SizedBox(height: 24),
              Container(
                height: 52,
                decoration: BoxDecoration(
                  color: Colors.white,
                  borderRadius: BorderRadius.circular(30),
                  boxShadow: [BoxShadow(color: Colors.black.withOpacity(0.05), blurRadius: 10, offset: Offset(0, 4))],
                ),
                child: Row(
                  children: [
                    const SizedBox(width: 16),
                    const Icon(Icons.search, color: Colors.blue),
                    const SizedBox(width: 12),
                    Expanded(
                      child: GestureDetector(
                        onTap: () {
                          Navigator.push(context, MaterialPageRoute(builder: (_) => const DoctorSearchScreen()));
                        },
                        child: const Text("Tìm bác sĩ, chuyên khoa...", style: TextStyle(color: Colors.black45, fontSize: 15)),
                      ),
                    ),
                    Container(height: 30, width: 1, color: Colors.grey[200]),
                    IconButton(
                      icon: const Icon(Icons.mic, color: Colors.blue),
                      onPressed: () {
                        Navigator.push(context, MaterialPageRoute(builder: (_) => const DoctorSearchScreen(autoListen: true)));
                      },
                    ),
                    const SizedBox(width: 5),
                  ],
                ),
              ),
              const SizedBox(height: 24),
              Row(mainAxisAlignment: MainAxisAlignment.spaceBetween, children: [
                const Text("Chuyên khoa", style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold)),
                TextButton(onPressed: () { Navigator.push(context, MaterialPageRoute(builder: (_) => const AllSpecialtiesScreen())); }, child: const Text("Xem tất cả"))
              ]),
              const SizedBox(height: 10),
              GridView.builder(
                shrinkWrap: true,
                physics: const NeverScrollableScrollPhysics(),
                gridDelegate: const SliverGridDelegateWithFixedCrossAxisCount(crossAxisCount: 4, childAspectRatio: 0.8, crossAxisSpacing: 12, mainAxisSpacing: 12),
                itemCount: _departments.take(8).length,
                itemBuilder: (context, index) {
                  final dept = _departments[index];
                  return GestureDetector(
                    onTap: () { Navigator.push(context, MaterialPageRoute(builder: (_) => DoctorSearchScreen(initialQuery: dept['name']))); },
                    child: Column(
                      children: [
                        Container(
                          height: 56, width: 56,
                          decoration: BoxDecoration(color: (dept['color'] as Color).withOpacity(0.1), shape: BoxShape.circle),
                          child: Icon(dept['icon'], color: dept['color'], size: 26),
                        ),
                        const SizedBox(height: 8),
                        Text(dept['name'], style: const TextStyle(fontSize: 12, color: Colors.black87, fontWeight: FontWeight.w500), textAlign: TextAlign.center, maxLines: 1, overflow: TextOverflow.ellipsis)
                      ],
                    ),
                  );
                },
              ),
              const SizedBox(height: 24),
              Row(mainAxisAlignment: MainAxisAlignment.spaceBetween, children: [
                const Text('Bác sĩ nổi bật', style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold)),
                TextButton(onPressed: () => Navigator.push(context, MaterialPageRoute(builder: (_) => const DoctorSearchScreen())), child: const Text("Xem thêm"))
              ]),
              const SizedBox(height: 12),
              if (_loadingDocs) const SizedBox(height: 200, child: Center(child: CircularProgressIndicator()))
              else if (_allDoctors.isEmpty) const SizedBox(height: 100, child: Center(child: Text("Không tìm thấy bác sĩ.", style: TextStyle(color: Colors.grey))))
              else GridView.builder(
                  shrinkWrap: true, physics: const NeverScrollableScrollPhysics(),
                  gridDelegate: const SliverGridDelegateWithFixedCrossAxisCount(crossAxisCount: 2, childAspectRatio: 0.7, crossAxisSpacing: 12, mainAxisSpacing: 12),
                  itemCount: _allDoctors.take(6).length,
                  itemBuilder: (ctx, i) => DoctorCard(doctor: _allDoctors[i], onTap: () => _bookDoctor(_allDoctors[i])),
                ),
              const SizedBox(height: 30),
            ],
          ),
        ),
      ),
    );
  }
}
