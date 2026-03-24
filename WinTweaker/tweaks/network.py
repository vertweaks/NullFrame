"""
Network tweaks.
"""
import winreg
from utils.registry import set_reg_value, get_reg_value, delete_reg_value
from utils.system import run_powershell, run_cmd

HKLM = winreg.HKEY_LOCAL_MACHINE

TCP_KEY     = r"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters"
TCP6_KEY    = r"SYSTEM\CurrentControlSet\Services\Tcpip6\Parameters"
PROFILE_KEY = r"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile"
DO_KEY      = r"SOFTWARE\Policies\Microsoft\Windows\DeliveryOptimization"

# ── Disable Automatic TCP Adjustment ─────────────────────────────────────────

def _disable_auto_tcp():
    run_cmd("netsh int tcp set global autotuninglevel=disabled")
    run_cmd("netsh int tcp set global chimney=disabled")
    return True

def _enable_auto_tcp():
    run_cmd("netsh int tcp set global autotuninglevel=normal")
    run_cmd("netsh int tcp set global chimney=enabled")
    return True

def _check_auto_tcp_disabled():
    ok, out = run_cmd("netsh int tcp show global", capture=True)
    return "disabled" in out.lower() and "receive window auto-tuning level" in out.lower()

# ── Disable IPv6 ──────────────────────────────────────────────────────────────

def _disable_ipv6():
    set_reg_value(HKLM, TCP6_KEY, "DisabledComponents", 0xFF)
    run_powershell("Disable-NetAdapterBinding -Name * -ComponentID ms_tcpip6 -ErrorAction SilentlyContinue")
    return True

def _enable_ipv6():
    set_reg_value(HKLM, TCP6_KEY, "DisabledComponents", 0x00)
    run_powershell("Enable-NetAdapterBinding -Name * -ComponentID ms_tcpip6 -ErrorAction SilentlyContinue")
    return True

def _check_ipv6_disabled():
    return get_reg_value(HKLM, TCP6_KEY, "DisabledComponents") == 0xFF

# ── Disable IPv6 Transition Services ─────────────────────────────────────────

def _disable_ipv6_transition():
    run_cmd("netsh interface teredo set state disabled")
    run_cmd("netsh interface 6to4 set state disabled")
    run_cmd("netsh interface isatap set state disabled")
    run_powershell("Set-NetTeredoConfiguration -Type Disabled -ErrorAction SilentlyContinue")
    return True

def _enable_ipv6_transition():
    run_cmd("netsh interface teredo set state default")
    run_cmd("netsh interface 6to4 set state default")
    run_cmd("netsh interface isatap set state default")
    return True

# ── Disable Security Profiles (Firewall) ──────────────────────────────────────

def _disable_firewall_profiles():
    run_powershell("Set-NetFirewallProfile -Profile Domain,Public,Private -Enabled False -ErrorAction SilentlyContinue")
    return True

def _enable_firewall_profiles():
    run_powershell("Set-NetFirewallProfile -Profile Domain,Public,Private -Enabled True -ErrorAction SilentlyContinue")
    return True

# ── Enable Weak Host Send/Receive ─────────────────────────────────────────────

def _enable_weak_host():
    run_powershell(r"""
        $adapters = Get-NetAdapter | Where-Object {$_.Status -eq "Up"}
        foreach ($a in $adapters) {
            netsh interface ipv4 set interface $a.InterfaceIndex weakhostreceive=enabled store=persistent
            netsh interface ipv4 set interface $a.InterfaceIndex weakhostsend=enabled store=persistent
        }
    """)
    return True

def _disable_weak_host():
    run_powershell(r"""
        $adapters = Get-NetAdapter | Where-Object {$_.Status -eq "Up"}
        foreach ($a in $adapters) {
            netsh interface ipv4 set interface $a.InterfaceIndex weakhostreceive=disabled store=persistent
            netsh interface ipv4 set interface $a.InterfaceIndex weakhostsend=disabled store=persistent
        }
    """)
    return True

# ── Enhance Connection Stability ──────────────────────────────────────────────

def _enhance_stability():
    set_reg_value(HKLM, TCP_KEY, "TcpMaxDataRetransmissions",    5)
    set_reg_value(HKLM, TCP_KEY, "TcpMaxConnectRetransmissions", 2)
    set_reg_value(HKLM, TCP_KEY, "EnablePMTUDiscovery",          1)
    set_reg_value(HKLM, TCP_KEY, "DefaultTTL",                   64)
    return True

def _reset_stability():
    for k in ("TcpMaxDataRetransmissions", "TcpMaxConnectRetransmissions",
              "EnablePMTUDiscovery", "DefaultTTL"):
        delete_reg_value(HKLM, TCP_KEY, k)
    return True

def _check_stability():
    return get_reg_value(HKLM, TCP_KEY, "EnablePMTUDiscovery") == 1

# ── Improve Network Packet Acknowledgement ────────────────────────────────────

def _optimize_ack():
    set_reg_value(HKLM, TCP_KEY, "TcpAckFrequency", 1)
    set_reg_value(HKLM, TCP_KEY, "TCPNoDelay",      1)
    set_reg_value(HKLM, TCP_KEY, "TcpDelAckTicks",  0)
    run_cmd("netsh int tcp set global dca=enabled")
    return True

def _reset_ack():
    delete_reg_value(HKLM, TCP_KEY, "TcpAckFrequency")
    delete_reg_value(HKLM, TCP_KEY, "TCPNoDelay")
    delete_reg_value(HKLM, TCP_KEY, "TcpDelAckTicks")
    return True

def _check_ack():
    return get_reg_value(HKLM, TCP_KEY, "TCPNoDelay") == 1

# ── Optimize IPv6 Address Handling ────────────────────────────────────────────

def _optimize_ipv6_addr():
    # Prefer IPv4 over IPv6 without fully disabling IPv6
    set_reg_value(HKLM, TCP6_KEY, "DisabledComponents", 0x20)
    return True

def _reset_ipv6_addr():
    delete_reg_value(HKLM, TCP6_KEY, "DisabledComponents")
    return True

# ── Optimize MTU Size ─────────────────────────────────────────────────────────

def _optimize_mtu():
    run_powershell(r"""
        $adapters = Get-NetAdapter | Where-Object {$_.Status -eq "Up"}
        foreach ($a in $adapters) {
            netsh interface ipv4 set subinterface $a.InterfaceIndex mtu=1500 store=persistent
        }
    """)
    return True

# ── Optimize Neighbor Cache ───────────────────────────────────────────────────

def _optimize_neighbor_cache():
    set_reg_value(HKLM, TCP_KEY, "ArpCacheLife",             60)
    set_reg_value(HKLM, TCP_KEY, "ArpCacheMinReferencedLife", 10)
    run_cmd("netsh interface ipv4 set neighbors store=persistent")
    return True

def _reset_neighbor_cache():
    delete_reg_value(HKLM, TCP_KEY, "ArpCacheLife")
    delete_reg_value(HKLM, TCP_KEY, "ArpCacheMinReferencedLife")
    return True

def _check_neighbor_cache():
    return get_reg_value(HKLM, TCP_KEY, "ArpCacheLife") == 60

# ── Optimize Offloading Features ──────────────────────────────────────────────

def _optimize_offloading():
    run_powershell(r"""
        $adapters = Get-NetAdapter | Where-Object {$_.Status -eq "Up"}
        foreach ($a in $adapters) {
            Disable-NetAdapterChecksumOffload -Name $a.Name -ErrorAction SilentlyContinue
            Set-NetAdapterLso -Name $a.Name -IPv4Enabled $false -IPv6Enabled $false -ErrorAction SilentlyContinue
        }
    """)
    return True

def _reset_offloading():
    run_powershell(r"""
        $adapters = Get-NetAdapter | Where-Object {$_.Status -eq "Up"}
        foreach ($a in $adapters) {
            Enable-NetAdapterChecksumOffload -Name $a.Name -ErrorAction SilentlyContinue
            Set-NetAdapterLso -Name $a.Name -IPv4Enabled $true -IPv6Enabled $true -ErrorAction SilentlyContinue
        }
    """)
    return True

# ── Optimize Packet Coalescing ────────────────────────────────────────────────

def _optimize_coalescing():
    run_cmd("netsh int tcp set global rsc=disabled")
    run_powershell(r"""
        $adapters = Get-NetAdapter | Where-Object {$_.Status -eq "Up"}
        foreach ($a in $adapters) {
            Set-NetAdapterPacketDirect -Name $a.Name -Enabled $true -ErrorAction SilentlyContinue
        }
    """)
    return True

def _reset_coalescing():
    run_cmd("netsh int tcp set global rsc=enabled")
    return True

# ── Optimize Winsock Buffer ───────────────────────────────────────────────────

def _optimize_winsock():
    set_reg_value(HKLM, TCP_KEY, "GlobalMaxTcpWindowSize", 65535)
    set_reg_value(HKLM, TCP_KEY, "TcpWindowSize",          65535)
    set_reg_value(HKLM, TCP_KEY, "Tcp1323Opts",            1)
    run_cmd("netsh int tcp set global autotuninglevel=highlyrestricted")
    return True

def _reset_winsock():
    delete_reg_value(HKLM, TCP_KEY, "GlobalMaxTcpWindowSize")
    delete_reg_value(HKLM, TCP_KEY, "TcpWindowSize")
    delete_reg_value(HKLM, TCP_KEY, "Tcp1323Opts")
    return True

def _check_winsock():
    return get_reg_value(HKLM, TCP_KEY, "Tcp1323Opts") == 1

# ── Reduce Network Background Interference ────────────────────────────────────

def _reduce_bg_net():
    set_reg_value(HKLM, PROFILE_KEY, "NetworkThrottlingIndex", 0xFFFFFFFF)
    return True

def _reset_bg_net():
    delete_reg_value(HKLM, PROFILE_KEY, "NetworkThrottlingIndex")
    return True

def _check_bg_net():
    return get_reg_value(HKLM, PROFILE_KEY, "NetworkThrottlingIndex") == 0xFFFFFFFF

# ── Set Stable Network Routing ────────────────────────────────────────────────

def _set_stable_routing():
    set_reg_value(HKLM, TCP_KEY, "DisableMediaSenseEventLog", 1)
    set_reg_value(HKLM, TCP_KEY, "EnableDeadGWDetect",        0)
    return True

def _reset_stable_routing():
    delete_reg_value(HKLM, TCP_KEY, "DisableMediaSenseEventLog")
    delete_reg_value(HKLM, TCP_KEY, "EnableDeadGWDetect")
    return True

def _check_stable_routing():
    return get_reg_value(HKLM, TCP_KEY, "DisableMediaSenseEventLog") == 1

# ── Disable Heuristics ────────────────────────────────────────────────────────

def _disable_heuristics():
    run_cmd("netsh int tcp set heuristics disabled")
    run_cmd("netsh int tcp set global heuristics=disabled")
    return True

def _enable_heuristics():
    run_cmd("netsh int tcp set heuristics enabled")
    return True

# ── Disable MPF (RSS) ─────────────────────────────────────────────────────────

def _disable_mpf():
    run_cmd("netsh int tcp set global rss=disabled")
    return True

def _enable_mpf():
    run_cmd("netsh int tcp set global rss=enabled")
    return True


# ── Tweak definitions ─────────────────────────────────────────────────────────

NETWORK_TWEAKS = [
    {
        "name": "Disable Automatic TCP Adjustment",
        "desc": "Disables Windows TCP auto-tuning to prevent automatic window size adjustments that can cause latency.",
        "type": "toggle",
        "warning": False,
        "enable": _disable_auto_tcp,
        "disable": _enable_auto_tcp,
        "check": _check_auto_tcp_disabled,
    },
    {
        "name": "Disable IPv6",
        "desc": "Disables IPv6 on all adapters to reduce network overhead and force IPv4.",
        "type": "toggle",
        "warning": False,
        "enable": _disable_ipv6,
        "disable": _enable_ipv6,
        "check": _check_ipv6_disabled,
    },
    {
        "name": "Disable IPv6 Transition Services",
        "desc": "Disables Teredo, 6to4, and ISATAP tunneling protocols to reduce network overhead.",
        "type": "toggle",
        "warning": False,
        "enable": _disable_ipv6_transition,
        "disable": _enable_ipv6_transition,
    },
    {
        "name": "Disable Security Profiles",
        "desc": "Disables Windows Firewall network profiles. Reduces security — use with caution.",
        "type": "toggle",
        "warning": True,
        "enable": _disable_firewall_profiles,
        "disable": _enable_firewall_profiles,
    },
    {
        "name": "Enable Weak Host Send/Receive",
        "desc": "Allows sending/receiving packets on any interface, improving routing flexibility and reducing latency.",
        "type": "toggle",
        "warning": False,
        "enable": _enable_weak_host,
        "disable": _disable_weak_host,
    },
    {
        "name": "Enhance Connection Stability",
        "desc": "Tunes TCP retransmission limits, TTL, and MTU discovery for more stable connections.",
        "type": "toggle",
        "warning": False,
        "enable": _enhance_stability,
        "disable": _reset_stability,
        "check": _check_stability,
    },
    {
        "name": "Improve Network Packet Acknowledgement",
        "desc": "Disables delayed ACKs and enables TCP no-delay for faster packet acknowledgement.",
        "type": "toggle",
        "warning": False,
        "enable": _optimize_ack,
        "disable": _reset_ack,
        "check": _check_ack,
    },
    {
        "name": "Optimize IPv6 Address Handling",
        "desc": "Configures IPv6 stack to prefer IPv4, reducing address lookup overhead without fully disabling IPv6.",
        "type": "toggle",
        "warning": False,
        "enable": _optimize_ipv6_addr,
        "disable": _reset_ipv6_addr,
    },
    {
        "name": "Optimize MTU Size",
        "desc": "Sets MTU to 1500 bytes on all active adapters for optimal packet size and reduced fragmentation.",
        "type": "apply",
        "warning": False,
        "apply": _optimize_mtu,
    },
    {
        "name": "Optimize Neighbor Cache",
        "desc": "Sets ARP cache lifetime and makes neighbour entries persistent for faster address resolution.",
        "type": "toggle",
        "warning": False,
        "enable": _optimize_neighbor_cache,
        "disable": _reset_neighbor_cache,
        "check": _check_neighbor_cache,
    },
    {
        "name": "Optimize Offloading Features",
        "desc": "Disables TCP checksum offloading and LSO to reduce driver latency and improve consistency.",
        "type": "toggle",
        "warning": False,
        "enable": _optimize_offloading,
        "disable": _reset_offloading,
    },
    {
        "name": "Optimize Packet Coalescing",
        "desc": "Disables Receive Segment Coalescing (RSC) to reduce packet batching latency.",
        "type": "toggle",
        "warning": False,
        "enable": _optimize_coalescing,
        "disable": _reset_coalescing,
    },
    {
        "name": "Optimize Winsock Buffer Network",
        "desc": "Tunes TCP window sizes and Winsock buffer parameters for better throughput and lower latency.",
        "type": "toggle",
        "warning": False,
        "enable": _optimize_winsock,
        "disable": _reset_winsock,
        "check": _check_winsock,
    },
    {
        "name": "Reduce Network Background Interference",
        "desc": "Disables Windows network throttling so games and apps are never limited by background network usage.",
        "type": "toggle",
        "warning": False,
        "enable": _reduce_bg_net,
        "disable": _reset_bg_net,
        "check": _check_bg_net,
    },
    {
        "name": "Set Stable Network Routing",
        "desc": "Disables dead gateway detection and media sense logging for more stable routing.",
        "type": "toggle",
        "warning": False,
        "enable": _set_stable_routing,
        "disable": _reset_stable_routing,
        "check": _check_stable_routing,
    },
    {
        "name": "Disable Heuristics",
        "desc": "Disables TCP heuristics that can interfere with connection behaviour and cause inconsistent latency.",
        "type": "toggle",
        "warning": False,
        "enable": _disable_heuristics,
        "disable": _enable_heuristics,
    },
    {
        "name": "Disable MPF",
        "desc": "Disables multi-processor RSS for network processing to reduce CPU scheduling overhead.",
        "type": "toggle",
        "warning": False,
        "enable": _disable_mpf,
        "disable": _enable_mpf,
    },
]

# ── FREE: Flush DNS Cache ─────────────────────────────────────────────────────

def _flush_dns():
    run_cmd("ipconfig /flushdns")
    run_powershell("Clear-DnsClientCache -ErrorAction SilentlyContinue")
    return True

# ── FREE: Disable Windows Update Delivery Optimization P2P ───────────────────

_WUDO_KEY = r"SOFTWARE\Policies\Microsoft\Windows\DeliveryOptimization"

def _disable_wudo():
    set_reg_value(HKLM, _WUDO_KEY, "DODownloadMode", 0)
    run_powershell("""
        Stop-Service  -Name DoSvc -ErrorAction SilentlyContinue
        Set-Service   -Name DoSvc -StartupType Disabled -ErrorAction SilentlyContinue
    """)
    return True

def _enable_wudo():
    delete_reg_value(HKLM, _WUDO_KEY, "DODownloadMode")
    run_powershell("""
        Set-Service  -Name DoSvc -StartupType Automatic -ErrorAction SilentlyContinue
        Start-Service -Name DoSvc -ErrorAction SilentlyContinue
    """)
    return True

def _check_wudo_disabled():
    return get_reg_value(HKLM, _WUDO_KEY, "DODownloadMode") == 0

# ── FREE: Disable LLMNR ───────────────────────────────────────────────────────

_LLMNR_KEY = r"SOFTWARE\Policies\Microsoft\Windows NT\DNSClient"

def _disable_llmnr():
    set_reg_value(HKLM, _LLMNR_KEY, "EnableMulticast", 0)
    return True

def _enable_llmnr():
    delete_reg_value(HKLM, _LLMNR_KEY, "EnableMulticast")
    return True

def _check_llmnr_disabled():
    return get_reg_value(HKLM, _LLMNR_KEY, "EnableMulticast") == 0

# ── FREE: Reset Winsock ───────────────────────────────────────────────────────

def _reset_winsock_catalog():
    run_cmd("netsh winsock reset")
    run_cmd("netsh int ip reset")
    return True


NETWORK_FREE_TWEAKS = [
    {
        "name": "Flush DNS Cache (FREE)",
        "desc": "Clears the local DNS resolver cache to resolve stale entries and improve name resolution speed.",
        "type": "apply",
        "warning": False,
        "apply": _flush_dns,
    },
    {
        "name": "Disable Windows Update P2P Sharing (FREE)",
        "desc": "Stops Windows from uploading updates to other PCs over the internet, freeing upload bandwidth.",
        "type": "toggle",
        "warning": False,
        "enable": _disable_wudo,
        "disable": _enable_wudo,
        "check": _check_wudo_disabled,
    },
    {
        "name": "Disable LLMNR Protocol (FREE)",
        "desc": "Disables Link-Local Multicast Name Resolution to reduce unnecessary network broadcasts and latency.",
        "type": "toggle",
        "warning": False,
        "enable": _disable_llmnr,
        "disable": _enable_llmnr,
        "check": _check_llmnr_disabled,
    },
    {
        "name": "Reset Winsock & IP Stack (FREE)",
        "desc": "Resets the Winsock catalog and IP stack to fix corrupted network settings. Requires reboot.",
        "type": "apply",
        "warning": True,
        "apply": _reset_winsock_catalog,
    },
]

NETWORK_TWEAKS = NETWORK_TWEAKS + NETWORK_FREE_TWEAKS
