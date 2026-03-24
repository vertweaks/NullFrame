using System.Collections.Generic;
using Microsoft.Win32;
using NullFrame.Models;
using NullFrame.Services;

namespace NullFrame.Tweaks
{
    public static class StorageTweaks
    {
        private const string PrefetchKey = @"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management\PrefetchParameters";
        private const string NtfsKey = @"SYSTEM\CurrentControlSet\Control\FileSystem";

        public static List<Tweak> GetTweaks() => new()
        {
            // ── FREE: Disable 8.3 Short Name Creation ──
            new Tweak
            {
                Name = "Disable 8.3 Short Name Creation",
                Description = "Disables NTFS 8.3 filename generation to reduce file system overhead and improve directory read speed.",
                Type = TweakType.Toggle,
                IsFree = true,
                HasWarning = false,
                Enable = () => SystemHelper.RunCmd("fsutil behavior set disable8dot3 1").success,
                Disable = () => SystemHelper.RunCmd("fsutil behavior set disable8dot3 0").success,
                Check = () =>
                {
                    var (ok, output) = SystemHelper.RunCmd("fsutil behavior query disable8dot3", true);
                    return output.Contains("1");
                },
            },

            // ── FREE: Disable Last Access Time Stamp ──
            new Tweak
            {
                Name = "Disable Last Access Time Stamp",
                Description = "Stops NTFS from updating the last-accessed timestamp on every file read, reducing disk I/O overhead.",
                Type = TweakType.Toggle,
                IsFree = true,
                HasWarning = false,
                Enable = () => SystemHelper.RunCmd("fsutil behavior set disablelastaccess 1").success,
                Disable = () => SystemHelper.RunCmd("fsutil behavior set disablelastaccess 0").success,
                Check = () =>
                {
                    var (ok, output) = SystemHelper.RunCmd("fsutil behavior query disablelastaccess", true);
                    return output.Contains("1");
                },
            },

            // ── FREE: Enable TRIM for SSD ──
            new Tweak
            {
                Name = "Enable TRIM for SSD",
                Description = "Ensures Windows notifies the SSD of deleted blocks so the drive can reclaim and maintain peak write speed.",
                Type = TweakType.Toggle,
                IsFree = true,
                HasWarning = false,
                Enable = () => SystemHelper.RunCmd("fsutil behavior set DisableDeleteNotify 0").success,
                Disable = () => SystemHelper.RunCmd("fsutil behavior set DisableDeleteNotify 1").success,
                Check = () =>
                {
                    var (ok, output) = SystemHelper.RunCmd("fsutil behavior query DisableDeleteNotify", true);
                    return output.Contains("0");
                },
            },

            // ── FREE: Disable Prefetch & SuperFetch ──
            new Tweak
            {
                Name = "Disable Prefetch & SuperFetch",
                Description = "Disables Windows prefetch and SuperFetch to reduce unnecessary SSD writes and background disk activity.",
                Type = TweakType.Toggle,
                IsFree = true,
                HasWarning = false,
                Enable = () =>
                {
                    RegistryHelper.SetDword(RegistryHive.LocalMachine, PrefetchKey, "EnablePrefetcher", 0);
                    RegistryHelper.SetDword(RegistryHive.LocalMachine, PrefetchKey, "EnableSuperfetch", 0);
                    return true;
                },
                Disable = () =>
                {
                    RegistryHelper.SetDword(RegistryHive.LocalMachine, PrefetchKey, "EnablePrefetcher", 3);
                    RegistryHelper.SetDword(RegistryHive.LocalMachine, PrefetchKey, "EnableSuperfetch", 3);
                    return true;
                },
                Check = () => RegistryHelper.GetDword(RegistryHive.LocalMachine, PrefetchKey, "EnablePrefetcher") == 0,
            },

            // ── FREE: Disable SysMain Service ──
            new Tweak
            {
                Name = "Disable SysMain Service",
                Description = "Disables the SysMain (SuperFetch) background service to reduce idle disk usage and free RAM.",
                Type = TweakType.Toggle,
                IsFree = true,
                HasWarning = false,
                Enable = () => SystemHelper.RunPowerShell(
                    "Stop-Service -Name SysMain -ErrorAction SilentlyContinue; " +
                    "Set-Service -Name SysMain -StartupType Disabled -ErrorAction SilentlyContinue").success,
                Disable = () => SystemHelper.RunPowerShell(
                    "Set-Service -Name SysMain -StartupType Automatic -ErrorAction SilentlyContinue; " +
                    "Start-Service -Name SysMain -ErrorAction SilentlyContinue").success,
                Check = () =>
                {
                    var (ok, output) = SystemHelper.RunPowerShell(
                        "(Get-Service -Name SysMain -ErrorAction SilentlyContinue).StartType", true);
                    return output.ToLower().Contains("disabled");
                },
            },

            // ── FREE: Optimize NTFS Memory Usage ──
            new Tweak
            {
                Name = "Optimize NTFS Memory Usage",
                Description = "Increases NTFS internal memory usage to 2 for better caching, and disables legacy filename overhead.",
                Type = TweakType.Toggle,
                IsFree = true,
                HasWarning = false,
                Enable = () =>
                {
                    SystemHelper.RunCmd("fsutil behavior set memoryusage 2");
                    RegistryHelper.SetDword(RegistryHive.LocalMachine, NtfsKey, "NtfsDisable8dot3NameCreation", 1);
                    RegistryHelper.SetDword(RegistryHive.LocalMachine, NtfsKey, "NtfsDisableLastAccessUpdate", 1);
                    return true;
                },
                Disable = () =>
                {
                    SystemHelper.RunCmd("fsutil behavior set memoryusage 1");
                    RegistryHelper.DeleteValue(RegistryHive.LocalMachine, NtfsKey, "NtfsDisable8dot3NameCreation");
                    RegistryHelper.DeleteValue(RegistryHive.LocalMachine, NtfsKey, "NtfsDisableLastAccessUpdate");
                    return true;
                },
                Check = () => RegistryHelper.GetDword(RegistryHive.LocalMachine, NtfsKey, "NtfsDisable8dot3NameCreation") == 1,
            },

            // ── FREE: Disable Windows Search Indexing ──
            new Tweak
            {
                Name = "Disable Windows Search Indexing",
                Description = "Disables the Windows Search indexer service to stop background disk scanning and CPU usage.",
                Type = TweakType.Toggle,
                IsFree = true,
                HasWarning = false,
                Enable = () => SystemHelper.RunPowerShell(
                    "Stop-Service -Name WSearch -ErrorAction SilentlyContinue; " +
                    "Set-Service -Name WSearch -StartupType Disabled -ErrorAction SilentlyContinue").success,
                Disable = () => SystemHelper.RunPowerShell(
                    "Set-Service -Name WSearch -StartupType Automatic -ErrorAction SilentlyContinue; " +
                    "Start-Service -Name WSearch -ErrorAction SilentlyContinue").success,
                Check = () =>
                {
                    var (ok, output) = SystemHelper.RunPowerShell(
                        "(Get-Service -Name WSearch -ErrorAction SilentlyContinue).StartType", true);
                    return output.ToLower().Contains("disabled");
                },
            },

            // ── FREE: Run Disk Cleanup ──
            new Tweak
            {
                Name = "Run Disk Cleanup",
                Description = "Launches the Windows Disk Cleanup utility to remove temporary files and free up disk space.",
                Type = TweakType.Apply,
                IsFree = true,
                HasWarning = false,
                ApplyAction = () => SystemHelper.RunPowerShell(
                    @"Start-Process -FilePath 'cleanmgr.exe' -ArgumentList '/d C:' -NoNewWindow -ErrorAction SilentlyContinue").success,
            },
        };
    }
}
