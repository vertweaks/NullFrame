"""
Backup & Restore — Windows System Restore Point helpers.
All operations require Administrator privileges.
"""
import json
from utils.system import run_powershell, run_cmd

# Restore-point type int → human label
_TYPE_MAP = {
    0:  "Application Install",
    1:  "Application Uninstall",
    10: "Device Driver Install",
    12: "System Settings Change",
    13: "Cancelled Operation",
    6:  "Unforeseen Problem",
    7:  "System Checkpoint",
}


def get_restore_points() -> list[dict]:
    """
    Return a list of restore-point dicts, newest first.
    Each dict has: SequenceNumber, Description, CreationTime (formatted), RestorePointType (str).
    """
    script = r"""
        $points = Get-ComputerRestorePoint -ErrorAction SilentlyContinue
        if (-not $points) { Write-Output '[]'; exit }
        $out = $points | Sort-Object SequenceNumber -Descending | ForEach-Object {
            $typeInt = [int]$_.RestorePointType
            $typeStr = switch ($typeInt) {
                0  { "Application Install" }
                1  { "Application Uninstall" }
                6  { "Unforeseen Problem" }
                7  { "System Checkpoint" }
                10 { "Device Driver Install" }
                12 { "System Settings Change" }
                13 { "Cancelled Operation" }
                default { "System Restore" }
            }
            try {
                $dt = [Management.ManagementDateTimeConverter]::ToDateTime($_.CreationTime)
                $dtStr = $dt.ToString("MMM dd yyyy  HH:mm")
            } catch {
                $dtStr = $_.CreationTime
            }
            [PSCustomObject]@{
                SequenceNumber   = $_.SequenceNumber
                Description      = $_.Description
                CreationTime     = $dtStr
                RestorePointType = $typeStr
            }
        }
        $out | ConvertTo-Json -Compress
    """
    ok, out = run_powershell(script, capture=True)
    if not out:
        return []
    try:
        data = json.loads(out.strip())
        if isinstance(data, dict):
            data = [data]
        return data if isinstance(data, list) else []
    except Exception:
        return []


def create_restore_point(description: str = "VER TWEAKS Backup") -> bool:
    """
    Create a new System Restore point.
    Enables System Restore on the system drive first (safe to call even if already enabled).
    """
    safe_desc = description.replace('"', "'")
    ok, _ = run_powershell(f"""
        Enable-ComputerRestore -Drive "$env:SystemDrive\\" -ErrorAction SilentlyContinue
        Checkpoint-Computer -Description "{safe_desc}" -RestorePointType "MODIFY_SETTINGS"
    """)
    return ok


def open_system_restore() -> bool:
    """Open the Windows System Restore wizard (rstrui.exe)."""
    ok, _ = run_cmd("rstrui.exe")
    return ok


def restore_to_point(sequence_number: int) -> bool:
    """
    Initiate a system restore to the given sequence number.
    Windows will schedule the restore and restart the machine.
    """
    ok, _ = run_powershell(
        f"Restore-Computer -RestorePoint {int(sequence_number)} -Confirm:$false"
    )
    return ok


def enable_system_restore(drive: str = "C:\\") -> bool:
    """Ensure System Restore is enabled on the given drive."""
    ok, _ = run_powershell(
        f'Enable-ComputerRestore -Drive "{drive}" -ErrorAction SilentlyContinue'
    )
    return ok
