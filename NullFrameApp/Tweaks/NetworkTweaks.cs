using System.Collections.Generic;
using Microsoft.Win32;
using NullFrame.Models;
using NullFrame.Services;

namespace NullFrame.Tweaks
{
    public static class NetworkTweaks
    {
        private const string TcpKey = @"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters";
        private const string Tcp6Key = @"SYSTEM\CurrentControlSet\Services\Tcpip6\Parameters";
        private const string ProfileKey = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile";
        private const string WudoKey = @"SOFTWARE\Policies\Microsoft\Windows\DeliveryOptimization";
        private const string LlmnrKey = @"SOFTWARE\Policies\Microsoft\Windows NT\DNSClient";

        public static List<Tweak> GetTweaks() => new()
        {
            // ── Disable Automatic TCP Adjustment ──
            new Tweak
            {
                Name = "Disable Automatic TCP Adjustment",
                Description = "Disables Windows TCP auto-tuning to prevent automatic window size adjustments that can cause latency.",
                Type = TweakType.Toggle,
                IsFree = false,
                HasWarning = false,
                Enable = () =>
                {
                    SystemHelper.RunCmd("netsh int tcp set global autotuninglevel=disabled");
                    SystemHelper.RunCmd("netsh int tcp set global chimney=disabled");
                    return true;
                },
                Disable = () =>
                {
                    SystemHelper.RunCmd("netsh int tcp set global autotuninglevel=normal");
                    SystemHelper.RunCmd("netsh int tcp set global chimney=enabled");
                    return true;
                },
                Check = () =>
                {
                    var (ok, output) = SystemHelper.RunCmd("netsh int tcp show global", true);
                    var lower = output.ToLower();
                    return lower.Contains("disabled") && lower.Contains("receive window auto-tuning level");
                },
            },

            // ── Disable IPv6 ──
            new Tweak
            {
                Name = "Disable IPv6",
                Description = "Disables IPv6 on all adapters to reduce network overhead and force IPv4.",
                Type = TweakType.Toggle,
                IsFree = false,
                HasWarning = false,
                Enable = () =>
                {
                    RegistryHelper.SetDword(RegistryHive.LocalMachine, Tcp6Key, "DisabledComponents", 0xFF);
                    SystemHelper.RunPowerShell("Disable-NetAdapterBinding -Name * -ComponentID ms_tcpip6 -ErrorAction SilentlyContinue");
                    return true;
                },
                Disable = () =>
                {
                    RegistryHelper.SetDword(RegistryHive.LocalMachine, Tcp6Key, "DisabledComponents", 0x00);
                    SystemHelper.RunPowerShell("Enable-NetAdapterBinding -Name * -ComponentID ms_tcpip6 -ErrorAction SilentlyContinue");
                    return true;
                },
                Check = () => RegistryHelper.GetDword(RegistryHive.LocalMachine, Tcp6Key, "DisabledComponents") == 0xFF,
            },

            // ── Disable IPv6 Transition Services ──
            new Tweak
            {
                Name = "Disable IPv6 Transition Services",
                Description = "Disables Teredo, 6to4, and ISATAP tunneling protocols to reduce network overhead.",
                Type = TweakType.Toggle,
                IsFree = false,
                HasWarning = false,
                Enable = () =>
                {
                    SystemHelper.RunCmd("netsh interface teredo set state disabled");
                    SystemHelper.RunCmd("netsh interface 6to4 set state disabled");
                    SystemHelper.RunCmd("netsh interface isatap set state disabled");
                    SystemHelper.RunPowerShell("Set-NetTeredoConfiguration -Type Disabled -ErrorAction SilentlyContinue");
                    return true;
                },
                Disable = () =>
                {
                    SystemHelper.RunCmd("netsh interface teredo set state default");
                    SystemHelper.RunCmd("netsh interface 6to4 set state default");
                    SystemHelper.RunCmd("netsh interface isatap set state default");
                    return true;
                },
                Check = () => { var (ok, output) = SystemHelper.RunCmd("netsh interface teredo show state", true); return ok && output.ToLower().Contains("disabled"); },
            },

            // ── Disable Security Profiles ──
            new Tweak
            {
                Name = "Disable Security Profiles",
                Description = "Disables Windows Firewall network profiles. Reduces security — use with caution.",
                Type = TweakType.Toggle,
                IsFree = false,
                HasWarning = true,
                Enable = () => SystemHelper.RunPowerShell(
                    "Set-NetFirewallProfile -Profile Domain,Public,Private -Enabled False -ErrorAction SilentlyContinue").success,
                Disable = () => SystemHelper.RunPowerShell(
                    "Set-NetFirewallProfile -Profile Domain,Public,Private -Enabled True -ErrorAction SilentlyContinue").success,
                Check = () => { var (ok, output) = SystemHelper.RunPowerShell("(Get-NetFirewallProfile -Profile Domain).Enabled", true); return ok && output.Trim() == "False"; },
            },

            // ── Enable Weak Host Send/Receive ──
            new Tweak
            {
                Name = "Enable Weak Host Send/Receive",
                Description = "Allows sending/receiving packets on any interface, improving routing flexibility and reducing latency.",
                Type = TweakType.Toggle,
                IsFree = false,
                HasWarning = false,
                Enable = () => SystemHelper.RunPowerShell(
                    @"$adapters = Get-NetAdapter | Where-Object {$_.Status -eq 'Up'}; " +
                    @"foreach ($a in $adapters) { " +
                    @"netsh interface ipv4 set interface $a.InterfaceIndex weakhostreceive=enabled store=persistent; " +
                    @"netsh interface ipv4 set interface $a.InterfaceIndex weakhostsend=enabled store=persistent }").success,
                Disable = () => SystemHelper.RunPowerShell(
                    @"$adapters = Get-NetAdapter | Where-Object {$_.Status -eq 'Up'}; " +
                    @"foreach ($a in $adapters) { " +
                    @"netsh interface ipv4 set interface $a.InterfaceIndex weakhostreceive=disabled store=persistent; " +
                    @"netsh interface ipv4 set interface $a.InterfaceIndex weakhostsend=disabled store=persistent }").success,
                Check = () => { var (ok, output) = SystemHelper.RunPowerShell("(Get-NetIPInterface -AddressFamily IPv4 | Select-Object -First 1).WeakHostReceive", true); return ok && output.Trim() == "Enabled"; },
            },

            // ── Enhance Connection Stability ──
            new Tweak
            {
                Name = "Enhance Connection Stability",
                Description = "Tunes TCP retransmission limits, TTL, and MTU discovery for more stable connections.",
                Type = TweakType.Toggle,
                IsFree = false,
                HasWarning = false,
                Enable = () =>
                {
                    RegistryHelper.SetDword(RegistryHive.LocalMachine, TcpKey, "TcpMaxDataRetransmissions", 5);
                    RegistryHelper.SetDword(RegistryHive.LocalMachine, TcpKey, "TcpMaxConnectRetransmissions", 2);
                    RegistryHelper.SetDword(RegistryHive.LocalMachine, TcpKey, "EnablePMTUDiscovery", 1);
                    RegistryHelper.SetDword(RegistryHive.LocalMachine, TcpKey, "DefaultTTL", 64);
                    return true;
                },
                Disable = () =>
                {
                    RegistryHelper.DeleteValue(RegistryHive.LocalMachine, TcpKey, "TcpMaxDataRetransmissions");
                    RegistryHelper.DeleteValue(RegistryHive.LocalMachine, TcpKey, "TcpMaxConnectRetransmissions");
                    RegistryHelper.DeleteValue(RegistryHive.LocalMachine, TcpKey, "EnablePMTUDiscovery");
                    RegistryHelper.DeleteValue(RegistryHive.LocalMachine, TcpKey, "DefaultTTL");
                    return true;
                },
                Check = () => RegistryHelper.GetDword(RegistryHive.LocalMachine, TcpKey, "EnablePMTUDiscovery") == 1,
            },

            // ── Improve Network Packet Acknowledgement ──
            new Tweak
            {
                Name = "Improve Network Packet Acknowledgement",
                Description = "Disables delayed ACKs and enables TCP no-delay for faster packet acknowledgement.",
                Type = TweakType.Toggle,
                IsFree = false,
                HasWarning = false,
                Enable = () =>
                {
                    RegistryHelper.SetDword(RegistryHive.LocalMachine, TcpKey, "TcpAckFrequency", 1);
                    RegistryHelper.SetDword(RegistryHive.LocalMachine, TcpKey, "TCPNoDelay", 1);
                    RegistryHelper.SetDword(RegistryHive.LocalMachine, TcpKey, "TcpDelAckTicks", 0);
                    SystemHelper.RunCmd("netsh int tcp set global dca=enabled");
                    return true;
                },
                Disable = () =>
                {
                    RegistryHelper.DeleteValue(RegistryHive.LocalMachine, TcpKey, "TcpAckFrequency");
                    RegistryHelper.DeleteValue(RegistryHive.LocalMachine, TcpKey, "TCPNoDelay");
                    RegistryHelper.DeleteValue(RegistryHive.LocalMachine, TcpKey, "TcpDelAckTicks");
                    return true;
                },
                Check = () => RegistryHelper.GetDword(RegistryHive.LocalMachine, TcpKey, "TCPNoDelay") == 1,
            },

            // ── Optimize IPv6 Address Handling ──
            new Tweak
            {
                Name = "Optimize IPv6 Address Handling",
                Description = "Configures IPv6 stack to prefer IPv4, reducing address lookup overhead without fully disabling IPv6.",
                Type = TweakType.Toggle,
                IsFree = false,
                HasWarning = false,
                Enable = () => RegistryHelper.SetDword(RegistryHive.LocalMachine, Tcp6Key, "DisabledComponents", 0x20),
                Disable = () => RegistryHelper.DeleteValue(RegistryHive.LocalMachine, Tcp6Key, "DisabledComponents"),
            },

            // ── Optimize MTU Size ──
            new Tweak
            {
                Name = "Optimize MTU Size",
                Description = "Sets MTU to 1500 bytes on all active adapters for optimal packet size and reduced fragmentation.",
                Type = TweakType.Apply,
                IsFree = false,
                HasWarning = false,
                ApplyAction = () => SystemHelper.RunPowerShell(
                    @"$adapters = Get-NetAdapter | Where-Object {$_.Status -eq 'Up'}; " +
                    @"foreach ($a in $adapters) { " +
                    @"netsh interface ipv4 set subinterface $a.InterfaceIndex mtu=1500 store=persistent }").success,
            },

            // ── Optimize Neighbor Cache ──
            new Tweak
            {
                Name = "Optimize Neighbor Cache",
                Description = "Sets ARP cache lifetime and makes neighbour entries persistent for faster address resolution.",
                Type = TweakType.Toggle,
                IsFree = false,
                HasWarning = false,
                Enable = () =>
                {
                    RegistryHelper.SetDword(RegistryHive.LocalMachine, TcpKey, "ArpCacheLife", 60);
                    RegistryHelper.SetDword(RegistryHive.LocalMachine, TcpKey, "ArpCacheMinReferencedLife", 10);
                    SystemHelper.RunCmd("netsh interface ipv4 set neighbors store=persistent");
                    return true;
                },
                Disable = () =>
                {
                    RegistryHelper.DeleteValue(RegistryHive.LocalMachine, TcpKey, "ArpCacheLife");
                    RegistryHelper.DeleteValue(RegistryHive.LocalMachine, TcpKey, "ArpCacheMinReferencedLife");
                    return true;
                },
                Check = () => RegistryHelper.GetDword(RegistryHive.LocalMachine, TcpKey, "ArpCacheLife") == 60,
            },

            // ── Optimize Offloading Features ──
            new Tweak
            {
                Name = "Optimize Offloading Features",
                Description = "Disables TCP checksum offloading and LSO to reduce driver latency and improve consistency.",
                Type = TweakType.Toggle,
                IsFree = false,
                HasWarning = false,
                Enable = () => SystemHelper.RunPowerShell(
                    @"$adapters = Get-NetAdapter | Where-Object {$_.Status -eq 'Up'}; " +
                    @"foreach ($a in $adapters) { " +
                    @"Disable-NetAdapterChecksumOffload -Name $a.Name -ErrorAction SilentlyContinue; " +
                    @"Set-NetAdapterLso -Name $a.Name -IPv4Enabled $false -IPv6Enabled $false -ErrorAction SilentlyContinue }").success,
                Disable = () => SystemHelper.RunPowerShell(
                    @"$adapters = Get-NetAdapter | Where-Object {$_.Status -eq 'Up'}; " +
                    @"foreach ($a in $adapters) { " +
                    @"Enable-NetAdapterChecksumOffload -Name $a.Name -ErrorAction SilentlyContinue; " +
                    @"Set-NetAdapterLso -Name $a.Name -IPv4Enabled $true -IPv6Enabled $true -ErrorAction SilentlyContinue }").success,
                Check = () => { var (ok, output) = SystemHelper.RunPowerShell("(Get-NetAdapterChecksumOffload | Select-Object -First 1).TcpIPv4", true); return ok && output.Trim() == "Disabled"; },
            },

            // ── Optimize Packet Coalescing ──
            new Tweak
            {
                Name = "Optimize Packet Coalescing",
                Description = "Disables Receive Segment Coalescing (RSC) to reduce packet batching latency.",
                Type = TweakType.Toggle,
                IsFree = false,
                HasWarning = false,
                Enable = () =>
                {
                    SystemHelper.RunCmd("netsh int tcp set global rsc=disabled");
                    SystemHelper.RunPowerShell(
                        @"$adapters = Get-NetAdapter | Where-Object {$_.Status -eq 'Up'}; " +
                        @"foreach ($a in $adapters) { " +
                        @"Set-NetAdapterPacketDirect -Name $a.Name -Enabled $true -ErrorAction SilentlyContinue }");
                    return true;
                },
                Disable = () => SystemHelper.RunCmd("netsh int tcp set global rsc=enabled").success,
                Check = () => { var (ok, output) = SystemHelper.RunCmd("netsh int tcp show global", true); return ok && output.ToLower().Contains("receive-side coalescing state") && output.ToLower().Contains("disabled"); },
            },

            // ── Optimize Winsock Buffer Network ──
            new Tweak
            {
                Name = "Optimize Winsock Buffer Network",
                Description = "Tunes TCP window sizes and Winsock buffer parameters for better throughput and lower latency.",
                Type = TweakType.Toggle,
                IsFree = false,
                HasWarning = false,
                Enable = () =>
                {
                    RegistryHelper.SetDword(RegistryHive.LocalMachine, TcpKey, "GlobalMaxTcpWindowSize", 65535);
                    RegistryHelper.SetDword(RegistryHive.LocalMachine, TcpKey, "TcpWindowSize", 65535);
                    RegistryHelper.SetDword(RegistryHive.LocalMachine, TcpKey, "Tcp1323Opts", 1);
                    SystemHelper.RunCmd("netsh int tcp set global autotuninglevel=highlyrestricted");
                    return true;
                },
                Disable = () =>
                {
                    RegistryHelper.DeleteValue(RegistryHive.LocalMachine, TcpKey, "GlobalMaxTcpWindowSize");
                    RegistryHelper.DeleteValue(RegistryHive.LocalMachine, TcpKey, "TcpWindowSize");
                    RegistryHelper.DeleteValue(RegistryHive.LocalMachine, TcpKey, "Tcp1323Opts");
                    return true;
                },
                Check = () => RegistryHelper.GetDword(RegistryHive.LocalMachine, TcpKey, "Tcp1323Opts") == 1,
            },

            // ── Reduce Network Background Interference ──
            new Tweak
            {
                Name = "Reduce Network Background Interference",
                Description = "Disables Windows network throttling so games and apps are never limited by background network usage.",
                Type = TweakType.Toggle,
                IsFree = false,
                HasWarning = false,
                Enable = () => RegistryHelper.SetDword(RegistryHive.LocalMachine, ProfileKey, "NetworkThrottlingIndex", unchecked((int)0xFFFFFFFF)),
                Disable = () => RegistryHelper.DeleteValue(RegistryHive.LocalMachine, ProfileKey, "NetworkThrottlingIndex"),
                Check = () => RegistryHelper.GetDword(RegistryHive.LocalMachine, ProfileKey, "NetworkThrottlingIndex") == unchecked((int)0xFFFFFFFF),
            },

            // ── Set Stable Network Routing ──
            new Tweak
            {
                Name = "Set Stable Network Routing",
                Description = "Disables dead gateway detection and media sense logging for more stable routing.",
                Type = TweakType.Toggle,
                IsFree = false,
                HasWarning = false,
                Enable = () =>
                {
                    RegistryHelper.SetDword(RegistryHive.LocalMachine, TcpKey, "DisableMediaSenseEventLog", 1);
                    RegistryHelper.SetDword(RegistryHive.LocalMachine, TcpKey, "EnableDeadGWDetect", 0);
                    return true;
                },
                Disable = () =>
                {
                    RegistryHelper.DeleteValue(RegistryHive.LocalMachine, TcpKey, "DisableMediaSenseEventLog");
                    RegistryHelper.DeleteValue(RegistryHive.LocalMachine, TcpKey, "EnableDeadGWDetect");
                    return true;
                },
                Check = () => RegistryHelper.GetDword(RegistryHive.LocalMachine, TcpKey, "DisableMediaSenseEventLog") == 1,
            },

            // ── Disable Heuristics ──
            new Tweak
            {
                Name = "Disable Heuristics",
                Description = "Disables TCP heuristics that can interfere with connection behaviour and cause inconsistent latency.",
                Type = TweakType.Toggle,
                IsFree = false,
                HasWarning = false,
                Enable = () =>
                {
                    SystemHelper.RunCmd("netsh int tcp set heuristics disabled");
                    SystemHelper.RunCmd("netsh int tcp set global heuristics=disabled");
                    return true;
                },
                Disable = () => SystemHelper.RunCmd("netsh int tcp set heuristics enabled").success,
                Check = () => { var (ok, output) = SystemHelper.RunCmd("netsh int tcp show heuristics", true); return ok && output.ToLower().Contains("disabled"); },
            },

            // ── Disable MPF ──
            new Tweak
            {
                Name = "Disable MPF",
                Description = "Disables multi-processor RSS for network processing to reduce CPU scheduling overhead.",
                Type = TweakType.Toggle,
                IsFree = false,
                HasWarning = false,
                Enable = () => SystemHelper.RunCmd("netsh int tcp set global rss=disabled").success,
                Disable = () => SystemHelper.RunCmd("netsh int tcp set global rss=enabled").success,
                Check = () => { var (ok, output) = SystemHelper.RunCmd("netsh int tcp show global", true); return ok && output.ToLower().Contains("receive-side scaling state") && output.ToLower().Contains("disabled"); },
            },

            // ── FREE: Flush DNS Cache ──
            new Tweak
            {
                Name = "Flush DNS Cache",
                Description = "Clears the local DNS resolver cache to resolve stale entries and improve name resolution speed.",
                Type = TweakType.Apply,
                IsFree = true,
                HasWarning = false,
                ApplyAction = () =>
                {
                    SystemHelper.RunCmd("ipconfig /flushdns");
                    SystemHelper.RunPowerShell("Clear-DnsClientCache -ErrorAction SilentlyContinue");
                    return true;
                },
            },

            // ── FREE: Disable Windows Update P2P Sharing ──
            new Tweak
            {
                Name = "Disable Windows Update P2P Sharing",
                Description = "Stops Windows from uploading updates to other PCs over the internet, freeing upload bandwidth.",
                Type = TweakType.Toggle,
                IsFree = true,
                HasWarning = false,
                Enable = () =>
                {
                    RegistryHelper.SetDword(RegistryHive.LocalMachine, WudoKey, "DODownloadMode", 0);
                    SystemHelper.RunPowerShell(
                        "Stop-Service -Name DoSvc -ErrorAction SilentlyContinue; " +
                        "Set-Service -Name DoSvc -StartupType Disabled -ErrorAction SilentlyContinue");
                    return true;
                },
                Disable = () =>
                {
                    RegistryHelper.DeleteValue(RegistryHive.LocalMachine, WudoKey, "DODownloadMode");
                    SystemHelper.RunPowerShell(
                        "Set-Service -Name DoSvc -StartupType Automatic -ErrorAction SilentlyContinue; " +
                        "Start-Service -Name DoSvc -ErrorAction SilentlyContinue");
                    return true;
                },
                Check = () => RegistryHelper.GetDword(RegistryHive.LocalMachine, WudoKey, "DODownloadMode") == 0,
            },

            // ── FREE: Disable LLMNR Protocol ──
            new Tweak
            {
                Name = "Disable LLMNR Protocol",
                Description = "Disables Link-Local Multicast Name Resolution to reduce unnecessary network broadcasts and latency.",
                Type = TweakType.Toggle,
                IsFree = true,
                HasWarning = false,
                Enable = () => RegistryHelper.SetDword(RegistryHive.LocalMachine, LlmnrKey, "EnableMulticast", 0),
                Disable = () => RegistryHelper.DeleteValue(RegistryHive.LocalMachine, LlmnrKey, "EnableMulticast"),
                Check = () => RegistryHelper.GetDword(RegistryHive.LocalMachine, LlmnrKey, "EnableMulticast") == 0,
            },

            // ── FREE: Reset Winsock & IP Stack ──
            new Tweak
            {
                Name = "Reset Winsock & IP Stack",
                Description = "Resets the Winsock catalog and IP stack to fix corrupted network settings. Requires reboot.",
                Type = TweakType.Apply,
                IsFree = true,
                HasWarning = true,
                ApplyAction = () =>
                {
                    SystemHelper.RunCmd("netsh winsock reset");
                    SystemHelper.RunCmd("netsh int ip reset");
                    return true;
                },
            },
        };
    }
}
