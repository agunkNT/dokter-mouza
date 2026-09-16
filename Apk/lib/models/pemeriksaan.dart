class PemeriksaanMedia {
  final int id;
  final int pemeriksaanId;
  final String contentType;
  final String fileName;
  final String type; // 'Foto' atau 'Video'

  PemeriksaanMedia({
    required this.id,
    required this.pemeriksaanId,
    required this.contentType,
    required this.fileName,
    required this.type,
  });

  bool get isFoto => type.toLowerCase() == 'foto';

  factory PemeriksaanMedia.fromMap(Map<String, dynamic> map) {
    return PemeriksaanMedia(
      id: (map['id'] as num).toInt(),
      pemeriksaanId: (map['pemeriksaanid'] as num?)?.toInt() ?? 0,
      contentType: map['contenttype']?.toString() ?? 'image/jpeg',
      fileName: map['filename']?.toString() ?? '',
      type: map['type']?.toString() ?? 'Foto',
    );
  }
}

class Pemeriksaan {
  final int id;
  final String jenisPemeriksaan;
  final DateTime tanggal;
  final int pasienId;
  final String? pasienNama;
  final String? keluhanUtama;
  final String? riwayatPenyakitSekarang;
  final int? tindakanId;
  final String? tindakanNama;
  final String keterangan;
  final List<PemeriksaanMedia> medias;
  final String createBy;
  final DateTime createDate;
  final String updateBy;
  final DateTime updateDate;
  final bool isActive;

  Pemeriksaan({
    required this.id,
    required this.jenisPemeriksaan,
    required this.tanggal,
    required this.pasienId,
    this.pasienNama,
    this.keluhanUtama,
    this.riwayatPenyakitSekarang,
    this.tindakanId,
    this.tindakanNama,
    required this.keterangan,
    required this.medias,
    required this.createBy,
    required this.createDate,
    required this.updateBy,
    required this.updateDate,
    required this.isActive,
  });

  factory Pemeriksaan.fromMap(Map<String, dynamic> map, {List<PemeriksaanMedia>? medias}) {
    final isDeleted = map['isdeleted'] as bool? ?? false;
    return Pemeriksaan(
      id: map['id'] as int,
      jenisPemeriksaan: map['jenispemeriksaan']?.toString() ?? 'KONSULTASI_PRE',
      tanggal: map['tanggal'] != null ? DateTime.tryParse(map['tanggal'].toString()) ?? DateTime.now() : DateTime.now(),
      pasienId: map['pasienid'] as int,
      pasienNama: map['pasiennama']?.toString(),
      keluhanUtama: map['keluhanutama']?.toString(),
      riwayatPenyakitSekarang: map['riwayatpenyakitsekarang']?.toString(),
      tindakanId: map['tindakanid'] as int?,
      tindakanNama: map['tindakannama']?.toString(),
      keterangan: map['keterangan']?.toString() ?? '',
      medias: medias ?? [],
      createBy: map['createdby']?.toString() ?? 'System',
      createDate: map['createdat'] != null ? DateTime.tryParse(map['createdat'].toString()) ?? DateTime.now() : DateTime.now(),
      updateBy: map['updatedby']?.toString() ?? 'System',
      updateDate: map['updatedat'] != null ? DateTime.tryParse(map['updatedat'].toString()) ?? DateTime.now() : DateTime.now(),
      isActive: !isDeleted,
    );
  }
}
