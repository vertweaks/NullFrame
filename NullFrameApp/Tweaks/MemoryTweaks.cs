using System.Collections.Generic;
using Microsoft.Win32;
using NullFrame.Models;
using NullFrame.Services;

namespace NullFrame.Tweaks
{
    public static class MemoryTweaks
    {
        private const string MemKey = @"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management";
        private const string CrashCtlKey = @"SYSTEM\CurrentControlSet\Control\CrashControl";

        public static List<Tweak> GetTweaks() => new()
        {
            // ── FREE: Disable Paging Executive ──
            new Tweak
            {
                Name = "Disable Paging Executive",
                Description = "Keeps kernel and driver code in physical RAM instead of paging to disk, reducing latency spikes.",
                Type = TweakType.Toggle,
                IsFree = true,
                HasWarning = false,
                Enable = () => RegistryHelper.SetDword(RegistryHive.LocalMachine, MemKey, "DisablePagingExecutive", 1),
                Disable = () => RegistryHelper.SetDword(RegistryHive.LocalMachine, MemKey, "DisablePagingExecutive", 0),
                Check = () => RegistryHelper.GetDword(RegistryHive.LocalMachine, MemKey, "DisablePagingExecutive") == 1,
            },

            // ── FREE: Set Large System Cache ──
            new Tweak
            {
                Name = "Set Large System Cache",
                Description = "Allows Windows to use more RAM for the file system cache, improving I/O throughput for games and apps.",
                Type = TweakType.Toggle,
                IsFree = true,
                HasWarning = false,
                Enable = () => RegistryHelper.SetDword(RegistryHive.LocalMachine, MemKey, "LargeSystemCache", 1),
                Disable = () => RegistryHelper.SetDword(RegistryHive.LocalMachine, MemKey, "LargeSystemCache", 0),
                Check = () => RegistryHelper.GetDword(RegistryHive.LocalMachine, MemKey, "LargeSystemCache") == 1,
            },

            // ── FREE: Disable Memory Compression ──
            new Tweak
            {
                Name = "Disable Memory Compression",
                Description = "Disables Windows RAM compression to eliminate CPU overhead from compressing/decompressing memory pages.",
                Type = TweakType.Toggle,
                IsFree = true,
                HasWarning = false,
                Enable = () => SystemHelper.RunPowerShell("Disable-MMAgent -MemoryCompression -ErrorAction SilentlyContinue").success,
                Disable = () => SystemHelper.RunPowerShell("Enable-MMAgent -MemoryCompression -ErrorAction SilentlyContinue").success,
                Check = () =>
                {
                    var (ok, output) = SystemHelper.RunPowerShell(
                        "(Get-MMAgent -ErrorAction SilentlyContinue).MemoryCompression", true);
                    return output.ToLower().Contains("false");
                },
            },

            // ── FREE: Clear Standby Memory List ──
            new Tweak
            {
                Name = "Clear Standby Memory List",
                Description = "Immediately flushes the Windows standby RAM list to free physical memory for active processes.",
                Type = TweakType.Apply,
                IsFree = true,
                HasWarning = false,
                ApplyAction = () => SystemHelper.RunPowerShell(
                    @"$code = @""
using System;
using System.Runtime.InteropServices;
public class MemoryHelper {
    [DllImport(""ntdll.dll"")] public static extern uint NtSetSystemInformation(int InfoClass, IntPtr Info, int Length);
    public static void ClearStandbyList() {
        IntPtr buf = Marshal.AllocHGlobal(4);
        Marshal.WriteInt32(buf, 4);
        NtSetSystemInformation(24, buf, 4);
        Marshal.FreeHGlobal(buf);
    }
}
""@
Add-Type -TypeDefinition $code -ErrorAction SilentlyContinue; [MemoryHelper]::ClearStandbyList()").success,
            },

            // ── FREE: Optimize Virtual Memory (Page File) ──
            new Tweak
            {
                Name = "Optimize Virtual Memory (Page File)",
                Description = "Sets a fixed page file size (1\u20134 GB) to reduce fragmentation and paging latency spikes.",
                Type = TweakType.Toggle,
                IsFree = true,
                HasWarning = true,
                Enable = () => SystemHelper.RunPowerShell(
                    @"$cs = Get-WmiObject -Class Win32_ComputerSystem -ErrorAction SilentlyContinue; " +
                    @"if ($cs) { $cs.AutomaticManagedPagefile = $false; $cs.Put() | Out-Null }; " +
                    @"$pf = Get-WmiObject -Class Win32_PageFileSetting -ErrorAction SilentlyContinue; " +
                    @"if ($pf) { $pf.InitialSize = 1024; $pf.MaximumSize = 4096; $pf.Put() | Out-Null }").success,
                Disable = () => SystemHelper.RunPowerShell(
                    @"$cs = Get-WmiObject -Class Win32_ComputerSystem -ErrorAction SilentlyContinue; " +
                    @"if ($cs) { $cs.AutomaticManagedPagefile = $true; $cs.Put() | Out-Null }").success,
            },

            // ── FREE: Disable Kernel Crash Dump ──
            new Tweak
            {
                Name = "Disable Kernel Crash Dump",
                Description = "Disables writing crash dump files to disk, reducing I/O overhead during system stress.",
                Type = TweakType.Toggle,
                IsFree = true,
                HasWarning = false,
                Enable = () => RegistryHelper.SetDword(RegistryHive.LocalMachine, CrashCtlKey, "CrashDumpEnabled", 0),
                Disable = () => RegistryHelper.SetDword(RegistryHive.LocalMachine, CrashCtlKey, "CrashDumpEnabled", 7),
                Check = () => RegistryHelper.GetDword(RegistryHive.LocalMachine, CrashCtlKey, "CrashDumpEnabled") == 0,
            },
        };
    }
}
