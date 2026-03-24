"""
GPU & Gaming tweaks.
"""
import winreg
from utils.registry import set_reg_value, get_reg_value, delete_reg_value
from utils.system import run_powershell, run_cmd

HKLM = winreg.HKEY_LOCAL_MACHINE

GAMES_KEY   = r"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games"
PROFILE_KEY = r"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile"
POWER_KEY   = r"SYSTEM\CurrentControlSet\Control\Power"
SESSION_KEY = r"SYSTEM\CurrentControlSet\Control\Session Manager"
TCP_KEY     = r"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters"

# ── GPU Priority ──────────────────────────────────────────────────────────────

def _set_gpu_priority_high():
    set_reg_value(HKLM, GAMES_KEY, "GPU Priority", 8)
    set_reg_value(HKLM, GAMES_KEY, "Priority", 6)
    set_reg_value(HKLM, GAMES_KEY, "Scheduling Category", "High", winreg.REG_SZ)
    set_reg_value(HKLM, GAMES_KEY, "SFIO Priority",        "High", winreg.REG_SZ)
    return True

def _set_gpu_priority_default():
    set_reg_value(HKLM, GAMES_KEY, "GPU Priority", 2)
    set_reg_value(HKLM, GAMES_KEY, "Priority", 2)
    set_reg_value(HKLM, GAMES_KEY, "Scheduling Category", "Medium", winreg.REG_SZ)
    set_reg_value(HKLM, GAMES_KEY, "SFIO Priority",        "Normal", winreg.REG_SZ)
    return True

def _check_gpu_priority_high():
    return get_reg_value(HKLM, GAMES_KEY, "GPU Priority") == 8

# ── I/O Priority ──────────────────────────────────────────────────────────────

def _set_io_priority_high():
    set_reg_value(HKLM, GAMES_KEY,   "SFIO Priority",       "High", winreg.REG_SZ)
    set_reg_value(HKLM, PROFILE_KEY, "SystemResponsiveness", 0)
    return True

def _set_io_priority_default():
    set_reg_value(HKLM, GAMES_KEY,   "SFIO Priority",        "Normal", winreg.REG_SZ)
    set_reg_value(HKLM, PROFILE_KEY, "SystemResponsiveness", 20)
    return True

def _check_io_priority_high():
    return get_reg_value(HKLM, PROFILE_KEY, "SystemResponsiveness") == 0

# ── Segment Heap ──────────────────────────────────────────────────────────────

def _disable_segment_heap():
    return set_reg_value(HKLM, SESSION_KEY, "SegmentHeap", 0)

def _enable_segment_heap():
    return delete_reg_value(HKLM, SESSION_KEY, "SegmentHeap")

def _check_segment_heap_disabled():
    return get_reg_value(HKLM, SESSION_KEY, "SegmentHeap") == 0

# ── PCIe ASPM ─────────────────────────────────────────────────────────────────

def _disable_pcie_aspm():
    set_reg_value(HKLM, POWER_KEY, "PlatformAoAcOverride", 0)
    set_reg_value(HKLM, POWER_KEY, "PcieAspmPolicy", 0)
    return True

def _enable_pcie_aspm():
    delete_reg_value(HKLM, POWER_KEY, "PlatformAoAcOverride")
    delete_reg_value(HKLM, POWER_KEY, "PcieAspmPolicy")
    return True

def _check_pcie_aspm_disabled():
    return get_reg_value(HKLM, POWER_KEY, "PcieAspmPolicy") == 0

# ── Disable Platform Idle (GPU alias) ─────────────────────────────────────────

def _disable_platform_idle():
    run_powershell("""
        powercfg /setacvalueindex SCHEME_CURRENT 2e601130-5351-4d9d-8e04-252966bad054 d502f7ee-1dc7-4efd-a55d-f04b6f5c0545 1
        powercfg /setdcvalueindex SCHEME_CURRENT 2e601130-5351-4d9d-8e04-252966bad054 d502f7ee-1dc7-4efd-a55d-f04b6f5c0545 1
        powercfg /apply
    """)
    return True

def _enable_platform_idle():
    run_powershell("""
        powercfg /setacvalueindex SCHEME_CURRENT 2e601130-5351-4d9d-8e04-252966bad054 d502f7ee-1dc7-4efd-a55d-f04b6f5c0545 0
        powercfg /setdcvalueindex SCHEME_CURRENT 2e601130-5351-4d9d-8e04-252966bad054 d502f7ee-1dc7-4efd-a55d-f04b6f5c0545 0
        powercfg /apply
    """)
    return True

# ── Processor Idle Disable ───────────────────────────────────────────────────

def _disable_proc_idle():
    run_powershell("""
        powercfg /setacvalueindex SCHEME_CURRENT SUB_PROCESSOR IDLEDISABLE 1
        powercfg /setdcvalueindex SCHEME_CURRENT SUB_PROCESSOR IDLEDISABLE 1
        powercfg /apply
    """)
    return True

def _enable_proc_idle():
    run_powershell("""
        powercfg /setacvalueindex SCHEME_CURRENT SUB_PROCESSOR IDLEDISABLE 0
        powercfg /setdcvalueindex SCHEME_CURRENT SUB_PROCESSOR IDLEDISABLE 0
        powercfg /apply
    """)
    return True

# ── Kill Explorer Temporarily ─────────────────────────────────────────────────

def _kill_explorer():
    run_powershell("Stop-Process -Name explorer -Force -ErrorAction SilentlyContinue")
    return True

def _restart_explorer():
    run_powershell("Start-Process explorer")
    return True

# ── SBR Connections ───────────────────────────────────────────────────────────

def _set_sbr_connections():
    set_reg_value(HKLM, TCP_KEY, "MaxUserPort",       65534)
    set_reg_value(HKLM, TCP_KEY, "TcpTimedWaitDelay", 30)
    set_reg_value(HKLM, TCP_KEY, "MaxFreeTcbs",        16000)
    return True

def _reset_sbr_connections():
    delete_reg_value(HKLM, TCP_KEY, "MaxUserPort")
    delete_reg_value(HKLM, TCP_KEY, "TcpTimedWaitDelay")
    delete_reg_value(HKLM, TCP_KEY, "MaxFreeTcbs")
    return True

def _check_sbr_connections():
    return get_reg_value(HKLM, TCP_KEY, "MaxUserPort") == 65534


# ── Tweak definitions ─────────────────────────────────────────────────────────

GPU_TWEAKS = [
    {
        "name": "Set GPU Priority for Current Game",
        "desc": "Increases the GPU task scheduling priority for game processes, improving frame pacing and stability.",
        "type": "toggle",
        "warning": False,
        "enable": _set_gpu_priority_high,
        "disable": _set_gpu_priority_default,
        "check": _check_gpu_priority_high,
    },
    {
        "name": "Set I/O Priority for Current Game",
        "desc": "Assigns higher I/O priority to game processes so they get faster access to disk reads and writes.",
        "type": "toggle",
        "warning": False,
        "enable": _set_io_priority_high,
        "disable": _set_io_priority_default,
        "check": _check_io_priority_high,
    },
    {
        "name": "Disable Segment Heap",
        "desc": "Disables UWP/Win32 segment heap to reduce memory overhead and improve performance.",
        "type": "toggle",
        "warning": False,
        "enable": _disable_segment_heap,
        "disable": _enable_segment_heap,
        "check": _check_segment_heap_disabled,
    },
    {
        "name": "Disable PCIe ASPM",
        "desc": "Disables PCIe Active State Power Management so GPU/PCIe devices don't downclock mid-game.",
        "type": "toggle",
        "warning": True,
        "enable": _disable_pcie_aspm,
        "disable": _enable_pcie_aspm,
        "check": _check_pcie_aspm_disabled,
    },
    {
        "name": "Disable Platform Idle",
        "desc": "Prevents the processor from entering idle states so the GPU always has CPU bandwidth available.",
        "type": "toggle",
        "warning": True,
        "enable": _disable_platform_idle,
        "disable": _enable_platform_idle,
    },
    {
        "name": "Processor Idle Disable",
        "desc": "Disables low-power idle states to allow cores to stay fully active during gaming.",
        "type": "toggle",
        "warning": True,
        "enable": _disable_proc_idle,
        "disable": _enable_proc_idle,
    },
    {
        "name": "Temporarily Kill Explorer",
        "desc": "Stops Windows Explorer to free CPU, GPU, and RAM resources. Toggle again to restart Explorer.",
        "type": "toggle",
        "warning": True,
        "enable": _kill_explorer,
        "disable": _restart_explorer,
    },
    {
        "name": "Set SBR Connections",
        "desc": "Optimizes TCP connection limits and wait delays for better in-game network performance.",
        "type": "toggle",
        "warning": False,
        "enable": _set_sbr_connections,
        "disable": _reset_sbr_connections,
        "check": _check_sbr_connections,
    },
]

# ── FREE: Enable Hardware Accelerated GPU Scheduling (HAGS) ──────────────────

_GFXDRIVERS_KEY = r"SYSTEM\CurrentControlSet\Control\GraphicsDrivers"

def _enable_hags():
    set_reg_value(HKLM, _GFXDRIVERS_KEY, "HwSchMode", 2)
    return True

def _disable_hags():
    set_reg_value(HKLM, _GFXDRIVERS_KEY, "HwSchMode", 1)
    return True

def _check_hags_enabled():
    return get_reg_value(HKLM, _GFXDRIVERS_KEY, "HwSchMode") == 2

# ── FREE: Disable Xbox Game Bar ───────────────────────────────────────────────

import winreg as _wreg
_HKCU = _wreg.HKEY_CURRENT_USER
_GAMEBAR_KEY = r"SOFTWARE\Microsoft\Windows\CurrentVersion\GameDVR"
_GAMECFG_KEY = r"System\GameConfigStore"

def _disable_xbox_gamebar():
    set_reg_value(_HKCU, _GAMEBAR_KEY, "AppCaptureEnabled", 0)
    set_reg_value(_HKCU, _GAMECFG_KEY, "GameDVR_Enabled", 0)
    run_powershell("Get-AppxPackage Microsoft.XboxGamingOverlay | Remove-AppxPackage -ErrorAction SilentlyContinue")
    return True

def _enable_xbox_gamebar():
    set_reg_value(_HKCU, _GAMEBAR_KEY, "AppCaptureEnabled", 1)
    set_reg_value(_HKCU, _GAMECFG_KEY, "GameDVR_Enabled", 1)
    return True

def _check_xbox_gamebar_disabled():
    return get_reg_value(_HKCU, _GAMEBAR_KEY, "AppCaptureEnabled") == 0

# ── FREE: Disable Nagle's Algorithm ──────────────────────────────────────────

def _disable_nagle():
    set_reg_value(HKLM, TCP_KEY, "TcpAckFrequency", 1)
    set_reg_value(HKLM, TCP_KEY, "TCPNoDelay", 1)
    return True

def _enable_nagle():
    delete_reg_value(HKLM, TCP_KEY, "TcpAckFrequency")
    delete_reg_value(HKLM, TCP_KEY, "TCPNoDelay")
    return True

def _check_nagle_disabled():
    return get_reg_value(HKLM, TCP_KEY, "TCPNoDelay") == 1


GPU_FREE_TWEAKS = [
    {
        "name": "Enable Hardware Accelerated GPU Scheduling (FREE)",
        "desc": "Enables HAGS to allow the GPU to manage its own video memory, reducing CPU overhead and latency.",
        "type": "toggle",
        "warning": False,
        "enable": _enable_hags,
        "disable": _disable_hags,
        "check": _check_hags_enabled,
    },
    {
        "name": "Disable Xbox Game Bar (FREE)",
        "desc": "Disables the Xbox Game Bar overlay and background capture to free GPU and CPU resources while gaming.",
        "type": "toggle",
        "warning": False,
        "enable": _disable_xbox_gamebar,
        "disable": _enable_xbox_gamebar,
        "check": _check_xbox_gamebar_disabled,
    },
    {
        "name": "Disable Nagle's Algorithm (FREE)",
        "desc": "Disables Nagle's algorithm to send packets immediately without buffering, reducing in-game network latency.",
        "type": "toggle",
        "warning": False,
        "enable": _disable_nagle,
        "disable": _enable_nagle,
        "check": _check_nagle_disabled,
    },
]

GPU_TWEAKS = GPU_TWEAKS + GPU_FREE_TWEAKS
