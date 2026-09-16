class Diagnosa {
  final int id;
  final String kodeICD;
  final String nama;
  final String createBy;
  final DateTime createDate;
  final String updateBy;
  final DateTime updateDate;
  final bool isActive;

  Diagnosa({
    required this.id,
    required this.kodeICD,
    required this.nama,
    required this.createBy,
    required this.createDate,
    required this.updateBy,
    required this.updateDate,
    required this.isActive,
  });

  factory Diagnosa.fromMap(Map<String, dynamic> map) {
    final isDeleted = map['isdeleted'] as bool? ?? false;
    return Diagnosa(
      id: map['id'] as int,
      kodeICD: map['kodeicd']?.toString() ?? '',
      nama: map['nama']?.toString() ?? '',
      createBy: map['createdby']?.toString() ?? 'System',
      createDate: map['createdat'] != null ? DateTime.tryParse(map['createdat'].toString()) ?? DateTime.now() : DateTime.now(),
      updateBy: map['updatedby']?.toString() ?? 'System',
      updateDate: map['updatedat'] != null ? DateTime.tryParse(map['updatedat'].toString()) ?? DateTime.now() : DateTime.now(),
      isActive: !isDeleted,
    );
  }
}
