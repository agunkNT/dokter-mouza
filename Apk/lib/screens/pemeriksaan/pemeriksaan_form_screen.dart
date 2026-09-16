import 'dart:io';
import 'package:flutter/material.dart';
import 'package:image_picker/image_picker.dart';
import 'package:intl/intl.dart';
import 'package:provider/provider.dart';
import '../../models/pemeriksaan.dart';
import '../../models/pasien.dart';
import '../../models/tindakan.dart';
import '../../services/database_service.dart';
import '../../services/api_service.dart';
import '../../providers/auth_provider.dart';

class PemeriksaanFormScreen extends StatefulWidget {
  final Pemeriksaan? item;
  const PemeriksaanFormScreen({super.key, this.item});

  @override
  State<PemeriksaanFormScreen> createState() => _PemeriksaanFormScreenState();
}

class _PemeriksaanFormScreenState extends State<PemeriksaanFormScreen> {
  final DatabaseService _db = DatabaseService();
  final _formKey = GlobalKey<FormState>();

  String _jenisPemeriksaan = 'KONSULTASI_PRE';
  DateTime _tanggal = DateTime.now();
  int? _selectedPasienId;
  int? _selectedTindakanId;

  final TextEditingController _keluhanController = TextEditingController();
  final TextEditingController _riwayatController = TextEditingController();
  final TextEditingController _keteranganController = TextEditingController();

  List<Pasien> _pasienList = [];
  List<Tindakan> _tindakanList = [];
  final List<XFile> _pickedImages = [];
  final List<XFile> _pickedVideos = [];
  bool _isLoading = true;
  bool _isSaving = false;

  @override
  void initState() {
    super.initState();
    if (widget.item != null) {
      _jenisPemeriksaan = widget.item!.jenisPemeriksaan;
      _tanggal = widget.item!.tanggal;
      _selectedPasienId = widget.item!.pasienId;
      _selectedTindakanId = widget.item!.tindakanId;
      _keluhanController.text = widget.item!.keluhanUtama ?? '';
      _riwayatController.text = widget.item!.riwayatPenyakitSekarang ?? '';
      _keteranganController.text = widget.item!.keterangan;
    }
    _loadDropdowns();
  }

  Future<void> _loadDropdowns() async {
    try {
      final p = await _db.getPasienList();
      final t = await _db.getTindakanList();
      setState(() {
        _pasienList = p;
        _tindakanList = t;
        _isLoading = false;
      });
    } catch (e) {
      setState(() => _isLoading = false);
    }
  }

  void _handleSave() async {
    if (_formKey.currentState!.validate()) {
      if (_selectedPasienId == null) {
        ScaffoldMessenger.of(context).showSnackBar(const SnackBar(content: Text('Pilih Pasien')));
        return;
      }

      setState(() => _isSaving = true);
      final user = Provider.of<AuthProvider>(context, listen: false).currentAdmin?.username ?? 'Admin';

      // Simpan data pemeriksaan — dapat ID dari savePemeriksaanReturnId jika ada, atau query setelah save
      final success = await _db.savePemeriksaan(
        id: widget.item?.id,
        jenisPemeriksaan: _jenisPemeriksaan,
        tanggal: _tanggal,
        pasienId: _selectedPasienId!,
        keluhanUtama: _keluhanController.text.trim(),
        riwayatPenyakitSekarang: _riwayatController.text.trim(),
        tindakanId: _selectedTindakanId,
        keterangan: _keteranganController.text.trim(),
        user: user,
      );

      // Upload foto jika ada
      if (success && _pickedImages.isNotEmpty) {
        try {
          // Dapatkan ID yang baru disimpan (ambil record terbaru milik pasien ini)
          final savedId = await _db.getLastPemeriksaanId(
            pasienId: _selectedPasienId!,
            editId: widget.item?.id,
          );
          if (savedId != null) {
            final apiService = ApiService();
            final result = await apiService.uploadPemeriksaanMedia(
              id: savedId,
              fotos: _pickedImages,
              videos: _pickedVideos,
            );
            if (result.errors.isNotEmpty && mounted) {
              ScaffoldMessenger.of(context).showSnackBar(
                SnackBar(content: Text('Upload sebagian gagal: ${result.errors.join(', ')}')),
              );
            }
          }
        } catch (_) {}
      }

      setState(() => _isSaving = false);
      if (success && mounted) {
        Navigator.pop(context, true);
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: Text(widget.item == null ? 'Buat Pemeriksaan' : 'Edit Pemeriksaan'),
        backgroundColor: const Color(0xFF004D40),
        foregroundColor: Colors.white,
      ),
      body: _isLoading
          ? const Center(child: CircularProgressIndicator())
          : SingleChildScrollView(
              padding: const EdgeInsets.all(16.0),
              child: Form(
                key: _formKey,
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    DropdownButtonFormField<String>(
                      initialValue: _jenisPemeriksaan,
                      decoration: const InputDecoration(labelText: 'Jenis Pemeriksaan *', border: OutlineInputBorder()),
                      items: const [
                        DropdownMenuItem(value: 'KONSULTASI_PRE', child: Text('Konsultasi Pre-Op')),
                        DropdownMenuItem(value: 'CONTROL_POST', child: Text('Control Post-Op')),
                      ],
                      onChanged: (v) {
                        if (v != null) setState(() => _jenisPemeriksaan = v);
                      },
                    ),
                    const SizedBox(height: 12),

                    ListTile(
                      contentPadding: EdgeInsets.zero,
                      title: const Text('Tanggal Pemeriksaan *'),
                      subtitle: Text(DateFormat('dd MMMM yyyy').format(_tanggal)),
                      trailing: const Icon(Icons.calendar_today, color: Color(0xFF004D40)),
                      onTap: () async {
                        final picked = await showDatePicker(
                          context: context,
                          initialDate: _tanggal,
                          firstDate: DateTime(2000),
                          lastDate: DateTime(2100),
                        );
                        if (picked != null) setState(() => _tanggal = picked);
                      },
                    ),
                    const SizedBox(height: 12),

                    DropdownButtonFormField<int>(
                      initialValue: _selectedPasienId,
                      decoration: const InputDecoration(labelText: 'Pasien *', border: OutlineInputBorder()),
                      items: _pasienList.map((p) => DropdownMenuItem(value: p.id, child: Text('${p.nama} (${p.noRM})'))).toList(),
                      onChanged: (v) => setState(() => _selectedPasienId = v),
                      validator: (v) => v == null ? 'Pilih pasien' : null,
                    ),
                    const SizedBox(height: 12),

                    TextFormField(
                      controller: _keluhanController,
                      maxLines: 2,
                      decoration: const InputDecoration(labelText: 'Keluhan Utama', border: OutlineInputBorder()),
                    ),
                    const SizedBox(height: 12),

                    TextFormField(
                      controller: _riwayatController,
                      maxLines: 3,
                      decoration: const InputDecoration(labelText: 'Riwayat Penyakit Sekarang', border: OutlineInputBorder()),
                    ),
                    const SizedBox(height: 12),

                    DropdownButtonFormField<int>(
                      initialValue: _selectedTindakanId,
                      decoration: const InputDecoration(labelText: 'Rencana / Tindakan Terkait', border: OutlineInputBorder()),
                      items: _tindakanList.map((t) => DropdownMenuItem(value: t.id, child: Text(t.nama))).toList(),
                      onChanged: (v) => setState(() => _selectedTindakanId = v),
                    ),
                    const SizedBox(height: 12),

                    TextFormField(
                      controller: _keteranganController,
                      maxLines: 3,
                      decoration: const InputDecoration(labelText: 'Keterangan Tambahan', border: OutlineInputBorder()),
                    ),
                    const SizedBox(height: 16),

                    // ─── Upload Foto ───────────────────────────────
                    Card(
                      elevation: 2,
                      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
                      child: Padding(
                        padding: const EdgeInsets.all(12.0),
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Row(
                              mainAxisAlignment: MainAxisAlignment.spaceBetween,
                              children: const [
                                Text('Upload Foto / Dokumentasi', style: TextStyle(fontWeight: FontWeight.bold, fontSize: 14, color: Color(0xFF004D40))),
                                Icon(Icons.camera_alt_outlined, color: Color(0xFF004D40)),
                              ],
                            ),
                            const SizedBox(height: 8),
                            Row(
                              children: [
                                ElevatedButton.icon(
                                  onPressed: () async {
                                    final picker = ImagePicker();
                                    final img = await picker.pickImage(source: ImageSource.camera, imageQuality: 85);
                                    if (img != null) setState(() => _pickedImages.add(img));
                                  },
                                  icon: const Icon(Icons.camera, size: 18),
                                  label: const Text('Kamera', style: TextStyle(fontSize: 12)),
                                  style: ElevatedButton.styleFrom(backgroundColor: const Color(0xFF004D40), foregroundColor: Colors.white),
                                ),
                                const SizedBox(width: 8),
                                OutlinedButton.icon(
                                  onPressed: () async {
                                    final picker = ImagePicker();
                                    final img = await picker.pickImage(source: ImageSource.gallery, imageQuality: 85);
                                    if (img != null) setState(() => _pickedImages.add(img));
                                  },
                                  icon: const Icon(Icons.photo_library, size: 18),
                                  label: const Text('Galeri', style: TextStyle(fontSize: 12)),
                                ),
                              ],
                            ),
                            const SizedBox(height: 8),
                            Row(
                              children: [
                                OutlinedButton.icon(
                                  onPressed: () async {
                                    final picker = ImagePicker();
                                    final video = await picker.pickVideo(source: ImageSource.gallery);
                                    if (video != null) setState(() => _pickedVideos.add(video));
                                  },
                                  icon: const Icon(Icons.video_library, size: 18),
                                  label: const Text('Video Galeri', style: TextStyle(fontSize: 12)),
                                ),
                              ],
                            ),
                            if (_pickedImages.isNotEmpty || _pickedVideos.isNotEmpty) ...[
                              const SizedBox(height: 10),
                              SizedBox(
                                height: 75,
                                child: ListView.builder(
                                  scrollDirection: Axis.horizontal,
                                  itemCount: _pickedImages.length,
                                  itemBuilder: (ctx, idx) {
                                    return Stack(
                                      children: [
                                        Container(
                                          margin: const EdgeInsets.only(right: 8),
                                          width: 75,
                                          height: 75,
                                          decoration: BoxDecoration(
                                            borderRadius: BorderRadius.circular(8),
                                            image: DecorationImage(
                                              image: FileImage(File(_pickedImages[idx].path)),
                                              fit: BoxFit.cover,
                                            ),
                                          ),
                                        ),
                                        Positioned(
                                          top: 0,
                                          right: 8,
                                          child: GestureDetector(
                                            onTap: () => setState(() => _pickedImages.removeAt(idx)),
                                            child: const CircleAvatar(
                                              radius: 10,
                                              backgroundColor: Colors.red,
                                              child: Icon(Icons.close, size: 12, color: Colors.white),
                                            ),
                                          ),
                                        ),
                                      ],
                                    );
                                  },
                                ),
                              ),
                            ],
                            if (_pickedVideos.isNotEmpty) ...[
                              const SizedBox(height: 10),
                              const Text('Video Terpilih:', style: TextStyle(fontSize: 12, fontWeight: FontWeight.bold)),
                              const SizedBox(height: 4),
                              Column(
                                children: _pickedVideos.asMap().entries.map((entry) {
                                  final idx = entry.key;
                                  final video = entry.value;
                                  return ListTile(
                                    contentPadding: EdgeInsets.zero,
                                    leading: const Icon(Icons.videocam, color: Color(0xFF004D40)),
                                    title: Text(video.name, maxLines: 1, overflow: TextOverflow.ellipsis, style: const TextStyle(fontSize: 12)),
                                    trailing: IconButton(
                                      icon: const Icon(Icons.close, color: Colors.red, size: 20),
                                      onPressed: () => setState(() => _pickedVideos.removeAt(idx)),
                                    ),
                                  );
                                }).toList(),
                              ),
                            ],
                          ],
                        ),
                      ),
                    ),
                    const SizedBox(height: 24),

                    SizedBox(
                      width: double.infinity,
                      height: 50,
                      child: ElevatedButton(
                        style: ElevatedButton.styleFrom(
                          backgroundColor: const Color(0xFF004D40),
                          foregroundColor: Colors.white,
                          shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
                        ),
                        onPressed: _isSaving ? null : _handleSave,
                        child: _isSaving
                            ? const CircularProgressIndicator(color: Colors.white)
                            : const Text('SIMPAN PEMERIKSAAN', style: TextStyle(fontWeight: FontWeight.bold)),
                      ),
                    ),
                  ],
                ),
              ),
            ),
    );
  }
}
