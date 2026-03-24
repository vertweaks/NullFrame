using System.Collections.Generic;
using Microsoft.Win32;
using NullFrame.Models;
using NullFrame.Services;

namespace NullFrame.Tweaks
{
    public static class PrivacyTweaks
    {
        private const string TelemetryKey = @"SOFTWARE\Policies\Microsoft\Windows\DataCollection";
        private const string CortanaKey = @"SOFTWARE\Policies\Microsoft\Windows\Windows Search";
        private const string TipsKey = @"SOFTWARE\Policies\Microsoft\Windows\CloudContent";
        private const string ActivityKey = @"SOFTWARE\Policies\Microsoft\Windows\System";
        private const string LocationKey = @"SOFTWARE\Policies\Microsoft\Windows\LocationAndSensors";
        private const string OneDriveKey = @"SOFTWARE\Policies\Microsoft\Windows\OneDrive";
        private const string AdvertisingKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\AdvertisingInfo";
        private const string BgAppKey = @"SOFTWARE\Policies\Microsoft\Windows\AppPrivacy";

        public static List<Tweak> GetTweaks() => new()
        {
            // ── FREE: Disable Telemetry & Diagnostic Data ──
            new Tweak
            {
                Name = "Disable Telemetry & Diagnostic Data",
                Description = "Sets telemetry to Security level (0) and stops the DiagTrack/dmwappushservice background services.",
                Type = TweakType.Toggle,
                IsFree = true,
                HasWarning = false,
                Enable = () =>
                {
                    RegistryHelper.SetDword(RegistryHive.LocalMachine, TelemetryKey, "AllowTelemetry", 0);
                    SystemHelper.RunPowerShell(
                        "Stop-Service -Name DiagTrack -ErrorAction SilentlyContinue; " +
                        "Set-Service -Name DiagTrack -StartupType Disabled -ErrorAction SilentlyContinue; " +
                        "Stop-Service -Name dmwappushservice -ErrorAction SilentlyContinue; " +
                        "Set-Service -Name dmwappushservice -StartupType Disabled -ErrorAction SilentlyContinue");
                    return true;
                },
                Disable = () =>
                {
                    RegistryHelper.DeleteValue(RegistryHive.LocalMachine, TelemetryKey, "AllowTelemetry");
                    SystemHelper.RunPowerShell(
                        "Set-Service -Name DiagTrack -StartupType Automatic -ErrorAction SilentlyContinue; " +
                        "Set-Service -Name dmwappushservice -StartupType Automatic -ErrorAction SilentlyContinue");
                    return true;
                },
                Check = () => RegistryHelper.GetDword(RegistryHive.LocalMachine, TelemetryKey, "AllowTelemetry") == 0,
            },

            // ── FREE: Disable Cortana & Web Search ──
            new Tweak
            {
                Name = "Disable Cortana & Web Search",
                Description = "Prevents Cortana from loading and disables the web search integration in the Start menu.",
                Type = TweakType.Toggle,
                IsFree = true,
                HasWarning = false,
                Enable = () =>
                {
                    RegistryHelper.SetDword(RegistryHive.LocalMachine, CortanaKey, "AllowCortana", 0);
                    RegistryHelper.SetDword(RegistryHive.LocalMachine, CortanaKey, "DisableWebSearch", 1);
                    return true;
                },
                Disable = () =>
                {
                    RegistryHelper.DeleteValue(RegistryHive.LocalMachine, CortanaKey, "AllowCortana");
                    RegistryHelper.DeleteValue(RegistryHive.LocalMachine, CortanaKey, "DisableWebSearch");
                    return true;
                },
                Check = () => RegistryHelper.GetDword(RegistryHive.LocalMachine, CortanaKey, "AllowCortana") == 0,
            },

            // ── FREE: Disable Windows Tips & Suggestions ──
            new Tweak
            {
                Name = "Disable Windows Tips & Suggestions",
                Description = "Removes Windows Spotlight, lock screen ads, and unsolicited tips from the OS.",
                Type = TweakType.Toggle,
                IsFree = true,
                HasWarning = false,
                Enable = () =>
                {
                    RegistryHelper.SetDword(RegistryHive.LocalMachine, TipsKey, "DisableSoftLanding", 1);
                    RegistryHelper.SetDword(RegistryHive.LocalMachine, TipsKey, "DisableWindowsSpotlightFeatures", 1);
                    RegistryHelper.SetDword(RegistryHive.LocalMachine, TipsKey, "DisableTailoredExperiencesWithDiagnosticData", 1);
                    return true;
                },
                Disable = () =>
                {
                    RegistryHelper.DeleteValue(RegistryHive.LocalMachine, TipsKey, "DisableSoftLanding");
                    RegistryHelper.DeleteValue(RegistryHive.LocalMachine, TipsKey, "DisableWindowsSpotlightFeatures");
                    RegistryHelper.DeleteValue(RegistryHive.LocalMachine, TipsKey, "DisableTailoredExperiencesWithDiagnosticData");
                    return true;
                },
                Check = () => RegistryHelper.GetDword(RegistryHive.LocalMachine, TipsKey, "DisableSoftLanding") == 1,
            },

            // ── FREE: Disable Activity History ──
            new Tweak
            {
                Name = "Disable Activity History",
                Description = "Stops Windows from tracking and uploading your app usage and browsing activity to Microsoft.",
                Type = TweakType.Toggle,
                IsFree = true,
                HasWarning = false,
                Enable = () =>
                {
                    RegistryHelper.SetDword(RegistryHive.LocalMachine, ActivityKey, "EnableActivityFeed", 0);
                    RegistryHelper.SetDword(RegistryHive.LocalMachine, ActivityKey, "PublishUserActivities", 0);
                    RegistryHelper.SetDword(RegistryHive.LocalMachine, ActivityKey, "UploadUserActivities", 0);
                    return true;
                },
                Disable = () =>
                {
                    RegistryHelper.DeleteValue(RegistryHive.LocalMachine, ActivityKey, "EnableActivityFeed");
                    RegistryHelper.DeleteValue(RegistryHive.LocalMachine, ActivityKey, "PublishUserActivities");
                    RegistryHelper.DeleteValue(RegistryHive.LocalMachine, ActivityKey, "UploadUserActivities");
                    return true;
                },
                Check = () => RegistryHelper.GetDword(RegistryHive.LocalMachine, ActivityKey, "EnableActivityFeed") == 0,
            },

            // ── FREE: Disable Location Services ──
            new Tweak
            {
                Name = "Disable Location Services",
                Description = "Disables Windows location tracking so apps and the OS cannot access your physical location.",
                Type = TweakType.Toggle,
                IsFree = true,
                HasWarning = false,
                Enable = () =>
                {
                    RegistryHelper.SetDword(RegistryHive.LocalMachine, LocationKey, "DisableLocation", 1);
                    RegistryHelper.SetDword(RegistryHive.LocalMachine, LocationKey, "DisableLocationScripting", 1);
                    return true;
                },
                Disable = () =>
                {
                    RegistryHelper.DeleteValue(RegistryHive.LocalMachine, LocationKey, "DisableLocation");
                    RegistryHelper.DeleteValue(RegistryHive.LocalMachine, LocationKey, "DisableLocationScripting");
                    return true;
                },
                Check = () => RegistryHelper.GetDword(RegistryHive.LocalMachine, LocationKey, "DisableLocation") == 1,
            },

            // ── FREE: Disable OneDrive Auto-Start ──
            new Tweak
            {
                Name = "Disable OneDrive Auto-Start",
                Description = "Prevents OneDrive from launching at startup and syncing in the background.",
                Type = TweakType.Toggle,
                IsFree = true,
                HasWarning = false,
                Enable = () =>
                {
                    RegistryHelper.SetDword(RegistryHive.LocalMachine, OneDriveKey, "DisableFileSyncNGSC", 1);
                    SystemHelper.RunPowerShell(
                        @"Remove-ItemProperty -Path 'HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\Run' -Name 'OneDrive' -ErrorAction SilentlyContinue");
                    return true;
                },
                Disable = () => RegistryHelper.DeleteValue(RegistryHive.LocalMachine, OneDriveKey, "DisableFileSyncNGSC"),
                Check = () => RegistryHelper.GetDword(RegistryHive.LocalMachine, OneDriveKey, "DisableFileSyncNGSC") == 1,
            },

            // ── FREE: Disable Advertising ID ──
            new Tweak
            {
                Name = "Disable Advertising ID",
                Description = "Disables the Windows advertising identifier that apps use to serve targeted ads.",
                Type = TweakType.Toggle,
                IsFree = true,
                HasWarning = false,
                Enable = () => RegistryHelper.SetDword(RegistryHive.CurrentUser, AdvertisingKey, "Enabled", 0),
                Disable = () => RegistryHelper.SetDword(RegistryHive.CurrentUser, AdvertisingKey, "Enabled", 1),
                Check = () => RegistryHelper.GetDword(RegistryHive.CurrentUser, AdvertisingKey, "Enabled") == 0,
            },

            // ── FREE: Disable Background App Access ──
            new Tweak
            {
                Name = "Disable Background App Access",
                Description = "Blocks all UWP apps from running in the background, reducing idle CPU and network usage.",
                Type = TweakType.Toggle,
                IsFree = true,
                HasWarning = false,
                Enable = () => RegistryHelper.SetDword(RegistryHive.LocalMachine, BgAppKey, "LetAppsRunInBackground", 2),
                Disable = () => RegistryHelper.DeleteValue(RegistryHive.LocalMachine, BgAppKey, "LetAppsRunInBackground"),
                Check = () => RegistryHelper.GetDword(RegistryHive.LocalMachine, BgAppKey, "LetAppsRunInBackground") == 2,
            },
        };
    }
}
