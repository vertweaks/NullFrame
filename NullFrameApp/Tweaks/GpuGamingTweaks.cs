using System.Collections.Generic;
using Microsoft.Win32;
using NullFrame.Models;
using NullFrame.Services;

namespace NullFrame.Tweaks
{
    public static class GpuGamingTweaks
    {
        private const string GamesKey = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games";
        private const string ProfileKey = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile";
        private const string PowerKey = @"SYSTEM\CurrentControlSet\Control\Power";
        private const string SessionKey = @"SYSTEM\CurrentControlSet\Control\Session Manager";
        private const string TcpKey = @"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters";
        private const string GfxDriversKey = @"SYSTEM\CurrentControlSet\Control\GraphicsDrivers";
        private const string GameBarKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\GameDVR";
        private const string GameCfgKey = @"System\GameConfigStore";

        private const string IdleGuid = "2e601130-5351-4d9d-8e04-252966bad054";
        private const string IdleSubkey = "d502f7ee-1dc7-4efd-a55d-f04b6f5c0545";

        public static List<Tweak> GetTweaks() => new()
        {
            // ── Set GPU Priority for Current Game ──
            new Tweak
            {
                Name = "Set GPU Priority for Current Game",
                Description = "Increases the GPU task scheduling priority for game processes, improving frame pacing and stability.",
                Type = TweakType.Toggle,
                IsFree = false,
                HasWarning = false,
                Enable = () =>
                {
                    RegistryHelper.SetDword(RegistryHive.LocalMachine, GamesKey, "GPU Priority", 8);
                    RegistryHelper.SetDword(RegistryHive.LocalMachine, GamesKey, "Priority", 6);
                    RegistryHelper.SetString(RegistryHive.LocalMachine, GamesKey, "Scheduling Category", "High");
                    RegistryHelper.SetString(RegistryHive.LocalMachine, GamesKey, "SFIO Priority", "High");
                    return true;
                },
                Disable = () =>
                {
                    RegistryHelper.SetDword(RegistryHive.LocalMachine, GamesKey, "GPU Priority", 2);
                    RegistryHelper.SetDword(RegistryHive.LocalMachine, GamesKey, "Priority", 2);
                    RegistryHelper.SetString(RegistryHive.LocalMachine, GamesKey, "Scheduling Category", "Medium");
                    RegistryHelper.SetString(RegistryHive.LocalMachine, GamesKey, "SFIO Priority", "Normal");
                    return true;
                },
                Check = () => RegistryHelper.GetDword(RegistryHive.LocalMachine, GamesKey, "GPU Priority") == 8,
            },

            // ── Set I/O Priority for Current Game ──
            new Tweak
            {
                Name = "Set I/O Priority for Current Game",
                Description = "Assigns higher I/O priority to game processes so they get faster access to disk reads and writes.",
                Type = TweakType.Toggle,
                IsFree = false,
                HasWarning = false,
                Enable = () =>
                {
                    RegistryHelper.SetString(RegistryHive.LocalMachine, GamesKey, "SFIO Priority", "High");
                    RegistryHelper.SetDword(RegistryHive.LocalMachine, ProfileKey, "SystemResponsiveness", 0);
                    return true;
                },
                Disable = () =>
                {
                    RegistryHelper.SetString(RegistryHive.LocalMachine, GamesKey, "SFIO Priority", "Normal");
                    RegistryHelper.SetDword(RegistryHive.LocalMachine, ProfileKey, "SystemResponsiveness", 20);
                    return true;
                },
                Check = () => RegistryHelper.GetDword(RegistryHive.LocalMachine, ProfileKey, "SystemResponsiveness") == 0,
            },

            // ── Disable Segment Heap ──
            new Tweak
            {
                Name = "Disable Segment Heap",
                Description = "Disables UWP/Win32 segment heap to reduce memory overhead and improve performance.",
                Type = TweakType.Toggle,
                IsFree = false,
                HasWarning = false,
                Enable = () => RegistryHelper.SetDword(RegistryHive.LocalMachine, SessionKey, "SegmentHeap", 0),
                Disable = () => RegistryHelper.DeleteValue(RegistryHive.LocalMachine, SessionKey, "SegmentHeap"),
                Check = () => RegistryHelper.GetDword(RegistryHive.LocalMachine, SessionKey, "SegmentHeap") == 0,
            },

            // ── Disable PCIe ASPM ──
            new Tweak
            {
                Name = "Disable PCIe ASPM",
                Description = "Disables PCIe Active State Power Management so GPU/PCIe devices don't downclock mid-game.",
                Type = TweakType.Toggle,
                IsFree = false,
                HasWarning = true,
                Enable = () =>
                {
                    RegistryHelper.SetDword(RegistryHive.LocalMachine, PowerKey, "PlatformAoAcOverride", 0);
                    RegistryHelper.SetDword(RegistryHive.LocalMachine, PowerKey, "PcieAspmPolicy", 0);
                    return true;
                },
                Disable = () =>
                {
                    RegistryHelper.DeleteValue(RegistryHive.LocalMachine, PowerKey, "PlatformAoAcOverride");
                    RegistryHelper.DeleteValue(RegistryHive.LocalMachine, PowerKey, "PcieAspmPolicy");
                    return true;
                },
                Check = () => RegistryHelper.GetDword(RegistryHive.LocalMachine, PowerKey, "PcieAspmPolicy") == 0,
            },

            // ── Disable Platform Idle ──
            new Tweak
            {
                Name = "Disable Platform Idle",
                Description = "Prevents the processor from entering idle states so the GPU always has CPU bandwidth available.",
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
                Description = "Disables low-power idle states to allow cores to stay fully active during gaming.",
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

            // ── Temporarily Kill Explorer ──
            new Tweak
            {
                Name = "Temporarily Kill Explorer",
                Description = "Stops Windows Explorer to free CPU, GPU, and RAM resources. Toggle again to restart Explorer.",
                Type = TweakType.Toggle,
                IsFree = false,
                HasWarning = true,
                Enable = () => SystemHelper.RunPowerShell("Stop-Process -Name explorer -Force -ErrorAction SilentlyContinue").success,
                Disable = () => SystemHelper.RunPowerShell("Start-Process explorer").success,
                Check = () => { var (ok, output) = SystemHelper.RunPowerShell("(Get-Process explorer -ErrorAction SilentlyContinue) -eq $null", true); return output.Trim() == "True"; },
            },

            // ── Set SBR Connections ──
            new Tweak
            {
                Name = "Set SBR Connections",
                Description = "Optimizes TCP connection limits and wait delays for better in-game network performance.",
                Type = TweakType.Toggle,
                IsFree = false,
                HasWarning = false,
                Enable = () =>
                {
                    RegistryHelper.SetDword(RegistryHive.LocalMachine, TcpKey, "MaxUserPort", 65534);
                    RegistryHelper.SetDword(RegistryHive.LocalMachine, TcpKey, "TcpTimedWaitDelay", 30);
                    RegistryHelper.SetDword(RegistryHive.LocalMachine, TcpKey, "MaxFreeTcbs", 16000);
                    return true;
                },
                Disable = () =>
                {
                    RegistryHelper.DeleteValue(RegistryHive.LocalMachine, TcpKey, "MaxUserPort");
                    RegistryHelper.DeleteValue(RegistryHive.LocalMachine, TcpKey, "TcpTimedWaitDelay");
                    RegistryHelper.DeleteValue(RegistryHive.LocalMachine, TcpKey, "MaxFreeTcbs");
                    return true;
                },
                Check = () => RegistryHelper.GetDword(RegistryHive.LocalMachine, TcpKey, "MaxUserPort") == 65534,
            },

            // ── FREE: Enable Hardware Accelerated GPU Scheduling ──
            new Tweak
            {
                Name = "Enable Hardware Accelerated GPU Scheduling",
                Description = "Enables HAGS to allow the GPU to manage its own video memory, reducing CPU overhead and latency.",
                Type = TweakType.Toggle,
                IsFree = true,
                HasWarning = false,
                Enable = () => RegistryHelper.SetDword(RegistryHive.LocalMachine, GfxDriversKey, "HwSchMode", 2),
                Disable = () => RegistryHelper.SetDword(RegistryHive.LocalMachine, GfxDriversKey, "HwSchMode", 1),
                Check = () => RegistryHelper.GetDword(RegistryHive.LocalMachine, GfxDriversKey, "HwSchMode") == 2,
            },

            // ── FREE: Disable Xbox Game Bar ──
            new Tweak
            {
                Name = "Disable Xbox Game Bar",
                Description = "Disables the Xbox Game Bar overlay and background capture to free GPU and CPU resources while gaming.",
                Type = TweakType.Toggle,
                IsFree = true,
                HasWarning = false,
                Enable = () =>
                {
                    RegistryHelper.SetDword(RegistryHive.CurrentUser, GameBarKey, "AppCaptureEnabled", 0);
                    RegistryHelper.SetDword(RegistryHive.CurrentUser, GameCfgKey, "GameDVR_Enabled", 0);
                    SystemHelper.RunPowerShell("Get-AppxPackage Microsoft.XboxGamingOverlay | Remove-AppxPackage -ErrorAction SilentlyContinue");
                    return true;
                },
                Disable = () =>
                {
                    RegistryHelper.SetDword(RegistryHive.CurrentUser, GameBarKey, "AppCaptureEnabled", 1);
                    RegistryHelper.SetDword(RegistryHive.CurrentUser, GameCfgKey, "GameDVR_Enabled", 1);
                    return true;
                },
                Check = () => RegistryHelper.GetDword(RegistryHive.CurrentUser, GameBarKey, "AppCaptureEnabled") == 0,
            },

            // ── FREE: Disable Nagle's Algorithm ──
            new Tweak
            {
                Name = "Disable Nagle's Algorithm",
                Description = "Disables Nagle's algorithm to send packets immediately without buffering, reducing in-game network latency.",
                Type = TweakType.Toggle,
                IsFree = true,
                HasWarning = false,
                Enable = () =>
                {
                    RegistryHelper.SetDword(RegistryHive.LocalMachine, TcpKey, "TcpAckFrequency", 1);
                    RegistryHelper.SetDword(RegistryHive.LocalMachine, TcpKey, "TCPNoDelay", 1);
                    return true;
                },
                Disable = () =>
                {
                    RegistryHelper.DeleteValue(RegistryHive.LocalMachine, TcpKey, "TcpAckFrequency");
                    RegistryHelper.DeleteValue(RegistryHive.LocalMachine, TcpKey, "TCPNoDelay");
                    return true;
                },
                Check = () => RegistryHelper.GetDword(RegistryHive.LocalMachine, TcpKey, "TCPNoDelay") == 1,
            },
        };
    }
}
