import 'package:flutter/material.dart';
import '../models/admin.dart';
import '../services/auth_service.dart';

class AuthProvider extends ChangeNotifier {
  final AuthService _authService = AuthService();
  Admin? _currentAdmin;
  bool _isLoading = true;
  String? _errorMessage;

  Admin? get currentAdmin => _currentAdmin;
  bool get isLoading => _isLoading;
  bool get isAuthenticated => _currentAdmin != null;
  String? get errorMessage => _errorMessage;

  AuthProvider() {
    checkLoginStatus();
  }

  Future<void> checkLoginStatus() async {
    _isLoading = true;
    notifyListeners();

    try {
      final username = await _authService.getCurrentUsername();
      if (username != null) {
        // Fetch current info
        _currentAdmin = await _authService.login(username, '');
      }
    } catch (_) {
      // Ignored during status check
    } finally {
      _isLoading = false;
      notifyListeners();
    }
  }

  Future<bool> isBiometricAvailable() async {
    return await _authService.isBiometricAvailable();
  }

  Future<bool> loginWithBiometric() async {
    _isLoading = true;
    _errorMessage = null;
    notifyListeners();

    try {
      final admin = await _authService.loginWithBiometric();
      if (admin != null) {
        _currentAdmin = admin;
        _isLoading = false;
        notifyListeners();
        return true;
      } else {
        _errorMessage = 'Autentikasi sidik jari tidak tersedia atau tidak cocok';
      }
    } catch (e) {
      _errorMessage = 'Error sidik jari: $e';
    }

    _isLoading = false;
    notifyListeners();
    return false;
  }

  Future<bool> login(String username, String password) async {
    _isLoading = true;
    _errorMessage = null;
    notifyListeners();

    try {
      final admin = await _authService.login(username, password);
      if (admin != null) {
        _currentAdmin = admin;
        _isLoading = false;
        notifyListeners();
        return true;
      } else {
        _errorMessage = 'Username atau Password salah';
      }
    } catch (e) {
      _errorMessage = 'Gagal terhubung ke database: $e';
    }

    _isLoading = false;
    notifyListeners();
    return false;
  }

  Future<bool> changePassword(String currentPassword, String newPassword) async {
    _isLoading = true;
    _errorMessage = null;
    notifyListeners();

    try {
      final success = await _authService.changePassword(currentPassword, newPassword);
      if (success) {
        _isLoading = false;
        notifyListeners();
        return true;
      } else {
        _errorMessage = 'Password lama tidak sesuai';
      }
    } catch (e) {
      _errorMessage = 'Gagal mengubah password: $e';
    }

    _isLoading = false;
    notifyListeners();
    return false;
  }

  Future<void> logout() async {
    await _authService.logout();
    _currentAdmin = null;
    notifyListeners();
  }
}
