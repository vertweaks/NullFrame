using System.Collections.Generic;
using Microsoft.Win32;
using NullFrame.Models;
using NullFrame.Services;

namespace NullFrame.Tweaks
{
    public static class UsbInputTweaks
    {
        private const string UsbSuspendAc = "2a737441-1930-4402-8d77-b2bebba308a3";
        private const string UsbSuspendSk = "48e6b7a6-50f5-4782-a5d4-53bb8f07e226";

        private const string MouseKey = @"Control Panel\Mouse";
        private const string KeyboardKey = @"Control Panel\Keyboard";
        private const string DesktopKey = @"Control Panel\Desktop";
        private const string StickyKey = @"Control Panel\Accessibility\StickyKeys";
        private const string ToggleKeyPath = @"Control Panel\Accessibility\ToggleKeys";
        private const string MouclassKey = @"SYSTEM\CurrentControlSet\Services\mouclass\Parameters";
        private const string KbdclassKey = @"SYSTEM\CurrentControlSet\Services\kbdclass\Parameters";
        private const string ProfileKey = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile";
        private const string UsbHubKey = @"SYSTEM\CurrentControlSet\Services\usbhub\Parameters";
        private const string UsbKey = @"SYSTEM\CurrentControlSet\Services\USB";
        private const string RawInputKey = @"SOFTWARE\Microsoft\DirectInput";
        private const string FilterKeysKey = @"Control Panel\Accessibility\Keyboard Response";

        public static List<Tweak> GetTweaks() => new()
        {
            // ── Optimize USB Ports & Drivers for Keyboard & Mouse ──
            new Tweak
            {
                Name = "Optimize USB Ports & Drivers for Keyboard & Mouse",
                Description = "Optimizes USB ports & drivers for the lowest possible latency on keyboard and mouse. Disables latency-inducing driver behaviours, improves USB transaction priority, disables USB power saving, and stabilises data transmission/polling.",
                Type = TweakType.Toggle,
                IsFree = false,
                HasWarning = false,
                Enable = () =>
                {
                    SystemHelper.RunPowerShell(
                        $"powercfg /setacvalueindex SCHEME_CURRENT {UsbSuspendAc} {UsbSuspendSk} 0; " +
                        $"powercfg /setdcvalueindex SCHEME_CURRENT {UsbSuspendAc} {UsbSuspendSk} 0; " +
                        "powercfg /apply");
                    RegistryHelper.SetDword(RegistryHive.LocalMachine, UsbHubKey, "DisableSelectiveSuspend", 1);
                    RegistryHelper.SetDword(RegistryHive.LocalMachine, UsbKey, "DisableSelectiveSuspend", 1);
                    RegistryHelper.SetDword(RegistryHive.LocalMachine, MouclassKey, "MouseDataQueueSize", 0x64);
                    RegistryHelper.SetDword(RegistryHive.LocalMachine, KbdclassKey, "KeyboardDataQueueSize", 0x64);
                    RegistryHelper.SetDword(RegistryHive.LocalMachine, ProfileKey, "SystemResponsiveness", 0);
                    return true;
                },
                Disable = () =>
                {
                    SystemHelper.RunPowerShell(
                        $"powercfg /setacvalueindex SCHEME_CURRENT {UsbSuspendAc} {UsbSuspendSk} 1; " +
                        $"powercfg /setdcvalueindex SCHEME_CURRENT {UsbSuspendAc} {UsbSuspendSk} 1; " +
                        "powercfg /apply");
                    RegistryHelper.DeleteValue(RegistryHive.LocalMachine, UsbHubKey, "DisableSelectiveSuspend");
                    return true;
                },
                Check = () => RegistryHelper.GetDword(RegistryHive.LocalMachine, UsbHubKey, "DisableSelectiveSuspend") == 1,
            },

            // ── Disable USB Selective Suspend ──
            new Tweak
            {
                Name = "Disable USB Selective Suspend",
                Description = "Disables idle on USB ports, keeps connected devices active, improving input consistency.",
                Type = TweakType.Toggle,
                IsFree = false,
                HasWarning = false,
                Enable = () =>
                {
                    SystemHelper.RunPowerShell(
                        $"powercfg /setacvalueindex SCHEME_CURRENT {UsbSuspendAc} {UsbSuspendSk} 0; " +
                        $"powercfg /setdcvalueindex SCHEME_CURRENT {UsbSuspendAc} {UsbSuspendSk} 0; " +
                        "powercfg /apply");
                    RegistryHelper.SetDword(RegistryHive.LocalMachine, UsbHubKey, "DisableSelectiveSuspend", 1);
                    return true;
                },
                Disable = () =>
                {
                    SystemHelper.RunPowerShell(
                        $"powercfg /setacvalueindex SCHEME_CURRENT {UsbSuspendAc} {UsbSuspendSk} 1; " +
                        $"powercfg /setdcvalueindex SCHEME_CURRENT {UsbSuspendAc} {UsbSuspendSk} 1; " +
                        "powercfg /apply");
                    RegistryHelper.DeleteValue(RegistryHive.LocalMachine, UsbHubKey, "DisableSelectiveSuspend");
                    return true;
                },
                Check = () => RegistryHelper.GetDword(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Services\USB", "DisableSelectiveSuspend", 0) == 1,
            },

            // ── Set Debug Poll Interval ──
            new Tweak
            {
                Name = "Set Debug Poll Interval",
                Description = "Sets kernel debug polling to 1000 ms for stability and reduced input latency.",
                Type = TweakType.Toggle,
                IsFree = false,
                HasWarning = false,
                Enable = () =>
                {
                    RegistryHelper.SetDword(RegistryHive.LocalMachine, MouclassKey, "MouseDataQueueSize", 0x64);
                    RegistryHelper.SetDword(RegistryHive.LocalMachine, KbdclassKey, "KeyboardDataQueueSize", 0x64);
                    RegistryHelper.SetDword(RegistryHive.LocalMachine, ProfileKey, "SystemResponsiveness", 0);
                    return true;
                },
                Disable = () =>
                {
                    RegistryHelper.DeleteValue(RegistryHive.LocalMachine, MouclassKey, "MouseDataQueueSize");
                    RegistryHelper.DeleteValue(RegistryHive.LocalMachine, KbdclassKey, "KeyboardDataQueueSize");
                    return true;
                },
                Check = () => RegistryHelper.GetDword(RegistryHive.LocalMachine, MouclassKey, "MouseDataQueueSize") == 0x64,
            },

            // ── Disable All Hidden USB Power Saving ──
            new Tweak
            {
                Name = "Disable All Hidden USB Power Saving",
                Description = "Disables all hidden USB power saving features across USB hubs to reduce peripheral input latency.",
                Type = TweakType.Toggle,
                IsFree = false,
                HasWarning = false,
                Enable = () =>
                {
                    SystemHelper.RunPowerShell(
                        $"powercfg /setacvalueindex SCHEME_CURRENT {UsbSuspendAc} {UsbSuspendSk} 0; " +
                        "powercfg /apply; " +
                        @"$hubs = Get-PnpDevice -Class USB -ErrorAction SilentlyContinue | Where-Object {$_.FriendlyName -like '*Hub*'}; " +
                        @"foreach ($h in $hubs) { " +
                        @"$path = 'HKLM:\SYSTEM\CurrentControlSet\Enum\' + $h.InstanceId + '\Device Parameters'; " +
                        @"if (Test-Path $path) { Set-ItemProperty -Path $path -Name 'EnhancedPowerManagementEnabled' -Value 0 -Type DWord -Force -ErrorAction SilentlyContinue } }");
                    return true;
                },
                Disable = () => SystemHelper.RunPowerShell(
                    $"powercfg /setacvalueindex SCHEME_CURRENT {UsbSuspendAc} {UsbSuspendSk} 1; " +
                    "powercfg /apply").success,
                Check = () => { var (ok, output) = SystemHelper.RunPowerShell("(Get-ItemProperty 'HKLM:\\SYSTEM\\CurrentControlSet\\Services\\USB' -Name 'DisableSelectiveSuspend' -ErrorAction SilentlyContinue).DisableSelectiveSuspend", true); return ok && output.Trim() == "1"; },
            },

            // ── Disable Mouse Acceleration ──
            new Tweak
            {
                Name = "Disable Mouse Acceleration",
                Description = "Removes pointer precision/acceleration so mouse movement is 1:1 with physical movement.",
                Type = TweakType.Toggle,
                IsFree = false,
                HasWarning = false,
                Enable = () =>
                {
                    RegistryHelper.SetString(RegistryHive.CurrentUser, MouseKey, "MouseSpeed", "0");
                    RegistryHelper.SetString(RegistryHive.CurrentUser, MouseKey, "MouseThreshold1", "0");
                    RegistryHelper.SetString(RegistryHive.CurrentUser, MouseKey, "MouseThreshold2", "0");
                    return true;
                },
                Disable = () =>
                {
                    RegistryHelper.SetString(RegistryHive.CurrentUser, MouseKey, "MouseSpeed", "1");
                    RegistryHelper.SetString(RegistryHive.CurrentUser, MouseKey, "MouseThreshold1", "6");
                    RegistryHelper.SetString(RegistryHive.CurrentUser, MouseKey, "MouseThreshold2", "10");
                    return true;
                },
                Check = () => (string?)RegistryHelper.GetValue(RegistryHive.CurrentUser, MouseKey, "MouseSpeed") == "0",
            },

            // ── Disable Sticky Keys ──
            new Tweak
            {
                Name = "Disable Sticky Keys",
                Description = "Prevents the Sticky Keys accessibility shortcut from triggering during gaming.",
                Type = TweakType.Toggle,
                IsFree = false,
                HasWarning = false,
                Enable = () => RegistryHelper.SetString(RegistryHive.CurrentUser, StickyKey, "Flags", "506"),
                Disable = () => RegistryHelper.SetString(RegistryHive.CurrentUser, StickyKey, "Flags", "510"),
                Check = () => (string?)RegistryHelper.GetValue(RegistryHive.CurrentUser, StickyKey, "Flags") == "506",
            },

            // ── Disable Toggle Keys ──
            new Tweak
            {
                Name = "Disable Toggle Keys",
                Description = "Prevents the Toggle Keys accessibility feature from triggering during gameplay.",
                Type = TweakType.Toggle,
                IsFree = false,
                HasWarning = false,
                Enable = () => RegistryHelper.SetString(RegistryHive.CurrentUser, ToggleKeyPath, "Flags", "58"),
                Disable = () => RegistryHelper.SetString(RegistryHive.CurrentUser, ToggleKeyPath, "Flags", "62"),
                Check = () => (string?)RegistryHelper.GetValue(RegistryHive.CurrentUser, ToggleKeyPath, "Flags") == "58",
            },

            // ── Disable 11-Pixel Mouse Movement Threshold ──
            new Tweak
            {
                Name = "Disable 11-Pixel Mouse Movement Threshold",
                Description = "Reduces the minimum drag threshold from 4 px to 1 px for more precise click-drag detection.",
                Type = TweakType.Toggle,
                IsFree = false,
                HasWarning = false,
                Enable = () =>
                {
                    RegistryHelper.SetString(RegistryHive.CurrentUser, DesktopKey, "DragWidth", "1");
                    RegistryHelper.SetString(RegistryHive.CurrentUser, DesktopKey, "DragHeight", "1");
                    return true;
                },
                Disable = () =>
                {
                    RegistryHelper.SetString(RegistryHive.CurrentUser, DesktopKey, "DragWidth", "4");
                    RegistryHelper.SetString(RegistryHive.CurrentUser, DesktopKey, "DragHeight", "4");
                    return true;
                },
                Check = () => (string?)RegistryHelper.GetValue(RegistryHive.CurrentUser, DesktopKey, "DragWidth") == "1",
            },

            // ── Reduce Keyboard Repeat Delay ──
            new Tweak
            {
                Name = "Reduce Keyboard Repeat Delay",
                Description = "Sets keyboard repeat delay to minimum and repeat rate to maximum for snappier key response.",
                Type = TweakType.Toggle,
                IsFree = false,
                HasWarning = false,
                Enable = () =>
                {
                    RegistryHelper.SetString(RegistryHive.CurrentUser, KeyboardKey, "KeyboardDelay", "0");
                    RegistryHelper.SetString(RegistryHive.CurrentUser, KeyboardKey, "KeyboardSpeed", "31");
                    return true;
                },
                Disable = () =>
                {
                    RegistryHelper.SetString(RegistryHive.CurrentUser, KeyboardKey, "KeyboardDelay", "1");
                    RegistryHelper.SetString(RegistryHive.CurrentUser, KeyboardKey, "KeyboardSpeed", "31");
                    return true;
                },
                Check = () => (string?)RegistryHelper.GetValue(RegistryHive.CurrentUser, KeyboardKey, "KeyboardDelay") == "0",
            },

            // ── Disable Idle and Sleep States ──
            new Tweak
            {
                Name = "Disable Idle and Sleep States",
                Description = "Prevents the monitor and system from sleeping so gaming sessions are never interrupted.",
                Type = TweakType.Toggle,
                IsFree = false,
                HasWarning = false,
                Enable = () => SystemHelper.RunPowerShell(
                    "powercfg /change monitor-timeout-ac 0; " +
                    "powercfg /change standby-timeout-ac 0; " +
                    "powercfg /change hibernate-timeout-ac 0").success,
                Disable = () => SystemHelper.RunPowerShell(
                    "powercfg /change monitor-timeout-ac 15; " +
                    "powercfg /change standby-timeout-ac 30; " +
                    "powercfg /change hibernate-timeout-ac 0").success,
                Check = () => { var (ok, output) = SystemHelper.RunPowerShell("powercfg /query SCHEME_CURRENT SUB_SLEEP STANDBYIDLE | Select-String 'Current AC' | ForEach-Object { ($_ -split ':')[1].Trim() }", true); return ok && output.Trim() == "0x00000000"; },
            },

            // ── FREE: Disable Pointer Ballistics ──
            new Tweak
            {
                Name = "Disable Pointer Ballistics",
                Description = "Applies a flat mouse pointer curve to eliminate Windows pointer ballistics for 1:1 tracking.",
                Type = TweakType.Toggle,
                IsFree = true,
                HasWarning = false,
                Enable = () => SystemHelper.RunPowerShell(
                    @"Set-ItemProperty -Path 'HKCU:\Control Panel\Mouse' -Name 'SmoothMouseXCurve' " +
                    @"-Value ([byte[]](0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00," +
                    @"0xC0,0xCC,0x0C,0x00,0x00,0x00,0x00,0x00," +
                    @"0x80,0x99,0x19,0x00,0x00,0x00,0x00,0x00," +
                    @"0x40,0x66,0x26,0x00,0x00,0x00,0x00,0x00," +
                    @"0x00,0x33,0x33,0x00,0x00,0x00,0x00,0x00)) " +
                    @"-Type Binary -Force -ErrorAction SilentlyContinue; " +
                    @"Set-ItemProperty -Path 'HKCU:\Control Panel\Mouse' -Name 'SmoothMouseYCurve' " +
                    @"-Value ([byte[]](0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00," +
                    @"0xC0,0xCC,0x0C,0x00,0x00,0x00,0x00,0x00," +
                    @"0x80,0x99,0x19,0x00,0x00,0x00,0x00,0x00," +
                    @"0x40,0x66,0x26,0x00,0x00,0x00,0x00,0x00," +
                    @"0x00,0x33,0x33,0x00,0x00,0x00,0x00,0x00)) " +
                    @"-Type Binary -Force -ErrorAction SilentlyContinue").success,
                Disable = () => SystemHelper.RunPowerShell(
                    @"Remove-ItemProperty -Path 'HKCU:\Control Panel\Mouse' -Name 'SmoothMouseXCurve' -ErrorAction SilentlyContinue; " +
                    @"Remove-ItemProperty -Path 'HKCU:\Control Panel\Mouse' -Name 'SmoothMouseYCurve' -ErrorAction SilentlyContinue").success,
            },

            // ── FREE: Enable Raw Mouse Input Routing ──
            new Tweak
            {
                Name = "Enable Raw Mouse Input Routing",
                Description = "Routes mouse wheel input through DirectInput raw input path to reduce pointer processing overhead.",
                Type = TweakType.Toggle,
                IsFree = true,
                HasWarning = false,
                Enable = () => RegistryHelper.SetDword(RegistryHive.CurrentUser, RawInputKey, "MouseWheelRouting", 2),
                Disable = () => RegistryHelper.DeleteValue(RegistryHive.CurrentUser, RawInputKey, "MouseWheelRouting"),
                Check = () => RegistryHelper.GetDword(RegistryHive.CurrentUser, RawInputKey, "MouseWheelRouting") == 2,
            },

            // ── FREE: Disable Filter Keys ──
            new Tweak
            {
                Name = "Disable Filter Keys",
                Description = "Prevents the Filter Keys accessibility shortcut from triggering and adding input delays.",
                Type = TweakType.Toggle,
                IsFree = true,
                HasWarning = false,
                Enable = () => RegistryHelper.SetString(RegistryHive.CurrentUser, FilterKeysKey, "Flags", "122"),
                Disable = () => RegistryHelper.SetString(RegistryHive.CurrentUser, FilterKeysKey, "Flags", "126"),
                Check = () => (string?)RegistryHelper.GetValue(RegistryHive.CurrentUser, FilterKeysKey, "Flags") == "122",
            },
        };
    }
}
