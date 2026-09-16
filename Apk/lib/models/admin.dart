class Admin {
  final String id;
  final String username;
  final String password;
  final String role;
  final DateTime? lastLogin;
  final String createBy;
  final DateTime createDate;
  final String updateBy;
  final DateTime updateDate;
  final bool isActive;

  Admin({
    required this.id,
    required this.username,
    required this.password,
    required this.role,
    this.lastLogin,
    required this.createBy,
    required this.createDate,
    required this.updateBy,
    required this.updateDate,
    required this.isActive,
  });

  factory Admin.fromMap(Map<String, dynamic> map) {
    String roleStr = 'Admin';
    final rawRole = map['role'];
    if (rawRole != null) {
      if (rawRole.toString() == '0') {
        roleStr = 'SuperAdmin';
      } else if (rawRole.toString() == '1') {
        roleStr = 'Dokter';
      } else {
        roleStr = rawRole.toString();
      }
    }

    return Admin(
      id: map['id']?.toString() ?? '',
      username: map['username']?.toString() ?? '',
      password: map['password']?.toString() ?? '',
      role: roleStr,
      lastLogin: map['lastlogin'] != null ? DateTime.tryParse(map['lastlogin'].toString()) : null,
      createBy: map['createdby']?.toString() ?? map['createby']?.toString() ?? 'System',
      createDate: map['createddate'] != null ? DateTime.tryParse(map['createddate'].toString()) ?? DateTime.now() : DateTime.now(),
      updateBy: map['updateby']?.toString() ?? 'System',
      updateDate: map['updatedate'] != null ? DateTime.tryParse(map['updatedate'].toString()) ?? DateTime.now() : DateTime.now(),
      isActive: map['isactive'] as bool? ?? true,
    );
  }
}
