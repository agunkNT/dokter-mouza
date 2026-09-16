import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../../models/pasien.dart';
import '../../services/database_service.dart';
import '../../providers/auth_provider.dart';

class PasienScreen extends StatefulWidget {
  const PasienScreen({super.key});

  @override
  State<PasienScreen> createState() => _PasienScreenState();
}

class _PasienScreenState extends State<PasienScreen> {
  final DatabaseService _db = DatabaseService();
  final TextEditingController _searchController = TextEditingController();
  final ScrollController _scrollController = ScrollController();

  List<Pasien> _list = [];
  bool _isLoading = true;
  bool _isLoadingMore = false;
  bool _hasMore = true;
  int _offset = 0;
  static const int _limit = 25;

  @override
  void initState() {
    super.initState();
    _loadInitialData();
    _scrollController.addListener(_onScroll);
  }

  @override
  void dispose() {
    _searchController.dispose();
    _scrollController.dispose();
    super.dispose();
  }

  void _onScroll() {
    if (_scrollController.position.pixels >= _scrollController.position.maxScrollExtent - 200 &&
        !_isLoadingMore &&
        !_isLoading &&
        _hasMore) {
      _loadMoreData();
    }
  }

  Future<void> _loadInitialData() async {
    setState(() {
      _isLoading = true;
      _offset = 0;
      _hasMore = true;
      _list.clear();
    });

    try {
      final data = await _db.getPasienList(
        search: _searchController.text.trim(),
        limit: _limit,
        offset: 0,
      );
      setState(() {
        _list = data;
        _isLoading = false;
        _hasMore = data.length == _limit;
      });
    } catch (e) {
      setState(() => _isLoading = false);
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text('Error: $e')));
      }
    }
  }

  Future<void> _loadMoreData() async {
    setState(() => _isLoadingMore = true);
    _offset += _limit;

    try {
      final data = await _db.getPasienList(
        search: _searchController.text.trim(),
        limit: _limit,
        offset: _offset,
      );
      setState(() {
        _list.addAll(data);
        _isLoadingMore = false;
        _hasMore = data.length == _limit;
      });
    } catch (e) {
      setState(() => _isLoadingMore = false);
    }
  }

  void _showFormModal([Pasien? item]) {
    final nameController = TextEditingController(text: item?.nama ?? '');
    final noRMController = TextEditingController(text: item?.noRM ?? '');
    final alamatController = TextEditingController(text: item?.alamat ?? '');
    final usiaController = TextEditingController(text: item?.usia ?? '');
    String jenisKelamin = item?.jenisKelamin.isNotEmpty == true ? item!.jenisKelamin : 'Laki-laki';
    DateTime? tglLahir = item?.tanggalLahir;

    showDialog(
      context: context,
      builder: (context) {
        return StatefulBuilder(
          builder: (context, setModalState) {
            return AlertDialog(
              shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
              title: Text(item == null ? 'Tambah Pasien' : 'Edit Pasien'),
              content: SingleChildScrollView(
                child: Column(
                  mainAxisSize: MainAxisSize.min,
                  children: [
                    TextField(
                      controller: nameController,
                      decoration: const InputDecoration(labelText: 'Nama Pasien *'),
                    ),
                    const SizedBox(height: 12),
                    TextField(
                      controller: noRMController,
                      decoration: const InputDecoration(labelText: 'No. Rekam Medis (RM)'),
                    ),
                    const SizedBox(height: 12),
                    DropdownButtonFormField<String>(
                      value: jenisKelamin,
                      decoration: const InputDecoration(labelText: 'Jenis Kelamin'),
                      items: const [
                        DropdownMenuItem(value: 'Laki-laki', child: Text('Laki-laki')),
                        DropdownMenuItem(value: 'Perempuan', child: Text('Perempuan')),
                      ],
                      onChanged: (val) {
                        if (val != null) setModalState(() => jenisKelamin = val);
                      },
                    ),
                    const SizedBox(height: 12),
                    TextField(
                      controller: usiaController,
                      decoration: const InputDecoration(labelText: 'Usia (Thn/Bln)'),
                    ),
                    const SizedBox(height: 12),
                    TextField(
                      controller: alamatController,
                      decoration: const InputDecoration(labelText: 'Alamat'),
                      maxLines: 2,
                    ),
                  ],
                ),
              ),
              actions: [
                TextButton(
                  onPressed: () => Navigator.pop(context),
                  child: const Text('Batal'),
                ),
                ElevatedButton(
                  style: ElevatedButton.styleFrom(backgroundColor: const Color(0xFF004D40), foregroundColor: Colors.white),
                  onPressed: () async {
                    if (nameController.text.trim().isEmpty) return;
                    final user = Provider.of<AuthProvider>(context, listen: false).currentAdmin?.username ?? 'Admin';
                    await _db.savePasien(
                      id: item?.id,
                      nama: nameController.text.trim(),
                      noRM: noRMController.text.trim(),
                      jenisKelamin: jenisKelamin,
                      alamat: alamatController.text.trim(),
                      tanggalLahir: tglLahir,
                      usia: usiaController.text.trim(),
                      user: user,
                    );
                    if (mounted) {
                      Navigator.pop(context);
                      _loadInitialData();
                    }
                  },
                  child: const Text('Simpan'),
                ),
              ],
            );
          },
        );
      },
    );
  }

  void _confirmDelete(Pasien item) {
    showDialog(
      context: context,
      builder: (ctx) => AlertDialog(
        title: const Text('Hapus Pasien'),
        content: Text('Yakin ingin menghapus pasien "${item.nama}"?'),
        actions: [
          TextButton(onPressed: () => Navigator.pop(ctx), child: const Text('Batal')),
          ElevatedButton(
            style: ElevatedButton.styleFrom(backgroundColor: Colors.red, foregroundColor: Colors.white),
            onPressed: () async {
              final user = Provider.of<AuthProvider>(context, listen: false).currentAdmin?.username ?? 'Admin';
              await _db.deletePasien(item.id, user);
              if (mounted) {
                Navigator.pop(ctx);
                _loadInitialData();
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
        title: const Text('Master Data Pasien'),
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
                hintText: 'Cari Nama Pasien atau No. RM...',
                prefixIcon: const Icon(Icons.search),
                suffixIcon: IconButton(
                  icon: const Icon(Icons.clear),
                  onPressed: () {
                    _searchController.clear();
                    _loadInitialData();
                  },
                ),
                border: OutlineInputBorder(borderRadius: BorderRadius.circular(12)),
              ),
              onSubmitted: (_) => _loadInitialData(),
            ),
          ),
          Expanded(
            child: _isLoading
                ? const Center(child: CircularProgressIndicator())
                : _list.isEmpty
                    ? const Center(child: Text('Belum ada data pasien'))
                    : RefreshIndicator(
                        onRefresh: _loadInitialData,
                        child: ListView.builder(
                          controller: _scrollController,
                          itemCount: _list.length + (_isLoadingMore ? 1 : 0),
                          itemBuilder: (context, index) {
                            if (index == _list.length) {
                              return const Padding(
                                padding: EdgeInsets.symmetric(vertical: 16.0),
                                child: Center(child: CircularProgressIndicator()),
                              );
                            }

                            final item = _list[index];
                            return Card(
                              margin: const EdgeInsets.symmetric(horizontal: 12, vertical: 6),
                              shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
                              child: ListTile(
                                leading: CircleAvatar(
                                  backgroundColor: const Color(0xFF004D40).withOpacity(0.1),
                                  child: Text(
                                    item.nama.isNotEmpty ? item.nama[0].toUpperCase() : 'P',
                                    style: const TextStyle(color: Color(0xFF004D40), fontWeight: FontWeight.bold),
                                  ),
                                ),
                                title: Text(item.nama, style: const TextStyle(fontWeight: FontWeight.bold)),
                                subtitle: Text('No RM: ${item.noRM} | JK: ${item.jenisKelamin} | Usia: ${item.usia ?? "-"}'),
                                trailing: Row(
                                  mainAxisSize: MainAxisSize.min,
                                  children: [
                                    IconButton(
                                      icon: const Icon(Icons.edit, color: Colors.blue),
                                      onPressed: () => _showFormModal(item),
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
          ),
        ],
      ),
    );
  }
}
