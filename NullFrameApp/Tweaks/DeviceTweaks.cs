using System.Collections.Generic;
using Microsoft.Win32;
using NullFrame.Models;
using NullFrame.Services;

namespace NullFrame.Tweaks
{
    public static class DeviceTweaks
    {
        private const string DoPolicyKey = @"SOFTWARE\Policies\Microsoft\Windows\DeliveryOptimization";

        public static List<Tweak> GetTweaks() => new()
        {
            // ── Disable Communication Ports ──
            new Tweak
            {
                Name = "Disable Communication Ports",
                Description = "Disables COM/serial communication ports that are not in use to reduce interrupt overhead.",
                Type = TweakType.Toggle,
                IsFree = false,
                HasWarning = true,
                Enable = () => SystemHelper.RunPowerShell(
                    @"$ports = Get-WmiObject -Class Win32_SerialPort -ErrorAction SilentlyContinue; " +
                    @"foreach ($p in $ports) { Disable-PnpDevice -InstanceId $p.PNPDeviceID -Confirm:$false -ErrorAction SilentlyContinue }").success,
                Disable = () => SystemHelper.RunPowerShell(
                    @"$ports = Get-PnpDevice -Class Ports -ErrorAction SilentlyContinue; " +
                    @"foreach ($p in $ports) { Enable-PnpDevice -InstanceId $p.InstanceId -Confirm:$false -ErrorAction SilentlyContinue }").success,
            },

            // ── Disable Bus Programmable Interrupt Controller ──
            new Tweak
            {
                Name = "Disable Bus Programmable Interrupt Controller",
                Description = "Switches from legacy APIC/PIC mode to modern IOAPIC for better interrupt routing and lower latency.",
                Type = TweakType.Toggle,
                IsFree = false,
                HasWarning = true,
                Enable = () => SystemHelper.RunBcdedit("/set uselegacyapicmode no"),
                Disable = () => SystemHelper.RunBcdedit("/deletevalue uselegacyapicmode"),
            },

            // ── Disable High Precision Event Timer ──
            new Tweak
            {
                Name = "Disable High Precision Event Timer",
                Description = "Disables HPET and enables TSC-based timing for lower timer latency on modern CPUs.",
                Type = TweakType.Toggle,
                IsFree = false,
                HasWarning = true,
                Enable = () =>
                {
                    SystemHelper.RunBcdedit("/deletevalue useplatformclock");
                    SystemHelper.RunBcdedit("/set disabledynamictick yes");
                    SystemHelper.RunPowerShell(
                        @"$hpet = Get-PnpDevice | Where-Object {$_.FriendlyName -like '*High Precision Event Timer*'} -ErrorAction SilentlyContinue; " +
                        @"if ($hpet) { Disable-PnpDevice -InstanceId $hpet.InstanceId -Confirm:$false -ErrorAction SilentlyContinue }");
                    return true;
                },
                Disable = () =>
                {
                    SystemHelper.RunBcdedit("/set useplatformclock true");
                    SystemHelper.RunBcdedit("/deletevalue disabledynamictick");
                    SystemHelper.RunPowerShell(
                        @"$hpet = Get-PnpDevice | Where-Object {$_.FriendlyName -like '*High Precision Event Timer*'} -ErrorAction SilentlyContinue; " +
                        @"if ($hpet) { Enable-PnpDevice -InstanceId $hpet.InstanceId -Confirm:$false -ErrorAction SilentlyContinue }");
                    return true;
                },
            },

            // ── Disable Microsoft Do Moveable Sync ──
            new Tweak
            {
                Name = "Disable Microsoft Do Moveable Sync",
                Description = "Disables Windows Delivery Optimisation background sync service to free network and CPU resources.",
                Type = TweakType.Toggle,
                IsFree = false,
                HasWarning = false,
                Enable = () =>
                {
                    SystemHelper.RunPowerShell(
                        "Stop-Service -Name DoSvc -ErrorAction SilentlyContinue; " +
                        "Set-Service -Name DoSvc -StartupType Disabled -ErrorAction SilentlyContinue");
                    RegistryHelper.SetDword(RegistryHive.LocalMachine, DoPolicyKey, "DODownloadMode", 0);
                    return true;
                },
                Disable = () =>
                {
                    SystemHelper.RunPowerShell(
                        "Set-Service -Name DoSvc -StartupType Automatic -ErrorAction SilentlyContinue; " +
                        "Start-Service -Name DoSvc -ErrorAction SilentlyContinue");
                    RegistryHelper.DeleteValue(RegistryHive.LocalMachine, DoPolicyKey, "DODownloadMode");
                    return true;
                },
                Check = () => RegistryHelper.GetDword(RegistryHive.LocalMachine, DoPolicyKey, "DODownloadMode") == 0,
            },

            // ── Disable Microsoft Hyper-V Infrastructure Driver ──
            new Tweak
            {
                Name = "Disable Microsoft Hyper-V Infrastructure Driver",
                Description = "Disables Hyper-V virtualisation services and drivers, freeing CPU overhead for native performance.",
                Type = TweakType.Toggle,
                IsFree = false,
                HasWarning = true,
                Enable = () => SystemHelper.RunPowerShell(
                    @"foreach ($s in @('HvHost','vmicheartbeat','vmickvpexchange','vmicrdv','vmicshutdown','vmictimesync','vmicvss')) { " +
                    @"Stop-Service -Name $s -Force -ErrorAction SilentlyContinue; " +
                    @"Set-Service -Name $s -StartupType Disabled -ErrorAction SilentlyContinue }").success,
                Disable = () => SystemHelper.RunPowerShell(
                    @"foreach ($s in @('HvHost','vmicheartbeat','vmickvpexchange','vmicrdv','vmicshutdown','vmictimesync','vmicvss')) { " +
                    @"Set-Service -Name $s -StartupType Manual -ErrorAction SilentlyContinue }").success,
            },

            // ── Disable Remote Desktop Device Redirector Bus ──
            new Tweak
            {
                Name = "Disable Remote Desktop Device Redirector Bus",
                Description = "Disables the RDP device redirection bus driver to reduce driver overhead when not using Remote Desktop.",
                Type = TweakType.Toggle,
                IsFree = false,
                HasWarning = false,
                Enable = () => SystemHelper.RunPowerShell(
                    "Stop-Service -Name RDPDR -ErrorAction SilentlyContinue; " +
                    "Set-Service -Name RDPDR -StartupType Disabled -ErrorAction SilentlyContinue; " +
                    "Stop-Service -Name RdpBus -ErrorAction SilentlyContinue; " +
                    "Set-Service -Name RdpBus -StartupType Disabled -ErrorAction SilentlyContinue").success,
                Disable = () => SystemHelper.RunPowerShell(
                    "Set-Service -Name RDPDR -StartupType Manual -ErrorAction SilentlyContinue; " +
                    "Set-Service -Name RdpBus -StartupType Manual -ErrorAction SilentlyContinue").success,
            },

            // ── Disable Serial Ports ──
            new Tweak
            {
                Name = "Disable Serial Ports",
                Description = "Disables all serial port devices to reduce IRQ and driver overhead.",
                Type = TweakType.Toggle,
                IsFree = false,
                HasWarning = true,
                Enable = () => SystemHelper.RunPowerShell(
                    @"$ports = Get-PnpDevice -Class Ports -ErrorAction SilentlyContinue; " +
                    @"foreach ($p in $ports) { Disable-PnpDevice -InstanceId $p.InstanceId -Confirm:$false -ErrorAction SilentlyContinue }; " +
                    "Stop-Service -Name Serial -ErrorAction SilentlyContinue; " +
                    "Set-Service -Name Serial -StartupType Disabled -ErrorAction SilentlyContinue").success,
                Disable = () => SystemHelper.RunPowerShell(
                    @"$ports = Get-PnpDevice -Class Ports -ErrorAction SilentlyContinue; " +
                    @"foreach ($p in $ports) { Enable-PnpDevice -InstanceId $p.InstanceId -Confirm:$false -ErrorAction SilentlyContinue }; " +
                    "Set-Service -Name Serial -StartupType Manual -ErrorAction SilentlyContinue").success,
            },
        };
    }
}
