"""
Privacy & Debloat tweaks (FREE).
"""
import winreg
from utils.registry import set_reg_value, get_reg_value, delete_reg_value
from utils.system import run_powershell, run_cmd

HKLM = winreg.HKEY_LOCAL_MACHINE
HKCU = winreg.HKEY_CURRENT_USER

_TELEMETRY_KEY   = r"SOFTWARE\Policies\Microsoft\Windows\DataCollection"
_CORTANA_KEY     = r"SOFTWARE\Policies\Microsoft\Windows\Windows Search"
_TIPS_KEY        = r"SOFTWARE\Policies\Microsoft\Windows\CloudContent"
_ACTIVITY_KEY    = r"SOFTWARE\Policies\Microsoft\Windows\System"
_LOCATION_KEY    = r"SOFTWARE\Policies\Microsoft\Windows\LocationAndSensors"
_ONEDRIVE_KEY    = r"SOFTWARE\Policies\Microsoft\Windows\OneDrive"
_ADVERTISING_KEY = r"SOFTWARE\Microsoft\Windows\CurrentVersion\AdvertisingInfo"

# ── FREE: Disable Telemetry ───────────────────────────────────────────────────

def _disable_telemetry():
    set_reg_value(HKLM, _TELEMETRY_KEY, "AllowTelemetry", 0)
    run_powershell("""
        Stop-Service  -Name DiagTrack   -ErrorAction SilentlyContinue
        Set-Service   -Name DiagTrack   -StartupType Disabled -ErrorAction SilentlyContinue
        Stop-Service  -Name dmwappushservice -ErrorAction SilentlyContinue
        Set-Service   -Name dmwappushservice -StartupType Disabled -ErrorAction SilentlyContinue
    """)
    return True

def _enable_telemetry():
    delete_reg_value(HKLM, _TELEMETRY_KEY, "AllowTelemetry")
    run_powershell("""
        Set-Service -Name DiagTrack        -StartupType Automatic -ErrorAction SilentlyContinue
        Set-Service -Name dmwappushservice -StartupType Automatic -ErrorAction SilentlyContinue
    """)
    return True

def _check_telemetry_disabled():
    return get_reg_value(HKLM, _TELEMETRY_KEY, "AllowTelemetry") == 0

# ── FREE: Disable Cortana ─────────────────────────────────────────────────────

def _disable_cortana():
    set_reg_value(HKLM, _CORTANA_KEY, "AllowCortana", 0)
    set_reg_value(HKLM, _CORTANA_KEY, "DisableWebSearch", 1)
    return True

def _enable_cortana():
    delete_reg_value(HKLM, _CORTANA_KEY, "AllowCortana")
    delete_reg_value(HKLM, _CORTANA_KEY, "DisableWebSearch")
    return True

def _check_cortana_disabled():
    return get_reg_value(HKLM, _CORTANA_KEY, "AllowCortana") == 0

# ── FREE: Disable Windows Tips & Suggestions ──────────────────────────────────

def _disable_tips():
    set_reg_value(HKLM, _TIPS_KEY, "DisableSoftLanding",        1)
    set_reg_value(HKLM, _TIPS_KEY, "DisableWindowsSpotlightFeatures", 1)
    set_reg_value(HKLM, _TIPS_KEY, "DisableTailoredExperiencesWithDiagnosticData", 1)
    return True

def _enable_tips():
    delete_reg_value(HKLM, _TIPS_KEY, "DisableSoftLanding")
    delete_reg_value(HKLM, _TIPS_KEY, "DisableWindowsSpotlightFeatures")
    delete_reg_value(HKLM, _TIPS_KEY, "DisableTailoredExperiencesWithDiagnosticData")
    return True

def _check_tips_disabled():
    return get_reg_value(HKLM, _TIPS_KEY, "DisableSoftLanding") == 1

# ── FREE: Disable Activity History ────────────────────────────────────────────

def _disable_activity():
    set_reg_value(HKLM, _ACTIVITY_KEY, "EnableActivityFeed",      0)
    set_reg_value(HKLM, _ACTIVITY_KEY, "PublishUserActivities",   0)
    set_reg_value(HKLM, _ACTIVITY_KEY, "UploadUserActivities",    0)
    return True

def _enable_activity():
    delete_reg_value(HKLM, _ACTIVITY_KEY, "EnableActivityFeed")
    delete_reg_value(HKLM, _ACTIVITY_KEY, "PublishUserActivities")
    delete_reg_value(HKLM, _ACTIVITY_KEY, "UploadUserActivities")
    return True

def _check_activity_disabled():
    return get_reg_value(HKLM, _ACTIVITY_KEY, "EnableActivityFeed") == 0

# ── FREE: Disable Location Services ──────────────────────────────────────────

def _disable_location():
    set_reg_value(HKLM, _LOCATION_KEY, "DisableLocation",          1)
    set_reg_value(HKLM, _LOCATION_KEY, "DisableLocationScripting", 1)
    return True

def _enable_location():
    delete_reg_value(HKLM, _LOCATION_KEY, "DisableLocation")
    delete_reg_value(HKLM, _LOCATION_KEY, "DisableLocationScripting")
    return True

def _check_location_disabled():
    return get_reg_value(HKLM, _LOCATION_KEY, "DisableLocation") == 1

# ── FREE: Disable OneDrive Auto-Start ────────────────────────────────────────

def _disable_onedrive():
    set_reg_value(HKLM, _ONEDRIVE_KEY, "DisableFileSyncNGSC", 1)
    run_powershell(r"""
        Remove-ItemProperty -Path "HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\Run" `
            -Name "OneDrive" -ErrorAction SilentlyContinue
    """)
    return True

def _enable_onedrive():
    delete_reg_value(HKLM, _ONEDRIVE_KEY, "DisableFileSyncNGSC")
    return True

def _check_onedrive_disabled():
    return get_reg_value(HKLM, _ONEDRIVE_KEY, "DisableFileSyncNGSC") == 1

# ── FREE: Disable Advertising ID ─────────────────────────────────────────────

def _disable_advertising():
    set_reg_value(HKCU, _ADVERTISING_KEY, "Enabled", 0)
    return True

def _enable_advertising():
    set_reg_value(HKCU, _ADVERTISING_KEY, "Enabled", 1)
    return True

def _check_advertising_disabled():
    return get_reg_value(HKCU, _ADVERTISING_KEY, "Enabled") == 0

# ── FREE: Disable Background Apps ─────────────────────────────────────────────

_BGAPP_KEY = r"SOFTWARE\Policies\Microsoft\Windows\AppPrivacy"

def _disable_background_apps():
    set_reg_value(HKLM, _BGAPP_KEY, "LetAppsRunInBackground", 2)
    return True

def _enable_background_apps():
    delete_reg_value(HKLM, _BGAPP_KEY, "LetAppsRunInBackground")
    return True

def _check_background_apps_disabled():
    return get_reg_value(HKLM, _BGAPP_KEY, "LetAppsRunInBackground") == 2


# ── Tweak definitions ─────────────────────────────────────────────────────────

PRIVACY_TWEAKS = [
    {
        "name": "Disable Telemetry & Diagnostic Data (FREE)",
        "desc": "Sets telemetry to Security level (0) and stops the DiagTrack/dmwappushservice background services.",
        "type": "toggle",
        "warning": False,
        "enable": _disable_telemetry,
        "disable": _enable_telemetry,
        "check": _check_telemetry_disabled,
    },
    {
        "name": "Disable Cortana & Web Search (FREE)",
        "desc": "Prevents Cortana from loading and disables the web search integration in the Start menu.",
        "type": "toggle",
        "warning": False,
        "enable": _disable_cortana,
        "disable": _enable_cortana,
        "check": _check_cortana_disabled,
    },
    {
        "name": "Disable Windows Tips & Suggestions (FREE)",
        "desc": "Removes Windows Spotlight, lock screen ads, and unsolicited tips from the OS.",
        "type": "toggle",
        "warning": False,
        "enable": _disable_tips,
        "disable": _enable_tips,
        "check": _check_tips_disabled,
    },
    {
        "name": "Disable Activity History (FREE)",
        "desc": "Stops Windows from tracking and uploading your app usage and browsing activity to Microsoft.",
        "type": "toggle",
        "warning": False,
        "enable": _disable_activity,
        "disable": _enable_activity,
        "check": _check_activity_disabled,
    },
    {
        "name": "Disable Location Services (FREE)",
        "desc": "Disables Windows location tracking so apps and the OS cannot access your physical location.",
        "type": "toggle",
        "warning": False,
        "enable": _disable_location,
        "disable": _enable_location,
        "check": _check_location_disabled,
    },
    {
        "name": "Disable OneDrive Auto-Start (FREE)",
        "desc": "Prevents OneDrive from launching at startup and syncing in the background.",
        "type": "toggle",
        "warning": False,
        "enable": _disable_onedrive,
        "disable": _enable_onedrive,
        "check": _check_onedrive_disabled,
    },
    {
        "name": "Disable Advertising ID (FREE)",
        "desc": "Disables the Windows advertising identifier that apps use to serve targeted ads.",
        "type": "toggle",
        "warning": False,
        "enable": _disable_advertising,
        "disable": _enable_advertising,
        "check": _check_advertising_disabled,
    },
    {
        "name": "Disable Background App Access (FREE)",
        "desc": "Blocks all UWP apps from running in the background, reducing idle CPU and network usage.",
        "type": "toggle",
        "warning": False,
        "enable": _disable_background_apps,
        "disable": _enable_background_apps,
        "check": _check_background_apps_disabled,
    },
]
