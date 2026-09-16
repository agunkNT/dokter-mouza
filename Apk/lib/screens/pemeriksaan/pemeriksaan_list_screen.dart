import 'package:flutter/material.dart';
import 'package:intl/intl.dart';
import 'package:provider/provider.dart';
import '../../models/pemeriksaan.dart';
import '../../services/database_service.dart';
import '../../providers/auth_provider.dart';
import 'pemeriksaan_form_screen.dart';
import 'pemeriksaan_detail_screen.dart';

class PemeriksaanListScreen extends StatefulWidget {
  const PemeriksaanListScreen({super.key});

  @override
  State<PemeriksaanListScreen> createState() => _PemeriksaanListScreenState();
}

class _PemeriksaanListScreenState extends State<PemeriksaanListScreen> with SingleTickerProviderStateMixin {
  final DatabaseService _db = DatabaseService();
  final TextEditingController _searchController = TextEditingController();
  late TabController _tabController;

  List<Pemeriksaan> _list = [];
  bool _isLoading = true;

  @override
  void initState() {
    super.initState();
    _tabController = TabController(length: 3, vsync: this);
    _tabController.addListener(() => _loadData());
    _loadData();
  }

  @override
  void dispose() {
    _tabController.dispose();
    _searchController.dispose();
    super.dispose();
  }

  Future<void> _loadData() async {
    setState(() => _isLoading = true);
    try {
      String? filterJenis;
      if (_tabController.index == 1) filterJenis = 'KONSULTASI_PRE';
      if (_tabController.index == 2) filterJenis = 'CONTROL_POST';

      final data = await _db.getPemeriksaanList(
        search: _searchController.text.trim(),
        jenis: filterJenis,
      );
      setState(() {
        _list = data;
        _isLoading = false;
      });
    } catch (e) {
      setState(() => _isLoading = false);
    }
  }

  void _confirmDelete(Pemeriksaan item) {
    showDialog(
      context: context,
      builder: (ctx) => AlertDialog(
        title: const Text('Hapus Pemeriksaan'),
        content: Text('Yakin menghapus pemeriksaan pasien "${item.pasienNama}"?'),
        actions: [
          TextButton(onPressed: () => Navigator.pop(ctx), child: const Text('Batal')),
          ElevatedButton(
            style: ElevatedButton.styleFrom(backgroundColor: Colors.red, foregroundColor: Colors.white),
            onPressed: () async {
              final user = Provider.of<AuthProvider>(context, listen: false).currentAdmin?.username ?? 'Admin';
              await _db.deletePemeriksaan(item.id, user);
              if (mounted) {
                Navigator.pop(ctx);
                _loadData();
              }
            },
            child: const Text('Hapus'),
          ),
        ],
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    final dateFormat = DateFormat('dd MMM yyyy');

    return Scaffold(
      appBar: AppBar(
        title: const Text('Pemeriksaan Pasien'),
        backgroundColor: const Color(0xFF004D40),
        foregroundColor: Colors.white,
        bottom: TabBar(
          controller: _tabController,
          indicatorColor: Colors.amber,
          labelColor: Colors.white,
          unselectedLabelColor: Colors.white70,
          tabs: const [
            Tab(text: 'Semua'),
            Tab(text: 'Konsultasi Pre'),
            Tab(text: 'Control Post'),
          ],
        ),
      ),
      floatingActionButton: FloatingActionButton.extended(
        backgroundColor: const Color(0xFF004D40),
        foregroundColor: Colors.white,
        icon: const Icon(Icons.add),
        label: const Text('Buat Pemeriksaan'),
        onPressed: () async {
          final res = await Navigator.push(
            context,
            MaterialPageRoute(builder: (_) => const PemeriksaanFormScreen()),
          );
          if (res == true) _loadData();
        },
      ),
      body: Column(
        children: [
          Padding(
            padding: const EdgeInsets.all(12.0),
            child: TextField(
              controller: _searchController,
              decoration: InputDecoration(
                hintText: 'Cari Nama Pasien / No RM...',
                prefixIcon: const Icon(Icons.search),
                border: OutlineInputBorder(borderRadius: BorderRadius.circular(12)),
              ),
              onSubmitted: (_) => _loadData(),
            ),
          ),
          Expanded(
            child: _isLoading
                ? const Center(child: CircularProgressIndicator())
                : _list.isEmpty
                    ? const Center(child: Text('Belum ada data pemeriksaan'))
                    : ListView.builder(
                        itemCount: _list.length,
                        itemBuilder: (context, index) {
                          final item = _list[index];
                          final isPre = item.jenisPemeriksaan == 'KONSULTASI_PRE';
                          return Card(
                            margin: const EdgeInsets.symmetric(horizontal: 12, vertical: 6),
                            child: ListTile(
                              onTap: () {
                                Navigator.push(
                                  context,
                                  MaterialPageRoute(builder: (_) => PemeriksaanDetailScreen(item: item)),
                                );
                              },
                              leading: Container(
                                padding: const EdgeInsets.all(8),
                                decoration: BoxDecoration(
                                  color: isPre ? Colors.blue.withOpacity(0.15) : Colors.orange.withOpacity(0.15),
                                  shape: BoxShape.circle,
                                ),
                                child: Icon(
                                  isPre ? Icons.medical_services_outlined : Icons.healing_outlined,
                                  color: isPre ? Colors.blue[800] : Colors.orange[800],
                                ),
                              ),
                              title: Text(item.pasienNama ?? 'Pasien ID ${item.pasienId}', style: const TextStyle(fontWeight: FontWeight.bold)),
                              subtitle: Column(
                                crossAxisAlignment: CrossAxisAlignment.start,
                                children: [
                                  Text('${isPre ? "Konsultasi Pre-Op" : "Control Post-Op"} • ${dateFormat.format(item.tanggal)}'),
                                  if (item.tindakanNama != null) Text('Tindakan: ${item.tindakanNama}', style: const TextStyle(fontSize: 12)),
                                ],
                              ),
                              trailing: Row(
                                mainAxisSize: MainAxisSize.min,
                                children: [
                                  IconButton(
                                    icon: const Icon(Icons.edit, color: Colors.blue),
                                    onPressed: () async {
                                      final res = await Navigator.push(
                                        context,
                                        MaterialPageRoute(builder: (_) => PemeriksaanFormScreen(item: item)),
                                      );
                                      if (res == true) _loadData();
                                    },
                                  ),
                                  IconButton(
                                    icon: const Icon(Icons.delete, color: Colors.red),
                                    onPressed: () => _confirmDelete(item),
                                  ),
                                ],
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
