using System;
using System.Collections.Generic;
using System.Text.Json;

namespace NullFrame.Services
{
    public class RestorePoint
    {
        public int SequenceNumber { get; set; }
        public string Description { get; set; } = "";
        public string CreationTime { get; set; } = "";
        public string RestorePointType { get; set; } = "";
    }

    public static class BackupService
    {
        public static List<RestorePoint> GetRestorePoints()
        {
            var script = @"
                $points = Get-ComputerRestorePoint -ErrorAction SilentlyContinue
                if (-not $points) { Write-Output '[]'; exit }
                $out = $points | Sort-Object SequenceNumber -Descending | ForEach-Object {
                    $typeInt = [int]$_.RestorePointType
                    $typeStr = switch ($typeInt) {
                        0  { 'Application Install' }
                        1  { 'Application Uninstall' }
                        6  { 'Unforeseen Problem' }
                        7  { 'System Checkpoint' }
                        10 { 'Device Driver Install' }
                        12 { 'System Settings Change' }
                        13 { 'Cancelled Operation' }
                        default { 'System Restore' }
                    }
                    try {
                        $dt = [Management.ManagementDateTimeConverter]::ToDateTime($_.CreationTime)
                        $dtStr = $dt.ToString('MMM dd yyyy  HH:mm')
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
            ";

            var (ok, output) = SystemHelper.RunPowerShell(script, capture: true);
            if (string.IsNullOrWhiteSpace(output)) return new List<RestorePoint>();

            try
            {
                var trimmed = output.Trim();
                // PowerShell returns a single object (not array) when there's only one result
                if (trimmed.StartsWith("{"))
                    trimmed = "[" + trimmed + "]";

                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                return JsonSerializer.Deserialize<List<RestorePoint>>(trimmed, options)
                       ?? new List<RestorePoint>();
            }
            catch
            {
                return new List<RestorePoint>();
            }
        }

        public static bool CreateRestorePoint(string description = "NullFrame Backup")
        {
            var safeDesc = description.Replace("'", "''");
            // Bypass the 24-hour frequency limit, enable System Restore, then create
            var script = $@"
                $key = 'HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\SystemRestore'
                Set-ItemProperty -Path $key -Name 'SystemRestorePointCreationFrequency' -Value 0 -Type DWord -ErrorAction SilentlyContinue
                Enable-ComputerRestore -Drive ""$env:SystemDrive\"" -ErrorAction SilentlyContinue
                Checkpoint-Computer -Description '{safeDesc}' -RestorePointType 'MODIFY_SETTINGS'
            ";
            var (ok, _) = SystemHelper.RunPowerShell(script);
            return ok;
        }

        public static bool OpenSystemRestore()
        {
            var (ok, _) = SystemHelper.RunCmd("rstrui.exe");
            return ok;
        }

        public static bool RestoreToPoint(int sequenceNumber)
        {
            var (ok, _) = SystemHelper.RunPowerShell(
                $"Restore-Computer -RestorePoint {sequenceNumber} -Confirm:$false"
            );
            return ok;
        }

        public static bool EnableSystemRestore(string drive = "C:\\")
        {
            var (ok, _) = SystemHelper.RunPowerShell(
                $"Enable-ComputerRestore -Drive \"{drive}\" -ErrorAction SilentlyContinue"
            );
            return ok;
        }
    }
}
