import 'dart:async';
import 'dart:convert';
import 'package:flutter/foundation.dart';
import 'package:signalr_core/signalr_core.dart';
import 'api_service.dart';
import 'auth_service.dart';
import '../models/chat_message.dart';
import '../models/cskh_model.dart';

class ChatService {
  static final ChatService _instance = ChatService._internal();
  factory ChatService() => _instance;
  ChatService._internal();

  HubConnection? _hubConnection;
  final ApiService _api = ApiService();
  final String _chatHubUrl = "${ApiService.base}/chatHub";

  final _messageController = StreamController<ChatMessage>.broadcast();
  Stream<ChatMessage> get onMessageReceived => _messageController.stream;

  final _statusController = StreamController<String>.broadcast();
  Stream<String> get onStatusChanged => _statusController.stream;

  final _onlineListController = StreamController<List<CskhModel>>.broadcast();
  Stream<List<CskhModel>> get onOnlineListUpdated => _onlineListController.stream;

  // --- 1. KẾT NỐI SIGNALR ---
  Future<void> initialize() async {
    final userId = await AuthService().getCurrentUserId();
    if (userId == null) return;

    // Nếu đang kết nối thì thôi, hoặc reconnect nếu cần
    if (_hubConnection?.state == HubConnectionState.connected) {
      // Fetch lại list cho chắc
      fetchOnlineCSKH();
      return;
    }

    // 👇 QUAN TRỌNG: Nối userId vào URL để Server nhận diện
    final String connectionUrl = "$_chatHubUrl?userId=$userId";

    _hubConnection = HubConnectionBuilder()
        .withUrl(connectionUrl, HttpConnectionOptions(
      logging: (level, message) {
        if (kDebugMode) print('ChatHub Log: $message');
      },
      // KHÔNG DÙNG HEADERS Ở ĐÂY NỮA
    ))
        .withAutomaticReconnect()
        .build();

    // --- ĐĂNG KÝ SỰ KIỆN ---

    // A. Nhận tin nhắn
    _hubConnection!.on("ReceiveMessage", (arguments) {
      if (arguments != null && arguments.length >= 2) {
        String senderInfo = arguments[0].toString();
        String content = arguments[1].toString();
        bool isFromCSKH = senderInfo.startsWith("CSKH");

        final msg = ChatMessage(
          message: content,
          isMe: !isFromCSKH,
          time: "${DateTime.now().hour}:${DateTime.now().minute.toString().padLeft(2, '0')}",
          createdAt: DateTime.now(),
        );
        _messageController.add(msg);
      }
    });

    // B. Nhận tín hiệu Chấp nhận từ CSKH
    _hubConnection!.on("ChatAccepted", (_) {
      debugPrint("🚀 Server báo: ChatAccepted -> Chuyển màn hình!");
      _statusController.add("Accepted");
    });

    _hubConnection!.on("ConnectionDenied", (_) => _statusController.add("Denied"));

    // C. Danh sách CSKH thay đổi
    _hubConnection!.on("OnlineListChanged", (_) {
      fetchOnlineCSKH();
    });

    // --- BẮT ĐẦU KẾT NỐI ---
    try {
      await _hubConnection!.start();
      debugPrint("✅ ChatHub Connected với ID: KH_$userId");
      fetchOnlineCSKH();
    } catch (e) {
      debugPrint("❌ ChatHub Error: $e");
      _statusController.add("Error");
    }
  }

  // ... (Các hàm API sendMessageAPI, getHistoryAPI giữ nguyên như cũ) ...

  Future<void> requestChat(int cskhId) async {
    if (_hubConnection?.state == HubConnectionState.connected) {
      await _hubConnection!.invoke("RequestChat", args: [cskhId]);
    }
  }

  Future<void> cancelRequest(int cskhId) async {
    if (_hubConnection?.state == HubConnectionState.connected) {
      await _hubConnection!.invoke("CancelRequest", args: [cskhId]);
    }
  }

  Future<void> fetchOnlineCSKH() async {
    try {
      final res = await _api.get('/Chat/GetListCSKHJson');
      if (res['status'] == 200) {
        final List list = jsonDecode(res['body']);
        final models = list.map((e) => CskhModel.fromJson(e)).toList();
        _onlineListController.add(models);
      }
    } catch (e) {
      debugPrint("Lỗi tải CSKH Online: $e");
    }
  }

  // API Gửi tin (HTTP)
  Future<bool> sendMessageAPI(String message, int receiverId) async {
    final myId = await AuthService().getCurrentUserId();
    if (myId == null) return false;
    final res = await _api.postJson('/Chat/SendMessage', {
      "SenderId": myId,
      "ReceiverId": receiverId,
      "Message": message
    });
    return res['status'] == 200;
  }

  // API Lịch sử (HTTP)
  Future<List<ChatMessage>> getHistoryAPI(int receiverId) async {
    final myId = await AuthService().getCurrentUserId();
    if (myId == null) return [];
    final res = await _api.get('/Chat/GetHistory?receiverId=$receiverId&mobileUserId=$myId');
    if (res['status'] == 200) {
      try {
        final List list = jsonDecode(res['body']);
        return list.map((e) => ChatMessage.fromJson(e)).toList();
      } catch (_) {}
    }
    return [];
  }

  void stop() {
    // _hubConnection?.stop();
  }
}