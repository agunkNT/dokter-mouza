class RumahSakit {
  final int id;
  final String nama;
  final String alamat;
  final String createBy;
  final DateTime createDate;
  final String updateBy;
  final DateTime updateDate;
  final bool isActive;

  RumahSakit({
    required this.id,
    required this.nama,
    required this.alamat,
    required this.createBy,
    required this.createDate,
    required this.updateBy,
    required this.updateDate,
    required this.isActive,
  });

  factory RumahSakit.fromMap(Map<String, dynamic> map) {
    final isDeleted = map['isdeleted'] as bool? ?? false;
    return RumahSakit(
      id: map['id'] as int,
      nama: map['nama']?.toString() ?? '',
      alamat: map['alamat']?.toString() ?? '',
      createBy: map['createdby']?.toString() ?? 'System',
      createDate: map['createdat'] != null ? DateTime.tryParse(map['createdat'].toString()) ?? DateTime.now() : DateTime.now(),
      updateBy: map['updatedby']?.toString() ?? 'System',
      updateDate: map['updatedat'] != null ? DateTime.tryParse(map['updatedat'].toString()) ?? DateTime.now() : DateTime.now(),
      isActive: !isDeleted,
    );
  }
}
