import 'dart:io';
import 'package:flutter/material.dart';
import 'package:image_picker/image_picker.dart';
import 'package:intl/intl.dart';
import 'package:provider/provider.dart';
import '../../models/laporan_operasi.dart';
import '../../models/pasien.dart';
import '../../models/dokter.dart';
import '../../models/rumah_sakit.dart';
import '../../models/diagnosa.dart';
import '../../models/tindakan.dart';
import '../../models/jenis_tindakan.dart';
import '../../services/database_service.dart';
import '../../services/api_service.dart';
import '../../providers/auth_provider.dart';

class LaporanOperasiFormScreen extends StatefulWidget {
  final LaporanOperasi? item;
  const LaporanOperasiFormScreen({super.key, this.item});

  @override
  State<LaporanOperasiFormScreen> createState() => _LaporanOperasiFormScreenState();
}

class _LaporanOperasiFormScreenState extends State<LaporanOperasiFormScreen> {
  final DatabaseService _db = DatabaseService();
  final _formKey = GlobalKey<FormState>();

  DateTime _tanggal = DateTime.now();
  int? _selectedPasienId;
  int? _selectedTindakanId;
  int? _selectedOperatorId;
  int? _selectedDPJPId;
  int? _selectedRumahSakitId;
  int? _selectedGOS;
  final TextEditingController _keteranganController = TextEditingController();
  final TextEditingController _umurController = TextEditingController();
  final TextEditingController _noRMController = TextEditingController();

  List<Pasien> _pasienList = [];
  List<Dokter> _dokterList = [];
  List<RumahSakit> _rsList = [];
  List<Tindakan> _tindakanList = [];
  List<Diagnosa> _diagnosaList = [];
  List<JenisTindakan> _jenisTindakanList = [];

  List<int> _selectedDiagnosaIds = [];
  List<int> _selectedJenisTindakanIds = [];
  final List<XFile> _pickedImages = [];
  final List<XFile> _pickedVideos = [];
  bool _isLoading = true;
  bool _isSaving = false;

  @override
  void initState() {
    super.initState();
    if (widget.item != null) {
      _tanggal = widget.item!.tanggal;
      _selectedPasienId = widget.item!.pasienId;
      _selectedTindakanId = widget.item!.tindakanId;
      _selectedOperatorId = widget.item!.operatorId;
      _selectedDPJPId = widget.item!.dpjpId;
      _selectedRumahSakitId = widget.item!.rumahSakitId;
      _selectedGOS = widget.item!.gos;
      _keteranganController.text = widget.item!.keterangan;
      _umurController.text = widget.item!.umur;
      _noRMController.text = widget.item!.noRM;
      _selectedDiagnosaIds = List.from(widget.item!.diagnosaIds);
      _selectedJenisTindakanIds = List.from(widget.item!.jenisTindakanIds);
    }
    _loadDropdowns();
  }

  Future<void> _loadDropdowns() async {
    try {
      final p = await _db.getPasienList();
      final d = await _db.getDokterList();
      final rs = await _db.getRumahSakitList();
      final t = await _db.getTindakanList();
      final diag = await _db.getDiagnosaList();
      final jt = await _db.getJenisTindakanList();

      setState(() {
        _pasienList = p;
        _dokterList = d;
        _rsList = rs;
        _tindakanList = t;
        _diagnosaList = diag;
        _jenisTindakanList = jt;
        _isLoading = false;
      });
    } catch (e) {
      setState(() => _isLoading = false);
    }
  }

  void _onPasienChanged(int? id) {
    setState(() {
      _selectedPasienId = id;
      if (id != null) {
        final p = _pasienList.firstWhere((element) => element.id == id);
        _noRMController.text = p.noRM;
        _umurController.text = p.usia ?? '';
      }
    });
  }

  void _selectDiagnosaMulti() {
    showDialog(
      context: context,
      builder: (context) {
        final tempSelected = List<int>.from(_selectedDiagnosaIds);
        return StatefulBuilder(
          builder: (context, setModalState) {
            return AlertDialog(
              title: const Text('Pilih Diagnosa (Multi)'),
              content: SizedBox(
                width: double.maxFinite,
                height: 400,
                child: ListView.builder(
                  itemCount: _diagnosaList.length,
                  itemBuilder: (context, index) {
                    final item = _diagnosaList[index];
                    final isChecked = tempSelected.contains(item.id);
                    return CheckboxListTile(
                      title: Text('${item.kodeICD} - ${item.nama}'),
                      value: isChecked,
                      onChanged: (val) {
                        setModalState(() {
                          if (val == true) {
                            tempSelected.add(item.id);
                          } else {
                            tempSelected.remove(item.id);
                          }
                        });
                      },
                    );
                  },
                ),
              ),
              actions: [
                TextButton(onPressed: () => Navigator.pop(context), child: const Text('Batal')),
                ElevatedButton(
                  onPressed: () {
                    setState(() => _selectedDiagnosaIds = tempSelected);
                    Navigator.pop(context);
                  },
                  child: const Text('Selesai'),
                ),
              ],
            );
          },
        );
      },
    );
  }

  void _selectJenisTindakanMulti() {
    showDialog(
      context: context,
      builder: (context) {
        final tempSelected = List<int>.from(_selectedJenisTindakanIds);
        return StatefulBuilder(
          builder: (context, setModalState) {
            return AlertDialog(
              title: const Text('Pilih Jenis Tindakan (Multi)'),
              content: SizedBox(
                width: double.maxFinite,
                height: 400,
                child: ListView.builder(
                  itemCount: _jenisTindakanList.length,
                  itemBuilder: (context, index) {
                    final item = _jenisTindakanList[index];
                    final isChecked = tempSelected.contains(item.id);
                    return CheckboxListTile(
                      title: Text(item.nama),
                      value: isChecked,
                      onChanged: (val) {
                        setModalState(() {
                          if (val == true) {
                            tempSelected.add(item.id);
                          } else {
                            tempSelected.remove(item.id);
                          }
                        });
                      },
                    );
                  },
                ),
              ),
              actions: [
                TextButton(onPressed: () => Navigator.pop(context), child: const Text('Batal')),
                ElevatedButton(
                  onPressed: () {
                    setState(() => _selectedJenisTindakanIds = tempSelected);
                    Navigator.pop(context);
                  },
                  child: const Text('Selesai'),
                ),
              ],
            );
          },
        );
      },
    );
  }

  void _handleSave() async {
    if (_formKey.currentState!.validate()) {
      if (_selectedPasienId == null) {
        ScaffoldMessenger.of(context).showSnackBar(const SnackBar(content: Text('Pilih Pasien')));
        return;
      }
      if (_selectedTindakanId == null) {
        ScaffoldMessenger.of(context).showSnackBar(const SnackBar(content: Text('Pilih Tindakan')));
        return;
      }

      setState(() => _isSaving = true);
      final user = Provider.of<AuthProvider>(context, listen: false).currentAdmin?.username ?? 'Admin';

      final savedId = await _db.saveLaporanOperasiReturnId(
        id: widget.item?.id,
        tanggal: _tanggal,
        pasienId: _selectedPasienId!,
        umur: _umurController.text.trim(),
        noRM: _noRMController.text.trim(),
        tindakanId: _selectedTindakanId!,
        operatorId: _selectedOperatorId ?? 0,
        dpjpId: _selectedDPJPId ?? 0,
        rumahSakitId: _selectedRumahSakitId ?? 0,
        diagnosaIds: _selectedDiagnosaIds,
        jenisTindakanIds: _selectedJenisTindakanIds,
        gos: _selectedGOS,
        keterangan: _keteranganController.text.trim(),
        user: user,
      );

      // Upload foto ke server via HTTP API
      if (savedId != null && _pickedImages.isNotEmpty) {
        try {
          final apiService = ApiService();
          final result = await apiService.uploadLaporanOperasiMedia(
            id: savedId,
            fotos: _pickedImages,
            videos: _pickedVideos,
          );
          if (result.errors.isNotEmpty && mounted) {
            ScaffoldMessenger.of(context).showSnackBar(
              SnackBar(content: Text('Upload sebagian gagal: ${result.errors.join(', ')}')),
            );
          }
        } catch (_) {}
      }

      setState(() => _isSaving = false);
      if (savedId != null && mounted) {
        Navigator.pop(context, true);
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: Text(widget.item == null ? 'Buat Laporan Operasi' : 'Edit Laporan Operasi'),
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
                    // Tanggal Operasi
                    ListTile(
                      contentPadding: EdgeInsets.zero,
                      title: const Text('Tanggal Operasi *'),
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

                    // Pasien Dropdown
                    DropdownButtonFormField<int>(
                      value: _selectedPasienId,
                      decoration: const InputDecoration(labelText: 'Pasien *', border: OutlineInputBorder()),
                      items: _pasienList.map((p) => DropdownMenuItem(value: p.id, child: Text('${p.nama} (${p.noRM})'))).toList(),
                      onChanged: _onPasienChanged,
                      validator: (v) => v == null ? 'Pilih pasien' : null,
                    ),
                    const SizedBox(height: 12),

                    Row(
                      children: [
                        Expanded(
                          child: TextFormField(
                            controller: _noRMController,
                            decoration: const InputDecoration(labelText: 'No RM Snapshot', border: OutlineInputBorder()),
                          ),
                        ),
                        const SizedBox(width: 12),
                        Expanded(
                          child: TextFormField(
                            controller: _umurController,
                            decoration: const InputDecoration(labelText: 'Umur Snapshot', border: OutlineInputBorder()),
                          ),
                        ),
                      ],
                    ),
                    const SizedBox(height: 12),

                    // Operator & DPJP
                    DropdownButtonFormField<int>(
                      value: _selectedOperatorId,
                      decoration: const InputDecoration(labelText: 'Operator (Dokter)', border: OutlineInputBorder()),
                      items: _dokterList.map((d) => DropdownMenuItem(value: d.id, child: Text(d.nama))).toList(),
                      onChanged: (v) => setState(() => _selectedOperatorId = v),
                    ),
                    const SizedBox(height: 12),

                    DropdownButtonFormField<int>(
                      value: _selectedDPJPId,
                      decoration: const InputDecoration(labelText: 'DPJP (Dokter)', border: OutlineInputBorder()),
                      items: _dokterList.map((d) => DropdownMenuItem(value: d.id, child: Text(d.nama))).toList(),
                      onChanged: (v) => setState(() => _selectedDPJPId = v),
                    ),
                    const SizedBox(height: 12),

                    // Rumah Sakit
                    DropdownButtonFormField<int>(
                      value: _selectedRumahSakitId,
                      decoration: const InputDecoration(labelText: 'Rumah Sakit', border: OutlineInputBorder()),
                      items: _rsList.map((rs) => DropdownMenuItem(value: rs.id, child: Text(rs.nama))).toList(),
                      onChanged: (v) => setState(() => _selectedRumahSakitId = v),
                    ),
                    const SizedBox(height: 12),

                    // Tindakan Primary
                    DropdownButtonFormField<int>(
                      value: _selectedTindakanId,
                      decoration: const InputDecoration(labelText: 'Tindakan Utama *', border: OutlineInputBorder()),
                      items: _tindakanList.map((t) => DropdownMenuItem(value: t.id, child: Text(t.nama))).toList(),
                      onChanged: (v) => setState(() => _selectedTindakanId = v),
                      validator: (v) => v == null ? 'Pilih tindakan utama' : null,
                    ),
                    const SizedBox(height: 16),

                    // Multi Diagnosa Button
                    Card(
                      child: ListTile(
                        title: const Text('Diagnosa (Multi-select)'),
                        subtitle: Text(_selectedDiagnosaIds.isEmpty ? 'Belum ada diagnosa dipilih' : '${_selectedDiagnosaIds.length} diagnosa dipilih'),
                        trailing: const Icon(Icons.arrow_forward_ios, size: 16),
                        onTap: _selectDiagnosaMulti,
                      ),
                    ),
                    const SizedBox(height: 8),

                    // Multi Jenis Tindakan Button
                    Card(
                      child: ListTile(
                        title: const Text('Jenis Tindakan (Multi-select)'),
                        subtitle: Text(_selectedJenisTindakanIds.isEmpty ? 'Belum ada jenis tindakan dipilih' : '${_selectedJenisTindakanIds.length} jenis tindakan dipilih'),
                        trailing: const Icon(Icons.arrow_forward_ios, size: 16),
                        onTap: _selectJenisTindakanMulti,
                      ),
                    ),
                    const SizedBox(height: 16),

                    // GOS Dropdown
                    DropdownButtonFormField<int>(
                      value: _selectedGOS,
                      decoration: const InputDecoration(labelText: 'Glasgow Outcome Scale (GOS)', border: OutlineInputBorder()),
                      items: const [
                        DropdownMenuItem(value: 1, child: Text('1 - Meninggal')),
                        DropdownMenuItem(value: 2, child: Text('2 - Vegetatif')),
                        DropdownMenuItem(value: 3, child: Text('3 - Cacat Berat')),
                        DropdownMenuItem(value: 4, child: Text('4 - Cacat Sedang')),
                        DropdownMenuItem(value: 5, child: Text('5 - Sembuh / Baik')),
                      ],
                      onChanged: (v) => setState(() => _selectedGOS = v),
                    ),
                    const SizedBox(height: 12),

                    TextFormField(
                      controller: _keteranganController,
                      maxLines: 3,
                      decoration: const InputDecoration(labelText: 'Keterangan / Catatan Operasi', border: OutlineInputBorder()),
                    ),
                    const SizedBox(height: 16),

                    // Media / Foto Hasil Operasi Upload
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
                                Text('Upload Foto Hasil Operasi', style: TextStyle(fontWeight: FontWeight.bold, fontSize: 14, color: Color(0xFF004D40))),
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
                      height: 52,
                      child: ElevatedButton(
                        style: ElevatedButton.styleFrom(
                          backgroundColor: const Color(0xFF004D40),
                          foregroundColor: Colors.white,
                          shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
                        ),
                        onPressed: _isSaving ? null : _handleSave,
                        child: _isSaving
                            ? const CircularProgressIndicator(color: Colors.white)
                            : const Text('SIMPAN LAPORAN OPERASI', style: TextStyle(fontWeight: FontWeight.bold, fontSize: 16)),
                      ),
                    ),
                  ],
                ),
              ),
            ),
    );
  }
}
