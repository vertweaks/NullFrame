"""
Storage & SSD tweaks (FREE).
"""
import winreg
from utils.registry import set_reg_value, get_reg_value, delete_reg_value
from utils.system import run_powershell, run_cmd

HKLM = winreg.HKEY_LOCAL_MACHINE

_PREFETCH_KEY = r"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management\PrefetchParameters"
_NTFS_KEY     = r"SYSTEM\CurrentControlSet\Control\FileSystem"

# ── FREE: Disable 8.3 Short Name Creation ────────────────────────────────────

def _disable_8dot3():
    run_cmd("fsutil behavior set disable8dot3 1")
    return True

def _enable_8dot3():
    run_cmd("fsutil behavior set disable8dot3 0")
    return True

def _check_8dot3_disabled():
    ok, out = run_cmd("fsutil behavior query disable8dot3", capture=True)
    return "1" in out

# ── FREE: Disable Last Access Time Stamp Updates ──────────────────────────────

def _disable_last_access():
    run_cmd("fsutil behavior set disablelastaccess 1")
    return True

def _enable_last_access():
    run_cmd("fsutil behavior set disablelastaccess 0")
    return True

def _check_last_access_disabled():
    ok, out = run_cmd("fsutil behavior query disablelastaccess", capture=True)
    return "1" in out

# ── FREE: Enable TRIM for SSD ─────────────────────────────────────────────────

def _enable_trim():
    run_cmd("fsutil behavior set DisableDeleteNotify 0")
    return True

def _disable_trim():
    run_cmd("fsutil behavior set DisableDeleteNotify 1")
    return True

def _check_trim_enabled():
    ok, out = run_cmd("fsutil behavior query DisableDeleteNotify", capture=True)
    return "0" in out

# ── FREE: Disable Prefetch & SuperFetch ───────────────────────────────────────

def _disable_prefetch():
    set_reg_value(HKLM, _PREFETCH_KEY, "EnablePrefetcher",  0)
    set_reg_value(HKLM, _PREFETCH_KEY, "EnableSuperfetch",  0)
    return True

def _enable_prefetch():
    set_reg_value(HKLM, _PREFETCH_KEY, "EnablePrefetcher",  3)
    set_reg_value(HKLM, _PREFETCH_KEY, "EnableSuperfetch",  3)
    return True

def _check_prefetch_disabled():
    return get_reg_value(HKLM, _PREFETCH_KEY, "EnablePrefetcher") == 0

# ── FREE: Disable SysMain Service ─────────────────────────────────────────────

def _disable_sysmain():
    run_powershell("""
        Stop-Service  -Name SysMain -ErrorAction SilentlyContinue
        Set-Service   -Name SysMain -StartupType Disabled -ErrorAction SilentlyContinue
    """)
    return True

def _enable_sysmain():
    run_powershell("""
        Set-Service  -Name SysMain -StartupType Automatic -ErrorAction SilentlyContinue
        Start-Service -Name SysMain -ErrorAction SilentlyContinue
    """)
    return True

def _check_sysmain_disabled():
    ok, out = run_powershell(
        "(Get-Service -Name SysMain -ErrorAction SilentlyContinue).StartType", capture=True
    )
    return "disabled" in out.lower()

# ── FREE: Optimize NTFS Memory Usage ─────────────────────────────────────────

def _optimize_ntfs():
    run_cmd("fsutil behavior set memoryusage 2")
    set_reg_value(HKLM, _NTFS_KEY, "NtfsDisable8dot3NameCreation", 1)
    set_reg_value(HKLM, _NTFS_KEY, "NtfsDisableLastAccessUpdate",  1)
    return True

def _reset_ntfs():
    run_cmd("fsutil behavior set memoryusage 1")
    delete_reg_value(HKLM, _NTFS_KEY, "NtfsDisable8dot3NameCreation")
    delete_reg_value(HKLM, _NTFS_KEY, "NtfsDisableLastAccessUpdate")
    return True

def _check_ntfs_optimized():
    return get_reg_value(HKLM, _NTFS_KEY, "NtfsDisable8dot3NameCreation") == 1

# ── FREE: Disable Windows Search Indexing ─────────────────────────────────────

def _disable_search_index():
    run_powershell("""
        Stop-Service  -Name WSearch -ErrorAction SilentlyContinue
        Set-Service   -Name WSearch -StartupType Disabled -ErrorAction SilentlyContinue
    """)
    return True

def _enable_search_index():
    run_powershell("""
        Set-Service  -Name WSearch -StartupType Automatic -ErrorAction SilentlyContinue
        Start-Service -Name WSearch -ErrorAction SilentlyContinue
    """)
    return True

def _check_search_index_disabled():
    ok, out = run_powershell(
        "(Get-Service -Name WSearch -ErrorAction SilentlyContinue).StartType", capture=True
    )
    return "disabled" in out.lower()

# ── FREE: Run Disk Cleanup ────────────────────────────────────────────────────

def _run_disk_cleanup():
    run_powershell(r"""
        $vol = (Get-WmiObject -Class Win32_Volume -Filter "DriveLetter='C:'" -ErrorAction SilentlyContinue).DeviceID
        Start-Process -FilePath "cleanmgr.exe" -ArgumentList "/d C:" -NoNewWindow -ErrorAction SilentlyContinue
    """)
    return True


# ── Tweak definitions ─────────────────────────────────────────────────────────

STORAGE_TWEAKS = [
    {
        "name": "Disable 8.3 Short Name Creation (FREE)",
        "desc": "Disables NTFS 8.3 filename generation to reduce file system overhead and improve directory read speed.",
        "type": "toggle",
        "warning": False,
        "enable": _disable_8dot3,
        "disable": _enable_8dot3,
        "check": _check_8dot3_disabled,
    },
    {
        "name": "Disable Last Access Time Stamp (FREE)",
        "desc": "Stops NTFS from updating the last-accessed timestamp on every file read, reducing disk I/O overhead.",
        "type": "toggle",
        "warning": False,
        "enable": _disable_last_access,
        "disable": _enable_last_access,
        "check": _check_last_access_disabled,
    },
    {
        "name": "Enable TRIM for SSD (FREE)",
        "desc": "Ensures Windows notifies the SSD of deleted blocks so the drive can reclaim and maintain peak write speed.",
        "type": "toggle",
        "warning": False,
        "enable": _enable_trim,
        "disable": _disable_trim,
        "check": _check_trim_enabled,
    },
    {
        "name": "Disable Prefetch & SuperFetch (FREE)",
        "desc": "Disables Windows prefetch and SuperFetch to reduce unnecessary SSD writes and background disk activity.",
        "type": "toggle",
        "warning": False,
        "enable": _disable_prefetch,
        "disable": _enable_prefetch,
        "check": _check_prefetch_disabled,
    },
    {
        "name": "Disable SysMain Service (FREE)",
        "desc": "Disables the SysMain (SuperFetch) background service to reduce idle disk usage and free RAM.",
        "type": "toggle",
        "warning": False,
        "enable": _disable_sysmain,
        "disable": _enable_sysmain,
        "check": _check_sysmain_disabled,
    },
    {
        "name": "Optimize NTFS Memory Usage (FREE)",
        "desc": "Increases NTFS internal memory usage to 2 for better caching, and disables legacy filename overhead.",
        "type": "toggle",
        "warning": False,
        "enable": _optimize_ntfs,
        "disable": _reset_ntfs,
        "check": _check_ntfs_optimized,
    },
    {
        "name": "Disable Windows Search Indexing (FREE)",
        "desc": "Disables the Windows Search indexer service to stop background disk scanning and CPU usage.",
        "type": "toggle",
        "warning": False,
        "enable": _disable_search_index,
        "disable": _enable_search_index,
        "check": _check_search_index_disabled,
    },
    {
        "name": "Run Disk Cleanup (FREE)",
        "desc": "Launches the Windows Disk Cleanup utility to remove temporary files and free up disk space.",
        "type": "apply",
        "warning": False,
        "apply": _run_disk_cleanup,
    },
]
