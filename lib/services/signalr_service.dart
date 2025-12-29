import 'dart:async';
import 'package:flutter/foundation.dart';
import 'package:signalr_core/signalr_core.dart';
import 'auth_service.dart';

class SignalRService {
  static final SignalRService _instance = SignalRService._internal();
  factory SignalRService() => _instance;
  SignalRService._internal();

  HubConnection? _hubConnection;

  final String _serverUrl = "http://172.20.10.6:5062/bookingHub";

  final _dataUpdatedController = StreamController<void>.broadcast();
  Stream<void> get onDataUpdated => _dataUpdatedController.stream;

  Future<void> initialize() async {
    final userInfo = await AuthService().getUserInfo();
    if (userInfo == null) {
      debugPrint("⚠️ SignalR: Chưa đăng nhập, không thể kết nối.");
      return;
    }

    final userId = userInfo['userId'];
    if (_hubConnection != null && _hubConnection!.state == HubConnectionState.connected) {
      return;
    }

    final connectionUrl = "$_serverUrl?userId=$userId";

    debugPrint("🚀 SignalR: Đang khởi tạo tới $connectionUrl");

    _hubConnection = HubConnectionBuilder()
        .withUrl(connectionUrl, HttpConnectionOptions(
      logging: (level, message) => debugPrint('SignalR Log: $message'),
      client: null,
    ))
        .withAutomaticReconnect()
        .build();
    _registerHubEvents();

    try {
      await _hubConnection!.start();
      debugPrint("✅ SignalR: Đã kết nối thành công! (User ID: $userId)");
    } catch (e) {
      debugPrint("❌ SignalR Lỗi kết nối: $e");
      Future.delayed(const Duration(seconds: 5), () => initialize());
    }

    _hubConnection!.onclose((error) {
      debugPrint("⚠️ SignalR: Kết nối bị ngắt. Lỗi: $error");
    });
  }

  void _registerHubEvents() {
    if (_hubConnection == null) return;

    _hubConnection!.on("ReceiveNotification", (arguments) {
      debugPrint("🔔 SignalR: Nhận sự kiện 'ReceiveNotification'");
      _notifyUI();
    });

    _hubConnection!.on("ReceiveAppointmentUpdate", (arguments) {
      debugPrint("⚡ SignalR: Nhận sự kiện 'ReceiveAppointmentUpdate'");
      _notifyUI();
    });

    _hubConnection!.on("ReceiveStatusChange", (arguments) {
      debugPrint("⚡ SignalR: Nhận sự kiện 'ReceiveStatusChange' -> ${arguments.toString()}");
      _notifyUI();
    });

    _hubConnection!.on("ReceiveNewBooking", (arguments) {
    });
  }

  void _notifyUI() {
    _dataUpdatedController.add(null);
  }

  void stop() {
    try {
      _hubConnection?.stop();
      _hubConnection = null;
      debugPrint("🛑 SignalR: Đã ngắt kết nối.");
    } catch (e) {
      debugPrint("Lỗi khi stop SignalR: $e");
    }
  }
}