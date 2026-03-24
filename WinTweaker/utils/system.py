"""
System helpers: admin check, PowerShell/CMD execution, bcdedit wrappers.
"""
import ctypes
import sys
import subprocess
import os


def check_admin():
    """Return True if the process is running as Administrator."""
    try:
        return bool(ctypes.windll.shell32.IsUserAnAdmin())
    except Exception:
        return False


def run_as_admin():
    """Re-launch the current script with administrator privileges."""
    ctypes.windll.shell32.ShellExecuteW(
        None, "runas", sys.executable, " ".join(sys.argv), None, 1
    )


def run_powershell(command, capture=False):
    """
    Run a PowerShell command.
    Returns (success: bool, output: str).
    """
    try:
        result = subprocess.run(
            [
                "powershell",
                "-NonInteractive",
                "-NoProfile",
                "-ExecutionPolicy", "Bypass",
                "-Command", command,
            ],
            capture_output=capture,
            text=True,
            creationflags=subprocess.CREATE_NO_WINDOW,
        )
        return result.returncode == 0, (result.stdout or "").strip()
    except Exception as e:
        return False, str(e)


def run_cmd(command, capture=False):
    """
    Run a shell/CMD command.
    Returns (success: bool, output: str).
    """
    try:
        result = subprocess.run(
            command,
            shell=True,
            capture_output=capture,
            text=True,
            creationflags=subprocess.CREATE_NO_WINDOW,
        )
        return result.returncode == 0, (result.stdout or "").strip()
    except Exception as e:
        return False, str(e)


def run_bcdedit(args):
    """Run a bcdedit command. Returns (success, output)."""
    return run_cmd(f'bcdedit {args}', capture=True)


def get_active_power_scheme():
    """Return the GUID of the active power scheme, or empty string on failure."""
    ok, out = run_cmd("powercfg /getactivescheme", capture=True)
    if ok and out:
        parts = out.split()
        for part in parts:
            if len(part) == 36 and part.count("-") == 4:
                return part
    return ""
