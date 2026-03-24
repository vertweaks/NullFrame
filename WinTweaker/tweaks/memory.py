"""
Memory & RAM tweaks (FREE).
"""
import winreg
from utils.registry import set_reg_value, get_reg_value, delete_reg_value
from utils.system import run_powershell, run_cmd

HKLM = winreg.HKEY_LOCAL_MACHINE

_MEM_KEY = r"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management"

# ── FREE: Disable Paging Executive ────────────────────────────────────────────

def _disable_paging_exec():
    return set_reg_value(HKLM, _MEM_KEY, "DisablePagingExecutive", 1)

def _enable_paging_exec():
    return set_reg_value(HKLM, _MEM_KEY, "DisablePagingExecutive", 0)

def _check_paging_exec_disabled():
    return get_reg_value(HKLM, _MEM_KEY, "DisablePagingExecutive") == 1

# ── FREE: Set Large System Cache ──────────────────────────────────────────────

def _set_large_cache():
    return set_reg_value(HKLM, _MEM_KEY, "LargeSystemCache", 1)

def _reset_large_cache():
    return set_reg_value(HKLM, _MEM_KEY, "LargeSystemCache", 0)

def _check_large_cache():
    return get_reg_value(HKLM, _MEM_KEY, "LargeSystemCache") == 1

# ── FREE: Disable Memory Compression ─────────────────────────────────────────

def _disable_mem_compression():
    run_powershell("Disable-MMAgent -MemoryCompression -ErrorAction SilentlyContinue")
    return True

def _enable_mem_compression():
    run_powershell("Enable-MMAgent -MemoryCompression -ErrorAction SilentlyContinue")
    return True

def _check_mem_compression_disabled():
    ok, out = run_powershell(
        "(Get-MMAgent -ErrorAction SilentlyContinue).MemoryCompression", capture=True
    )
    return "false" in out.lower()

# ── FREE: Clear Standby List ──────────────────────────────────────────────────

def _clear_standby_list():
    run_powershell(r"""
        $code = @"
using System;
using System.Runtime.InteropServices;
public class MemoryHelper {
    [DllImport("ntdll.dll")] public static extern uint NtSetSystemInformation(int InfoClass, IntPtr Info, int Length);
    public static void ClearStandbyList() {
        IntPtr buf = Marshal.AllocHGlobal(4);
        Marshal.WriteInt32(buf, 4);
        NtSetSystemInformation(24, buf, 4);
        Marshal.FreeHGlobal(buf);
    }
}
"@
        Add-Type -TypeDefinition $code -ErrorAction SilentlyContinue
        [MemoryHelper]::ClearStandbyList()
    """)
    return True

# ── FREE: Optimize Virtual Memory (Page File) ─────────────────────────────────

def _optimize_pagefile():
    run_powershell(r"""
        $cs = Get-WmiObject -Class Win32_ComputerSystem -ErrorAction SilentlyContinue
        if ($cs) {
            $cs.AutomaticManagedPagefile = $false
            $cs.Put() | Out-Null
        }
        $pf = Get-WmiObject -Class Win32_PageFileSetting -ErrorAction SilentlyContinue
        if ($pf) {
            $pf.InitialSize = 1024
            $pf.MaximumSize = 4096
            $pf.Put() | Out-Null
        }
    """)
    return True

def _reset_pagefile():
    run_powershell(r"""
        $cs = Get-WmiObject -Class Win32_ComputerSystem -ErrorAction SilentlyContinue
        if ($cs) {
            $cs.AutomaticManagedPagefile = $true
            $cs.Put() | Out-Null
        }
    """)
    return True

# ── FREE: Disable Kernel Crash Dump ──────────────────────────────────────────

_CRASHCTL_KEY = r"SYSTEM\CurrentControlSet\Control\CrashControl"

def _disable_crash_dump():
    set_reg_value(HKLM, _CRASHCTL_KEY, "CrashDumpEnabled", 0)
    return True

def _enable_crash_dump():
    set_reg_value(HKLM, _CRASHCTL_KEY, "CrashDumpEnabled", 7)
    return True

def _check_crash_dump_disabled():
    return get_reg_value(HKLM, _CRASHCTL_KEY, "CrashDumpEnabled") == 0


# ── Tweak definitions ─────────────────────────────────────────────────────────

MEMORY_TWEAKS = [
    {
        "name": "Disable Paging Executive (FREE)",
        "desc": "Keeps kernel and driver code in physical RAM instead of paging to disk, reducing latency spikes.",
        "type": "toggle",
        "warning": False,
        "enable": _disable_paging_exec,
        "disable": _enable_paging_exec,
        "check": _check_paging_exec_disabled,
    },
    {
        "name": "Set Large System Cache (FREE)",
        "desc": "Allows Windows to use more RAM for the file system cache, improving I/O throughput for games and apps.",
        "type": "toggle",
        "warning": False,
        "enable": _set_large_cache,
        "disable": _reset_large_cache,
        "check": _check_large_cache,
    },
    {
        "name": "Disable Memory Compression (FREE)",
        "desc": "Disables Windows RAM compression to eliminate CPU overhead from compressing/decompressing memory pages.",
        "type": "toggle",
        "warning": False,
        "enable": _disable_mem_compression,
        "disable": _enable_mem_compression,
        "check": _check_mem_compression_disabled,
    },
    {
        "name": "Clear Standby Memory List (FREE)",
        "desc": "Immediately flushes the Windows standby RAM list to free physical memory for active processes.",
        "type": "apply",
        "warning": False,
        "apply": _clear_standby_list,
    },
    {
        "name": "Optimize Virtual Memory (Page File) (FREE)",
        "desc": "Sets a fixed page file size (1–4 GB) to reduce fragmentation and paging latency spikes.",
        "type": "toggle",
        "warning": True,
        "enable": _optimize_pagefile,
        "disable": _reset_pagefile,
    },
    {
        "name": "Disable Kernel Crash Dump (FREE)",
        "desc": "Disables writing crash dump files to disk, reducing I/O overhead during system stress.",
        "type": "toggle",
        "warning": False,
        "enable": _disable_crash_dump,
        "disable": _enable_crash_dump,
        "check": _check_crash_dump_disabled,
    },
]
