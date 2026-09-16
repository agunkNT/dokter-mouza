import 'dart:io';
import 'package:http/http.dart' as http;
import 'package:image_picker/image_picker.dart';

/// Service untuk komunikasi dengan Web API ASP.NET.
/// Digunakan untuk upload media (foto/video) dari Flutter ke server.
class ApiService {
  static final ApiService _instance = ApiService._internal();
  factory ApiService() => _instance;
  ApiService._internal();

  /// ─── Konfigurasi ────────────────────────────────────────────────
  /// Ganti URL ini sesuai alamat server production.
  static const String baseUrl = 'http://103.151.140.138:5000';
  static const String _apiKey = 'DokterAbdurrahman-MediaKey-2026!';

  static Map<String, String> get _headers => {'X-Api-Key': _apiKey};

  // ─────────────────────────────────────────────────────────────────
  // LAPORAN OPERASI MEDIA
  // ─────────────────────────────────────────────────────────────────

  /// Upload foto dan/atau video untuk LaporanOperasi.
  /// [id] : ID LaporanOperasi
  /// [fotos] : daftar XFile foto dari image_picker
  /// [videos] : daftar XFile video dari image_picker
  Future<({int uploaded, List<String> errors})> uploadLaporanOperasiMedia({
    required int id,
    List<XFile> fotos = const [],
    List<XFile> videos = const [],
  }) async {
    return _uploadMedia(
      url: '$baseUrl/api/media/laporanoperasi/$id',
      fotos: fotos,
      videos: videos,
    );
  }

  // ─────────────────────────────────────────────────────────────────
  // PEMERIKSAAN MEDIA
  // ─────────────────────────────────────────────────────────────────

  /// Upload foto dan/atau video untuk Pemeriksaan.
  /// [id] : ID Pemeriksaan
  Future<({int uploaded, List<String> errors})> uploadPemeriksaanMedia({
    required int id,
    List<XFile> fotos = const [],
    List<XFile> videos = const [],
  }) async {
    return _uploadMedia(
      url: '$baseUrl/api/media/pemeriksaan/$id',
      fotos: fotos,
      videos: videos,
    );
  }

  // ─────────────────────────────────────────────────────────────────
  // Helper
  // ─────────────────────────────────────────────────────────────────

  Future<({int uploaded, List<String> errors})> _uploadMedia({
    required String url,
    required List<XFile> fotos,
    required List<XFile> videos,
  }) async {
    try {
      final request = http.MultipartRequest('POST', Uri.parse(url));
      request.headers.addAll(_headers);

      for (final foto in fotos) {
        request.files.add(await http.MultipartFile.fromPath(
          'fotos',
          foto.path,
          filename: foto.name,
        ));
      }

      for (final video in videos) {
        request.files.add(await http.MultipartFile.fromPath(
          'videos',
          video.path,
          filename: video.name,
        ));
      }

      final response = await request.send().timeout(const Duration(minutes: 5));
      final body = await response.stream.bytesToString();

      if (response.statusCode == 200) {
        // Parse JSON response
        final uploaded = _parseIntFromJson(body, 'uploaded');
        final errors = _parseStringListFromJson(body, 'errors');
        return (uploaded: uploaded, errors: errors);
      } else {
        return (uploaded: 0, errors: ['Server error: HTTP ${response.statusCode}']);
      }
    } on SocketException {
      return (uploaded: 0, errors: ['Tidak dapat terhubung ke server. Periksa koneksi jaringan.']);
    } catch (e) {
      return (uploaded: 0, errors: ['Error: $e']);
    }
  }

  /// URL untuk menampilkan media LaporanOperasi via Image.network()
  static String getLaporanOperasiMediaUrl(int mediaId) =>
      '$baseUrl/LaporanOperasi/Media/$mediaId';

  /// URL untuk menampilkan media Pemeriksaan via Image.network()
  static String getPemeriksaanMediaUrl(int mediaId) =>
      '$baseUrl/Pemeriksaan/Media/$mediaId';

  // ─────────────────────────────────────────────────────────────────
  // JSON parsing helpers (tanpa dependency json package)
  // ─────────────────────────────────────────────────────────────────
  int _parseIntFromJson(String json, String key) {
    final pattern = RegExp('"$key"\\s*:\\s*(\\d+)');
    final match = pattern.firstMatch(json);
    return int.tryParse(match?.group(1) ?? '0') ?? 0;
  }

  List<String> _parseStringListFromJson(String json, String key) {
    final pattern = RegExp('"$key"\\s*:\\s*\\[([^\\]]*)\\]');
    final match = pattern.firstMatch(json);
    if (match == null || match.group(1)!.trim().isEmpty) return [];
    return match
        .group(1)!
        .split(',')
        .map((s) => s.trim().replaceAll('"', ''))
        .where((s) => s.isNotEmpty)
        .toList();
  }
}
