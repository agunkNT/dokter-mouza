import 'package:bcrypt/bcrypt.dart';
import 'package:local_auth/local_auth.dart';
import 'package:shared_preferences/shared_preferences.dart';
import '../models/admin.dart';
import 'database_service.dart';

class AuthService {
  static const String _keyUsername = 'auth_username';
  static const String _keyRole = 'auth_role';
  static const String _keyAdminId = 'auth_admin_id';

  final DatabaseService _db = DatabaseService();
  final LocalAuthentication _localAuth = LocalAuthentication();

  Future<Admin?> login(String username, String password) async {
    try {
      final admin = await _db.getAdminByUsername(username);
      if (admin == null) return null;

      // Verify BCrypt password
      bool isMatch = false;
      try {
        isMatch = BCrypt.checkpw(password, admin.password);
      } catch (e) {
        // Fallback check
        isMatch = (password == admin.password);
      }

      if (isMatch) {
        await _db.updateAdminLastLogin(admin.id);
        final prefs = await SharedPreferences.getInstance();
        await prefs.setString(_keyUsername, admin.username);
        await prefs.setString(_keyRole, admin.role);
        await prefs.setString(_keyAdminId, admin.id);
        return admin;
      }
    } catch (e) {
      rethrow;
    }
    return null;
  }

  Future<bool> isBiometricAvailable() async {
    try {
      final canAuthenticateWithBiometrics = await _localAuth.canCheckBiometrics;
      final isDeviceSupported = await _localAuth.isDeviceSupported();
      return canAuthenticateWithBiometrics && isDeviceSupported;
    } catch (_) {
      return false;
    }
  }

  Future<Admin?> loginWithBiometric() async {
    try {
      final available = await isBiometricAvailable();
      if (!available) return null;

      final prefs = await SharedPreferences.getInstance();
      final lastUsername = prefs.getString(_keyUsername);
      if (lastUsername == null || lastUsername.isEmpty) return null;

      final authenticated = await _localAuth.authenticate(
        localizedReason: 'Pindai sidik jari Anda untuk masuk ke MAN APPS',
      );

      if (authenticated) {
        final admin = await _db.getAdminByUsername(lastUsername);
        if (admin != null) {
          await _db.updateAdminLastLogin(admin.id);
          await prefs.setString(_keyUsername, admin.username);
          await prefs.setString(_keyRole, admin.role);
          await prefs.setString(_keyAdminId, admin.id);
          return admin;
        }
      }
    } catch (_) {}
    return null;
  }

  Future<bool> changePassword(String currentPassword, String newPassword) async {
    final prefs = await SharedPreferences.getInstance();
    final username = prefs.getString(_keyUsername);
    if (username == null) return false;

    final admin = await _db.getAdminByUsername(username);
    if (admin == null) return false;

    bool isMatch = false;
    try {
      isMatch = BCrypt.checkpw(currentPassword, admin.password);
    } catch (e) {
      isMatch = (currentPassword == admin.password);
    }

    if (!isMatch) return false;

    final newHash = BCrypt.hashpw(newPassword, BCrypt.gensalt());
    return await _db.updateAdminPassword(username, newHash, username);
  }

  Future<void> logout() async {
    final prefs = await SharedPreferences.getInstance();
    await prefs.remove(_keyUsername);
    await prefs.remove(_keyRole);
    await prefs.remove(_keyAdminId);
  }

  Future<String?> getCurrentUsername() async {
    final prefs = await SharedPreferences.getInstance();
    return prefs.getString(_keyUsername);
  }

  Future<bool> isLoggedIn() async {
    final prefs = await SharedPreferences.getInstance();
    return prefs.containsKey(_keyUsername);
  }
}
