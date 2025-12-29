import 'dart:async';
import 'package:flutter/material.dart';
import 'package:speech_to_text/speech_to_text.dart' as stt;
import 'package:permission_handler/permission_handler.dart';
import '../services/doctor_service.dart';
import '../models/doctor.dart';
import '../services/api_service.dart';
import 'booking_screen.dart';

class DoctorSearchScreen extends StatefulWidget {
  final bool autoListen;
  final String? initialQuery;

  const DoctorSearchScreen({
    super.key,
    this.autoListen = false,
    this.initialQuery,
  });

  @override
  State<DoctorSearchScreen> createState() => _DoctorSearchScreenState();
}

class _DoctorSearchScreenState extends State<DoctorSearchScreen> {
  final DoctorService _doctorService = DoctorService();
  final TextEditingController _searchController = TextEditingController();


  late stt.SpeechToText _speech;
  bool _isListening = false;
  String _textPlaceholder = "Nhập tên bác sĩ, khoa...";


  List<Doctor> _doctors = [];
  bool _isLoading = false;
  Timer? _debounce;

  @override
  void initState() {
    super.initState();
    _speech = stt.SpeechToText();


    if (widget.autoListen) {

      WidgetsBinding.instance.addPostFrameCallback((_) {
        _listen();
      });
    } else if (widget.initialQuery != null && widget.initialQuery!.isNotEmpty) {

      _searchController.text = widget.initialQuery!;
      _searchDoctors(widget.initialQuery!);
    } else {

      _searchDoctors("");
    }
  }

  @override
  void dispose() {
    _searchController.dispose();
    _debounce?.cancel();
    super.dispose();
  }


  void _searchDoctors(String query) async {
    if (!mounted) return;
    setState(() => _isLoading = true);

    try {

      final res = await _doctorService.getDoctors(page: 1, search: query);

      if (mounted) {
        setState(() {
          _doctors = res['doctors'];
          _isLoading = false;
        });
      }
    } catch (e) {
      print("Lỗi tìm kiếm: $e");
      if (mounted) setState(() => _isLoading = false);
    }
  }


  void _onSearchChanged(String query) {
    if (_debounce?.isActive ?? false) _debounce!.cancel();
    _debounce = Timer(const Duration(milliseconds: 500), () {
      _searchDoctors(query);
    });
  }


  Future<void> _listen() async {

    var status = await Permission.microphone.status;
    if (status.isDenied) {
      status = await Permission.microphone.request();
    }


    if (status.isPermanentlyDenied) {
      if (mounted) {
        showDialog(
          context: context,
          builder: (ctx) => AlertDialog(
            title: const Text("Cần quyền Micro"),
            content: const Text("Vui lòng mở Cài đặt để cấp quyền Micro cho ứng dụng."),
            actions: [
              TextButton(onPressed: () => Navigator.pop(ctx), child: const Text("Hủy")),
              TextButton(onPressed: () { Navigator.pop(ctx); openAppSettings(); }, child: const Text("Mở Cài đặt")),
            ],
          ),
        );
      }
      return;
    }

    if (!status.isGranted) return;


    if (!_isListening) {
      bool available = await _speech.initialize(
        onStatus: (val) {

          if (val == 'done' || val == 'notListening') {
            if (mounted) {
              setState(() {
                _isListening = false;
                _textPlaceholder = "Nhập tên bác sĩ, khoa...";
              });

              if (_searchController.text.isNotEmpty) {
                _searchDoctors(_searchController.text);
              }
            }
          }
        },
        onError: (val) => print('Voice Error: $val'),
      );

      if (available) {
        setState(() {
          _isListening = true;
          _textPlaceholder = "Đang nghe bạn nói...";
          _searchController.clear();

        });

        _speech.listen(
          onResult: (val) {
            setState(() {

              _searchController.text = val.recognizedWords;


              _searchController.selection = TextSelection.fromPosition(
                  TextPosition(offset: _searchController.text.length));


              _onSearchChanged(val.recognizedWords);
            });
          },
          localeId: 'vi_VN',

          partialResults: true,
          listenFor: const Duration(seconds: 30),
          pauseFor: const Duration(seconds: 3),
          cancelOnError: true,
        );
      }
    } else {
      setState(() => _isListening = false);
      _speech.stop();
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: Colors.grey[50],
      appBar: AppBar(
        title: Text(
          widget.initialQuery != null ? "Khoa: ${widget.initialQuery}" : "Tìm kiếm Bác sĩ",
          style: const TextStyle(color: Colors.black, fontWeight: FontWeight.bold),
        ),
        backgroundColor: Colors.white,
        elevation: 0,
        iconTheme: const IconThemeData(color: Colors.black),
      ),
      body: Column(
        children: [

          Container(
            padding: const EdgeInsets.all(16),
            color: Colors.white,
            child: TextField(
              controller: _searchController,
              onChanged: _onSearchChanged,
              decoration: InputDecoration(
                hintText: _textPlaceholder,
                hintStyle: TextStyle(color: _isListening ? Colors.red : Colors.grey),
                prefixIcon: const Icon(Icons.search),
                suffixIcon: GestureDetector(
                  onTap: _listen,
                  child: CircleAvatar(
                    backgroundColor: _isListening ? Colors.red : Colors.grey[100],
                    child: Icon(
                        _isListening ? Icons.mic : Icons.mic_none,
                        color: _isListening ? Colors.white : Colors.black54
                    ),
                  ),
                ),
                filled: true,
                fillColor: Colors.grey[100],
                border: OutlineInputBorder(borderRadius: BorderRadius.circular(12), borderSide: BorderSide.none),
                contentPadding: const EdgeInsets.symmetric(vertical: 14, horizontal: 20),
              ),
            ),
          ),


          Expanded(
            child: _isLoading
                ? const Center(child: CircularProgressIndicator())
                : _doctors.isEmpty
                ? Center(
              child: Column(
                mainAxisAlignment: MainAxisAlignment.center,
                children: [
                  Icon(Icons.search_off, size: 60, color: Colors.grey[300]),
                  const SizedBox(height: 10),
                  Text("Không tìm thấy bác sĩ nào.", style: TextStyle(color: Colors.grey[600])),
                ],
              ),
            )
                : ListView.builder(
              padding: const EdgeInsets.all(16),
              itemCount: _doctors.length,
              itemBuilder: (ctx, i) {
                final doc = _doctors[i];

                final imgUrl = doc.imageUrl != null
                    ? (ApiService.base + doc.imageUrl!)
                    : null;

                return Container(
                  margin: const EdgeInsets.only(bottom: 12),
                  decoration: BoxDecoration(
                    color: Colors.white,
                    borderRadius: BorderRadius.circular(12),
                    boxShadow: [BoxShadow(color: Colors.black.withOpacity(0.05), blurRadius: 5, offset: const Offset(0, 2))],
                  ),
                  child: ListTile(
                    contentPadding: const EdgeInsets.all(12),
                    leading: Container(
                      width: 60, height: 60,
                      decoration: BoxDecoration(
                        color: Colors.blue.shade50,
                        borderRadius: BorderRadius.circular(8),
                        image: imgUrl != null
                            ? DecorationImage(image: NetworkImage(imgUrl), fit: BoxFit.cover)
                            : null,
                      ),
                      child: imgUrl == null ? const Icon(Icons.person, color: Colors.blue) : null,
                    ),
                    title: Text(doc.name, style: const TextStyle(fontWeight: FontWeight.bold, fontSize: 16)),
                    subtitle: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        const SizedBox(height: 4),
                        Text(doc.specialty, style: TextStyle(color: Colors.blue.shade700, fontWeight: FontWeight.w500)),
                        const SizedBox(height: 4),
                        Text("SĐT: ${doc.phone}", style: TextStyle(color: Colors.grey.shade600, fontSize: 12)),
                      ],
                    ),
                    trailing: ElevatedButton(
                      onPressed: () {
                        Navigator.push(context, MaterialPageRoute(builder: (_) => BookingScreen(preSelectedDoctor: doc)));
                      },
                      style: ElevatedButton.styleFrom(
                        backgroundColor: Colors.blue,
                        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(8)),
                        padding: const EdgeInsets.symmetric(horizontal: 12),
                      ),
                      child: const Text("Đặt lịch", style: TextStyle(color: Colors.white)),
                    ),
                  ),
                );
              },
            ),
          ),
        ],
      ),
    );
  }
}