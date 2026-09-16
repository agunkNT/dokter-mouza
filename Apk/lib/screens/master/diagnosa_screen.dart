import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../../models/diagnosa.dart';
import '../../services/database_service.dart';
import '../../providers/auth_provider.dart';

class DiagnosaScreen extends StatefulWidget {
  const DiagnosaScreen({super.key});

  @override
  State<DiagnosaScreen> createState() => _DiagnosaScreenState();
}

class _DiagnosaScreenState extends State<DiagnosaScreen> {
  final DatabaseService _db = DatabaseService();
  final TextEditingController _searchController = TextEditingController();
  List<Diagnosa> _list = [];
  bool _isLoading = true;

  @override
  void initState() {
    super.initState();
    _loadData();
  }

  Future<void> _loadData() async {
    setState(() => _isLoading = true);
    try {
      final data = await _db.getDiagnosaList(search: _searchController.text.trim());
      setState(() {
        _list = data;
        _isLoading = false;
      });
    } catch (e) {
      setState(() => _isLoading = false);
    }
  }

  void _showFormModal([Diagnosa? item]) {
    final icdController = TextEditingController(text: item?.kodeICD ?? '');
    final nameController = TextEditingController(text: item?.nama ?? '');

    showDialog(
      context: context,
      builder: (context) {
        return AlertDialog(
          title: Text(item == null ? 'Tambah Diagnosa' : 'Edit Diagnosa'),
          content: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              TextField(controller: icdController, decoration: const InputDecoration(labelText: 'Kode ICD *')),
              const SizedBox(height: 12),
              TextField(controller: nameController, decoration: const InputDecoration(labelText: 'Nama Diagnosa *')),
            ],
          ),
          actions: [
            TextButton(onPressed: () => Navigator.pop(context), child: const Text('Batal')),
            ElevatedButton(
              style: ElevatedButton.styleFrom(backgroundColor: const Color(0xFF004D40), foregroundColor: Colors.white),
              onPressed: () async {
                if (nameController.text.trim().isEmpty) return;
                final user = Provider.of<AuthProvider>(context, listen: false).currentAdmin?.username ?? 'Admin';
                await _db.saveDiagnosa(
                  id: item?.id,
                  kodeICD: icdController.text.trim(),
                  nama: nameController.text.trim(),
                  user: user,
                );
                if (mounted) {
                  Navigator.pop(context);
                  _loadData();
                }
              },
              child: const Text('Simpan'),
            ),
          ],
        );
      },
    );
  }

  void _confirmDelete(Diagnosa item) {
    showDialog(
      context: context,
      builder: (ctx) => AlertDialog(
        title: const Text('Hapus Diagnosa'),
        content: Text('Yakin menghapus "${item.nama}"?'),
        actions: [
          TextButton(onPressed: () => Navigator.pop(ctx), child: const Text('Batal')),
          ElevatedButton(
            style: ElevatedButton.styleFrom(backgroundColor: Colors.red, foregroundColor: Colors.white),
            onPressed: () async {
              final user = Provider.of<AuthProvider>(context, listen: false).currentAdmin?.username ?? 'Admin';
              await _db.deleteDiagnosa(item.id, user);
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
    return Scaffold(
      appBar: AppBar(
        title: const Text('Master Diagnosa (ICD)'),
        backgroundColor: const Color(0xFF004D40),
        foregroundColor: Colors.white,
      ),
      floatingActionButton: FloatingActionButton(
        backgroundColor: const Color(0xFF004D40),
        foregroundColor: Colors.white,
        onPressed: () => _showFormModal(),
        child: const Icon(Icons.add),
      ),
      body: Column(
        children: [
          Padding(
            padding: const EdgeInsets.all(12.0),
            child: TextField(
              controller: _searchController,
              decoration: InputDecoration(
                hintText: 'Cari Kode ICD / Nama Diagnosa...',
                prefixIcon: const Icon(Icons.search),
                border: OutlineInputBorder(borderRadius: BorderRadius.circular(12)),
              ),
              onSubmitted: (_) => _loadData(),
            ),
          ),
          Expanded(
            child: _isLoading
                ? const Center(child: CircularProgressIndicator())
                : ListView.builder(
                    itemCount: _list.length,
                    itemBuilder: (context, index) {
                      final item = _list[index];
                      return Card(
                        margin: const EdgeInsets.symmetric(horizontal: 12, vertical: 6),
                        child: ListTile(
                          leading: Container(
                            padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
                            decoration: BoxDecoration(
                              color: const Color(0xFF004D40).withOpacity(0.1),
                              borderRadius: BorderRadius.circular(8),
                            ),
                            child: Text(
                              item.kodeICD,
                              style: const TextStyle(color: Color(0xFF004D40), fontWeight: FontWeight.bold),
                            ),
                          ),
                          title: Text(item.nama, style: const TextStyle(fontWeight: FontWeight.bold)),
                          trailing: Row(
                            mainAxisSize: MainAxisSize.min,
                            children: [
                              IconButton(icon: const Icon(Icons.edit, color: Colors.blue), onPressed: () => _showFormModal(item)),
                              IconButton(icon: const Icon(Icons.delete, color: Colors.red), onPressed: () => _confirmDelete(item)),
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
