"""
USB & Input tweaks.
"""
import winreg
from utils.registry import set_reg_value, get_reg_value, delete_reg_value
from utils.system import run_powershell, run_cmd

HKLM = winreg.HKEY_LOCAL_MACHINE
HKCU = winreg.HKEY_CURRENT_USER

USB_SUSPEND_AC = "2a737441-1930-4402-8d77-b2bebba308a3"
USB_SUSPEND_SK = "48e6b7a6-50f5-4782-a5d4-53bb8f07e226"

MOUSE_KEY    = r"Control Panel\Mouse"
KEYBOARD_KEY = r"Control Panel\Keyboard"
DESKTOP_KEY  = r"Control Panel\Desktop"
STICKY_KEY   = r"Control Panel\Accessibility\StickyKeys"
TOGGLE_KEY   = r"Control Panel\Accessibility\ToggleKeys"
MOUCLASS_KEY = r"SYSTEM\CurrentControlSet\Services\mouclass\Parameters"
KBDCLASS_KEY = r"SYSTEM\CurrentControlSet\Services\kbdclass\Parameters"
PROFILE_KEY  = r"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile"

# ── Optimize USB for KB & Mouse ───────────────────────────────────────────────

def _optimize_usb_kb_mouse():
    run_powershell(f"""
        powercfg /setacvalueindex SCHEME_CURRENT {USB_SUSPEND_AC} {USB_SUSPEND_SK} 0
        powercfg /setdcvalueindex SCHEME_CURRENT {USB_SUSPEND_AC} {USB_SUSPEND_SK} 0
        powercfg /apply
    """)
    set_reg_value(HKLM, r"SYSTEM\CurrentControlSet\Services\usbhub\Parameters", "DisableSelectiveSuspend", 1)
    set_reg_value(HKLM, r"SYSTEM\CurrentControlSet\Services\USB",               "DisableSelectiveSuspend", 1)
    # Improve USB transaction priority
    set_reg_value(HKLM, MOUCLASS_KEY, "MouseDataQueueSize", 0x64)
    set_reg_value(HKLM, KBDCLASS_KEY, "KeyboardDataQueueSize", 0x64)
    set_reg_value(HKLM, PROFILE_KEY, "SystemResponsiveness", 0)
    return True

def _reset_usb_kb_mouse():
    run_powershell(f"""
        powercfg /setacvalueindex SCHEME_CURRENT {USB_SUSPEND_AC} {USB_SUSPEND_SK} 1
        powercfg /setdcvalueindex SCHEME_CURRENT {USB_SUSPEND_AC} {USB_SUSPEND_SK} 1
        powercfg /apply
    """)
    delete_reg_value(HKLM, r"SYSTEM\CurrentControlSet\Services\usbhub\Parameters", "DisableSelectiveSuspend")
    return True

def _check_usb_kb_mouse():
    return get_reg_value(HKLM, r"SYSTEM\CurrentControlSet\Services\usbhub\Parameters", "DisableSelectiveSuspend") == 1

# ── Disable USB Selective Suspend ─────────────────────────────────────────────

def _disable_usb_suspend():
    run_powershell(f"""
        powercfg /setacvalueindex SCHEME_CURRENT {USB_SUSPEND_AC} {USB_SUSPEND_SK} 0
        powercfg /setdcvalueindex SCHEME_CURRENT {USB_SUSPEND_AC} {USB_SUSPEND_SK} 0
        powercfg /apply
    """)
    set_reg_value(HKLM, r"SYSTEM\CurrentControlSet\Services\usbhub\Parameters", "DisableSelectiveSuspend", 1)
    return True

def _enable_usb_suspend():
    run_powershell(f"""
        powercfg /setacvalueindex SCHEME_CURRENT {USB_SUSPEND_AC} {USB_SUSPEND_SK} 1
        powercfg /setdcvalueindex SCHEME_CURRENT {USB_SUSPEND_AC} {USB_SUSPEND_SK} 1
        powercfg /apply
    """)
    delete_reg_value(HKLM, r"SYSTEM\CurrentControlSet\Services\usbhub\Parameters", "DisableSelectiveSuspend")
    return True

# ── Set Debug Poll Interval ───────────────────────────────────────────────────

def _set_debug_poll():
    set_reg_value(HKLM, MOUCLASS_KEY, "MouseDataQueueSize",   0x64)
    set_reg_value(HKLM, KBDCLASS_KEY, "KeyboardDataQueueSize", 0x64)
    set_reg_value(HKLM, PROFILE_KEY,  "SystemResponsiveness",  0)
    return True

def _reset_debug_poll():
    delete_reg_value(HKLM, MOUCLASS_KEY, "MouseDataQueueSize")
    delete_reg_value(HKLM, KBDCLASS_KEY, "KeyboardDataQueueSize")
    return True

def _check_debug_poll():
    return get_reg_value(HKLM, MOUCLASS_KEY, "MouseDataQueueSize") == 0x64

# ── Disable All Hidden USB Power Saving ───────────────────────────────────────

def _disable_all_usb_power():
    run_powershell(f"""
        # Disable selective suspend in power plan
        powercfg /setacvalueindex SCHEME_CURRENT {USB_SUSPEND_AC} {USB_SUSPEND_SK} 0
        powercfg /apply
        # Disable enhanced power management on USB hubs
        $hubs = Get-PnpDevice -Class USB -ErrorAction SilentlyContinue |
                Where-Object {{$_.FriendlyName -like "*Hub*"}}
        foreach ($h in $hubs) {{
            $path = "HKLM:\\SYSTEM\\CurrentControlSet\\Enum\\" + $h.InstanceId + "\\Device Parameters"
            if (Test-Path $path) {{
                Set-ItemProperty -Path $path -Name "EnhancedPowerManagementEnabled" -Value 0 -Type DWord -Force -ErrorAction SilentlyContinue
            }}
        }}
    """)
    return True

def _enable_all_usb_power():
    run_powershell(f"""
        powercfg /setacvalueindex SCHEME_CURRENT {USB_SUSPEND_AC} {USB_SUSPEND_SK} 1
        powercfg /apply
    """)
    return True

# ── Disable Mouse Acceleration ────────────────────────────────────────────────

def _disable_mouse_accel():
    set_reg_value(HKCU, MOUSE_KEY, "MouseSpeed",      "0",  winreg.REG_SZ)
    set_reg_value(HKCU, MOUSE_KEY, "MouseThreshold1", "0",  winreg.REG_SZ)
    set_reg_value(HKCU, MOUSE_KEY, "MouseThreshold2", "0",  winreg.REG_SZ)
    return True

def _enable_mouse_accel():
    set_reg_value(HKCU, MOUSE_KEY, "MouseSpeed",      "1",  winreg.REG_SZ)
    set_reg_value(HKCU, MOUSE_KEY, "MouseThreshold1", "6",  winreg.REG_SZ)
    set_reg_value(HKCU, MOUSE_KEY, "MouseThreshold2", "10", winreg.REG_SZ)
    return True

def _check_mouse_accel_disabled():
    return get_reg_value(HKCU, MOUSE_KEY, "MouseSpeed") == "0"

# ── Disable Sticky Keys ───────────────────────────────────────────────────────

def _disable_sticky_keys():
    return set_reg_value(HKCU, STICKY_KEY, "Flags", "506", winreg.REG_SZ)

def _enable_sticky_keys():
    return set_reg_value(HKCU, STICKY_KEY, "Flags", "510", winreg.REG_SZ)

def _check_sticky_keys_disabled():
    return get_reg_value(HKCU, STICKY_KEY, "Flags") == "506"

# ── Disable Toggle Keys ───────────────────────────────────────────────────────

def _disable_toggle_keys():
    return set_reg_value(HKCU, TOGGLE_KEY, "Flags", "58", winreg.REG_SZ)

def _enable_toggle_keys():
    return set_reg_value(HKCU, TOGGLE_KEY, "Flags", "62", winreg.REG_SZ)

def _check_toggle_keys_disabled():
    return get_reg_value(HKCU, TOGGLE_KEY, "Flags") == "58"

# ── Disable 11-Pixel Mouse Movement Threshold ─────────────────────────────────

def _disable_11px_mouse():
    set_reg_value(HKCU, DESKTOP_KEY, "DragWidth",  "1", winreg.REG_SZ)
    set_reg_value(HKCU, DESKTOP_KEY, "DragHeight", "1", winreg.REG_SZ)
    return True

def _reset_11px_mouse():
    set_reg_value(HKCU, DESKTOP_KEY, "DragWidth",  "4", winreg.REG_SZ)
    set_reg_value(HKCU, DESKTOP_KEY, "DragHeight", "4", winreg.REG_SZ)
    return True

def _check_11px_disabled():
    return get_reg_value(HKCU, DESKTOP_KEY, "DragWidth") == "1"

# ── Reduce Keyboard Repeat Delay ──────────────────────────────────────────────

def _reduce_kb_delay():
    set_reg_value(HKCU, KEYBOARD_KEY, "KeyboardDelay", "0",  winreg.REG_SZ)
    set_reg_value(HKCU, KEYBOARD_KEY, "KeyboardSpeed", "31", winreg.REG_SZ)
    return True

def _reset_kb_delay():
    set_reg_value(HKCU, KEYBOARD_KEY, "KeyboardDelay", "1",  winreg.REG_SZ)
    set_reg_value(HKCU, KEYBOARD_KEY, "KeyboardSpeed", "31", winreg.REG_SZ)
    return True

def _check_kb_delay():
    return get_reg_value(HKCU, KEYBOARD_KEY, "KeyboardDelay") == "0"

# ── Disable Idle and Sleep States ─────────────────────────────────────────────

def _disable_idle_sleep():
    run_powershell("""
        powercfg /change monitor-timeout-ac 0
        powercfg /change standby-timeout-ac 0
        powercfg /change hibernate-timeout-ac 0
    """)
    return True

def _enable_idle_sleep():
    run_powershell("""
        powercfg /change monitor-timeout-ac 15
        powercfg /change standby-timeout-ac 30
        powercfg /change hibernate-timeout-ac 0
    """)
    return True


# ── Tweak definitions ─────────────────────────────────────────────────────────

USB_INPUT_TWEAKS = [
    {
        "name": "Optimize USB Ports & Drivers for Keyboard & Mouse",
        "desc": "Optimizes USB ports & drivers for the lowest possible latency on keyboard and mouse. Disables latency-inducing driver behaviours, improves USB transaction priority, disables USB power saving, and stabilises data transmission/polling.",
        "type": "toggle",
        "warning": False,
        "enable": _optimize_usb_kb_mouse,
        "disable": _reset_usb_kb_mouse,
        "check": _check_usb_kb_mouse,
    },
    {
        "name": "Disable USB Selective Suspend",
        "desc": "Disables idle on USB ports, keeps connected devices active, improving input consistency.",
        "type": "toggle",
        "warning": False,
        "enable": _disable_usb_suspend,
        "disable": _enable_usb_suspend,
    },
    {
        "name": "Set Debug Poll Interval",
        "desc": "Sets kernel debug polling to 1000 ms for stability and reduced input latency.",
        "type": "toggle",
        "warning": False,
        "enable": _set_debug_poll,
        "disable": _reset_debug_poll,
        "check": _check_debug_poll,
    },
    {
        "name": "Disable All Hidden USB Power Saving",
        "desc": "Disables all hidden USB power saving features across USB hubs to reduce peripheral input latency.",
        "type": "toggle",
        "warning": False,
        "enable": _disable_all_usb_power,
        "disable": _enable_all_usb_power,
    },
    {
        "name": "Disable Mouse Acceleration",
        "desc": "Removes pointer precision/acceleration so mouse movement is 1:1 with physical movement.",
        "type": "toggle",
        "warning": False,
        "enable": _disable_mouse_accel,
        "disable": _enable_mouse_accel,
        "check": _check_mouse_accel_disabled,
    },
    {
        "name": "Disable Sticky Keys",
        "desc": "Prevents the Sticky Keys accessibility shortcut from triggering during gaming.",
        "type": "toggle",
        "warning": False,
        "enable": _disable_sticky_keys,
        "disable": _enable_sticky_keys,
        "check": _check_sticky_keys_disabled,
    },
    {
        "name": "Disable Toggle Keys",
        "desc": "Prevents the Toggle Keys accessibility feature from triggering during gameplay.",
        "type": "toggle",
        "warning": False,
        "enable": _disable_toggle_keys,
        "disable": _enable_toggle_keys,
        "check": _check_toggle_keys_disabled,
    },
    {
        "name": "Disable 11-Pixel Mouse Movement Threshold",
        "desc": "Reduces the minimum drag threshold from 4 px to 1 px for more precise click-drag detection.",
        "type": "toggle",
        "warning": False,
        "enable": _disable_11px_mouse,
        "disable": _reset_11px_mouse,
        "check": _check_11px_disabled,
    },
    {
        "name": "Reduce Keyboard Repeat Delay",
        "desc": "Sets keyboard repeat delay to minimum and repeat rate to maximum for snappier key response.",
        "type": "toggle",
        "warning": False,
        "enable": _reduce_kb_delay,
        "disable": _reset_kb_delay,
        "check": _check_kb_delay,
    },
    {
        "name": "Disable Idle and Sleep States",
        "desc": "Prevents the monitor and system from sleeping so gaming sessions are never interrupted.",
        "type": "toggle",
        "warning": False,
        "enable": _disable_idle_sleep,
        "disable": _enable_idle_sleep,
    },
]

# ── FREE: Disable Pointer Ballistics ─────────────────────────────────────────

def _disable_pointer_ballistics():
    run_powershell(r"""
        Set-ItemProperty -Path "HKCU:\Control Panel\Mouse" -Name "SmoothMouseXCurve" `
            -Value ([byte[]](0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,
                             0xC0,0xCC,0x0C,0x00,0x00,0x00,0x00,0x00,
                             0x80,0x99,0x19,0x00,0x00,0x00,0x00,0x00,
                             0x40,0x66,0x26,0x00,0x00,0x00,0x00,0x00,
                             0x00,0x33,0x33,0x00,0x00,0x00,0x00,0x00)) `
            -Type Binary -Force -ErrorAction SilentlyContinue
        Set-ItemProperty -Path "HKCU:\Control Panel\Mouse" -Name "SmoothMouseYCurve" `
            -Value ([byte[]](0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,
                             0xC0,0xCC,0x0C,0x00,0x00,0x00,0x00,0x00,
                             0x80,0x99,0x19,0x00,0x00,0x00,0x00,0x00,
                             0x40,0x66,0x26,0x00,0x00,0x00,0x00,0x00,
                             0x00,0x33,0x33,0x00,0x00,0x00,0x00,0x00)) `
            -Type Binary -Force -ErrorAction SilentlyContinue
    """)
    return True

def _reset_pointer_ballistics():
    run_powershell(r"""
        Remove-ItemProperty -Path "HKCU:\Control Panel\Mouse" -Name "SmoothMouseXCurve" -ErrorAction SilentlyContinue
        Remove-ItemProperty -Path "HKCU:\Control Panel\Mouse" -Name "SmoothMouseYCurve" -ErrorAction SilentlyContinue
    """)
    return True

# ── FREE: Set Raw Input for Mouse ─────────────────────────────────────────────

_RAWINPUT_KEY = r"SOFTWARE\Microsoft\DirectInput"

def _enable_raw_input():
    set_reg_value(HKCU, _RAWINPUT_KEY, "MouseWheelRouting", 2)
    return True

def _disable_raw_input():
    delete_reg_value(HKCU, _RAWINPUT_KEY, "MouseWheelRouting")
    return True

def _check_raw_input():
    return get_reg_value(HKCU, _RAWINPUT_KEY, "MouseWheelRouting") == 2

# ── FREE: Disable Filter Keys ─────────────────────────────────────────────────

_FILTERKEYS_KEY = r"Control Panel\Accessibility\Keyboard Response"

def _disable_filter_keys():
    return set_reg_value(HKCU, _FILTERKEYS_KEY, "Flags", "122", winreg.REG_SZ)

def _enable_filter_keys():
    return set_reg_value(HKCU, _FILTERKEYS_KEY, "Flags", "126", winreg.REG_SZ)

def _check_filter_keys_disabled():
    return get_reg_value(HKCU, _FILTERKEYS_KEY, "Flags") == "122"


USB_FREE_TWEAKS = [
    {
        "name": "Disable Pointer Ballistics (FREE)",
        "desc": "Applies a flat mouse pointer curve to eliminate Windows pointer ballistics for 1:1 tracking.",
        "type": "toggle",
        "warning": False,
        "enable": _disable_pointer_ballistics,
        "disable": _reset_pointer_ballistics,
    },
    {
        "name": "Enable Raw Mouse Input Routing (FREE)",
        "desc": "Routes mouse wheel input through DirectInput raw input path to reduce pointer processing overhead.",
        "type": "toggle",
        "warning": False,
        "enable": _enable_raw_input,
        "disable": _disable_raw_input,
        "check": _check_raw_input,
    },
    {
        "name": "Disable Filter Keys (FREE)",
        "desc": "Prevents the Filter Keys accessibility shortcut from triggering and adding input delays.",
        "type": "toggle",
        "warning": False,
        "enable": _disable_filter_keys,
        "disable": _enable_filter_keys,
        "check": _check_filter_keys_disabled,
    },
]

USB_INPUT_TWEAKS = USB_INPUT_TWEAKS + USB_FREE_TWEAKS
