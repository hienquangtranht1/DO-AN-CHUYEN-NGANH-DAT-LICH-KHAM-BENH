class ChatMessage {
  final int id;
  final String message;
  final bool isMe;
  final String time;
  final DateTime? createdAt;

  ChatMessage({
    this.id = 0,
    required this.message,
    required this.isMe,
    required this.time,
    this.createdAt,
  });

  factory ChatMessage.fromJson(Map<String, dynamic> json) {
    return ChatMessage(
      id: json['id'] ?? 0,
      message: json['message'] ?? '',
      isMe: json['isMe'] ?? false,
      time: json['time'] ?? '',
      createdAt: json['createdAt'] != null
          ? DateTime.tryParse(json['createdAt'])
          : null,
    );
  }
}