import 'package:flutter/material.dart';
import 'package:intl/intl.dart';
import 'package:provider/provider.dart';
import 'package:fl_chart/fl_chart.dart';
import '../providers/auth_provider.dart';
import '../services/database_service.dart';
import 'login_screen.dart';
import 'change_password_screen.dart';
import 'master/pasien_screen.dart';
import 'master/dokter_screen.dart';
import 'master/rumah_sakit_screen.dart';
import 'master/diagnosa_screen.dart';
import 'master/tindakan_screen.dart';
import 'master/jenis_tindakan_screen.dart';
import 'laporan_operasi/laporan_operasi_list_screen.dart';
import 'pemeriksaan/pemeriksaan_list_screen.dart';

class DashboardScreen extends StatefulWidget {
  const DashboardScreen({super.key});

  @override
  State<DashboardScreen> createState() => _DashboardScreenState();
}

class _DashboardScreenState extends State<DashboardScreen> {
  final DatabaseService _db = DatabaseService();
  int _totalPasien = 0;
  int _totalOperasi = 0;
  int _totalPemeriksaan = 0;
  List<Map<String, dynamic>> _yearlyStats = [];
  Map<String, int> _flagStats = {};
  bool _isLoading = true;

  // Period Filters
  String _selectedPeriod = 'all'; // 'all', 'today', 'month', 'year', 'custom'
  DateTime? _customStartDate;
  DateTime? _customEndDate;

  @override
  void initState() {
    super.initState();
    _loadStats();
  }

  Future<void> _loadStats() async {
    setState(() => _isLoading = true);
    try {
      final stats = await _db.getDashboardStats(
        period: _selectedPeriod,
        startDate: _customStartDate,
        endDate: _customEndDate,
      );
      setState(() {
        _totalPasien = stats['totalPasien'] ?? 0;
        _totalOperasi = stats['totalOperasi'] ?? 0;
        _totalPemeriksaan = stats['totalPemeriksaan'] ?? 0;
        _yearlyStats = List<Map<String, dynamic>>.from(stats['yearlyStats'] ?? []);
        _flagStats = Map<String, int>.from(stats['flagStats'] ?? {});
        _isLoading = false;
      });
    } catch (e) {
      setState(() => _isLoading = false);
    }
  }

  void _handleLogout() async {
    final authProvider = Provider.of<AuthProvider>(context, listen: false);
    await authProvider.logout();
    if (mounted) {
      Navigator.of(context).pushReplacement(
        MaterialPageRoute(builder: (_) => const LoginScreen()),
      );
    }
  }

  Future<void> _selectCustomDateRange() async {
    final picked = await showDateRangePicker(
      context: context,
      firstDate: DateTime(2020),
      lastDate: DateTime.now(),
      initialDateRange: _customStartDate != null && _customEndDate != null
          ? DateTimeRange(start: _customStartDate!, end: _customEndDate!)
          : DateTimeRange(start: DateTime.now().subtract(const Duration(days: 30)), end: DateTime.now()),
    );
    if (picked != null) {
      setState(() {
        _selectedPeriod = 'custom';
        _customStartDate = picked.start;
        _customEndDate = picked.end;
      });
      _loadStats();
    }
  }

  @override
  Widget build(BuildContext context) {
    final authProvider = Provider.of<AuthProvider>(context);
    final adminName = authProvider.currentAdmin?.username ?? 'Admin';
    final dateFormat = DateFormat('dd/MM/yyyy');

    return Scaffold(
      appBar: AppBar(
        title: const Text('MAN APPS'),
        backgroundColor: const Color(0xFF004D40),
        foregroundColor: Colors.white,
        actions: [
          IconButton(
            icon: const Icon(Icons.refresh),
            onPressed: _loadStats,
            tooltip: 'Refresh',
          ),
          PopupMenuButton<String>(
            onSelected: (value) {
              if (value == 'change_password') {
                Navigator.push(context, MaterialPageRoute(builder: (_) => const ChangePasswordScreen()));
              } else if (value == 'logout') {
                _handleLogout();
              }
            },
            itemBuilder: (ctx) => [
              PopupMenuItem(
                enabled: false,
                child: Text('Login sebagai: $adminName', style: const TextStyle(fontWeight: FontWeight.bold, color: Color(0xFF004D40))),
              ),
              const PopupMenuDivider(),
              const PopupMenuItem(
                value: 'change_password',
                child: Row(
                  children: [Icon(Icons.lock_reset, color: Colors.teal), SizedBox(width: 8), Text('Ubah Password')],
                ),
              ),
              const PopupMenuItem(
                value: 'logout',
                child: Row(
                  children: [Icon(Icons.logout, color: Colors.red), SizedBox(width: 8), Text('Keluar / Logout')],
                ),
              ),
            ],
          ),
        ],
      ),
      drawer: _buildDrawer(adminName),
      body: RefreshIndicator(
        onRefresh: _loadStats,
        child: SingleChildScrollView(
          physics: const AlwaysScrollableScrollPhysics(),
          padding: const EdgeInsets.all(16.0),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              // Welcome Card
              Card(
                elevation: 4,
                shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
                color: const Color(0xFF004D40),
                child: Padding(
                  padding: const EdgeInsets.all(20.0),
                  child: Row(
                    children: [
                      const CircleAvatar(
                        radius: 28,
                        backgroundColor: Colors.white24,
                        child: Icon(Icons.person, size: 36, color: Colors.white),
                      ),
                      const SizedBox(width: 16),
                      Expanded(
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Text(
                              'Selamat Datang, $adminName',
                              style: const TextStyle(color: Colors.white, fontSize: 18, fontWeight: FontWeight.bold),
                            ),
                            const SizedBox(height: 4),
                            const Text(
                              'Sistem Informasi Laporan Operasi & Rekam Medis',
                              style: TextStyle(color: Colors.white70, fontSize: 13),
                            ),
                          ],
                        ),
                      ),
                    ],
                  ),
                ),
              ),
              const SizedBox(height: 24),

              const Text(
                'Ringkasan Data',
                style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold, color: Color(0xFF004D40)),
              ),
              const SizedBox(height: 12),

              // Summary Stats Row
              Row(
                children: [
                  Expanded(
                    child: _buildStatCard(
                      'Total Pasien',
                      _totalPasien.toString(),
                      Icons.people_alt_outlined,
                      Colors.blue,
                      () => Navigator.push(context, MaterialPageRoute(builder: (_) => const PasienScreen())),
                    ),
                  ),
                  const SizedBox(width: 10),
                  Expanded(
                    child: _buildStatCard(
                      'Laporan Operasi',
                      _totalOperasi.toString(),
                      Icons.medical_services_outlined,
                      Colors.teal,
                      () => Navigator.push(context, MaterialPageRoute(builder: (_) => const LaporanOperasiListScreen())),
                    ),
                  ),
                  const SizedBox(width: 10),
                  Expanded(
                    child: _buildStatCard(
                      'Pemeriksaan',
                      _totalPemeriksaan.toString(),
                      Icons.healing_outlined,
                      Colors.orange,
                      () => Navigator.push(context, MaterialPageRoute(builder: (_) => const PemeriksaanListScreen())),
                    ),
                  ),
                ],
              ),
              const SizedBox(height: 28),

              // CHARTS SECTION & PERIOD FILTER
              Row(
                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                children: const [
                  Text(
                    'Grafik & Analisis Operasi',
                    style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold, color: Color(0xFF004D40)),
                  ),
                ],
              ),
              const SizedBox(height: 10),

              // Filter Chips
              SingleChildScrollView(
                scrollDirection: Axis.horizontal,
                child: Row(
                  children: [
                    _buildFilterChip('Semua Periode', 'all'),
                    const SizedBox(width: 8),
                    _buildFilterChip('Hari Ini', 'today'),
                    const SizedBox(width: 8),
                    _buildFilterChip('Sebulan Lalu', 'month'),
                    const SizedBox(width: 8),
                    _buildFilterChip('1 Tahun Terakhir', 'year'),
                    const SizedBox(width: 8),
                    ActionChip(
                      avatar: const Icon(Icons.date_range, size: 16, color: Color(0xFF004D40)),
                      label: Text(
                        _selectedPeriod == 'custom' && _customStartDate != null && _customEndDate != null
                            ? '${dateFormat.format(_customStartDate!)} - ${dateFormat.format(_customEndDate!)}'
                            : 'Pilih Tanggal',
                        style: TextStyle(
                          color: _selectedPeriod == 'custom' ? const Color(0xFF004D40) : Colors.black87,
                          fontWeight: _selectedPeriod == 'custom' ? FontWeight.bold : FontWeight.normal,
                          fontSize: 12,
                        ),
                      ),
                      backgroundColor: _selectedPeriod == 'custom' ? const Color(0xFF004D40).withValues(alpha: 0.15) : Colors.grey[200],
                      onPressed: _selectCustomDateRange,
                    ),
                  ],
                ),
              ),
              const SizedBox(height: 16),

              // 1. Bar Chart: Yearly / Monthly Volume
              _buildYearlyBarChartCard(),
              const SizedBox(height: 16),

              // 2. Pie Chart: Indicators Distribution
              _buildFlagDistributionPieCard(),
              const SizedBox(height: 28),

              const Text(
                'Menu Utama',
                style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold, color: Color(0xFF004D40)),
              ),
              const SizedBox(height: 12),

              // Main Modules Grid
              GridView.count(
                shrinkWrap: true,
                physics: const NeverScrollableScrollPhysics(),
                crossAxisCount: 2,
                crossAxisSpacing: 12,
                mainAxisSpacing: 12,
                childAspectRatio: 1.3,
                children: [
                  _buildMenuCard(
                    'Laporan Operasi',
                    'Kelola & Catat Hasil Operasi',
                    Icons.description_outlined,
                    const Color(0xFF004D40),
                    () => Navigator.push(context, MaterialPageRoute(builder: (_) => const LaporanOperasiListScreen())),
                  ),
                  _buildMenuCard(
                    'Pemeriksaan Pasien',
                    'Pre-Op & Post-Op Control',
                    Icons.assignment_outlined,
                    Colors.deepOrange,
                    () => Navigator.push(context, MaterialPageRoute(builder: (_) => const PemeriksaanListScreen())),
                  ),
                  _buildMenuCard(
                    'Data Pasien',
                    'Master Pasien & Rekam Medis',
                    Icons.badge_outlined,
                    Colors.indigo,
                    () => Navigator.push(context, MaterialPageRoute(builder: (_) => const PasienScreen())),
                  ),
                  _buildMenuCard(
                    'Data Dokter',
                    'Master Operator & DPJP',
                    Icons.health_and_safety_outlined,
                    Colors.teal,
                    () => Navigator.push(context, MaterialPageRoute(builder: (_) => const DokterScreen())),
                  ),
                  _buildMenuCard(
                    'Master Rumah Sakit',
                    'Lokasi Operasi',
                    Icons.local_hospital_outlined,
                    Colors.purple,
                    () => Navigator.push(context, MaterialPageRoute(builder: (_) => const RumahSakitScreen())),
                  ),
                  _buildMenuCard(
                    'Master ICD Diagnosa',
                    'Kode ICD-10 & Diagnosa',
                    Icons.biotech_outlined,
                    Colors.brown,
                    () => Navigator.push(context, MaterialPageRoute(builder: (_) => const DiagnosaScreen())),
                  ),
                ],
              ),
            ],
          ),
        ),
      ),
    );
  }

  Widget _buildFilterChip(String label, String value) {
    final isSelected = _selectedPeriod == value;
    return ChoiceChip(
      label: Text(label, style: TextStyle(color: isSelected ? Colors.white : Colors.black87, fontSize: 12, fontWeight: isSelected ? FontWeight.bold : FontWeight.normal)),
      selected: isSelected,
      selectedColor: const Color(0xFF004D40),
      backgroundColor: Colors.grey[200],
      onSelected: (bool selected) {
        if (selected) {
          setState(() => _selectedPeriod = value);
          _loadStats();
        }
      },
    );
  }

  Widget _buildYearlyBarChartCard() {
    double maxY = 10;
    for (var item in _yearlyStats) {
      final val = (item['count'] as num).toDouble();
      if (val > maxY) maxY = val;
    }
    maxY = (maxY * 1.25).ceilToDouble();

    return Card(
      elevation: 3,
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
      child: Padding(
        padding: const EdgeInsets.all(16.0),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                const Icon(Icons.bar_chart, color: Color(0xFF004D40)),
                const SizedBox(width: 8),
                Text(
                  _selectedPeriod == 'year' || _selectedPeriod == 'month'
                      ? 'Tren Operasi Per Bulan'
                      : _selectedPeriod == 'today'
                          ? 'Tren Operasi Hari Ini'
                          : 'Volume Operasi Per Tahun',
                  style: const TextStyle(fontSize: 15, fontWeight: FontWeight.bold),
                ),
              ],
            ),
            const SizedBox(height: 20),
            _isLoading
                ? const SizedBox(height: 180, child: Center(child: CircularProgressIndicator()))
                : _yearlyStats.isEmpty
                    ? const SizedBox(height: 100, child: Center(child: Text('Data statistik belum tersedia pada periode ini')))
                    : SizedBox(
                        height: 200,
                        child: BarChart(
                          BarChartData(
                            alignment: BarChartAlignment.spaceAround,
                            maxY: maxY,
                            barTouchData: BarTouchData(
                              enabled: true,
                              touchTooltipData: BarTouchTooltipData(
                                getTooltipItem: (group, groupIndex, rod, rodIndex) {
                                  final year = _yearlyStats[groupIndex]['year'];
                                  final val = rod.toY.toInt();
                                  return BarTooltipItem('$year: $val kasus', const TextStyle(color: Colors.white, fontWeight: FontWeight.bold));
                                },
                              ),
                            ),
                            titlesData: FlTitlesData(
                              show: true,
                              bottomTitles: AxisTitles(
                                sideTitles: SideTitles(
                                  showTitles: true,
                                  getTitlesWidget: (val, meta) {
                                    final idx = val.toInt();
                                    if (idx >= 0 && idx < _yearlyStats.length) {
                                      return Padding(
                                        padding: const EdgeInsets.only(top: 6.0),
                                        child: Text(
                                          _yearlyStats[idx]['year'].toString(),
                                          style: const TextStyle(fontSize: 10, fontWeight: FontWeight.bold, color: Colors.black87),
                                        ),
                                      );
                                    }
                                    return const Text('');
                                  },
                                ),
                              ),
                              leftTitles: AxisTitles(
                                sideTitles: SideTitles(
                                  showTitles: true,
                                  reservedSize: 32,
                                  getTitlesWidget: (val, meta) {
                                    return Text(val.toInt().toString(), style: const TextStyle(fontSize: 10, color: Colors.grey));
                                  },
                                ),
                              ),
                              topTitles: const AxisTitles(sideTitles: SideTitles(showTitles: false)),
                              rightTitles: const AxisTitles(sideTitles: SideTitles(showTitles: false)),
                            ),
                            gridData: FlGridData(
                              show: true,
                              drawVerticalLine: false,
                              horizontalInterval: (maxY / 4).clamp(1, 100),
                              getDrawingHorizontalLine: (value) => FlLine(color: Colors.grey[200]!, strokeWidth: 1),
                            ),
                            borderData: FlBorderData(show: false),
                            barGroups: List.generate(_yearlyStats.length, (i) {
                              final count = (_yearlyStats[i]['count'] as num).toDouble();
                              return BarChartGroupData(
                                x: i,
                                barRods: [
                                  BarChartRodData(
                                    toY: count,
                                    color: const Color(0xFF006A67),
                                    width: _yearlyStats.length > 8 ? 12 : 18,
                                    borderRadius: const BorderRadius.vertical(top: Radius.circular(6)),
                                  ),
                                ],
                              );
                            }),
                          ),
                        ),
                      ),
          ],
        ),
      ),
    );
  }

  Widget _buildFlagDistributionPieCard() {
    final h = _flagStats['hidrosefalus'] ?? 0;
    final s = _flagStats['shunt'] ?? 0;
    final e = _flagStats['etv'] ?? 0;
    final eg = _flagStats['etvGagalShunt'] ?? 0;
    final totalFlags = h + s + e + eg;

    return Card(
      elevation: 3,
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
      child: Padding(
        padding: const EdgeInsets.all(16.0),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: const [
                Icon(Icons.pie_chart_outline, color: Color(0xFF004D40)),
                SizedBox(width: 8),
                Text('Distribusi Indikator Klinis', style: TextStyle(fontSize: 15, fontWeight: FontWeight.bold)),
              ],
            ),
            const SizedBox(height: 16),
            _isLoading
                ? const SizedBox(height: 180, child: Center(child: CircularProgressIndicator()))
                : totalFlags == 0
                    ? const SizedBox(height: 100, child: Center(child: Text('Data indikator belum tersedia pada periode ini')))
                    : Row(
                        children: [
                          SizedBox(
                            width: 140,
                            height: 140,
                            child: PieChart(
                              PieChartData(
                                sectionsSpace: 2,
                                centerSpaceRadius: 35,
                                sections: [
                                  if (h > 0) PieChartSectionData(color: Colors.blue[700]!, value: h.toDouble(), title: '$h', radius: 30, titleStyle: const TextStyle(color: Colors.white, fontWeight: FontWeight.bold, fontSize: 11)),
                                  if (s > 0) PieChartSectionData(color: Colors.teal[600]!, value: s.toDouble(), title: '$s', radius: 30, titleStyle: const TextStyle(color: Colors.white, fontWeight: FontWeight.bold, fontSize: 11)),
                                  if (e > 0) PieChartSectionData(color: Colors.purple[600]!, value: e.toDouble(), title: '$e', radius: 30, titleStyle: const TextStyle(color: Colors.white, fontWeight: FontWeight.bold, fontSize: 11)),
                                  if (eg > 0) PieChartSectionData(color: Colors.orange[700]!, value: eg.toDouble(), title: '$eg', radius: 30, titleStyle: const TextStyle(color: Colors.white, fontWeight: FontWeight.bold, fontSize: 11)),
                                ],
                              ),
                            ),
                          ),
                          const SizedBox(width: 16),
                          Expanded(
                            child: Column(
                              crossAxisAlignment: CrossAxisAlignment.start,
                              children: [
                                _buildLegendItem('Hidrosefalus', '$h kasus', Colors.blue[700]!),
                                const SizedBox(height: 6),
                                _buildLegendItem('Shunt', '$s kasus', Colors.teal[600]!),
                                const SizedBox(height: 6),
                                _buildLegendItem('ETV', '$e kasus', Colors.purple[600]!),
                                const SizedBox(height: 6),
                                _buildLegendItem('ETV Gagal Shunt', '$eg kasus', Colors.orange[700]!),
                              ],
                            ),
                          ),
                        ],
                      ),
          ],
        ),
      ),
    );
  }

  Widget _buildLegendItem(String label, String value, Color color) {
    return Row(
      children: [
        Container(width: 12, height: 12, decoration: BoxDecoration(color: color, shape: BoxShape.circle)),
        const SizedBox(width: 8),
        Expanded(child: Text(label, style: const TextStyle(fontSize: 12, fontWeight: FontWeight.w600))),
        Text(value, style: TextStyle(fontSize: 12, fontWeight: FontWeight.bold, color: Colors.grey[800])),
      ],
    );
  }

  Widget _buildStatCard(String title, String value, IconData icon, Color color, VoidCallback onTap) {
    return InkWell(
      onTap: onTap,
      borderRadius: BorderRadius.circular(16),
      child: Card(
        elevation: 3,
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
        child: Padding(
          padding: const EdgeInsets.symmetric(vertical: 16.0, horizontal: 8.0),
          child: Column(
            children: [
              Icon(icon, color: color, size: 28),
              const SizedBox(height: 8),
              _isLoading
                  ? const SizedBox(height: 20, width: 20, child: CircularProgressIndicator(strokeWidth: 2))
                  : Text(value, style: TextStyle(fontSize: 22, fontWeight: FontWeight.bold, color: color)),
              const SizedBox(height: 4),
              Text(
                title,
                textAlign: TextAlign.center,
                style: const TextStyle(fontSize: 11, fontWeight: FontWeight.w600, color: Colors.grey),
              ),
            ],
          ),
        ),
      ),
    );
  }

  Widget _buildMenuCard(String title, String subtitle, IconData icon, Color color, VoidCallback onTap) {
    return Card(
      elevation: 3,
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
      child: InkWell(
        onTap: onTap,
        borderRadius: BorderRadius.circular(16),
        child: Padding(
          padding: const EdgeInsets.all(14.0),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              Container(
                padding: const EdgeInsets.all(8),
                decoration: BoxDecoration(
                  color: color.withValues(alpha: 0.12),
                  borderRadius: BorderRadius.circular(10),
                ),
                child: Icon(icon, color: color, size: 26),
              ),
              const SizedBox(height: 10),
              Text(title, style: const TextStyle(fontSize: 14, fontWeight: FontWeight.bold)),
              const SizedBox(height: 2),
              Text(subtitle, style: TextStyle(fontSize: 11, color: Colors.grey[600]), maxLines: 1, overflow: TextOverflow.ellipsis),
            ],
          ),
        ),
      ),
    );
  }

  Widget _buildDrawer(String adminName) {
    return Drawer(
      child: ListView(
        padding: EdgeInsets.zero,
        children: [
          UserAccountsDrawerHeader(
            decoration: const BoxDecoration(color: Color(0xFF004D40)),
            accountName: Text(adminName, style: const TextStyle(fontWeight: FontWeight.bold, fontSize: 16)),
            accountEmail: const Text('Administrator Systems'),
            currentAccountPicture: const CircleAvatar(
              backgroundColor: Colors.white,
              child: Icon(Icons.person, size: 40, color: Color(0xFF004D40)),
            ),
          ),
          ListTile(
            leading: const Icon(Icons.dashboard_outlined, color: Color(0xFF004D40)),
            title: const Text('Dashboard'),
            onTap: () => Navigator.pop(context),
          ),
          const Divider(),
          const Padding(
            padding: EdgeInsets.only(left: 16, top: 8, bottom: 4),
            child: Text('TRANSAKSI', style: TextStyle(color: Colors.grey, fontSize: 12, fontWeight: FontWeight.bold)),
          ),
          ListTile(
            leading: const Icon(Icons.medical_services_outlined, color: Colors.teal),
            title: const Text('Laporan Operasi'),
            onTap: () {
              Navigator.pop(context);
              Navigator.push(context, MaterialPageRoute(builder: (_) => const LaporanOperasiListScreen()));
            },
          ),
          ListTile(
            leading: const Icon(Icons.healing_outlined, color: Colors.orange),
            title: const Text('Pemeriksaan Pasien'),
            onTap: () {
              Navigator.pop(context);
              Navigator.push(context, MaterialPageRoute(builder: (_) => const PemeriksaanListScreen()));
            },
          ),
          const Divider(),
          const Padding(
            padding: EdgeInsets.only(left: 16, top: 8, bottom: 4),
            child: Text('MASTER DATA', style: TextStyle(color: Colors.grey, fontSize: 12, fontWeight: FontWeight.bold)),
          ),
          ListTile(
            leading: const Icon(Icons.people_outline),
            title: const Text('Master Pasien'),
            onTap: () {
              Navigator.pop(context);
              Navigator.push(context, MaterialPageRoute(builder: (_) => const PasienScreen()));
            },
          ),
          ListTile(
            leading: const Icon(Icons.health_and_safety_outlined),
            title: const Text('Master Dokter'),
            onTap: () {
              Navigator.pop(context);
              Navigator.push(context, MaterialPageRoute(builder: (_) => const DokterScreen()));
            },
          ),
          ListTile(
            leading: const Icon(Icons.local_hospital_outlined),
            title: const Text('Master Rumah Sakit'),
            onTap: () {
              Navigator.pop(context);
              Navigator.push(context, MaterialPageRoute(builder: (_) => const RumahSakitScreen()));
            },
          ),
          ListTile(
            leading: const Icon(Icons.biotech_outlined),
            title: const Text('Master Diagnosa (ICD)'),
            onTap: () {
              Navigator.pop(context);
              Navigator.push(context, MaterialPageRoute(builder: (_) => const DiagnosaScreen()));
            },
          ),
          ListTile(
            leading: const Icon(Icons.list_alt_outlined),
            title: const Text('Master Tindakan'),
            onTap: () {
              Navigator.pop(context);
              Navigator.push(context, MaterialPageRoute(builder: (_) => const TindakanScreen()));
            },
          ),
          ListTile(
            leading: const Icon(Icons.category_outlined),
            title: const Text('Master Jenis Tindakan'),
            onTap: () {
              Navigator.pop(context);
              Navigator.push(context, MaterialPageRoute(builder: (_) => const JenisTindakanScreen()));
            },
          ),
          const Divider(),
          ListTile(
            leading: const Icon(Icons.lock_reset, color: Colors.blue),
            title: const Text('Ubah Password'),
            onTap: () {
              Navigator.pop(context);
              Navigator.push(context, MaterialPageRoute(builder: (_) => const ChangePasswordScreen()));
            },
          ),
          ListTile(
            leading: const Icon(Icons.logout, color: Colors.red),
            title: const Text('Logout'),
            onTap: () {
              Navigator.pop(context);
              _handleLogout();
            },
          ),
        ],
      ),
    );
  }
}
