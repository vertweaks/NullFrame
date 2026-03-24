using System;
using Microsoft.Win32;

namespace NullFrame.Services
{
    public static class RegistryHelper
    {
        public static bool SetValue(RegistryHive hive, string keyPath, string valueName, object value, RegistryValueKind kind)
        {
            try
            {
                using var baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Registry64);
                using var key = baseKey.CreateSubKey(keyPath, true);
                if (key == null) return false;
                key.SetValue(valueName, value, kind);
                return true;
            }
            catch { return false; }
        }

        public static object? GetValue(RegistryHive hive, string keyPath, string valueName, object? defaultValue = null)
        {
            try
            {
                using var baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Registry64);
                using var key = baseKey.OpenSubKey(keyPath);
                return key?.GetValue(valueName, defaultValue) ?? defaultValue;
            }
            catch { return defaultValue; }
        }

        public static bool DeleteValue(RegistryHive hive, string keyPath, string valueName)
        {
            try
            {
                using var baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Registry64);
                using var key = baseKey.OpenSubKey(keyPath, true);
                if (key == null) return true;
                key.DeleteValue(valueName, false);
                return true;
            }
            catch { return false; }
        }

        public static bool ValueExists(RegistryHive hive, string keyPath, string valueName)
        {
            try
            {
                using var baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Registry64);
                using var key = baseKey.OpenSubKey(keyPath);
                return key?.GetValue(valueName) != null;
            }
            catch { return false; }
        }

        // Convenience: set DWORD
        public static bool SetDword(RegistryHive hive, string keyPath, string valueName, int value)
            => SetValue(hive, keyPath, valueName, value, RegistryValueKind.DWord);

        // Convenience: get DWORD
        public static int GetDword(RegistryHive hive, string keyPath, string valueName, int defaultValue = -1)
        {
            var val = GetValue(hive, keyPath, valueName);
            if (val is int i) return i;
            return defaultValue;
        }

        // Convenience: set String
        public static bool SetString(RegistryHive hive, string keyPath, string valueName, string value)
            => SetValue(hive, keyPath, valueName, value, RegistryValueKind.String);
    }
}
