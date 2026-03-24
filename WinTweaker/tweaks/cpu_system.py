"""
CPU & System tweaks.
"""
import os
import winreg
from utils.registry import set_reg_value, get_reg_value, delete_reg_value
from utils.system import run_powershell, run_cmd, run_bcdedit

HKLM = winreg.HKEY_LOCAL_MACHINE
HKCU = winreg.HKEY_CURRENT_USER

PRIORITY_KEY = r"SYSTEM\CurrentControlSet\Control\PriorityControl"
POWER_KEY = r"SYSTEM\CurrentControlSet\Control\Power"
THROTTLE_KEY = r"SYSTEM\CurrentControlSet\Control\Power\PowerThrottling"
EXEC_KEY = r"SYSTEM\CurrentControlSet\Control\Session Manager\Executive"

# ── Win32 Priority Separation ────────────────────────────────────────────────

def _set_win32_priority(value):
    def _apply():
        return set_reg_value(HKLM, PRIORITY_KEY, "Win32PrioritySeparation", value)
    return _apply

def _check_win32_priority(value):
    return get_reg_value(HKLM, PRIORITY_KEY, "Win32PrioritySeparation") == value

# ── Tsync Policy ─────────────────────────────────────────────────────────────

def _tsync_legacy():
    run_bcdedit("/set useplatformclock true")
    run_bcdedit("/set disabledynamictick yes")
    return True

def _tsync_enhanced():
    run_bcdedit("/deletevalue useplatformclock")
    run_bcdedit("/set disabledynamictick yes")
    return True

def _tsync_default():
    run_bcdedit("/deletevalue useplatformclock")
    run_bcdedit("/deletevalue disabledynamictick")
    return True

# ── CPU Core Parking ─────────────────────────────────────────────────────────

_PARK_GUID   = "54533251-82be-4824-96c1-47b60b740d00"
_PARK_SUBKEY = "0cc5b647-c1df-4637-891a-dec35c318583"

def _disable_core_parking():
    run_powershell(f"""
        $g = (powercfg /getactivescheme).Split()[3]
        powercfg /setacvalueindex $g {_PARK_GUID} {_PARK_SUBKEY} 100
        powercfg /setdcvalueindex $g {_PARK_GUID} {_PARK_SUBKEY} 100
        powercfg /apply
    """)
    return True

def _enable_core_parking():
    run_powershell(f"""
        $g = (powercfg /getactivescheme).Split()[3]
        powercfg /setacvalueindex $g {_PARK_GUID} {_PARK_SUBKEY} 0
        powercfg /setdcvalueindex $g {_PARK_GUID} {_PARK_SUBKEY} 0
        powercfg /apply
    """)
    return True

def _check_core_parking_disabled():
    ok, out = run_powershell(
        f'powercfg /query SCHEME_CURRENT {_PARK_GUID} {_PARK_SUBKEY}', capture=True
    )
    return "0x00000064" in out  # 100%

# ── CPU Power Throttling ──────────────────────────────────────────────────────

def _disable_cpu_throttle():
    return set_reg_value(HKLM, THROTTLE_KEY, "PowerThrottlingOff", 1)

def _enable_cpu_throttle():
    return delete_reg_value(HKLM, THROTTLE_KEY, "PowerThrottlingOff")

def _check_cpu_throttle_disabled():
    return get_reg_value(HKLM, THROTTLE_KEY, "PowerThrottlingOff") == 1

# ── Platform Idle ─────────────────────────────────────────────────────────────

_IDLE_GUID   = "2e601130-5351-4d9d-8e04-252966bad054"
_IDLE_SUBKEY = "d502f7ee-1dc7-4efd-a55d-f04b6f5c0545"

def _disable_platform_idle():
    run_powershell(f"""
        powercfg /setacvalueindex SCHEME_CURRENT {_IDLE_GUID} {_IDLE_SUBKEY} 1
        powercfg /setdcvalueindex SCHEME_CURRENT {_IDLE_GUID} {_IDLE_SUBKEY} 1
        powercfg /apply
    """)
    return True

def _enable_platform_idle():
    run_powershell(f"""
        powercfg /setacvalueindex SCHEME_CURRENT {_IDLE_GUID} {_IDLE_SUBKEY} 0
        powercfg /setdcvalueindex SCHEME_CURRENT {_IDLE_GUID} {_IDLE_SUBKEY} 0
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

# ── Basic C-States ───────────────────────────────────────────────────────────

def _disable_basic_cstates():
    run_bcdedit("/set disabledynamictick yes")
    run_powershell("""
        powercfg /setacvalueindex SCHEME_CURRENT SUB_PROCESSOR IDLEDISABLE 1
        powercfg /apply
    """)
    return True

def _enable_basic_cstates():
    run_bcdedit("/deletevalue disabledynamictick")
    run_powershell("""
        powercfg /setacvalueindex SCHEME_CURRENT SUB_PROCESSOR IDLEDISABLE 0
        powercfg /apply
    """)
    return True

def _check_cstates_disabled():
    ok, out = run_bcdedit("/enum {current}")
    return "disabledynamictick" in out.lower() and "yes" in out.lower()

# ── Clockwise Timer (Dynamic Tick) ───────────────────────────────────────────

def _disable_clockwise_timer():
    run_bcdedit("/set disabledynamictick yes")
    return True

def _enable_clockwise_timer():
    run_bcdedit("/deletevalue disabledynamictick")
    return True

# ── Modern Standby ───────────────────────────────────────────────────────────

def _disable_modern_standby():
    return set_reg_value(HKLM, POWER_KEY, "PlatformAoAcOverride", 0)

def _enable_modern_standby():
    return delete_reg_value(HKLM, POWER_KEY, "PlatformAoAcOverride")

def _check_modern_standby_disabled():
    return get_reg_value(HKLM, POWER_KEY, "PlatformAoAcOverride") == 0

# ── Energy Performance Preference ────────────────────────────────────────────

def _set_energy_perf_high():
    run_powershell("""
        powercfg /setacvalueindex SCHEME_CURRENT SUB_PROCESSOR PERFBOOSTPOL 100
        powercfg /setacvalueindex SCHEME_CURRENT SUB_PROCESSOR PERFBOOSTMODE 2
        powercfg /apply
    """)
    return True

def _set_energy_perf_default():
    run_powershell("""
        powercfg /setacvalueindex SCHEME_CURRENT SUB_PROCESSOR PERFBOOSTPOL 50
        powercfg /setacvalueindex SCHEME_CURRENT SUB_PROCESSOR PERFBOOSTMODE 1
        powercfg /apply
    """)
    return True

# ── Min/Max Processor State ──────────────────────────────────────────────────

def _set_proc_state_100():
    run_powershell("""
        powercfg /setacvalueindex SCHEME_CURRENT SUB_PROCESSOR PROCTHROTTLEMIN 100
        powercfg /setacvalueindex SCHEME_CURRENT SUB_PROCESSOR PROCTHROTTLEMAX 100
        powercfg /apply
    """)
    return True

def _reset_proc_state():
    run_powershell("""
        powercfg /setacvalueindex SCHEME_CURRENT SUB_PROCESSOR PROCTHROTTLEMIN 5
        powercfg /setacvalueindex SCHEME_CURRENT SUB_PROCESSOR PROCTHROTTLEMAX 100
        powercfg /apply
    """)
    return True

# ── Kernel Worker Threads ─────────────────────────────────────────────────────

def _set_kernel_threads():
    count = os.cpu_count() or 4
    set_reg_value(HKLM, EXEC_KEY, "AdditionalCriticalWorkerThreads", count)
    set_reg_value(HKLM, EXEC_KEY, "AdditionalDelayedWorkerThreads", count)
    return True

def _reset_kernel_threads():
    delete_reg_value(HKLM, EXEC_KEY, "AdditionalCriticalWorkerThreads")
    delete_reg_value(HKLM, EXEC_KEY, "AdditionalDelayedWorkerThreads")
    return True

def _check_kernel_threads():
    return get_reg_value(HKLM, EXEC_KEY, "AdditionalCriticalWorkerThreads") is not None

# ── Disable Overprocessor (Connected Standby) ─────────────────────────────────

def _disable_overprocessor():
    return set_reg_value(HKLM, POWER_KEY, "CsEnabled", 0)

def _enable_overprocessor():
    return delete_reg_value(HKLM, POWER_KEY, "CsEnabled")

def _check_overprocessor_disabled():
    return get_reg_value(HKLM, POWER_KEY, "CsEnabled") == 0


# ── Tweak definitions ─────────────────────────────────────────────────────────

CPU_TWEAKS = [
    {
        "name": "Set Windows 32 Priority Separation",
        "desc": "Optimizes CPU scheduling by giving higher priority to the active foreground application for better performance and responsiveness.",
        "type": "preset",
        "warning": False,
        "presets": [
            {
                "name": "26 hex",
                "desc": "Variable interval, short quantum, 3× foreground boost — best for gaming",
                "recommended": True,
                "apply": _set_win32_priority(0x26),
                "check": lambda: _check_win32_priority(0x26),
            },
            {
                "name": "18 hex",
                "desc": "Variable interval, short quantum, equal foreground/background priority",
                "apply": _set_win32_priority(0x18),
                "check": lambda: _check_win32_priority(0x18),
            },
            {
                "name": "16 hex",
                "desc": "Fixed interval, short quantum, equal priority",
                "apply": _set_win32_priority(0x16),
                "check": lambda: _check_win32_priority(0x16),
            },
            {
                "name": "2A hex",
                "desc": "Fixed interval, short quantum, 3× foreground boost",
                "apply": _set_win32_priority(0x2A),
                "check": lambda: _check_win32_priority(0x2A),
            },
        ],
    },
    {
        "name": "Configure Tsync Policy",
        "desc": "Defines how the OS synchronizes timing events between CPU cores and system timers.",
        "type": "preset",
        "warning": False,
        "presets": [
            {
                "name": "Legacy",
                "desc": "Forces platform clock — lowest timer latency for gaming",
                "recommended": True,
                "apply": _tsync_legacy,
            },
            {
                "name": "Enhanced",
                "desc": "Dynamic tick disabled, platform clock removed",
                "apply": _tsync_enhanced,
            },
            {
                "name": "Default",
                "desc": "Windows default timer synchronization behavior",
                "apply": _tsync_default,
            },
        ],
    },
    {
        "name": "Disable CPU Core Parking",
        "desc": "Prevents Windows from parking CPU cores, keeping all cores active to reduce latency spikes.",
        "type": "toggle",
        "warning": False,
        "enable": _disable_core_parking,
        "disable": _enable_core_parking,
        "check": _check_core_parking_disabled,
    },
    {
        "name": "Disable CPU Power Throttling",
        "desc": "Disables Windows CPU power throttling to maintain full CPU performance at all times.",
        "type": "toggle",
        "warning": False,
        "enable": _disable_cpu_throttle,
        "disable": _enable_cpu_throttle,
        "check": _check_cpu_throttle_disabled,
    },
    {
        "name": "Disable Platform Idle",
        "desc": "Prevents the processor from entering idle states for maximum responsiveness.",
        "type": "toggle",
        "warning": True,
        "enable": _disable_platform_idle,
        "disable": _enable_platform_idle,
    },
    {
        "name": "Processor Idle Disable",
        "desc": "Disables low-power idle states to allow cores to stay fully active.",
        "type": "toggle",
        "warning": True,
        "enable": _disable_proc_idle,
        "disable": _enable_proc_idle,
    },
    {
        "name": "Disable Basic C-States",
        "desc": "Disables CPU power-saving C-states to reduce latency and improve consistency.",
        "type": "toggle",
        "warning": True,
        "enable": _disable_basic_cstates,
        "disable": _enable_basic_cstates,
        "check": _check_cstates_disabled,
    },
    {
        "name": "Disable Clockwise Timer",
        "desc": "Disables dynamic tick to maintain consistent timer intervals for better performance.",
        "type": "toggle",
        "warning": False,
        "enable": _disable_clockwise_timer,
        "disable": _enable_clockwise_timer,
    },
    {
        "name": "Disable Modern Standby",
        "desc": "Disables Windows Modern Standby (S0 low-power idle) to prevent background CPU activity.",
        "type": "toggle",
        "warning": True,
        "enable": _disable_modern_standby,
        "disable": _enable_modern_standby,
        "check": _check_modern_standby_disabled,
    },
    {
        "name": "Set Energy Performance Preference",
        "desc": "Sets CPU performance boost policy to maximum for sustained high performance.",
        "type": "toggle",
        "warning": False,
        "enable": _set_energy_perf_high,
        "disable": _set_energy_perf_default,
    },
    {
        "name": "Set Minimum and Maximum Processor State",
        "desc": "Locks CPU at 100% min/max state to prevent frequency scaling mid-game.",
        "type": "toggle",
        "warning": True,
        "enable": _set_proc_state_100,
        "disable": _reset_proc_state,
    },
    {
        "name": "Set Kernel Worker Threads",
        "desc": "Increases kernel worker threads based on CPU core count for better multi-threaded performance.",
        "type": "toggle",
        "warning": False,
        "enable": _set_kernel_threads,
        "disable": _reset_kernel_threads,
        "check": _check_kernel_threads,
    },
    {
        "name": "Disable Overprocessor",
        "desc": "Disables Windows Connected Standby (CsEnabled) to reduce background CPU overhead.",
        "type": "toggle",
        "warning": True,
        "enable": _disable_overprocessor,
        "disable": _enable_overprocessor,
        "check": _check_overprocessor_disabled,
    },
]

# ── FREE: Disable GameDVR ─────────────────────────────────────────────────────

_GAMEDVR_KEY = r"SOFTWARE\Microsoft\Windows\CurrentVersion\GameDVR"
_GAMECFG_KEY = r"System\GameConfigStore"

def _disable_gamedvr():
    set_reg_value(HKCU, _GAMEDVR_KEY, "AppCaptureEnabled", 0)
    set_reg_value(HKCU, _GAMECFG_KEY, "GameDVR_Enabled", 0)
    set_reg_value(HKCU, _GAMECFG_KEY, "GameDVR_FSEBehaviorMode", 2)
    return True

def _enable_gamedvr():
    set_reg_value(HKCU, _GAMEDVR_KEY, "AppCaptureEnabled", 1)
    delete_reg_value(HKCU, _GAMECFG_KEY, "GameDVR_Enabled")
    delete_reg_value(HKCU, _GAMECFG_KEY, "GameDVR_FSEBehaviorMode")
    return True

def _check_gamedvr_disabled():
    return get_reg_value(HKCU, _GAMEDVR_KEY, "AppCaptureEnabled") == 0

# ── FREE: Disable Fullscreen Optimizations ────────────────────────────────────

_GFXDRIVERS_KEY = r"SYSTEM\CurrentControlSet\Control\GraphicsDrivers"

def _disable_fso():
    set_reg_value(HKCU, _GAMECFG_KEY, "GameDVR_DXGIHonorFSEWindowScaling", 1)
    set_reg_value(HKLM, _GFXDRIVERS_KEY, "DisableWriteCombining", 1)
    return True

def _enable_fso():
    delete_reg_value(HKCU, _GAMECFG_KEY, "GameDVR_DXGIHonorFSEWindowScaling")
    delete_reg_value(HKLM, _GFXDRIVERS_KEY, "DisableWriteCombining")
    return True

def _check_fso_disabled():
    return get_reg_value(HKCU, _GAMECFG_KEY, "GameDVR_DXGIHonorFSEWindowScaling") == 1

# ── FREE: Set High Performance Power Plan ─────────────────────────────────────

def _set_high_perf_plan():
    run_powershell("powercfg /setactive 8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c")
    return True

def _set_balanced_plan():
    run_powershell("powercfg /setactive 381b4222-f694-41f0-9685-ff5bb260df2e")
    return True

# ── FREE: Disable Hibernate ───────────────────────────────────────────────────

def _disable_hibernate():
    run_powershell("powercfg /hibernate off")
    return True

def _enable_hibernate():
    run_powershell("powercfg /hibernate on")
    return True

# ── FREE: Set Visual Performance for Best Performance ────────────────────────

_VISUAL_KEY = r"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects"

def _set_visual_perf():
    set_reg_value(HKCU, _VISUAL_KEY, "VisualFXSetting", 2)
    run_powershell(r"""
        $path = "HKCU:\Control Panel\Desktop"
        Set-ItemProperty $path UserPreferencesMask -Value ([byte[]](0x90,0x12,0x03,0x80,0x10,0x00,0x00,0x00)) -ErrorAction SilentlyContinue
    """)
    return True

def _reset_visual_perf():
    set_reg_value(HKCU, _VISUAL_KEY, "VisualFXSetting", 0)
    return True

def _check_visual_perf():
    return get_reg_value(HKCU, _VISUAL_KEY, "VisualFXSetting") == 2

# ── FREE: Disable Windows Error Reporting ────────────────────────────────────

_WER_KEY = r"SOFTWARE\Microsoft\Windows\Windows Error Reporting"

def _disable_wer():
    set_reg_value(HKLM, _WER_KEY, "Disabled", 1)
    run_powershell("""
        Stop-Service  -Name WerSvc -ErrorAction SilentlyContinue
        Set-Service   -Name WerSvc -StartupType Disabled -ErrorAction SilentlyContinue
    """)
    return True

def _enable_wer():
    delete_reg_value(HKLM, _WER_KEY, "Disabled")
    run_powershell("Set-Service -Name WerSvc -StartupType Manual -ErrorAction SilentlyContinue")
    return True

def _check_wer_disabled():
    return get_reg_value(HKLM, _WER_KEY, "Disabled") == 1


CPU_FREE_TWEAKS = [
    {
        "name": "Disable GameDVR Recording (FREE)",
        "desc": "Disables Microsoft GameDVR and Xbox Game Bar background recording to free up CPU and GPU resources.",
        "type": "toggle",
        "warning": False,
        "enable": _disable_gamedvr,
        "disable": _enable_gamedvr,
        "check": _check_gamedvr_disabled,
    },
    {
        "name": "Disable Fullscreen Optimizations (FREE)",
        "desc": "Disables Windows fullscreen optimization layer so games run in true exclusive fullscreen mode for lower latency.",
        "type": "toggle",
        "warning": False,
        "enable": _disable_fso,
        "disable": _enable_fso,
        "check": _check_fso_disabled,
    },
    {
        "name": "Set High Performance Power Plan (FREE)",
        "desc": "Activates the Windows High Performance power plan so the CPU never throttles during gaming or heavy workloads.",
        "type": "toggle",
        "warning": False,
        "enable": _set_high_perf_plan,
        "disable": _set_balanced_plan,
    },
    {
        "name": "Disable Hibernate (FREE)",
        "desc": "Disables Windows hibernation to reclaim disk space and prevent wake latency.",
        "type": "toggle",
        "warning": False,
        "enable": _disable_hibernate,
        "disable": _enable_hibernate,
    },
    {
        "name": "Set Visual Effects for Best Performance (FREE)",
        "desc": "Disables Windows visual effects and animations to dedicate more CPU resources to applications.",
        "type": "toggle",
        "warning": False,
        "enable": _set_visual_perf,
        "disable": _reset_visual_perf,
        "check": _check_visual_perf,
    },
    {
        "name": "Disable Windows Error Reporting (FREE)",
        "desc": "Disables the WerSvc service and error reporting so crash dumps don't consume CPU and disk I/O.",
        "type": "toggle",
        "warning": False,
        "enable": _disable_wer,
        "disable": _enable_wer,
        "check": _check_wer_disabled,
    },
]

CPU_TWEAKS = CPU_TWEAKS + CPU_FREE_TWEAKS
