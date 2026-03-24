using System;
using System.Collections.Generic;
using Microsoft.Win32;
using NullFrame.Models;
using NullFrame.Services;

namespace NullFrame.Tweaks
{
    public static class CpuSystemTweaks
    {
        private const string PriorityKey = @"SYSTEM\CurrentControlSet\Control\PriorityControl";
        private const string PowerKey = @"SYSTEM\CurrentControlSet\Control\Power";
        private const string ThrottleKey = @"SYSTEM\CurrentControlSet\Control\Power\PowerThrottling";
        private const string ExecKey = @"SYSTEM\CurrentControlSet\Control\Session Manager\Executive";
        private const string GameDvrKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\GameDVR";
        private const string GameCfgKey = @"System\GameConfigStore";
        private const string GfxDriversKey = @"SYSTEM\CurrentControlSet\Control\GraphicsDrivers";
        private const string VisualKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects";
        private const string WerKey = @"SOFTWARE\Microsoft\Windows\Windows Error Reporting";

        private const string ParkGuid = "54533251-82be-4824-96c1-47b60b740d00";
        private const string ParkSubkey = "0cc5b647-c1df-4637-891a-dec35c318583";
        private const string IdleGuid = "2e601130-5351-4d9d-8e04-252966bad054";
        private const string IdleSubkey = "d502f7ee-1dc7-4efd-a55d-f04b6f5c0545";

        public static List<Tweak> GetTweaks() => new()
        {
            // ── Win32 Priority Separation ──
            new Tweak
            {
                Name = "Set Windows 32 Priority Separation",
                Description = "Optimizes CPU scheduling by giving higher priority to the active foreground application for better performance and responsiveness.",
                Type = TweakType.Preset,
                IsFree = false,
                HasWarning = false,
                Presets = new[]
                {
                    new TweakPreset
                    {
                        Name = "26 hex",
                        Description = "Variable interval, short quantum, 3× foreground boost — best for gaming",
                        Recommended = true,
                        Apply = () => RegistryHelper.SetDword(RegistryHive.LocalMachine, PriorityKey, "Win32PrioritySeparation", 0x26),
                        Check = () => RegistryHelper.GetDword(RegistryHive.LocalMachine, PriorityKey, "Win32PrioritySeparation") == 0x26,
                    },
                    new TweakPreset
                    {
                        Name = "18 hex",
                        Description = "Variable interval, short quantum, equal foreground/background priority",
                        Apply = () => RegistryHelper.SetDword(RegistryHive.LocalMachine, PriorityKey, "Win32PrioritySeparation", 0x18),
                        Check = () => RegistryHelper.GetDword(RegistryHive.LocalMachine, PriorityKey, "Win32PrioritySeparation") == 0x18,
                    },
                    new TweakPreset
                    {
                        Name = "16 hex",
                        Description = "Fixed interval, short quantum, equal priority",
                        Apply = () => RegistryHelper.SetDword(RegistryHive.LocalMachine, PriorityKey, "Win32PrioritySeparation", 0x16),
                        Check = () => RegistryHelper.GetDword(RegistryHive.LocalMachine, PriorityKey, "Win32PrioritySeparation") == 0x16,
                    },
                    new TweakPreset
                    {
                        Name = "2A hex",
                        Description = "Fixed interval, short quantum, 3× foreground boost",
                        Apply = () => RegistryHelper.SetDword(RegistryHive.LocalMachine, PriorityKey, "Win32PrioritySeparation", 0x2A),
                        Check = () => RegistryHelper.GetDword(RegistryHive.LocalMachine, PriorityKey, "Win32PrioritySeparation") == 0x2A,
                    },
                },
            },

            // ── Configure Tsync Policy ──
            new Tweak
            {
                Name = "Configure Tsync Policy",
                Description = "Defines how the OS synchronizes timing events between CPU cores and system timers.",
                Type = TweakType.Preset,
                IsFree = false,
                HasWarning = false,
                Presets = new[]
                {
                    new TweakPreset
                    {
                        Name = "Legacy",
                        Description = "Forces platform clock — lowest timer latency for gaming",
                        Recommended = true,
                        Apply = () =>
                        {
                            SystemHelper.RunBcdedit("/set useplatformclock true");
                            SystemHelper.RunBcdedit("/set disabledynamictick yes");
                            return true;
                        },
                    },
                    new TweakPreset
                    {
                        Name = "Enhanced",
                        Description = "Dynamic tick disabled, platform clock removed",
                        Apply = () =>
                        {
                            SystemHelper.RunBcdedit("/deletevalue useplatformclock");
                            SystemHelper.RunBcdedit("/set disabledynamictick yes");
                            return true;
                        },
                    },
                    new TweakPreset
                    {
                        Name = "Default",
                        Description = "Windows default timer synchronization behavior",
                        Apply = () =>
                        {
                            SystemHelper.RunBcdedit("/deletevalue useplatformclock");
                            SystemHelper.RunBcdedit("/deletevalue disabledynamictick");
                            return true;
                        },
                    },
                },
            },

            // ── Disable CPU Core Parking ──
            new Tweak
            {
                Name = "Disable CPU Core Parking",
                Description = "Prevents Windows from parking CPU cores, keeping all cores active to reduce latency spikes.",
                Type = TweakType.Toggle,
                IsFree = false,
                HasWarning = false,
                Enable = () => SystemHelper.RunPowerShell(
                    $"$g = (powercfg /getactivescheme).Split()[3]; " +
                    $"powercfg /setacvalueindex $g {ParkGuid} {ParkSubkey} 100; " +
                    $"powercfg /setdcvalueindex $g {ParkGuid} {ParkSubkey} 100; " +
                    "powercfg /apply").success,
                Disable = () => SystemHelper.RunPowerShell(
                    $"$g = (powercfg /getactivescheme).Split()[3]; " +
                    $"powercfg /setacvalueindex $g {ParkGuid} {ParkSubkey} 0; " +
                    $"powercfg /setdcvalueindex $g {ParkGuid} {ParkSubkey} 0; " +
                    "powercfg /apply").success,
                Check = () =>
                {
                    var (ok, output) = SystemHelper.RunPowerShell(
                        $"powercfg /query SCHEME_CURRENT {ParkGuid} {ParkSubkey}", true);
                    return output.Contains("0x00000064");
                },
            },

            // ── Disable CPU Power Throttling ──
            new Tweak
            {
                Name = "Disable CPU Power Throttling",
                Description = "Disables Windows CPU power throttling to maintain full CPU performance at all times.",
                Type = TweakType.Toggle,
                IsFree = false,
                HasWarning = false,
                Enable = () => RegistryHelper.SetDword(RegistryHive.LocalMachine, ThrottleKey, "PowerThrottlingOff", 1),
                Disable = () => RegistryHelper.DeleteValue(RegistryHive.LocalMachine, ThrottleKey, "PowerThrottlingOff"),
                Check = () => RegistryHelper.GetDword(RegistryHive.LocalMachine, ThrottleKey, "PowerThrottlingOff") == 1,
            },

            // ── Disable Platform Idle ──
            new Tweak
            {
                Name = "Disable Platform Idle",
                Description = "Prevents the processor from entering idle states for maximum responsiveness.",
                Type = TweakType.Toggle,
                IsFree = false,
                HasWarning = true,
                Enable = () => SystemHelper.RunPowerShell(
                    $"powercfg /setacvalueindex SCHEME_CURRENT {IdleGuid} {IdleSubkey} 1; " +
                    $"powercfg /setdcvalueindex SCHEME_CURRENT {IdleGuid} {IdleSubkey} 1; " +
                    "powercfg /apply").success,
                Disable = () => SystemHelper.RunPowerShell(
                    $"powercfg /setacvalueindex SCHEME_CURRENT {IdleGuid} {IdleSubkey} 0; " +
                    $"powercfg /setdcvalueindex SCHEME_CURRENT {IdleGuid} {IdleSubkey} 0; " +
                    "powercfg /apply").success,
                Check = () => { var val = SystemHelper.QueryPowerCfg("2e601130-5351-4d9d-8e04-252966bad054", "d502f7ee-1dc7-4efd-a55d-f04b6f5c0545"); return val.Contains("0x00000001"); },
            },

            // ── Processor Idle Disable ──
            new Tweak
            {
                Name = "Processor Idle Disable",
                Description = "Disables low-power idle states to allow cores to stay fully active.",
                Type = TweakType.Toggle,
                IsFree = false,
                HasWarning = true,
                Enable = () => SystemHelper.RunPowerShell(
                    "powercfg /setacvalueindex SCHEME_CURRENT SUB_PROCESSOR IDLEDISABLE 1; " +
                    "powercfg /setdcvalueindex SCHEME_CURRENT SUB_PROCESSOR IDLEDISABLE 1; " +
                    "powercfg /apply").success,
                Disable = () => SystemHelper.RunPowerShell(
                    "powercfg /setacvalueindex SCHEME_CURRENT SUB_PROCESSOR IDLEDISABLE 0; " +
                    "powercfg /setdcvalueindex SCHEME_CURRENT SUB_PROCESSOR IDLEDISABLE 0; " +
                    "powercfg /apply").success,
                Check = () => { var val = SystemHelper.QueryPowerCfg("SUB_PROCESSOR", "IDLEDISABLE"); return val.Contains("0x00000001"); },
            },

            // ── Disable Basic C-States ──
            new Tweak
            {
                Name = "Disable Basic C-States",
                Description = "Disables CPU power-saving C-states to reduce latency and improve consistency.",
                Type = TweakType.Toggle,
                IsFree = false,
                HasWarning = true,
                Enable = () =>
                {
                    SystemHelper.RunBcdedit("/set disabledynamictick yes");
                    SystemHelper.RunPowerShell(
                        "powercfg /setacvalueindex SCHEME_CURRENT SUB_PROCESSOR IDLEDISABLE 1; " +
                        "powercfg /apply");
                    return true;
                },
                Disable = () =>
                {
                    SystemHelper.RunBcdedit("/deletevalue disabledynamictick");
                    SystemHelper.RunPowerShell(
                        "powercfg /setacvalueindex SCHEME_CURRENT SUB_PROCESSOR IDLEDISABLE 0; " +
                        "powercfg /apply");
                    return true;
                },
                Check = () =>
                {
                    var (ok, output) = SystemHelper.RunCmd("bcdedit /enum {current}", true);
                    var lower = output.ToLower();
                    return lower.Contains("disabledynamictick") && lower.Contains("yes");
                },
            },

            // ── Disable Clockwise Timer ──
            new Tweak
            {
                Name = "Disable Clockwise Timer",
                Description = "Disables dynamic tick to maintain consistent timer intervals for better performance.",
                Type = TweakType.Toggle,
                IsFree = false,
                HasWarning = false,
                Enable = () => SystemHelper.RunBcdedit("/set disabledynamictick yes"),
                Disable = () => SystemHelper.RunBcdedit("/deletevalue disabledynamictick"),
                Check = () => SystemHelper.CheckBcdedit("disabledynamictick", "yes"),
            },

            // ── Disable Modern Standby ──
            new Tweak
            {
                Name = "Disable Modern Standby",
                Description = "Disables Windows Modern Standby (S0 low-power idle) to prevent background CPU activity.",
                Type = TweakType.Toggle,
                IsFree = false,
                HasWarning = true,
                Enable = () => RegistryHelper.SetDword(RegistryHive.LocalMachine, PowerKey, "PlatformAoAcOverride", 0),
                Disable = () => RegistryHelper.DeleteValue(RegistryHive.LocalMachine, PowerKey, "PlatformAoAcOverride"),
                Check = () => RegistryHelper.GetDword(RegistryHive.LocalMachine, PowerKey, "PlatformAoAcOverride") == 0,
            },

            // ── Set Energy Performance Preference ──
            new Tweak
            {
                Name = "Set Energy Performance Preference",
                Description = "Sets CPU performance boost policy to maximum for sustained high performance.",
                Type = TweakType.Toggle,
                IsFree = false,
                HasWarning = false,
                Enable = () => SystemHelper.RunPowerShell(
                    "powercfg /setacvalueindex SCHEME_CURRENT SUB_PROCESSOR PERFBOOSTPOL 100; " +
                    "powercfg /setacvalueindex SCHEME_CURRENT SUB_PROCESSOR PERFBOOSTMODE 2; " +
                    "powercfg /apply").success,
                Disable = () => SystemHelper.RunPowerShell(
                    "powercfg /setacvalueindex SCHEME_CURRENT SUB_PROCESSOR PERFBOOSTPOL 50; " +
                    "powercfg /setacvalueindex SCHEME_CURRENT SUB_PROCESSOR PERFBOOSTMODE 1; " +
                    "powercfg /apply").success,
                Check = () => { var val = SystemHelper.QueryPowerCfg("SUB_PROCESSOR", "PERFBOOSTPOL"); return val.Contains("0x00000064"); },
            },

            // ── Set Minimum and Maximum Processor State ──
            new Tweak
            {
                Name = "Set Minimum and Maximum Processor State",
                Description = "Locks CPU at 100% min/max state to prevent frequency scaling mid-game.",
                Type = TweakType.Toggle,
                IsFree = false,
                HasWarning = true,
                Enable = () => SystemHelper.RunPowerShell(
                    "powercfg /setacvalueindex SCHEME_CURRENT SUB_PROCESSOR PROCTHROTTLEMIN 100; " +
                    "powercfg /setacvalueindex SCHEME_CURRENT SUB_PROCESSOR PROCTHROTTLEMAX 100; " +
                    "powercfg /apply").success,
                Disable = () => SystemHelper.RunPowerShell(
                    "powercfg /setacvalueindex SCHEME_CURRENT SUB_PROCESSOR PROCTHROTTLEMIN 5; " +
                    "powercfg /setacvalueindex SCHEME_CURRENT SUB_PROCESSOR PROCTHROTTLEMAX 100; " +
                    "powercfg /apply").success,
                Check = () => { var val = SystemHelper.QueryPowerCfg("SUB_PROCESSOR", "PROCTHROTTLEMIN"); return val.Contains("0x00000064"); },
            },

            // ── Set Kernel Worker Threads ──
            new Tweak
            {
                Name = "Set Kernel Worker Threads",
                Description = "Increases kernel worker threads based on CPU core count for better multi-threaded performance.",
                Type = TweakType.Toggle,
                IsFree = false,
                HasWarning = false,
                Enable = () =>
                {
                    int count = Environment.ProcessorCount;
                    RegistryHelper.SetDword(RegistryHive.LocalMachine, ExecKey, "AdditionalCriticalWorkerThreads", count);
                    RegistryHelper.SetDword(RegistryHive.LocalMachine, ExecKey, "AdditionalDelayedWorkerThreads", count);
                    return true;
                },
                Disable = () =>
                {
                    RegistryHelper.DeleteValue(RegistryHive.LocalMachine, ExecKey, "AdditionalCriticalWorkerThreads");
                    RegistryHelper.DeleteValue(RegistryHive.LocalMachine, ExecKey, "AdditionalDelayedWorkerThreads");
                    return true;
                },
                Check = () => RegistryHelper.ValueExists(RegistryHive.LocalMachine, ExecKey, "AdditionalCriticalWorkerThreads"),
            },

            // ── Disable Overprocessor ──
            new Tweak
            {
                Name = "Disable Overprocessor",
                Description = "Disables Windows Connected Standby (CsEnabled) to reduce background CPU overhead.",
                Type = TweakType.Toggle,
                IsFree = false,
                HasWarning = true,
                Enable = () => RegistryHelper.SetDword(RegistryHive.LocalMachine, PowerKey, "CsEnabled", 0),
                Disable = () => RegistryHelper.DeleteValue(RegistryHive.LocalMachine, PowerKey, "CsEnabled"),
                Check = () => RegistryHelper.GetDword(RegistryHive.LocalMachine, PowerKey, "CsEnabled") == 0,
            },

            // ── FREE: Disable GameDVR Recording ──
            new Tweak
            {
                Name = "Disable GameDVR Recording",
                Description = "Disables Microsoft GameDVR and Xbox Game Bar background recording to free up CPU and GPU resources.",
                Type = TweakType.Toggle,
                IsFree = true,
                HasWarning = false,
                Enable = () =>
                {
                    RegistryHelper.SetDword(RegistryHive.CurrentUser, GameDvrKey, "AppCaptureEnabled", 0);
                    RegistryHelper.SetDword(RegistryHive.CurrentUser, GameCfgKey, "GameDVR_Enabled", 0);
                    RegistryHelper.SetDword(RegistryHive.CurrentUser, GameCfgKey, "GameDVR_FSEBehaviorMode", 2);
                    return true;
                },
                Disable = () =>
                {
                    RegistryHelper.SetDword(RegistryHive.CurrentUser, GameDvrKey, "AppCaptureEnabled", 1);
                    RegistryHelper.DeleteValue(RegistryHive.CurrentUser, GameCfgKey, "GameDVR_Enabled");
                    RegistryHelper.DeleteValue(RegistryHive.CurrentUser, GameCfgKey, "GameDVR_FSEBehaviorMode");
                    return true;
                },
                Check = () => RegistryHelper.GetDword(RegistryHive.CurrentUser, GameDvrKey, "AppCaptureEnabled") == 0,
            },

            // ── FREE: Disable Fullscreen Optimizations ──
            new Tweak
            {
                Name = "Disable Fullscreen Optimizations",
                Description = "Disables Windows fullscreen optimization layer so games run in true exclusive fullscreen mode for lower latency.",
                Type = TweakType.Toggle,
                IsFree = true,
                HasWarning = false,
                Enable = () =>
                {
                    RegistryHelper.SetDword(RegistryHive.CurrentUser, GameCfgKey, "GameDVR_DXGIHonorFSEWindowScaling", 1);
                    RegistryHelper.SetDword(RegistryHive.LocalMachine, GfxDriversKey, "DisableWriteCombining", 1);
                    return true;
                },
                Disable = () =>
                {
                    RegistryHelper.DeleteValue(RegistryHive.CurrentUser, GameCfgKey, "GameDVR_DXGIHonorFSEWindowScaling");
                    RegistryHelper.DeleteValue(RegistryHive.LocalMachine, GfxDriversKey, "DisableWriteCombining");
                    return true;
                },
                Check = () => RegistryHelper.GetDword(RegistryHive.CurrentUser, GameCfgKey, "GameDVR_DXGIHonorFSEWindowScaling") == 1,
            },

            // ── FREE: Set High Performance Power Plan ──
            new Tweak
            {
                Name = "Set High Performance Power Plan",
                Description = "Activates the Windows High Performance power plan so the CPU never throttles during gaming or heavy workloads.",
                Type = TweakType.Toggle,
                IsFree = true,
                HasWarning = false,
                Enable = () => SystemHelper.RunPowerShell("powercfg /setactive 8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c").success,
                Disable = () => SystemHelper.RunPowerShell("powercfg /setactive 381b4222-f694-41f0-9685-ff5bb260df2e").success,
            },

            // ── FREE: Disable Hibernate ──
            new Tweak
            {
                Name = "Disable Hibernate",
                Description = "Disables Windows hibernation to reclaim disk space and prevent wake latency.",
                Type = TweakType.Toggle,
                IsFree = true,
                HasWarning = false,
                Enable = () => SystemHelper.RunPowerShell("powercfg /hibernate off").success,
                Disable = () => SystemHelper.RunPowerShell("powercfg /hibernate on").success,
            },

            // ── FREE: Set Visual Effects for Best Performance ──
            new Tweak
            {
                Name = "Set Visual Effects for Best Performance",
                Description = "Disables Windows visual effects and animations to dedicate more CPU resources to applications.",
                Type = TweakType.Toggle,
                IsFree = true,
                HasWarning = false,
                Enable = () =>
                {
                    RegistryHelper.SetDword(RegistryHive.CurrentUser, VisualKey, "VisualFXSetting", 2);
                    SystemHelper.RunPowerShell(
                        @"$path = 'HKCU:\Control Panel\Desktop'; " +
                        @"Set-ItemProperty $path UserPreferencesMask -Value ([byte[]](0x90,0x12,0x03,0x80,0x10,0x00,0x00,0x00)) -ErrorAction SilentlyContinue");
                    return true;
                },
                Disable = () => RegistryHelper.SetDword(RegistryHive.CurrentUser, VisualKey, "VisualFXSetting", 0),
                Check = () => RegistryHelper.GetDword(RegistryHive.CurrentUser, VisualKey, "VisualFXSetting") == 2,
            },

            // ── FREE: Disable Windows Error Reporting ──
            new Tweak
            {
                Name = "Disable Windows Error Reporting",
                Description = "Disables the WerSvc service and error reporting so crash dumps don't consume CPU and disk I/O.",
                Type = TweakType.Toggle,
                IsFree = true,
                HasWarning = false,
                Enable = () =>
                {
                    RegistryHelper.SetDword(RegistryHive.LocalMachine, WerKey, "Disabled", 1);
                    SystemHelper.RunPowerShell(
                        "Stop-Service -Name WerSvc -ErrorAction SilentlyContinue; " +
                        "Set-Service -Name WerSvc -StartupType Disabled -ErrorAction SilentlyContinue");
                    return true;
                },
                Disable = () =>
                {
                    RegistryHelper.DeleteValue(RegistryHive.LocalMachine, WerKey, "Disabled");
                    SystemHelper.RunPowerShell("Set-Service -Name WerSvc -StartupType Manual -ErrorAction SilentlyContinue");
                    return true;
                },
                Check = () => RegistryHelper.GetDword(RegistryHive.LocalMachine, WerKey, "Disabled") == 1,
            },
        };
    }
}
