class LaporanOperasiMedia {
  final int id;
  final int laporanOperasiId;
  final String contentType;
  final String fileName;
  final String type; // 'Foto' atau 'Video'

  LaporanOperasiMedia({
    required this.id,
    required this.laporanOperasiId,
    required this.contentType,
    required this.fileName,
    required this.type,
  });

  bool get isFoto => type.toLowerCase() == 'foto';

  factory LaporanOperasiMedia.fromMap(Map<String, dynamic> map) {
    return LaporanOperasiMedia(
      id: (map['id'] as num).toInt(),
      laporanOperasiId: (map['laporanoperasiid'] as num?)?.toInt() ?? 0,
      contentType: map['contenttype']?.toString() ?? 'image/jpeg',
      fileName: map['filename']?.toString() ?? '',
      type: map['type']?.toString() ?? 'Foto',
    );
  }
}

class LaporanOperasi {
  final int id;
  final DateTime tanggal;
  final int? pasienId;
  final String? pasienNama;
  final String umur;
  final String noRM;
  final int? tindakanId;
  final String? tindakanNama;
  final int? operatorId;
  final String? operatorNama;
  final int? dpjpId;
  final String? dpjpNama;
  final int? rumahSakitId;
  final String? rumahSakitNama;
  final bool hidrosefalus;
  final bool etv;
  final bool shunt;
  final int? gos;
  final bool etvGagalShunt;
  final String keterangan;
  final List<String> diagnosaNames;
  final List<int> diagnosaIds;
  final List<String> jenisTindakanNames;
  final List<int> jenisTindakanIds;
  final List<LaporanOperasiMedia> medias;
  final String createBy;
  final DateTime createDate;
  final String updateBy;
  final DateTime updateDate;
  final bool isActive;

  LaporanOperasi({
    required this.id,
    required this.tanggal,
    this.pasienId,
    this.pasienNama,
    required this.umur,
    required this.noRM,
    this.tindakanId,
    this.tindakanNama,
    this.operatorId,
    this.operatorNama,
    this.dpjpId,
    this.dpjpNama,
    this.rumahSakitId,
    this.rumahSakitNama,
    required this.hidrosefalus,
    required this.etv,
    required this.shunt,
    this.gos,
    required this.etvGagalShunt,
    required this.keterangan,
    required this.diagnosaNames,
    required this.diagnosaIds,
    required this.jenisTindakanNames,
    required this.jenisTindakanIds,
    required this.medias,
    required this.createBy,
    required this.createDate,
    required this.updateBy,
    required this.updateDate,
    required this.isActive,
  });

  factory LaporanOperasi.fromMap(
    Map<String, dynamic> map, {
    List<String>? diagnosaNames,
    List<int>? diagnosaIds,
    List<String>? jenisTindakanNames,
    List<int>? jenisTindakanIds,
    List<LaporanOperasiMedia>? medias,
  }) {
    final isDeleted = map['isdeleted'] as bool? ?? false;
    return LaporanOperasi(
      id: map['id'] as int,
      tanggal: map['tanggal'] != null ? DateTime.tryParse(map['tanggal'].toString()) ?? DateTime.now() : DateTime.now(),
      pasienId: map['pasienid'] as int?,
      pasienNama: map['pasiennama']?.toString(),
      umur: map['umur']?.toString() ?? '',
      noRM: map['norm']?.toString() ?? '',
      tindakanId: map['tindakanid'] as int?,
      tindakanNama: map['tindakannama']?.toString(),
      operatorId: map['operatorid'] as int?,
      operatorNama: map['operatornama']?.toString(),
      dpjpId: map['dpjpid'] as int?,
      dpjpNama: map['dpjpnama']?.toString(),
      rumahSakitId: map['rumahsakitid'] as int?,
      rumahSakitNama: map['rumahsakitnama']?.toString(),
      hidrosefalus: map['hidrosefalus'] as bool? ?? false,
      etv: map['etv'] as bool? ?? false,
      shunt: map['shunt'] as bool? ?? false,
      gos: map['gos'] as int?,
      etvGagalShunt: map['etvgagalshunt'] as bool? ?? false,
      keterangan: map['keterangan']?.toString() ?? '',
      diagnosaNames: diagnosaNames ?? [],
      diagnosaIds: diagnosaIds ?? [],
      jenisTindakanNames: jenisTindakanNames ?? [],
      jenisTindakanIds: jenisTindakanIds ?? [],
      medias: medias ?? [],
      createBy: map['createdby']?.toString() ?? 'System',
      createDate: map['createdat'] != null ? DateTime.tryParse(map['createdat'].toString()) ?? DateTime.now() : DateTime.now(),
      updateBy: map['updatedby']?.toString() ?? 'System',
      updateDate: map['updatedat'] != null ? DateTime.tryParse(map['updatedat'].toString()) ?? DateTime.now() : DateTime.now(),
      isActive: !isDeleted,
    );
  }
}
