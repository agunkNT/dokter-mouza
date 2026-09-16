import 'package:flutter/material.dart';
import 'package:intl/intl.dart';
import '../../models/pemeriksaan.dart';
import '../../services/database_service.dart';
import '../../services/api_service.dart';

class PemeriksaanDetailScreen extends StatefulWidget {
  final Pemeriksaan item;
  const PemeriksaanDetailScreen({super.key, required this.item});

  @override
  State<PemeriksaanDetailScreen> createState() => _PemeriksaanDetailScreenState();
}

class _PemeriksaanDetailScreenState extends State<PemeriksaanDetailScreen> {
  final DatabaseService _db = DatabaseService();
  List<PemeriksaanMedia> _medias = [];
  bool _loadingMedia = true;

  @override
  void initState() {
    super.initState();
    _loadMedia();
  }

  Future<void> _loadMedia() async {
    try {
      final medias = await _db.getPemeriksaanMedias(widget.item.id);
      if (mounted) setState(() { _medias = medias; _loadingMedia = false; });
    } catch (_) {
      if (mounted) setState(() => _loadingMedia = false);
    }
  }

  void _showFullscreen(int mediaId, bool isFoto) {
    final url = ApiService.getPemeriksaanMediaUrl(mediaId);
    showDialog(
      context: context,
      builder: (_) => Dialog(
        backgroundColor: Colors.black,
        insetPadding: EdgeInsets.zero,
        child: Stack(
          children: [
            Center(
              child: isFoto
                  ? InteractiveViewer(
                      child: Image.network(
                        url,
                        fit: BoxFit.contain,
                        loadingBuilder: (_, child, progress) => progress == null
                            ? child
                            : const Center(child: CircularProgressIndicator(color: Colors.white)),
                        errorBuilder: (context, error, stack) => const Icon(Icons.broken_image, color: Colors.white, size: 64),
                      ),
                    )
                  : Column(
                      mainAxisAlignment: MainAxisAlignment.center,
                      children: [
                        const Icon(Icons.videocam, color: Colors.white, size: 64),
                        const SizedBox(height: 16),
                        const Text('Video tidak dapat ditampilkan inline.\nBuka di browser:', style: TextStyle(color: Colors.white70)),
                        const SizedBox(height: 8),
                        SelectableText(url, style: const TextStyle(color: Colors.tealAccent, fontSize: 12)),
                      ],
                    ),
            ),
            Positioned(
              top: 16,
              right: 16,
              child: IconButton(
                icon: const Icon(Icons.close, color: Colors.white, size: 28),
                onPressed: () => Navigator.pop(context),
              ),
            ),
          ],
        ),
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    final isPre = widget.item.jenisPemeriksaan == 'KONSULTASI_PRE';
    final dateFormat = DateFormat('dd MMMM yyyy');

    return Scaffold(
      appBar: AppBar(
        title: const Text('Detail Pemeriksaan'),
        backgroundColor: const Color(0xFF004D40),
        foregroundColor: Colors.white,
      ),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(16.0),
        child: Card(
          shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
          child: Padding(
            padding: const EdgeInsets.all(16.0),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                // ─── Header ───────────────────────────────────────
                Row(
                  mainAxisAlignment: MainAxisAlignment.spaceBetween,
                  children: [
                    Text(
                      isPre ? 'KONSULTASI PRE-OP' : 'CONTROL POST-OP',
                      style: TextStyle(
                        fontWeight: FontWeight.bold,
                        fontSize: 16,
                        color: isPre ? Colors.blue[800] : Colors.orange[800],
                      ),
                    ),
                    Text(dateFormat.format(widget.item.tanggal), style: const TextStyle(fontWeight: FontWeight.w600)),
                  ],
                ),
                const Divider(height: 24),

                Text(widget.item.pasienNama ?? 'Pasien ID ${widget.item.pasienId}', style: const TextStyle(fontSize: 18, fontWeight: FontWeight.bold)),
                const SizedBox(height: 16),

                const Text('Keluhan Utama:', style: TextStyle(fontWeight: FontWeight.bold)),
                Text(widget.item.keluhanUtama?.isNotEmpty == true ? widget.item.keluhanUtama! : '-'),
                const SizedBox(height: 12),

                const Text('Riwayat Penyakit Sekarang:', style: TextStyle(fontWeight: FontWeight.bold)),
                Text(widget.item.riwayatPenyakitSekarang?.isNotEmpty == true ? widget.item.riwayatPenyakitSekarang! : '-'),
                const SizedBox(height: 12),

                const Text('Tindakan Terkait:', style: TextStyle(fontWeight: FontWeight.bold)),
                Text(widget.item.tindakanNama ?? '-'),
                const SizedBox(height: 12),

                const Text('Keterangan:', style: TextStyle(fontWeight: FontWeight.bold)),
                Text(widget.item.keterangan.isNotEmpty ? widget.item.keterangan : '-'),
                const Divider(height: 28),

                // ─── Dokumentasi Media ────────────────────────────
                Row(
                  mainAxisAlignment: MainAxisAlignment.spaceBetween,
                  children: [
                    const Text('Dokumentasi', style: TextStyle(fontSize: 15, fontWeight: FontWeight.bold)),
                    if (!_loadingMedia)
                      Text('${_medias.length} file', style: const TextStyle(color: Colors.grey, fontSize: 13)),
                  ],
                ),
                const SizedBox(height: 10),
                if (_loadingMedia)
                  const Center(child: Padding(
                    padding: EdgeInsets.all(12),
                    child: CircularProgressIndicator(),
                  ))
                else if (_medias.isEmpty)
                  const Text('Belum ada dokumentasi.', style: TextStyle(color: Colors.grey))
                else
                  GridView.builder(
                    shrinkWrap: true,
                    physics: const NeverScrollableScrollPhysics(),
                    gridDelegate: const SliverGridDelegateWithFixedCrossAxisCount(
                      crossAxisCount: 3,
                      crossAxisSpacing: 8,
                      mainAxisSpacing: 8,
                    ),
                    itemCount: _medias.length,
                    itemBuilder: (ctx, idx) {
                      final media = _medias[idx];
                      final url = ApiService.getPemeriksaanMediaUrl(media.id);
                      return GestureDetector(
                        onTap: () => _showFullscreen(media.id, media.isFoto),
                        child: ClipRRect(
                          borderRadius: BorderRadius.circular(10),
                          child: Stack(
                            fit: StackFit.expand,
                            children: [
                              if (media.isFoto)
                                Image.network(
                                  url,
                                  fit: BoxFit.cover,
                                  loadingBuilder: (_, child, progress) => progress == null
                                      ? child
                                      : Container(color: Colors.grey[200], child: const Center(child: CircularProgressIndicator(strokeWidth: 2))),
                                  errorBuilder: (context, error, stack) => Container(
                                    color: Colors.grey[200],
                                    child: const Icon(Icons.broken_image, color: Colors.grey),
                                  ),
                                )
                              else
                                Container(
                                  color: Colors.teal[50],
                                  child: const Column(
                                    mainAxisAlignment: MainAxisAlignment.center,
                                    children: [
                                      Icon(Icons.videocam, color: Color(0xFF004D40), size: 32),
                                      SizedBox(height: 4),
                                      Text('Video', style: TextStyle(fontSize: 10, color: Color(0xFF004D40))),
                                    ],
                                  ),
                                ),
                              Positioned(
                                bottom: 4,
                                right: 4,
                                child: Icon(
                                  media.isFoto ? Icons.image : Icons.play_circle_fill,
                                  color: Colors.white.withValues(alpha: 0.9),
                                  size: 18,
                                  shadows: const [Shadow(color: Colors.black54, blurRadius: 4)],
                                ),
                              ),
                            ],
                          ),
                        ),
                      );
                    },
                  ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}
