"""
Devices & Hardware tweaks.
"""
import winreg
from utils.registry import set_reg_value, get_reg_value, delete_reg_value
from utils.system import run_powershell, run_cmd, run_bcdedit

HKLM = winreg.HKEY_LOCAL_MACHINE

DO_POLICY_KEY = r"SOFTWARE\Policies\Microsoft\Windows\DeliveryOptimization"

# ── Disable Communication Ports (COM) ─────────────────────────────────────────

def _disable_com_ports():
    run_powershell(r"""
        $ports = Get-WmiObject -Class Win32_SerialPort -ErrorAction SilentlyContinue
        foreach ($p in $ports) {
            Disable-PnpDevice -InstanceId $p.PNPDeviceID -Confirm:$false -ErrorAction SilentlyContinue
        }
    """)
    return True

def _enable_com_ports():
    run_powershell(r"""
        $ports = Get-PnpDevice -Class Ports -ErrorAction SilentlyContinue
        foreach ($p in $ports) {
            Enable-PnpDevice -InstanceId $p.InstanceId -Confirm:$false -ErrorAction SilentlyContinue
        }
    """)
    return True

# ── Disable Bus PIC (legacy APIC mode) ───────────────────────────────────────

def _disable_bus_pic():
    run_bcdedit("/set uselegacyapicmode no")
    return True

def _enable_bus_pic():
    run_bcdedit("/deletevalue uselegacyapicmode")
    return True

# ── Disable HPET ─────────────────────────────────────────────────────────────

def _disable_hpet():
    run_bcdedit("/deletevalue useplatformclock")
    run_bcdedit("/set disabledynamictick yes")
    run_powershell(r"""
        $hpet = Get-PnpDevice | Where-Object {$_.FriendlyName -like "*High Precision Event Timer*"} -ErrorAction SilentlyContinue
        if ($hpet) {
            Disable-PnpDevice -InstanceId $hpet.InstanceId -Confirm:$false -ErrorAction SilentlyContinue
        }
    """)
    return True

def _enable_hpet():
    run_bcdedit("/set useplatformclock true")
    run_bcdedit("/deletevalue disabledynamictick")
    run_powershell(r"""
        $hpet = Get-PnpDevice | Where-Object {$_.FriendlyName -like "*High Precision Event Timer*"} -ErrorAction SilentlyContinue
        if ($hpet) {
            Enable-PnpDevice -InstanceId $hpet.InstanceId -Confirm:$false -ErrorAction SilentlyContinue
        }
    """)
    return True

# ── Disable Microsoft Delivery Optimisation Sync ──────────────────────────────

def _disable_do_sync():
    run_powershell(r"""
        Stop-Service  -Name DoSvc -ErrorAction SilentlyContinue
        Set-Service   -Name DoSvc -StartupType Disabled -ErrorAction SilentlyContinue
    """)
    set_reg_value(HKLM, DO_POLICY_KEY, "DODownloadMode", 0)
    return True

def _enable_do_sync():
    run_powershell(r"""
        Set-Service  -Name DoSvc -StartupType Automatic -ErrorAction SilentlyContinue
        Start-Service -Name DoSvc -ErrorAction SilentlyContinue
    """)
    delete_reg_value(HKLM, DO_POLICY_KEY, "DODownloadMode")
    return True

def _check_do_sync_disabled():
    return get_reg_value(HKLM, DO_POLICY_KEY, "DODownloadMode") == 0

# ── Disable Hyper-V Infrastructure Driver ─────────────────────────────────────

_HYPERV_SERVICES = [
    "HvHost", "vmicheartbeat", "vmickvpexchange",
    "vmicrdv", "vmicshutdown", "vmictimesync", "vmicvss",
]

def _disable_hyperv():
    svc_list = " ".join(f'"{s}"' for s in _HYPERV_SERVICES)
    run_powershell(f"""
        foreach ($s in @({svc_list})) {{
            Stop-Service  -Name $s -Force   -ErrorAction SilentlyContinue
            Set-Service   -Name $s -StartupType Disabled -ErrorAction SilentlyContinue
        }}
    """)
    return True

def _enable_hyperv():
    svc_list = " ".join(f'"{s}"' for s in _HYPERV_SERVICES)
    run_powershell(f"""
        foreach ($s in @({svc_list})) {{
            Set-Service -Name $s -StartupType Manual -ErrorAction SilentlyContinue
        }}
    """)
    return True

# ── Disable Remote Desktop Device Redirector Bus ──────────────────────────────

def _disable_rdp_redirector():
    run_powershell(r"""
        Stop-Service -Name RDPDR  -ErrorAction SilentlyContinue
        Set-Service  -Name RDPDR  -StartupType Disabled -ErrorAction SilentlyContinue
        Stop-Service -Name RdpBus -ErrorAction SilentlyContinue
        Set-Service  -Name RdpBus -StartupType Disabled -ErrorAction SilentlyContinue
    """)
    return True

def _enable_rdp_redirector():
    run_powershell(r"""
        Set-Service -Name RDPDR  -StartupType Manual -ErrorAction SilentlyContinue
        Set-Service -Name RdpBus -StartupType Manual -ErrorAction SilentlyContinue
    """)
    return True

# ── Disable Serial Ports ──────────────────────────────────────────────────────

def _disable_serial_ports():
    run_powershell(r"""
        $ports = Get-PnpDevice -Class Ports -ErrorAction SilentlyContinue
        foreach ($p in $ports) {
            Disable-PnpDevice -InstanceId $p.InstanceId -Confirm:$false -ErrorAction SilentlyContinue
        }
        Stop-Service -Name Serial -ErrorAction SilentlyContinue
        Set-Service  -Name Serial -StartupType Disabled -ErrorAction SilentlyContinue
    """)
    return True

def _enable_serial_ports():
    run_powershell(r"""
        $ports = Get-PnpDevice -Class Ports -ErrorAction SilentlyContinue
        foreach ($p in $ports) {
            Enable-PnpDevice -InstanceId $p.InstanceId -Confirm:$false -ErrorAction SilentlyContinue
        }
        Set-Service -Name Serial -StartupType Manual -ErrorAction SilentlyContinue
    """)
    return True


# ── Tweak definitions ─────────────────────────────────────────────────────────

DEVICE_TWEAKS = [
    {
        "name": "Disable Communication Ports",
        "desc": "Disables COM/serial communication ports that are not in use to reduce interrupt overhead.",
        "type": "toggle",
        "warning": True,
        "enable": _disable_com_ports,
        "disable": _enable_com_ports,
    },
    {
        "name": "Disable Bus Programmable Interrupt Controller",
        "desc": "Switches from legacy APIC/PIC mode to modern IOAPIC for better interrupt routing and lower latency.",
        "type": "toggle",
        "warning": True,
        "enable": _disable_bus_pic,
        "disable": _enable_bus_pic,
    },
    {
        "name": "Disable High Precision Event Timer",
        "desc": "Disables HPET and enables TSC-based timing for lower timer latency on modern CPUs.",
        "type": "toggle",
        "warning": True,
        "enable": _disable_hpet,
        "disable": _enable_hpet,
    },
    {
        "name": "Disable Microsoft Do Moveable Sync",
        "desc": "Disables Windows Delivery Optimisation background sync service to free network and CPU resources.",
        "type": "toggle",
        "warning": False,
        "enable": _disable_do_sync,
        "disable": _enable_do_sync,
        "check": _check_do_sync_disabled,
    },
    {
        "name": "Disable Microsoft Hyper-V Infrastructure Driver",
        "desc": "Disables Hyper-V virtualisation services and drivers, freeing CPU overhead for native performance.",
        "type": "toggle",
        "warning": True,
        "enable": _disable_hyperv,
        "disable": _enable_hyperv,
    },
    {
        "name": "Disable Remote Desktop Device Redirector Bus",
        "desc": "Disables the RDP device redirection bus driver to reduce driver overhead when not using Remote Desktop.",
        "type": "toggle",
        "warning": False,
        "enable": _disable_rdp_redirector,
        "disable": _enable_rdp_redirector,
    },
    {
        "name": "Disable Serial Ports",
        "desc": "Disables all serial port devices to reduce IRQ and driver overhead.",
        "type": "toggle",
        "warning": True,
        "enable": _disable_serial_ports,
        "disable": _enable_serial_ports,
    },
]
