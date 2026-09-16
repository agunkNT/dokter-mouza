import 'package:flutter/material.dart';
import 'package:intl/intl.dart';
import 'package:provider/provider.dart';
import '../../models/laporan_operasi.dart';
import '../../services/database_service.dart';
import '../../providers/auth_provider.dart';
import 'laporan_operasi_form_screen.dart';
import 'laporan_operasi_detail_screen.dart';

class LaporanOperasiListScreen extends StatefulWidget {
  const LaporanOperasiListScreen({super.key});

  @override
  State<LaporanOperasiListScreen> createState() => _LaporanOperasiListScreenState();
}

class _LaporanOperasiListScreenState extends State<LaporanOperasiListScreen> {
  final DatabaseService _db = DatabaseService();
  final TextEditingController _searchController = TextEditingController();
  final ScrollController _scrollController = ScrollController();

  List<LaporanOperasi> _list = [];
  bool _isLoading = true;
  bool _isLoadingMore = false;
  bool _hasMore = true;
  int _offset = 0;
  static const int _limit = 20;

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
      final data = await _db.getLaporanOperasiList(
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
      final data = await _db.getLaporanOperasiList(
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

  void _confirmDelete(LaporanOperasi item) {
    showDialog(
      context: context,
      builder: (ctx) => AlertDialog(
        title: const Text('Hapus Laporan Operasi'),
        content: Text('Yakin menghapus laporan operasi untuk pasien "${item.pasienNama ?? item.noRM}"?'),
        actions: [
          TextButton(onPressed: () => Navigator.pop(ctx), child: const Text('Batal')),
          ElevatedButton(
            style: ElevatedButton.styleFrom(backgroundColor: Colors.red, foregroundColor: Colors.white),
            onPressed: () async {
              final user = Provider.of<AuthProvider>(context, listen: false).currentAdmin?.username ?? 'Admin';
              await _db.deleteLaporanOperasi(item.id, user);
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
    final dateFormat = DateFormat('dd MMM yyyy');

    return Scaffold(
      appBar: AppBar(
        title: const Text('Laporan Operasi'),
        backgroundColor: const Color(0xFF004D40),
        foregroundColor: Colors.white,
      ),
      floatingActionButton: FloatingActionButton.extended(
        backgroundColor: const Color(0xFF004D40),
        foregroundColor: Colors.white,
        icon: const Icon(Icons.add),
        label: const Text('Buat Laporan'),
        onPressed: () async {
          final res = await Navigator.push(
            context,
            MaterialPageRoute(builder: (_) => const LaporanOperasiFormScreen()),
          );
          if (res == true) _loadInitialData();
        },
      ),
      body: Column(
        children: [
          Padding(
            padding: const EdgeInsets.all(12.0),
            child: TextField(
              controller: _searchController,
              decoration: InputDecoration(
                hintText: 'Cari Nama Pasien / No. RM...',
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
                    ? const Center(child: Text('Belum ada laporan operasi'))
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
                              child: InkWell(
                                borderRadius: BorderRadius.circular(12),
                                onTap: () {
                                  Navigator.push(
                                    context,
                                    MaterialPageRoute(builder: (_) => LaporanOperasiDetailScreen(item: item)),
                                  );
                                },
                                child: Padding(
                                  padding: const EdgeInsets.all(14.0),
                                  child: Column(
                                    crossAxisAlignment: CrossAxisAlignment.start,
                                    children: [
                                      Row(
                                        mainAxisAlignment: MainAxisAlignment.spaceBetween,
                                        children: [
                                          Expanded(
                                            child: Text(
                                              item.pasienNama ?? 'Pasien No. ${item.noRM}',
                                              style: const TextStyle(
                                                fontWeight: FontWeight.bold,
                                                fontSize: 16,
                                                color: Color(0xFF004D40),
                                              ),
                                            ),
                                          ),
                                          Container(
                                            padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
                                            decoration: BoxDecoration(
                                              color: Colors.grey[200],
                                              borderRadius: BorderRadius.circular(6),
                                            ),
                                            child: Text(
                                              dateFormat.format(item.tanggal),
                                              style: const TextStyle(fontSize: 12, fontWeight: FontWeight.w600),
                                            ),
                                          ),
                                        ],
                                      ),
                                      const SizedBox(height: 6),
                                      Text('No RM: ${item.noRM} | RS: ${item.rumahSakitNama ?? "-"}'),
                                      Text('Operator: ${item.operatorNama ?? "-"} | DPJP: ${item.dpjpNama ?? "-"}'),
                                      const SizedBox(height: 8),
                                      Wrap(
                                        spacing: 6,
                                        runSpacing: 4,
                                        children: [
                                          if (item.hidrosefalus)
                                            _buildBadge('Hidrosefalus', Colors.blue),
                                          if (item.etv)
                                            _buildBadge('ETV', Colors.purple),
                                          if (item.shunt)
                                            _buildBadge('Shunt', Colors.teal),
                                          if (item.etvGagalShunt)
                                            _buildBadge('ETV Gagal Shunt', Colors.orange),
                                          if (item.gos != null)
                                            _buildBadge('GOS: ${item.gos}', Colors.green),
                                        ],
                                      ),
                                      const Divider(),
                                      Row(
                                        mainAxisAlignment: MainAxisAlignment.end,
                                        children: [
                                          IconButton(
                                            icon: const Icon(Icons.edit, color: Colors.blue),
                                            onPressed: () async {
                                              final res = await Navigator.push(
                                                context,
                                                MaterialPageRoute(builder: (_) => LaporanOperasiFormScreen(item: item)),
                                              );
                                              if (res == true) _loadInitialData();
                                            },
                                          ),
                                          IconButton(
                                            icon: const Icon(Icons.delete, color: Colors.red),
                                            onPressed: () => _confirmDelete(item),
                                          ),
                                        ],
                                      ),
                                    ],
                                  ),
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

  Widget _buildBadge(String label, Color color) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 3),
      decoration: BoxDecoration(
        color: color.withOpacity(0.12),
        borderRadius: BorderRadius.circular(6),
        border: Border.all(color: color.withOpacity(0.5)),
      ),
      child: Text(
        label,
        style: TextStyle(color: color, fontSize: 11, fontWeight: FontWeight.bold),
      ),
    );
  }
}
