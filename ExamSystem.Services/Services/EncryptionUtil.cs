using System;
using System.Security.Cryptography;
using System.Text;

namespace ExamSystem.Services
{
  /// <summary>
  /// 基于Windows DPAPI的加解密工具。
  /// </summary>
  public static class EncryptionUtil
  {
    public static string ProtectString(string plain)
    {
      if (plain == null) plain = string.Empty;
      var bytes = Encoding.UTF8.GetBytes(plain);
      var protectedBytes = ProtectedData.Protect(bytes, null, DataProtectionScope.CurrentUser);
      return Convert.ToBase64String(protectedBytes);
    }

    public static string UnprotectString(string protectedBase64)
    {
      if (string.IsNullOrEmpty(protectedBase64)) return string.Empty;
      var protectedBytes = Convert.FromBase64String(protectedBase64);
      var bytes = ProtectedData.Unprotect(protectedBytes, null, DataProtectionScope.CurrentUser);
      return Encoding.UTF8.GetString(bytes);
    }

    public static string Sha256Hash(string input)
    {
      using var sha = SHA256.Create();
      var bytes = Encoding.UTF8.GetBytes(input ?? string.Empty);
      var hash = sha.ComputeHash(bytes);
      return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    }
  }
}