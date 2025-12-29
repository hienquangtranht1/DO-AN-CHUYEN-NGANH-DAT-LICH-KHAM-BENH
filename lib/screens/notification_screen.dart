import 'dart:async';
import 'package:flutter/material.dart';
import 'package:intl/intl.dart';

import '../services/auth_service.dart';
import '../services/signalr_service.dart';
import '../models/notification_model.dart';
import 'notification_detail_screen.dart';

class NotificationScreen extends StatefulWidget {
  final int userId;
  const NotificationScreen({Key? key, required this.userId}) : super(key: key);

  @override
  State<NotificationScreen> createState() => _NotificationScreenState();
}

class _NotificationScreenState extends State<NotificationScreen> {
  final AuthService _auth = AuthService();

  List<NotificationModel> _notifications = [];
  bool _isLoading = true;

  int _page = 1;
  bool _isLoadMoreRunning = false;
  bool _hasNextPage = true;
  final int _pageSize = 20;
  late ScrollController _scrollController;

  StreamSubscription? _signalRSubscription;

  @override
  void initState() {
    super.initState();
    _scrollController = ScrollController()..addListener(_loadMore);
    _fetchNotifications();

    _signalRSubscription = SignalRService().onDataUpdated.listen((_) {
      print("🔔 [NotificationScreen] Có tin mới -> Tải lại danh sách...");
      _fetchNotifications();
    });
  }

  @override
  void dispose() {
    _scrollController.removeListener(_loadMore);
    _scrollController.dispose();
    _signalRSubscription?.cancel();
    super.dispose();
  }

  Future<void> _fetchNotifications() async {
    if (!mounted) return;
    setState(() {
      if (_notifications.isEmpty) _isLoading = true;
      _page = 1;
      _hasNextPage = true;
    });

    final dataList = await _auth.getNotifications(page: 1, pageSize: _pageSize);

    final List<NotificationModel> fetchedList = dataList
        .map((item) => NotificationModel.fromJson(item))
        .toList();

    if (mounted) {
      setState(() {
        _notifications = fetchedList;
        _isLoading = false;
        if (fetchedList.length < _pageSize) _hasNextPage = false;
      });
    }
  }

  Future<void> _loadMore() async {
    if (_hasNextPage && !_isLoading && !_isLoadMoreRunning &&
        _scrollController.position.extentAfter < 300) {

      setState(() => _isLoadMoreRunning = true);
      _page++;

      final dataList = await _auth.getNotifications(page: _page, pageSize: _pageSize);
      final List<NotificationModel> fetchedList = dataList
          .map((item) => NotificationModel.fromJson(item))
          .toList();

      if (mounted) {
        setState(() {
          if (fetchedList.isNotEmpty) {
            _notifications.addAll(fetchedList);
          } else {
            _hasNextPage = false;
          }
          _isLoadMoreRunning = false;
        });
      }
    }
  }

  Future<void> _onNotificationTap(NotificationModel item, int index) async {
    Navigator.push(
      context,
      MaterialPageRoute(builder: (_) => NotificationDetailScreen(notification: item)),
    );

    if (!item.daXem) {
      setState(() {
        _notifications[index] = NotificationModel(
          maThongBao: item.maThongBao,
          maNguoiDung: item.maNguoiDung,
          tieuDe: item.tieuDe,
          noiDung: item.noiDung,
          ngayTao: item.ngayTao,
          daXem: true,
          maLichHen: item.maLichHen,
        );
      });

      await _auth.markAsRead(item.maThongBao);
    }
  }

  Future<void> _markAllAsRead() async {
    await _auth.markAllRead();
    setState(() {
      _notifications = _notifications.map((item) => NotificationModel(
        maThongBao: item.maThongBao,
        maNguoiDung: item.maNguoiDung,
        tieuDe: item.tieuDe,
        noiDung: item.noiDung,
        ngayTao: item.ngayTao,
        daXem: true,
        maLichHen: item.maLichHen,
      )).toList();
    });
    ScaffoldMessenger.of(context).showSnackBar(const SnackBar(content: Text("Đã đánh dấu tất cả là đã đọc")));
  }

  Future<void> _deleteNotif(int id, int index) async {
    final deletedItem = _notifications[index];
    setState(() {
      _notifications.removeAt(index);
    });

    await _auth.deleteNotification(id);
  }

  String _formatDate(DateTime date) {
    return DateFormat('HH:mm dd/MM/yyyy').format(date);
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text("Thông báo"),
        centerTitle: true,
        backgroundColor: Colors.blue,
        foregroundColor: Colors.white,
        actions: [
          IconButton(
            icon: const Icon(Icons.done_all),
            tooltip: "Đánh dấu tất cả đã đọc",
            onPressed: _markAllAsRead,
          )
        ],
      ),
      body: _isLoading
          ? const Center(child: CircularProgressIndicator())
          : _notifications.isEmpty
          ? _buildEmptyState()
          : RefreshIndicator(
        onRefresh: _fetchNotifications,
        child: Column(
          children: [
            Expanded(
              child: ListView.builder(
                controller: _scrollController,
                itemCount: _notifications.length + (_isLoadMoreRunning ? 1 : 0),
                itemBuilder: (context, index) {
                  if (index == _notifications.length) {
                    return const Padding(
                      padding: EdgeInsets.all(10.0),
                      child: Center(child: CircularProgressIndicator()),
                    );
                  }

                  final item = _notifications[index];
                  final bool isRead = item.daXem;

                  return Dismissible(
                    key: ValueKey(item.maThongBao),
                    direction: DismissDirection.endToStart,
                    background: Container(
                      color: Colors.red,
                      alignment: Alignment.centerRight,
                      padding: const EdgeInsets.only(right: 20),
                      child: const Icon(Icons.delete, color: Colors.white),
                    ),
                    onDismissed: (_) => _deleteNotif(item.maThongBao, index),
                    child: Card(
                      elevation: isRead ? 0 : 2,
                      color: isRead ? Colors.white : Colors.blue.shade50,
                      margin: const EdgeInsets.symmetric(horizontal: 10, vertical: 5),
                      child: ListTile(
                        leading: CircleAvatar(
                          backgroundColor: isRead ? Colors.grey.shade300 : Colors.blueAccent,
                          child: Icon(
                            isRead ? Icons.notifications_none : Icons.notifications_active,
                            color: isRead ? Colors.grey : Colors.white,
                          ),
                        ),
                        title: Text(
                          item.tieuDe,
                          style: TextStyle(
                            fontWeight: isRead ? FontWeight.normal : FontWeight.bold,
                            color: isRead ? Colors.black87 : Colors.blue[900],
                          ),
                        ),
                        subtitle: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            const SizedBox(height: 5),
                            Text(
                              item.noiDung,
                              maxLines: 2,
                              overflow: TextOverflow.ellipsis,
                            ),
                            const SizedBox(height: 5),
                            Text(
                              _formatDate(item.ngayTao),
                              style: TextStyle(fontSize: 12, color: Colors.grey[600]),
                            ),
                          ],
                        ),
                        onTap: () => _onNotificationTap(item, index),
                      ),
                    ),
                  );
                },
              ),
            ),
            if (!_hasNextPage && _notifications.isNotEmpty)
              Container(
                padding: const EdgeInsets.symmetric(vertical: 10),
                width: double.infinity,
                color: Colors.grey.shade100,
                child: const Text("Đã hiển thị hết thông báo", textAlign: TextAlign.center, style: TextStyle(color: Colors.grey)),
              ),
          ],
        ),
      ),
    );
  }

  Widget _buildEmptyState() {
    return Center(
      child: Column(
        mainAxisAlignment: MainAxisAlignment.center,
        children: [
          Icon(Icons.notifications_off_outlined, size: 80, color: Colors.grey.shade300),
          const SizedBox(height: 16),
          Text("Bạn chưa có thông báo nào", style: TextStyle(color: Colors.grey.shade600, fontSize: 16)),
          const SizedBox(height: 20),
          ElevatedButton.icon(
            onPressed: _fetchNotifications,
            icon: const Icon(Icons.refresh),
            label: const Text("Tải lại"),
          )
        ],
      ),
    );
  }
}