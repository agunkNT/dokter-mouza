class Pasien {
  final int id;
  final String nama;
  final String noRM;
  final String jenisKelamin;
  final String alamat;
  final DateTime? tanggalLahir;
  final String? usia;
  final String createBy;
  final DateTime createDate;
  final String updateBy;
  final DateTime updateDate;
  final bool isActive;

  Pasien({
    required this.id,
    required this.nama,
    required this.noRM,
    required this.jenisKelamin,
    required this.alamat,
    this.tanggalLahir,
    this.usia,
    required this.createBy,
    required this.createDate,
    required this.updateBy,
    required this.updateDate,
    required this.isActive,
  });

  factory Pasien.fromMap(Map<String, dynamic> map) {
    final isDeleted = map['isdeleted'] as bool? ?? false;
    return Pasien(
      id: map['id'] as int,
      nama: map['nama']?.toString() ?? '',
      noRM: map['norm']?.toString() ?? '',
      jenisKelamin: map['jeniskelamin']?.toString() ?? '',
      alamat: map['alamat']?.toString() ?? '',
      tanggalLahir: map['tanggallahir'] != null ? DateTime.tryParse(map['tanggallahir'].toString()) : null,
      usia: map['usia']?.toString(),
      createBy: map['createdby']?.toString() ?? 'System',
      createDate: map['createdat'] != null ? DateTime.tryParse(map['createdat'].toString()) ?? DateTime.now() : DateTime.now(),
      updateBy: map['updatedby']?.toString() ?? 'System',
      updateDate: map['updatedat'] != null ? DateTime.tryParse(map['updatedat'].toString()) ?? DateTime.now() : DateTime.now(),
      isActive: !isDeleted,
    );
  }
}
