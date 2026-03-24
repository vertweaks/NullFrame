"""
Registry helper utilities for reading and writing Windows registry values.
"""
import winreg


def set_reg_value(hive, key_path, value_name, value, value_type=winreg.REG_DWORD):
    """Write a value to the Windows registry. Returns True on success."""
    try:
        key = winreg.CreateKeyEx(
            hive, key_path, 0,
            winreg.KEY_SET_VALUE | winreg.KEY_WOW64_64KEY
        )
        winreg.SetValueEx(key, value_name, 0, value_type, value)
        winreg.CloseKey(key)
        return True
    except Exception as e:
        print(f"[Registry] Write error — {key_path}\\{value_name}: {e}")
        return False


def get_reg_value(hive, key_path, value_name, default=None):
    """Read a value from the Windows registry. Returns default if not found."""
    try:
        key = winreg.OpenKey(
            hive, key_path, 0,
            winreg.KEY_READ | winreg.KEY_WOW64_64KEY
        )
        value, _ = winreg.QueryValueEx(key, value_name)
        winreg.CloseKey(key)
        return value
    except Exception:
        return default


def delete_reg_value(hive, key_path, value_name):
    """Delete a registry value. Returns True on success."""
    try:
        key = winreg.OpenKey(
            hive, key_path, 0,
            winreg.KEY_SET_VALUE | winreg.KEY_WOW64_64KEY
        )
        winreg.DeleteValue(key, value_name)
        winreg.CloseKey(key)
        return True
    except Exception:
        return False


def reg_value_exists(hive, key_path, value_name):
    """Check if a registry value exists."""
    return get_reg_value(hive, key_path, value_name) is not None
