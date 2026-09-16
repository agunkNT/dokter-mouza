import 'package:postgres/postgres.dart';
import '../models/admin.dart';
import '../models/pasien.dart';
import '../models/dokter.dart';
import '../models/rumah_sakit.dart';
import '../models/diagnosa.dart';
import '../models/tindakan.dart';
import '../models/jenis_tindakan.dart';
import '../models/laporan_operasi.dart';
import '../models/pemeriksaan.dart';

class DatabaseService {
  static final DatabaseService _instance = DatabaseService._internal();
  factory DatabaseService() => _instance;
  DatabaseService._internal();

  Connection? _connection;

  Future<Connection> get connection async {
    if (_connection != null && _connection!.isOpen) {
      return _connection!;
    }
    _connection = await _connect();
    return _connection!;
  }

  Future<Connection> _connect() async {
    final endpoint = Endpoint(
      host: '103.151.140.138',
      port: 5432,
      database: 'db_dokterabdurrahman',
      username: 'dokter_admin',
      password: r'P@ssw0rdDokter2026!',
    );

    return await Connection.open(
      endpoint,
      settings: const ConnectionSettings(
        sslMode: SslMode.disable,
      ),
    );
  }

  // --- ADMIN & AUTH ---
  Future<Admin?> getAdminByUsername(String username) async {
    final conn = await connection;
    final result = await conn.execute(
      Sql.named('SELECT * FROM "Admins" WHERE LOWER("Username") = LOWER(@username) AND "IsActive" = true LIMIT 1'),
      parameters: {'username': username},
    );

    if (result.isEmpty) return null;
    final row = result.first.toColumnMap();
    final map = <String, dynamic>{};
    row.forEach((key, value) => map[key.toLowerCase()] = value);
    return Admin.fromMap(map);
  }

  Future<bool> updateAdminPassword(String username, String newPasswordHash, String updatedBy) async {
    final conn = await connection;
    final result = await conn.execute(
      Sql.named('''
        UPDATE "Admins" 
        SET "Password" = @password, "UpdateBy" = @updateBy, "UpdateDate" = NOW() 
        WHERE LOWER("Username") = LOWER(@username)
      '''),
      parameters: {
        'username': username,
        'password': newPasswordHash,
        'updateBy': updatedBy,
      },
    );
    return result.affectedRows > 0;
  }

  Future<void> updateAdminLastLogin(String adminId) async {
    final conn = await connection;
    await conn.execute(
      Sql.named('UPDATE "Admins" SET "LastLogin" = NOW() WHERE "Id"::text = @id'),
      parameters: {'id': adminId},
    );
  }

  // --- DASHBOARD STATS & CHARTS ---
  Future<Map<String, dynamic>> getDashboardStats({
    String period = 'all', // 'all', 'today', 'month', 'year', 'custom'
    DateTime? startDate,
    DateTime? endDate,
  }) async {
    final conn = await connection;
    final resPasien = await conn.execute('SELECT COUNT(*) FROM "Pasien" WHERE "IsDeleted" = false');
    final resOperasi = await conn.execute('SELECT COUNT(*) FROM "LaporanOperasi" WHERE "IsDeleted" = false');
    final resPemeriksaan = await conn.execute('SELECT COUNT(*) FROM "Pemeriksaan" WHERE "IsDeleted" = false');

    final totalPasien = (resPasien.first.first as num?)?.toInt() ?? 0;
    final totalOperasi = (resOperasi.first.first as num?)?.toInt() ?? 0;
    final totalPemeriksaan = (resPemeriksaan.first.first as num?)?.toInt() ?? 0;

    String dateWhereClause = '';
    Map<String, dynamic> params = {};

    if (period == 'today') {
      dateWhereClause = 'AND "Tanggal"::date = CURRENT_DATE';
    } else if (period == 'month') {
      dateWhereClause = 'AND "Tanggal" >= (CURRENT_DATE - INTERVAL \'30 days\')';
    } else if (period == 'year') {
      dateWhereClause = 'AND "Tanggal" >= (CURRENT_DATE - INTERVAL \'1 year\')';
    } else if (period == 'custom' && startDate != null && endDate != null) {
      dateWhereClause = 'AND "Tanggal"::date >= @startDate AND "Tanggal"::date <= @endDate';
      params['startDate'] = startDate;
      params['endDate'] = endDate;
    }

    // Trend Query based on period
    String trendSql;
    if (period == 'year' || period == 'month') {
      trendSql = '''
        SELECT TO_CHAR("Tanggal", 'Mon YY') as label, EXTRACT(MONTH FROM "Tanggal") as m, COUNT(*) as count
        FROM "LaporanOperasi"
        WHERE "IsDeleted" = false $dateWhereClause
        GROUP BY TO_CHAR("Tanggal", 'Mon YY'), EXTRACT(MONTH FROM "Tanggal"), EXTRACT(YEAR FROM "Tanggal")
        ORDER BY EXTRACT(YEAR FROM "Tanggal") ASC, m ASC
      ''';
    } else if (period == 'today') {
      trendSql = '''
        SELECT TO_CHAR("Tanggal", 'HH24:00') as label, COUNT(*) as count
        FROM "LaporanOperasi"
        WHERE "IsDeleted" = false $dateWhereClause
        GROUP BY TO_CHAR("Tanggal", 'HH24:00')
        ORDER BY label ASC
      ''';
    } else {
      trendSql = '''
        SELECT EXTRACT(YEAR FROM "Tanggal")::text as label, COUNT(*) as count
        FROM "LaporanOperasi"
        WHERE "IsDeleted" = false $dateWhereClause AND EXTRACT(YEAR FROM "Tanggal") >= 2021
        GROUP BY EXTRACT(YEAR FROM "Tanggal")
        ORDER BY label ASC
      ''';
    }

    Result trendRes;
    if (params.isNotEmpty) {
      trendRes = await conn.execute(Sql.named(trendSql), parameters: params);
    } else {
      trendRes = await conn.execute(trendSql);
    }

    final yearlyStats = trendRes.map((r) {
      final label = r.first?.toString() ?? '';
      final count = (r.toColumnMap()['count'] as num?)?.toInt() ?? 0;
      return {'year': label, 'count': count};
    }).toList();

    // Flag Distribution Chart Data
    final flagSql = '''
      SELECT 
        SUM(CASE WHEN "Hidrosefalus" = true THEN 1 ELSE 0 END) as hidrosefalus,
        SUM(CASE WHEN "ETV" = true THEN 1 ELSE 0 END) as etv,
        SUM(CASE WHEN "Shunt" = true THEN 1 ELSE 0 END) as shunt,
        SUM(CASE WHEN "ETVGagalShunt" = true THEN 1 ELSE 0 END) as etvgagal
      FROM "LaporanOperasi"
      WHERE "IsDeleted" = false $dateWhereClause
    ''';

    Result flagRes;
    if (params.isNotEmpty) {
      flagRes = await conn.execute(Sql.named(flagSql), parameters: params);
    } else {
      flagRes = await conn.execute(flagSql);
    }

    final flagRow = flagRes.first.toColumnMap();
    final flagStats = {
      'hidrosefalus': (flagRow['hidrosefalus'] as num?)?.toInt() ?? 0,
      'etv': (flagRow['etv'] as num?)?.toInt() ?? 0,
      'shunt': (flagRow['shunt'] as num?)?.toInt() ?? 0,
      'etvGagalShunt': (flagRow['etvgagal'] as num?)?.toInt() ?? 0,
    };

    return {
      'totalPasien': totalPasien,
      'totalOperasi': totalOperasi,
      'totalPemeriksaan': totalPemeriksaan,
      'yearlyStats': yearlyStats,
      'flagStats': flagStats,
    };
  }

  // --- PASIEN CRUD ---
  Future<List<Pasien>> getPasienList({String search = '', int limit = 25, int offset = 0}) async {
    final conn = await connection;
    Result result;
    if (search.trim().isEmpty) {
      result = await conn.execute(
        Sql.named('SELECT * FROM "Pasien" WHERE "IsDeleted" = false ORDER BY "Id" DESC LIMIT @limit OFFSET @offset'),
        parameters: {'limit': limit, 'offset': offset},
      );
    } else {
      result = await conn.execute(
        Sql.named('SELECT * FROM "Pasien" WHERE "IsDeleted" = false AND (LOWER("Nama") LIKE LOWER(@search) OR LOWER("NoRM") LIKE LOWER(@search)) ORDER BY "Id" DESC LIMIT @limit OFFSET @offset'),
        parameters: {'search': '%$search%', 'limit': limit, 'offset': offset},
      );
    }
    return result.map((row) {
      final map = <String, dynamic>{};
      row.toColumnMap().forEach((key, value) => map[key.toLowerCase()] = value);
      return Pasien.fromMap(map);
    }).toList();
  }

  Future<bool> savePasien({
    int? id,
    required String nama,
    required String noRM,
    required String jenisKelamin,
    required String alamat,
    DateTime? tanggalLahir,
    String? usia,
    required String user,
  }) async {
    final conn = await connection;
    if (id == null || id == 0) {
      final res = await conn.execute(
        Sql.named('''
          INSERT INTO "Pasien" ("Nama", "NoRM", "JenisKelamin", "Alamat", "TanggalLahir", "Usia", "CreatedBy", "CreatedAt", "UpdatedBy", "UpdatedAt", "IsDeleted")
          VALUES (@nama, @noRM, @jenisKelamin, @alamat, @tanggalLahir, @usia, @user, NOW(), @user, NOW(), false)
        '''),
        parameters: {
          'nama': nama,
          'noRM': noRM,
          'jenisKelamin': jenisKelamin,
          'alamat': alamat,
          'tanggalLahir': tanggalLahir,
          'usia': usia,
          'user': user,
        },
      );
      return res.affectedRows > 0;
    } else {
      final res = await conn.execute(
        Sql.named('''
          UPDATE "Pasien"
          SET "Nama" = @nama, "NoRM" = @noRM, "JenisKelamin" = @jenisKelamin, "Alamat" = @alamat,
              "TanggalLahir" = @tanggalLahir, "Usia" = @usia, "UpdatedBy" = @user, "UpdatedAt" = NOW()
          WHERE "Id" = @id
        '''),
        parameters: {
          'id': id,
          'nama': nama,
          'noRM': noRM,
          'jenisKelamin': jenisKelamin,
          'alamat': alamat,
          'tanggalLahir': tanggalLahir,
          'usia': usia,
          'user': user,
        },
      );
      return res.affectedRows > 0;
    }
  }

  Future<bool> deletePasien(int id, String user) async {
    final conn = await connection;
    final res = await conn.execute(
      Sql.named('UPDATE "Pasien" SET "IsDeleted" = true, "DeletedBy" = @user, "DeletedAt" = NOW() WHERE "Id" = @id'),
      parameters: {'id': id, 'user': user},
    );
    return res.affectedRows > 0;
  }

  // --- DOKTER CRUD ---
  Future<List<Dokter>> getDokterList({String search = ''}) async {
    final conn = await connection;
    Result result;
    if (search.trim().isEmpty) {
      result = await conn.execute('SELECT * FROM "Dokter" WHERE "IsDeleted" = false ORDER BY "Nama" ASC');
    } else {
      result = await conn.execute(
        Sql.named('SELECT * FROM "Dokter" WHERE "IsDeleted" = false AND (LOWER("Nama") LIKE LOWER(@search) OR LOWER("Spesialisasi") LIKE LOWER(@search)) ORDER BY "Nama" ASC'),
        parameters: {'search': '%$search%'},
      );
    }
    return result.map((row) {
      final map = <String, dynamic>{};
      row.toColumnMap().forEach((key, value) => map[key.toLowerCase()] = value);
      return Dokter.fromMap(map);
    }).toList();
  }

  Future<bool> saveDokter({int? id, required String nama, required String spesialisasi, required String user}) async {
    final conn = await connection;
    if (id == null || id == 0) {
      final res = await conn.execute(
        Sql.named('''
          INSERT INTO "Dokter" ("Nama", "Spesialisasi", "CreatedBy", "CreatedAt", "UpdatedBy", "UpdatedAt", "IsDeleted")
          VALUES (@nama, @spesialisasi, @user, NOW(), @user, NOW(), false)
        '''),
        parameters: {'nama': nama, 'spesialisasi': spesialisasi, 'user': user},
      );
      return res.affectedRows > 0;
    } else {
      final res = await conn.execute(
        Sql.named('''
          UPDATE "Dokter" SET "Nama" = @nama, "Spesialisasi" = @spesialisasi, "UpdatedBy" = @user, "UpdatedAt" = NOW()
          WHERE "Id" = @id
        '''),
        parameters: {'id': id, 'nama': nama, 'spesialisasi': spesialisasi, 'user': user},
      );
      return res.affectedRows > 0;
    }
  }

  Future<bool> deleteDokter(int id, String user) async {
    final conn = await connection;
    final res = await conn.execute(
      Sql.named('UPDATE "Dokter" SET "IsDeleted" = true, "DeletedBy" = @user, "DeletedAt" = NOW() WHERE "Id" = @id'),
      parameters: {'id': id, 'user': user},
    );
    return res.affectedRows > 0;
  }

  // --- RUMAH SAKIT CRUD ---
  Future<List<RumahSakit>> getRumahSakitList({String search = ''}) async {
    final conn = await connection;
    Result result;
    if (search.trim().isEmpty) {
      result = await conn.execute('SELECT * FROM "RumahSakit" WHERE "IsDeleted" = false ORDER BY "Nama" ASC');
    } else {
      result = await conn.execute(
        Sql.named('SELECT * FROM "RumahSakit" WHERE "IsDeleted" = false AND LOWER("Nama") LIKE LOWER(@search) ORDER BY "Nama" ASC'),
        parameters: {'search': '%$search%'},
      );
    }
    return result.map((row) {
      final map = <String, dynamic>{};
      row.toColumnMap().forEach((key, value) => map[key.toLowerCase()] = value);
      return RumahSakit.fromMap(map);
    }).toList();
  }

  Future<bool> saveRumahSakit({int? id, required String nama, required String alamat, required String user}) async {
    final conn = await connection;
    if (id == null || id == 0) {
      final res = await conn.execute(
        Sql.named('''
          INSERT INTO "RumahSakit" ("Nama", "Alamat", "CreatedBy", "CreatedAt", "UpdatedBy", "UpdatedAt", "IsDeleted")
          VALUES (@nama, @alamat, @user, NOW(), @user, NOW(), false)
        '''),
        parameters: {'nama': nama, 'alamat': alamat, 'user': user},
      );
      return res.affectedRows > 0;
    } else {
      final res = await conn.execute(
        Sql.named('''
          UPDATE "RumahSakit" SET "Nama" = @nama, "Alamat" = @alamat, "UpdatedBy" = @user, "UpdatedAt" = NOW()
          WHERE "Id" = @id
        '''),
        parameters: {'id': id, 'nama': nama, 'alamat': alamat, 'user': user},
      );
      return res.affectedRows > 0;
    }
  }

  Future<bool> deleteRumahSakit(int id, String user) async {
    final conn = await connection;
    final res = await conn.execute(
      Sql.named('UPDATE "RumahSakit" SET "IsDeleted" = true, "DeletedBy" = @user, "DeletedAt" = NOW() WHERE "Id" = @id'),
      parameters: {'id': id, 'user': user},
    );
    return res.affectedRows > 0;
  }

  // --- DIAGNOSA CRUD ---
  Future<List<Diagnosa>> getDiagnosaList({String search = ''}) async {
    final conn = await connection;
    Result result;
    if (search.trim().isEmpty) {
      result = await conn.execute('SELECT * FROM "Diagnosa" WHERE "IsDeleted" = false ORDER BY "KodeICD" ASC');
    } else {
      result = await conn.execute(
        Sql.named('SELECT * FROM "Diagnosa" WHERE "IsDeleted" = false AND (LOWER("KodeICD") LIKE LOWER(@search) OR LOWER("Nama") LIKE LOWER(@search)) ORDER BY "KodeICD" ASC'),
        parameters: {'search': '%$search%'},
      );
    }
    return result.map((row) {
      final map = <String, dynamic>{};
      row.toColumnMap().forEach((key, value) => map[key.toLowerCase()] = value);
      return Diagnosa.fromMap(map);
    }).toList();
  }

  Future<bool> saveDiagnosa({int? id, required String kodeICD, required String nama, required String user}) async {
    final conn = await connection;
    if (id == null || id == 0) {
      final res = await conn.execute(
        Sql.named('''
          INSERT INTO "Diagnosa" ("KodeICD", "Nama", "CreatedBy", "CreatedAt", "UpdatedBy", "UpdatedAt", "IsDeleted")
          VALUES (@kodeICD, @nama, @user, NOW(), @user, NOW(), false)
        '''),
        parameters: {'kodeICD': kodeICD, 'nama': nama, 'user': user},
      );
      return res.affectedRows > 0;
    } else {
      final res = await conn.execute(
        Sql.named('''
          UPDATE "Diagnosa" SET "KodeICD" = @kodeICD, "Nama" = @nama, "UpdatedBy" = @user, "UpdatedAt" = NOW()
          WHERE "Id" = @id
        '''),
        parameters: {'id': id, 'kodeICD': kodeICD, 'nama': nama, 'user': user},
      );
      return res.affectedRows > 0;
    }
  }

  Future<bool> deleteDiagnosa(int id, String user) async {
    final conn = await connection;
    final res = await conn.execute(
      Sql.named('UPDATE "Diagnosa" SET "IsDeleted" = true, "DeletedBy" = @user, "DeletedAt" = NOW() WHERE "Id" = @id'),
      parameters: {'id': id, 'user': user},
    );
    return res.affectedRows > 0;
  }

  // --- TINDAKAN CRUD ---
  Future<List<Tindakan>> getTindakanList({String search = ''}) async {
    final conn = await connection;
    Result result;
    if (search.trim().isEmpty) {
      result = await conn.execute('SELECT * FROM "Tindakan" WHERE "IsDeleted" = false ORDER BY "Nama" ASC');
    } else {
      result = await conn.execute(
        Sql.named('SELECT * FROM "Tindakan" WHERE "IsDeleted" = false AND LOWER("Nama") LIKE LOWER(@search) ORDER BY "Nama" ASC'),
        parameters: {'search': '%$search%'},
      );
    }
    return result.map((row) {
      final map = <String, dynamic>{};
      row.toColumnMap().forEach((key, value) => map[key.toLowerCase()] = value);
      return Tindakan.fromMap(map);
    }).toList();
  }

  Future<bool> saveTindakan({int? id, required String nama, required String user}) async {
    final conn = await connection;
    if (id == null || id == 0) {
      final res = await conn.execute(
        Sql.named('''
          INSERT INTO "Tindakan" ("Nama", "CreatedBy", "CreatedAt", "UpdatedBy", "UpdatedAt", "IsDeleted")
          VALUES (@nama, @user, NOW(), @user, NOW(), false)
        '''),
        parameters: {'nama': nama, 'user': user},
      );
      return res.affectedRows > 0;
    } else {
      final res = await conn.execute(
        Sql.named('''
          UPDATE "Tindakan" SET "Nama" = @nama, "UpdatedBy" = @user, "UpdatedAt" = NOW() WHERE "Id" = @id
        '''),
        parameters: {'id': id, 'nama': nama, 'user': user},
      );
      return res.affectedRows > 0;
    }
  }

  Future<bool> deleteTindakan(int id, String user) async {
    final conn = await connection;
    final res = await conn.execute(
      Sql.named('UPDATE "Tindakan" SET "IsDeleted" = true, "DeletedBy" = @user, "DeletedAt" = NOW() WHERE "Id" = @id'),
      parameters: {'id': id, 'user': user},
    );
    return res.affectedRows > 0;
  }

  // --- JENIS TINDAKAN CRUD ---
  Future<List<JenisTindakan>> getJenisTindakanList({String search = ''}) async {
    final conn = await connection;
    Result result;
    if (search.trim().isEmpty) {
      result = await conn.execute('SELECT * FROM "JenisTindakan" WHERE "IsDeleted" = false ORDER BY "Nama" ASC');
    } else {
      result = await conn.execute(
        Sql.named('SELECT * FROM "JenisTindakan" WHERE "IsDeleted" = false AND LOWER("Nama") LIKE LOWER(@search) ORDER BY "Nama" ASC'),
        parameters: {'search': '%$search%'},
      );
    }
    return result.map((row) {
      final map = <String, dynamic>{};
      row.toColumnMap().forEach((key, value) => map[key.toLowerCase()] = value);
      return JenisTindakan.fromMap(map);
    }).toList();
  }

  Future<bool> saveJenisTindakan({int? id, required String nama, required String user}) async {
    final conn = await connection;
    if (id == null || id == 0) {
      final res = await conn.execute(
        Sql.named('''
          INSERT INTO "JenisTindakan" ("Nama", "CreatedBy", "CreatedAt", "UpdatedBy", "UpdatedAt", "IsDeleted")
          VALUES (@nama, @user, NOW(), @user, NOW(), false)
        '''),
        parameters: {'nama': nama, 'user': user},
      );
      return res.affectedRows > 0;
    } else {
      final res = await conn.execute(
        Sql.named('''
          UPDATE "JenisTindakan" SET "Nama" = @nama, "UpdatedBy" = @user, "UpdatedAt" = NOW() WHERE "Id" = @id
        '''),
        parameters: {'id': id, 'nama': nama, 'user': user},
      );
      return res.affectedRows > 0;
    }
  }

  Future<bool> deleteJenisTindakan(int id, String user) async {
    final conn = await connection;
    final res = await conn.execute(
      Sql.named('UPDATE "JenisTindakan" SET "IsDeleted" = true, "DeletedBy" = @user, "DeletedAt" = NOW() WHERE "Id" = @id'),
      parameters: {'id': id, 'user': user},
    );
    return res.affectedRows > 0;
  }

  // --- LAPORAN OPERASI CRUD ---
  Future<List<LaporanOperasi>> getLaporanOperasiList({
    String search = '',
    int limit = 20,
    int offset = 0,
  }) async {
    final conn = await connection;
    final sql = '''
      SELECT l.*, p."Nama" AS pasiennama, t."Nama" AS tindakannama, op."Nama" AS operatornama,
             dpjp."Nama" AS dpjpnama, rs."Nama" AS rumahsakitnama
      FROM "LaporanOperasi" l
      LEFT JOIN "Pasien" p ON l."PasienId" = p."Id"
      LEFT JOIN "Tindakan" t ON l."TindakanId" = t."Id"
      LEFT JOIN "Dokter" op ON l."OperatorId" = op."Id"
      LEFT JOIN "Dokter" dpjp ON l."DPJPId" = dpjp."Id"
      LEFT JOIN "RumahSakit" rs ON l."RumahSakitId" = rs."Id"
      WHERE l."IsDeleted" = false
      ${search.trim().isNotEmpty ? 'AND (LOWER(p."Nama") LIKE LOWER(@search) OR LOWER(l."NoRM") LIKE LOWER(@search))' : ''}
      ORDER BY l."Tanggal" DESC, l."Id" DESC
      LIMIT @limit OFFSET @offset
    ''';

    Result result;
    if (search.trim().isNotEmpty) {
      result = await conn.execute(Sql.named(sql), parameters: {'search': '%$search%', 'limit': limit, 'offset': offset});
    } else {
      result = await conn.execute(Sql.named(sql), parameters: {'limit': limit, 'offset': offset});
    }

    final list = <LaporanOperasi>[];
    for (var row in result) {
      final map = <String, dynamic>{};
      row.toColumnMap().forEach((key, value) => map[key.toLowerCase()] = value);
      final id = map['id'] as int;

      // Get Diagnosa names & IDs
      final diagRes = await conn.execute(
        Sql.named('''
          SELECT d."Id", d."Nama" 
          FROM "LaporanOperasiDiagnosas" ld
          JOIN "Diagnosa" d ON ld."DiagnosaId" = d."Id"
          WHERE ld."LaporanOperasiId" = @id
        '''),
        parameters: {'id': id},
      );
      final diagnosaIds = diagRes.map((r) => r.toColumnMap()['Id'] as int).toList();
      final diagnosaNames = diagRes.map((r) => r.toColumnMap()['Nama']?.toString() ?? '').toList();

      // Get JenisTindakan names & IDs
      final jtRes = await conn.execute(
        Sql.named('''
          SELECT jt."Id", jt."Nama"
          FROM "LaporanOperasiJenisTindakan" ljt
          JOIN "JenisTindakan" jt ON ljt."JenisTindakanId" = jt."Id"
          WHERE ljt."LaporanOperasiId" = @id
        '''),
        parameters: {'id': id},
      );
      final jtIds = jtRes.map((r) => r.toColumnMap()['Id'] as int).toList();
      final jtNames = jtRes.map((r) => r.toColumnMap()['Nama']?.toString() ?? '').toList();

      list.add(LaporanOperasi.fromMap(
        map,
        diagnosaIds: diagnosaIds,
        diagnosaNames: diagnosaNames,
        jenisTindakanIds: jtIds,
        jenisTindakanNames: jtNames,
      ));
    }

    return list;
  }

  Future<int> saveLaporanOperasiReturnId({
    int? id,
    required DateTime tanggal,
    required int pasienId,
    required String umur,
    required String noRM,
    required int tindakanId,
    required int operatorId,
    required int dpjpId,
    required int rumahSakitId,
    required List<int> diagnosaIds,
    required List<int> jenisTindakanIds,
    int? gos,
    required String keterangan,
    required String user,
  }) async {
    final conn = await connection;

    // Fetch names for compounded flags computation
    final diagNamesRes = await conn.execute(
      Sql.named('SELECT "Nama" FROM "Diagnosa" WHERE "Id" = ANY(@ids)'),
      parameters: {'ids': diagnosaIds},
    );
    final diagnosaNames = diagNamesRes.map((r) => r.first?.toString() ?? '').toList();

    final tindRes = await conn.execute(
      Sql.named('SELECT "Nama" FROM "Tindakan" WHERE "Id" = @id'),
      parameters: {'id': tindakanId},
    );
    final tindakanName = tindRes.isNotEmpty ? tindRes.first.first?.toString() : null;

    final jtNamesRes = await conn.execute(
      Sql.named('SELECT "Nama" FROM "JenisTindakan" WHERE "Id" = ANY(@ids)'),
      parameters: {'ids': jenisTindakanIds},
    );
    final jenisTindakanNames = jtNamesRes.map((r) => r.first?.toString() ?? '').toList();

    // Logic Computation
    bool hidrosefalus = diagnosaNames.any((d) {
      final l = d.toLowerCase();
      return l.contains("hidrosefalus") || l.contains("hydrocephalus") || l.contains("hydrancephaly") || l.contains("hidransefali") || l.contains("hidrocephalus");
    });

    bool etv = false;
    bool shunt = false;
    if (tindakanName != null) {
      final t = tindakanName.toLowerCase();
      etv = t.contains("endoscopic third ventriculostomy");
      shunt = t.contains("ventriculostomy insertion or replacement of external ventricular drain") ||
              t.contains("ventricular shunt to abdominal cavity") ||
              t.contains("ventricular shunt revision");
    }

    bool etvGagalShunt = jenisTindakanNames.any((jt) => jt.toLowerCase().contains("gagal"));

    int opId;
    if (id == null || id == 0) {
      final res = await conn.execute(
        Sql.named('''
          INSERT INTO "LaporanOperasi"
          ("Tanggal", "PasienId", "Umur", "NoRM", "TindakanId", "OperatorId", "DPJPId", "RumahSakitId",
           "Hidrosefalus", "ETV", "Shunt", "GOS", "ETVGagalShunt", "ETVGagalShunt2", "Keterangan",
           "CreatedBy", "CreatedAt", "UpdatedBy", "UpdatedAt", "IsDeleted")
          VALUES
          (@tanggal, @pasienId, @umur, @noRM, @tindakanId, @operatorId, @dpjpId, @rumahSakitId,
           @hidrosefalus, @etv, @shunt, @gos, @etvGagalShunt, false, @keterangan,
           @user, NOW(), @user, NOW(), false)
          RETURNING "Id"
        '''),
        parameters: {
          'tanggal': tanggal,
          'pasienId': pasienId,
          'umur': umur,
          'noRM': noRM,
          'tindakanId': tindakanId,
          'operatorId': operatorId,
          'dpjpId': dpjpId,
          'rumahSakitId': rumahSakitId,
          'hidrosefalus': hidrosefalus,
          'etv': etv,
          'shunt': shunt,
          'gos': gos,
          'etvGagalShunt': etvGagalShunt,
          'keterangan': keterangan,
          'user': user,
        },
      );
      opId = res.first.first as int;
    } else {
      opId = id;
      await conn.execute(
        Sql.named('''
          UPDATE "LaporanOperasi"
          SET "Tanggal" = @tanggal, "PasienId" = @pasienId, "Umur" = @umur, "NoRM" = @noRM,
              "TindakanId" = @tindakanId, "OperatorId" = @operatorId, "DPJPId" = @dpjpId, "RumahSakitId" = @rumahSakitId,
              "Hidrosefalus" = @hidrosefalus, "ETV" = @etv, "Shunt" = @shunt, "GOS" = @gos,
              "ETVGagalShunt" = @etvGagalShunt, "Keterangan" = @keterangan, "UpdatedBy" = @user, "UpdatedAt" = NOW()
          WHERE "Id" = @id
        '''),
        parameters: {
          'id': id,
          'tanggal': tanggal,
          'pasienId': pasienId,
          'umur': umur,
          'noRM': noRM,
          'tindakanId': tindakanId,
          'operatorId': operatorId,
          'dpjpId': dpjpId,
          'rumahSakitId': rumahSakitId,
          'hidrosefalus': hidrosefalus,
          'etv': etv,
          'shunt': shunt,
          'gos': gos,
          'etvGagalShunt': etvGagalShunt,
          'keterangan': keterangan,
          'user': user,
        },
      );

      // Clean old relations
      await conn.execute(Sql.named('DELETE FROM "LaporanOperasiDiagnosas" WHERE "LaporanOperasiId" = @id'), parameters: {'id': id});
      await conn.execute(Sql.named('DELETE FROM "LaporanOperasiJenisTindakan" WHERE "LaporanOperasiId" = @id'), parameters: {'id': id});
    }

    // Insert join tables
    for (var dId in diagnosaIds) {
      await conn.execute(
        Sql.named('INSERT INTO "LaporanOperasiDiagnosas" ("LaporanOperasiId", "DiagnosaId") VALUES (@opId, @dId)'),
        parameters: {'opId': opId, 'dId': dId},
      );
    }

    for (var jtId in jenisTindakanIds) {
      await conn.execute(
        Sql.named('INSERT INTO "LaporanOperasiJenisTindakan" ("LaporanOperasiId", "JenisTindakanId") VALUES (@opId, @jtId)'),
        parameters: {'opId': opId, 'jtId': jtId},
      );
    }

    return opId;
  }

  // Backward-compat bool wrapper
  Future<bool> saveLaporanOperasi({
    int? id,
    required DateTime tanggal,
    required int pasienId,
    required String umur,
    required String noRM,
    required int tindakanId,
    required int operatorId,
    required int dpjpId,
    required int rumahSakitId,
    required List<int> diagnosaIds,
    required List<int> jenisTindakanIds,
    int? gos,
    required String keterangan,
    required String user,
  }) async {
    final resultId = await saveLaporanOperasiReturnId(
      id: id,
      tanggal: tanggal,
      pasienId: pasienId,
      umur: umur,
      noRM: noRM,
      tindakanId: tindakanId,
      operatorId: operatorId,
      dpjpId: dpjpId,
      rumahSakitId: rumahSakitId,
      diagnosaIds: diagnosaIds,
      jenisTindakanIds: jenisTindakanIds,
      gos: gos,
      keterangan: keterangan,
      user: user,
    );
    return resultId != null;
  }

  Future<bool> deleteLaporanOperasi(int id, String user) async {
    final conn = await connection;
    final res = await conn.execute(
      Sql.named('UPDATE "LaporanOperasi" SET "IsDeleted" = true, "DeletedBy" = @user, "DeletedAt" = NOW() WHERE "Id" = @id'),
      parameters: {'id': id, 'user': user},
    );
    return res.affectedRows > 0;
  }

  // --- PEMERIKSAAN CRUD ---
  Future<List<Pemeriksaan>> getPemeriksaanList({String search = '', String? jenis}) async {
    final conn = await connection;
    String sql = '''
      SELECT pem.*, p."Nama" AS pasiennama, t."Nama" AS tindakannama
      FROM "Pemeriksaan" pem
      JOIN "Pasien" p ON pem."PasienId" = p."Id"
      LEFT JOIN "Tindakan" t ON pem."TindakanId" = t."Id"
      WHERE pem."IsDeleted" = false
    ''';

    if (jenis != null && jenis.isNotEmpty) {
      sql += ' AND pem."JenisPemeriksaan" = \'$jenis\'';
    }
    if (search.trim().isNotEmpty) {
      sql += ' AND (LOWER(p."Nama") LIKE LOWER(@search) OR LOWER(p."NoRM") LIKE LOWER(@search))';
    }
    sql += ' ORDER BY pem."Tanggal" DESC, pem."Id" DESC';

    Result result;
    if (search.trim().isNotEmpty) {
      result = await conn.execute(Sql.named(sql), parameters: {'search': '%$search%'});
    } else {
      result = await conn.execute(sql);
    }

    return result.map((row) {
      final map = <String, dynamic>{};
      row.toColumnMap().forEach((key, value) => map[key.toLowerCase()] = value);
      return Pemeriksaan.fromMap(map);
    }).toList();
  }

  Future<bool> savePemeriksaan({
    int? id,
    required String jenisPemeriksaan,
    required DateTime tanggal,
    required int pasienId,
    String? keluhanUtama,
    String? riwayatPenyakitSekarang,
    int? tindakanId,
    required String keterangan,
    required String user,
  }) async {
    final conn = await connection;
    if (id == null || id == 0) {
      final res = await conn.execute(
        Sql.named('''
          INSERT INTO "Pemeriksaan"
          ("JenisPemeriksaan", "Tanggal", "PasienId", "KeluhanUtama", "RiwayatPenyakitSekarang", "TindakanId", "Keterangan", "CreatedBy", "CreatedAt", "UpdatedBy", "UpdatedAt", "IsDeleted")
          VALUES
          (@jenisPemeriksaan, @tanggal, @pasienId, @keluhanUtama, @riwayatPenyakitSekarang, @tindakanId, @keterangan, @user, NOW(), @user, NOW(), false)
        '''),
        parameters: {
          'jenisPemeriksaan': jenisPemeriksaan,
          'tanggal': tanggal,
          'pasienId': pasienId,
          'keluhanUtama': keluhanUtama,
          'riwayatPenyakitSekarang': riwayatPenyakitSekarang,
          'tindakanId': tindakanId,
          'keterangan': keterangan,
          'user': user,
        },
      );
      return res.affectedRows > 0;
    } else {
      final res = await conn.execute(
        Sql.named('''
          UPDATE "Pemeriksaan"
          SET "JenisPemeriksaan" = @jenisPemeriksaan, "Tanggal" = @tanggal, "PasienId" = @pasienId,
              "KeluhanUtama" = @keluhanUtama, "RiwayatPenyakitSekarang" = @riwayatPenyakitSekarang,
              "TindakanId" = @tindakanId, "Keterangan" = @keterangan, "UpdatedBy" = @user, "UpdatedAt" = NOW()
          WHERE "Id" = @id
        '''),
        parameters: {
          'id': id,
          'jenisPemeriksaan': jenisPemeriksaan,
          'tanggal': tanggal,
          'pasienId': pasienId,
          'keluhanUtama': keluhanUtama,
          'riwayatPenyakitSekarang': riwayatPenyakitSekarang,
          'tindakanId': tindakanId,
          'keterangan': keterangan,
          'user': user,
        },
      );
      return res.affectedRows > 0;
    }
  }

  Future<bool> deletePemeriksaan(int id, String user) async {
    final conn = await connection;
    final res = await conn.execute(
      Sql.named('UPDATE "Pemeriksaan" SET "IsDeleted" = true, "DeletedBy" = @user, "DeletedAt" = NOW() WHERE "Id" = @id'),
      parameters: {'id': id, 'user': user},
    );
    return res.affectedRows > 0;
  }

  /// Dapatkan ID Pemeriksaan terakhir milik pasien.
  /// Untuk mode Edit: kembalikan [editId].
  /// Untuk mode Create: ambil record terbaru berdasarkan PasienId.
  Future<int?> getLastPemeriksaanId({required int pasienId, int? editId}) async {
    if (editId != null && editId > 0) return editId;
    final conn = await connection;
    final res = await conn.execute(
      Sql.named('SELECT "Id" FROM "Pemeriksaan" WHERE "PasienId" = @pasienId AND "IsDeleted" = false ORDER BY "Id" DESC LIMIT 1'),
      parameters: {'pasienId': pasienId},
    );
    if (res.isEmpty) return null;
    return res.first.first as int?;
  }

  // --- MEDIA QUERY METHODS ---

  /// Dapatkan daftar media LaporanOperasi dari DB.
  /// Hanya fetch metadata (Id, ContentType, FileName, Type) — bukan FileData.
  Future<List<LaporanOperasiMedia>> getLaporanOperasiMedias(int laporanOperasiId) async {
    final conn = await connection;
    final res = await conn.execute(
      Sql.named(
        'SELECT "Id", "LaporanOperasiId", "ContentType", "FileName", "Type"::text FROM "LaporanOperasiMedias" WHERE "LaporanOperasiId" = @id ORDER BY "Id" ASC',
      ),
      parameters: {'id': laporanOperasiId},
    );
    return res.map((r) {
      final map = <String, dynamic>{};
      r.toColumnMap().forEach((key, value) => map[key.toLowerCase()] = value);
      return LaporanOperasiMedia.fromMap(map);
    }).toList();
  }

  /// Dapatkan daftar media Pemeriksaan dari DB.
  /// Hanya fetch metadata (Id, ContentType, FileName, Type) — bukan FileData.
  Future<List<PemeriksaanMedia>> getPemeriksaanMedias(int pemeriksaanId) async {
    final conn = await connection;
    final res = await conn.execute(
      Sql.named(
        'SELECT "Id", "PemeriksaanId", "ContentType", "FileName", "Type"::text FROM "PemeriksaanMedias" WHERE "PemeriksaanId" = @id ORDER BY "Id" ASC',
      ),
      parameters: {'id': pemeriksaanId},
    );
    return res.map((r) {
      final map = <String, dynamic>{};
      r.toColumnMap().forEach((key, value) => map[key.toLowerCase()] = value);
      return PemeriksaanMedia.fromMap(map);
    }).toList();
  }
}
