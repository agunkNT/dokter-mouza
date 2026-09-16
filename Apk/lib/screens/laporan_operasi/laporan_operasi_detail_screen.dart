import 'package:flutter/material.dart';
import 'package:intl/intl.dart';
import '../../models/laporan_operasi.dart';
import '../../services/database_service.dart';
import '../../services/api_service.dart';

class LaporanOperasiDetailScreen extends StatefulWidget {
  final LaporanOperasi item;
  const LaporanOperasiDetailScreen({super.key, required this.item});

  @override
  State<LaporanOperasiDetailScreen> createState() => _LaporanOperasiDetailScreenState();
}

class _LaporanOperasiDetailScreenState extends State<LaporanOperasiDetailScreen> {
  final DatabaseService _db = DatabaseService();
  List<LaporanOperasiMedia> _medias = [];
  bool _loadingMedia = true;

  @override
  void initState() {
    super.initState();
    _loadMedia();
  }

  Future<void> _loadMedia() async {
    try {
      final medias = await _db.getLaporanOperasiMedias(widget.item.id);
      if (mounted) setState(() { _medias = medias; _loadingMedia = false; });
    } catch (_) {
      if (mounted) setState(() => _loadingMedia = false);
    }
  }

  void _showFullscreen(int mediaId, bool isFoto) {
    final url = ApiService.getLaporanOperasiMediaUrl(mediaId);
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
                        errorBuilder: (_, __, ___) => const Icon(Icons.broken_image, color: Colors.white, size: 64),
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
    final dateFormat = DateFormat('dd MMMM yyyy');

    return Scaffold(
      appBar: AppBar(
        title: const Text('Detail Laporan Operasi'),
        backgroundColor: const Color(0xFF004D40),
        foregroundColor: Colors.white,
      ),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(16.0),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            // ─── Info Utama ───────────────────────────────────────
            Card(
              shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
              child: Padding(
                padding: const EdgeInsets.all(16.0),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      widget.item.pasienNama ?? 'Pasien No RM: ${widget.item.noRM}',
                      style: const TextStyle(fontSize: 20, fontWeight: FontWeight.bold, color: Color(0xFF004D40)),
                    ),
                    const SizedBox(height: 8),
                    _buildRow('Tanggal Operasi:', dateFormat.format(widget.item.tanggal)),
                    _buildRow('No. Rekam Medis:', widget.item.noRM),
                    _buildRow('Usia Pasien:', widget.item.umur.isNotEmpty ? widget.item.umur : '-'),
                    _buildRow('Rumah Sakit:', widget.item.rumahSakitNama ?? '-'),
                    _buildRow('Operator:', widget.item.operatorNama ?? '-'),
                    _buildRow('DPJP:', widget.item.dpjpNama ?? '-'),
                    _buildRow('Tindakan Utama:', widget.item.tindakanNama ?? '-'),
                    _buildRow('Glasgow Outcome Scale (GOS):', widget.item.gos != null ? 'GOS ${widget.item.gos}' : '-'),
                  ],
                ),
              ),
            ),
            const SizedBox(height: 16),

            // ─── Diagnosa ─────────────────────────────────────────
            const Text('Diagnosa', style: TextStyle(fontSize: 16, fontWeight: FontWeight.bold)),
            const SizedBox(height: 6),
            widget.item.diagnosaNames.isEmpty
                ? const Text('- Tidak ada diagnosa', style: TextStyle(color: Colors.grey))
                : Wrap(
                    spacing: 8,
                    children: widget.item.diagnosaNames.map((d) => Chip(label: Text(d))).toList(),
                  ),
            const SizedBox(height: 16),

            // ─── Jenis Tindakan ───────────────────────────────────
            const Text('Jenis Tindakan', style: TextStyle(fontSize: 16, fontWeight: FontWeight.bold)),
            const SizedBox(height: 6),
            widget.item.jenisTindakanNames.isEmpty
                ? const Text('- Tidak ada jenis tindakan', style: TextStyle(color: Colors.grey))
                : Wrap(
                    spacing: 8,
                    children: widget.item.jenisTindakanNames.map((jt) => Chip(label: Text(jt))).toList(),
                  ),
            const SizedBox(height: 16),

            // ─── Flags ────────────────────────────────────────────
            const Text('Indikator Otomatis (Flags)', style: TextStyle(fontSize: 16, fontWeight: FontWeight.bold)),
            const SizedBox(height: 8),
            Wrap(
              spacing: 8,
              runSpacing: 8,
              children: [
                _buildFlagTile('Hidrosefalus', widget.item.hidrosefalus),
                _buildFlagTile('ETV', widget.item.etv),
                _buildFlagTile('Shunt', widget.item.shunt),
                _buildFlagTile('ETV Gagal Shunt', widget.item.etvGagalShunt),
              ],
            ),
            const SizedBox(height: 20),

            // ─── Keterangan ───────────────────────────────────────
            const Text('Keterangan / Catatan Operasi', style: TextStyle(fontSize: 16, fontWeight: FontWeight.bold)),
            const SizedBox(height: 6),
            Container(
              width: double.infinity,
              padding: const EdgeInsets.all(12),
              decoration: BoxDecoration(
                color: Colors.grey[100],
                borderRadius: BorderRadius.circular(12),
                border: Border.all(color: Colors.grey[300]!),
              ),
              child: Text(
                widget.item.keterangan.isNotEmpty ? widget.item.keterangan : 'Tidak ada keterangan tambahan.',
                style: const TextStyle(fontSize: 14),
              ),
            ),
            const SizedBox(height: 20),

            // ─── Dokumentasi Media ────────────────────────────────
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                const Text('Dokumentasi', style: TextStyle(fontSize: 16, fontWeight: FontWeight.bold)),
                if (!_loadingMedia)
                  Text('${_medias.length} file', style: const TextStyle(color: Colors.grey, fontSize: 13)),
              ],
            ),
            const SizedBox(height: 10),
            if (_loadingMedia)
              const Center(child: Padding(
                padding: EdgeInsets.all(16),
                child: CircularProgressIndicator(),
              ))
            else if (_medias.isEmpty)
              Container(
                width: double.infinity,
                padding: const EdgeInsets.all(16),
                decoration: BoxDecoration(
                  color: Colors.grey[50],
                  borderRadius: BorderRadius.circular(12),
                  border: Border.all(color: Colors.grey[200]!),
                ),
                child: const Text('Belum ada dokumentasi.', style: TextStyle(color: Colors.grey), textAlign: TextAlign.center),
              )
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
                  final url = ApiService.getLaporanOperasiMediaUrl(media.id);
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
                              errorBuilder: (_, __, ___) => Container(
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
            const SizedBox(height: 32),
          ],
        ),
      ),
    );
  }

  Widget _buildRow(String label, String value) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 4.0),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          SizedBox(width: 150, child: Text(label, style: const TextStyle(fontWeight: FontWeight.w600, color: Colors.black54))),
          Expanded(child: Text(value, style: const TextStyle(fontWeight: FontWeight.bold))),
        ],
      ),
    );
  }

  Widget _buildFlagTile(String label, bool value) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
      decoration: BoxDecoration(
        color: value ? Colors.teal.withValues(alpha: 0.15) : Colors.grey.withValues(alpha: 0.1),
        borderRadius: BorderRadius.circular(8),
        border: Border.all(color: value ? Colors.teal : Colors.grey[400]!),
      ),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          Icon(value ? Icons.check_circle : Icons.cancel_outlined, color: value ? Colors.teal : Colors.grey, size: 18),
          const SizedBox(width: 6),
          Text(label, style: TextStyle(color: value ? Colors.teal[800] : Colors.grey[700], fontWeight: FontWeight.bold)),
        ],
      ),
    );
  }
}
