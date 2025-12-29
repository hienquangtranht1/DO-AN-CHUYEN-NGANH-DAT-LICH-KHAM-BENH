import 'dart:async';
import 'package:flutter/material.dart';
import '../services/chat_service.dart';
import '../models/chat_message.dart';

class ChatScreen extends StatefulWidget {
  final int targetCskhId;
  final String targetName;

  const ChatScreen({
    super.key,
    required this.targetCskhId,
    required this.targetName,
  });

  @override
  State<ChatScreen> createState() => _ChatScreenState();
}

class _ChatScreenState extends State<ChatScreen> {
  final TextEditingController _msgController = TextEditingController();
  final ScrollController _scrollController = ScrollController();
  final ChatService _chatService = ChatService();

  // Trạng thái: 'init', 'waiting', 'chatting', 'ended'
  String _status = 'init';
  List<ChatMessage> _messages = [];

  StreamSubscription? _msgSub;
  StreamSubscription? _statusSub;

  @override
  void initState() {
    super.initState();
    _startSession();
  }

  @override
  void dispose() {
    _msgController.dispose();
    _scrollController.dispose();
    _msgSub?.cancel();
    _statusSub?.cancel();
    // Nếu đang chờ mà thoát -> Hủy yêu cầu
    if (_status == 'waiting') {
      _chatService.cancelRequest(widget.targetCskhId);
    }
    super.dispose();
  }

  Future<void> _startSession() async {
    // 1. Lắng nghe tin nhắn mới
    _msgSub = _chatService.onMessageReceived.listen((msg) {
      if (!mounted) return;
      setState(() {
        _messages.add(msg);
      });
      _scrollToBottom();
    });

    // 2. Lắng nghe trạng thái
    _statusSub = _chatService.onStatusChanged.listen((status) {
      if (!mounted) return;
      if (status == 'Accepted') {
        setState(() => _status = 'chatting');
        _loadHistory();
      } else if (status == 'Denied') {
        setState(() => _status = 'ended');
        ScaffoldMessenger.of(context).showSnackBar(
            const SnackBar(content: Text("Kết nối bị từ chối hoặc mất mạng."))
        );
      }
    });

    // 3. Gửi yêu cầu kết nối
    await _chatService.initialize();
    setState(() => _status = 'waiting');
    _chatService.requestChat(widget.targetCskhId);
  }

  Future<void> _loadHistory() async {
    final history = await _chatService.getHistoryAPI(widget.targetCskhId);
    if (mounted) {
      setState(() {
        _messages = history;
      });
      _scrollToBottom();
    }
  }

  Future<void> _sendMessage() async {
    final text = _msgController.text.trim();
    if (text.isEmpty || _status != 'chatting') return;

    _msgController.clear();

    // Hiện tin nhắn tạm thời (Optimistic UI)
    final tempMsg = ChatMessage(
      message: text,
      isMe: true,
      time: "${DateTime.now().hour}:${DateTime.now().minute.toString().padLeft(2, '0')}",
    );
    setState(() => _messages.add(tempMsg));
    _scrollToBottom();

    // Gọi API gửi
    final success = await _chatService.sendMessageAPI(text, widget.targetCskhId);
    if (!success) {
      ScaffoldMessenger.of(context).showSnackBar(const SnackBar(content: Text("Gửi lỗi!")));
    }
  }

  void _scrollToBottom() {
    Future.delayed(const Duration(milliseconds: 100), () {
      if (_scrollController.hasClients) {
        _scrollController.animateTo(
          _scrollController.position.maxScrollExtent,
          duration: const Duration(milliseconds: 300),
          curve: Curves.easeOut,
        );
      }
    });
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(widget.targetName, style: const TextStyle(fontSize: 16)),
            if (_status == 'chatting')
              const Text("● Đang chat", style: TextStyle(fontSize: 12, color: Colors.white70))
            else if (_status == 'waiting')
              const Text("Đang kết nối...", style: TextStyle(fontSize: 12, color: Colors.white70))
          ],
        ),
        backgroundColor: Colors.blue,
        foregroundColor: Colors.white,
        actions: [
          if (_status == 'chatting' || _status == 'waiting')
            IconButton(
              icon: const Icon(Icons.close),
              onPressed: () {
                _chatService.cancelRequest(widget.targetCskhId);
                Navigator.pop(context);
              },
            )
        ],
      ),
      body: Column(
        children: [
          Expanded(child: _buildBody()),
          if (_status == 'chatting') _buildInputArea(),
        ],
      ),
    );
  }

  Widget _buildBody() {
    if (_status == 'init' || _status == 'waiting') {
      return Center(
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            const CircularProgressIndicator(),
            const SizedBox(height: 20),
            Text("Đang kết nối tới ${widget.targetName}...", style: const TextStyle(color: Colors.grey)),
            const SizedBox(height: 30),
            OutlinedButton(
              onPressed: () {
                _chatService.cancelRequest(widget.targetCskhId);
                Navigator.pop(context);
              },
              child: const Text("Hủy"),
            )
          ],
        ),
      );
    }

    if (_status == 'ended') {
      return Center(
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            const Icon(Icons.person_off, size: 60, color: Colors.grey),
            const SizedBox(height: 10),
            const Text("Cuộc trò chuyện đã kết thúc"),
            const SizedBox(height: 20),
            ElevatedButton(onPressed: () => Navigator.pop(context), child: const Text("Quay lại"))
          ],
        ),
      );
    }

    return ListView.builder(
      controller: _scrollController,
      padding: const EdgeInsets.all(16),
      itemCount: _messages.length,
      itemBuilder: (ctx, i) {
        final msg = _messages[i];
        return Align(
          alignment: msg.isMe ? Alignment.centerRight : Alignment.centerLeft,
          child: Container(
            margin: const EdgeInsets.only(bottom: 12),
            padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 10),
            decoration: BoxDecoration(
              color: msg.isMe ? Colors.blue : Colors.grey[200],
              borderRadius: BorderRadius.circular(16).copyWith(
                bottomRight: msg.isMe ? Radius.zero : null,
                bottomLeft: !msg.isMe ? Radius.zero : null,
              ),
            ),
            constraints: BoxConstraints(maxWidth: MediaQuery.of(context).size.width * 0.75),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(msg.message, style: TextStyle(color: msg.isMe ? Colors.white : Colors.black87, fontSize: 15)),
                const SizedBox(height: 4),
                Text(msg.time, style: TextStyle(color: msg.isMe ? Colors.white70 : Colors.black54, fontSize: 10)),
              ],
            ),
          ),
        );
      },
    );
  }

  Widget _buildInputArea() {
    return Container(
      padding: const EdgeInsets.all(8),
      color: Colors.white,
      child: Row(
        children: [
          Expanded(
            child: TextField(
              controller: _msgController,
              decoration: InputDecoration(
                hintText: "Nhập tin nhắn...",
                contentPadding: const EdgeInsets.symmetric(horizontal: 16),
                border: OutlineInputBorder(borderRadius: BorderRadius.circular(25)),
                filled: true, fillColor: Colors.grey[100],
              ),
              onSubmitted: (_) => _sendMessage(),
            ),
          ),
          const SizedBox(width: 8),
          CircleAvatar(
            backgroundColor: Colors.blue,
            child: IconButton(icon: const Icon(Icons.send, color: Colors.white, size: 20), onPressed: _sendMessage),
          ),
        ],
      ),
    );
  }
}