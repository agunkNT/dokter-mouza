class Dokter {
  final int id;
  final String nama;
  final String spesialisasi;
  final String createBy;
  final DateTime createDate;
  final String updateBy;
  final DateTime updateDate;
  final bool isActive;

  Dokter({
    required this.id,
    required this.nama,
    required this.spesialisasi,
    required this.createBy,
    required this.createDate,
    required this.updateBy,
    required this.updateDate,
    required this.isActive,
  });

  factory Dokter.fromMap(Map<String, dynamic> map) {
    final isDeleted = map['isdeleted'] as bool? ?? false;
    return Dokter(
      id: map['id'] as int,
      nama: map['nama']?.toString() ?? '',
      spesialisasi: map['spesialisasi']?.toString() ?? '',
      createBy: map['createdby']?.toString() ?? 'System',
      createDate: map['createdat'] != null ? DateTime.tryParse(map['createdat'].toString()) ?? DateTime.now() : DateTime.now(),
      updateBy: map['updatedby']?.toString() ?? 'System',
      updateDate: map['updatedat'] != null ? DateTime.tryParse(map['updatedat'].toString()) ?? DateTime.now() : DateTime.now(),
      isActive: !isDeleted,
    );
  }
}
