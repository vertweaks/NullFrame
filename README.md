# NullFrame — Project Handoff Brief (Full)

## What This Is
A Windows performance optimization utility with two versions:
- **WinTweaker/** — Python/CustomTkinter GUI (complete, working)
- **NullFrameApp/** — C# WPF rewrite (in progress, ~40% done)

**GitHub repo:** `https://github.com/vertweaks/NullFrame`

---

## UI Design Vision

The app is modeled after the **nullframe.gg website** — a dark, aggressive, military-tech aesthetic. Think product cards from a high-end esports storefront, not a standard Windows settings panel.

### Color Palette
```
Background (page):     #080809   — near-black
Sidebar/Header panel:  #0C0C10   — slightly lighter panel
Card background:       #0F0F15   — tweak card fill
Surface/hover:         #141420   — active state, hover
Border (subtle):       #1A0507   — faint dark-red card border
Border (mid):          #480E18   — rules, dividers, active borders
Primary red:           #E8152B   — buttons, toggles ON, accents
Bright red:            #FF1F35   — hover, eyebrows, active nav
Deep red:              #AA0D1C   — pressed state
Warning orange:        #FF4D00   — ⚠ warning icon
White text:            #FFFFFF
Mid text:              #AAAAAA
Dim text:              #666677
Success green:         #4ADE80   — "APPLIED ✓" status
```

### Typography
- **Headings/Nav/Labels:** Segoe UI Bold — condensed, uppercase, tight tracking
- **Body/descriptions:** Segoe UI Regular
- All category names, tweak names, buttons → ALL CAPS
- Module labels use eyebrow format: `◆  NULLFRAME  //  MODULE 01`

---

## Layout — Full App Structure

```
┌──────────────────────────────────────────────────────────────────┐
│ [3px RED stripe across full top]                                 │
├────────────────┬─────────────────────────────────────────────────┤
│   SIDEBAR      │  HEADER BAR (Panel bg, 72px tall)               │
│   248px wide   │  [2px red line top]                             │
│   Panel bg     │  ◆  NULLFRAME  //  MODULE 01        13 TWEAKS   │
│                │  CPU & SYSTEM                       AVAILABLE   │
│  [NF badge]    ├─────────────────────────────────────────────────┤
│  NULL          │                                                  │
│  FRAME         │  [Scrollable tweak card list]                   │
│                │                                                  │
│  ─────────     │  ┌──────────────────────────────────────────┐   │
│  MODULES       │  │ [red bar] ⚠ TWEAK NAME    FREE  APPLIED✓ ══│  │
│                │  │           Description text here...           │  │
│  01 CPU        │  └──────────────────────────────────────────┘   │
│  02 GPU        │                                                  │
│  03 NETWORK    │  ┌──────────────────────────────────────────┐   │
│  04 USB        │  │ [red bar]  TWEAK NAME           [APPLY ▶]│   │
│  05 DEVICES    │  │            Description...                │   │
│  06 STORAGE    │  └──────────────────────────────────────────┘   │
│  07 MEMORY     │                                                  │
│  08 PRIVACY    │  ┌──────────────────────────────────────────┐   │
│                │  │ [red bar]  PRESET TWEAK    [CONFIGURE]   │   │
│  ─────────     │  │            Description...                │   │
│  NULLFRAME     │  └──────────────────────────────────────────┘   │
│  V1.0          │                                                  │
└────────────────┴─────────────────────────────────────────────────┘
```

---

## Sidebar Design Detail

```
[3px RED horizontal stripe — full width]

[NF badge — red square with rounded corners, "NF" in white bold]
NULL        ← white, 22px bold
FRAME       ← bright red (#FF1F35), 22px bold

[1px mid-border rule with padding]

MODULES     ← dim text, 9px bold, uppercase label

  01  CPU & SYSTEM        ← inactive: dim text, transparent bg
  02  GPU & GAMING
  03  NETWORK
  ...

Active button style:
  [3px RED bar on left edge]
  [Surface bg #141420]
  [Bright red text]

[spacer pushes footer to bottom]

[1px rule]
NULLFRAME  //  V1.0     ← 9px dim text
RUN AS ADMINISTRATOR
```

---

## Tweak Card Design Detail

Each card has a **3px red bar on the left edge** (the outer frame is red, inner content is Card color — creating a left border effect).

```
┌─────────────────────────────────────────────────────────────────┐
│[3px RED]│ [Card bg #0F0F15, 1px border #1A0507]                 │
│         │  ⚠   TWEAK NAME (ALL CAPS, 13px bold)   FREE  ✓APPLIED│
│         │      Description text in dim grey, wrapping if needed │
│         │                                          [toggle / btn]│
└─────────────────────────────────────────────────────────────────┘
```

**Card columns (left to right):**
1. `⚠` warning icon (orange, only shown if `HasWarning = true`)
2. Name (white, 13px bold uppercase) + Description (dim, 11px, wraps)
3. `FREE` badge (small, dark-red tinted background, bright red text) — only if `IsFree = true`
4. Status label (`APPLIED ✓` in green / `FAILED ✗` in bright red) — appears after action, clears after 3 seconds
5. Control — one of three:
   - **Toggle switch** — pill shape, grey when OFF, red when ON, white knob slides right
   - **`APPLY  ▶`** — solid red button, brightens on hover
   - **`CONFIGURE`** — transparent button with mid-border outline

---

## Preset Dialog Design

Opens as a modal (ToolWindow, `ShowDialog`, centered on owner).

```
┌────────────────────────────────┐
│[2px RED LINE]                  │
│[Panel bg]                      │
│  CONFIGURE PRESET   ← eyebrow  │
│  TWEAK NAME LARGE              │
│  Description text...           │
├────────────────────────────────┤
│  PRE-SET VALUES                │
│                                │
│  ┌────────────────────────┐    │
│  │ ○  OPTION NAME         │    │
│  │    Description         │    │
│  └────────────────────────┘    │
│  ┌────────────────────────┐    │
│  │ ●  RECOMMENDED OPTION ◆│    │ ← ◆ and bright red if recommended
│  │    Description         │    │
│  └────────────────────────┘    │
│                                │
│  [     APPLY  ▶     ] ← full  │
│          width red btn         │
└────────────────────────────────┘
```

---

## What's Done

### Python Version (`WinTweaker/`) — COMPLETE
Full working GUI matching the design above, plus:
- `tweaks/cpu_system.py` — 13 tweaks
- `tweaks/gpu_gaming.py` — 11 tweaks
- `tweaks/network.py` — 18 tweaks
- `tweaks/usb_input.py` — 13 tweaks
- `tweaks/devices.py` — 7 tweaks
- `tweaks/storage.py` — 8 tweaks (all FREE)
- `tweaks/memory.py` — 6 tweaks (all FREE)
- `tweaks/privacy.py` — 8 tweaks (all FREE)
- `tweaks/backup.py` — System Restore page
- `utils/registry.py` — Registry helpers
- `utils/system.py` — Admin check, PowerShell/CMD/bcdedit runners

### C# WPF Version (`NullFrameApp/`) — SKELETON DONE
- `NullFrame.csproj` — .NET 8 WPF, targets `net8.0-windows`
- `app.manifest` — `requireAdministrator`
- `App.xaml` + `App.xaml.cs` — entry point
- `Styles/Theme.xaml` — All colors, brushes, ScrollBar, NavButton, NavButtonActive (with red left bar), ToggleSwitch, ApplyButton, ConfigButton, ToolTip
- `MainWindow.xaml` — Full sidebar + header + scrollable tweak list with DataTemplate
- `MainWindow.xaml.cs` — Converters, nav logic, async toggle/apply/preset handlers, PresetDialog
- `Models/Tweak.cs` — `Tweak` (INotifyPropertyChanged), `TweakType` enum, `TweakPreset`
- `Services/RegistryHelper.cs` — SetValue, GetValue, DeleteValue, SetDword, GetDword, SetString
- `Services/SystemHelper.cs` — IsAdmin, RunPowerShell, RunCmd, RunBcdedit, RunNetsh, RunSchtasks

---

## What's Left — The Only Remaining Work

**Create 8 files in `NullFrameApp/Tweaks/`** by porting the Python tweak logic to C#.

| File | Source | Tweaks |
|------|--------|--------|
| `CpuSystemTweaks.cs` | `WinTweaker/tweaks/cpu_system.py` | 13 |
| `GpuGamingTweaks.cs` | `WinTweaker/tweaks/gpu_gaming.py` | 11 |
| `NetworkTweaks.cs` | `WinTweaker/tweaks/network.py` | 18 |
| `UsbInputTweaks.cs` | `WinTweaker/tweaks/usb_input.py` | 13 |
| `DeviceTweaks.cs` | `WinTweaker/tweaks/devices.py` | 7 |
| `StorageTweaks.cs` | `WinTweaker/tweaks/storage.py` | 8 |
| `MemoryTweaks.cs` | `WinTweaker/tweaks/memory.py` | 6 |
| `PrivacyTweaks.cs` | `WinTweaker/tweaks/privacy.py` | 8 |

---

## Code Templates

### File structure
```csharp
using System.Collections.Generic;
using Microsoft.Win32;
using NullFrame.Models;
using NullFrame.Services;

namespace NullFrame.Tweaks
{
    public static class CpuSystemTweaks
    {
        public static List<Tweak> GetTweaks() => new()
        {
            // tweaks here
        };
    }
}
```

### Toggle tweak (registry)
```csharp
new Tweak
{
    Name        = "Disable Core Parking",
    Description = "Forces all CPU cores active, reducing wake-up latency.",
    Type        = TweakType.Toggle,
    IsFree      = false,
    HasWarning  = false,
    Enable  = () => RegistryHelper.SetDword(RegistryHive.LocalMachine,
                    @"SYSTEM\CurrentControlSet\Control\Power", "CsEnabled", 0),
    Disable = () => RegistryHelper.SetDword(RegistryHive.LocalMachine,
                    @"SYSTEM\CurrentControlSet\Control\Power", "CsEnabled", 1),
    Check   = () => RegistryHelper.GetDword(RegistryHive.LocalMachine,
                    @"SYSTEM\CurrentControlSet\Control\Power", "CsEnabled", 1) == 0,
},
```

### Toggle tweak (PowerShell/CMD)
```csharp
new Tweak
{
    Name        = "Disable Hibernate",
    Description = "Frees disk space used by hiberfil.sys.",
    Type        = TweakType.Toggle,
    IsFree      = true,
    Enable  = () => SystemHelper.RunPowerShell("powercfg -h off").success,
    Disable = () => SystemHelper.RunPowerShell("powercfg -h on").success,
},
```

### Apply tweak (one-shot)
```csharp
new Tweak
{
    Name         = "Flush DNS Cache",
    Description  = "Clears the DNS resolver cache immediately.",
    Type         = TweakType.Apply,
    IsFree       = true,
    ApplyAction  = () => SystemHelper.RunCmd("ipconfig /flushdns").success,
},
```

### Preset tweak
```csharp
new Tweak
{
    Name        = "Win32 Priority Separation",
    Description = "Controls CPU time split between foreground and background.",
    Type        = TweakType.Preset,
    IsFree      = false,
    Presets = new[]
    {
        new TweakPreset
        {
            Name        = "Gaming (26)",
            Description = "Foreground boosted, short fixed quanta. Best for games.",
            Recommended = true,
            Apply = () => RegistryHelper.SetDword(RegistryHive.LocalMachine,
                          @"SYSTEM\CurrentControlSet\Control\PriorityControl",
                          "Win32PrioritySeparation", 26),
            Check = () => RegistryHelper.GetDword(RegistryHive.LocalMachine,
                          @"SYSTEM\CurrentControlSet\Control\PriorityControl",
                          "Win32PrioritySeparation") == 26,
        },
        new TweakPreset
        {
            Name  = "Balanced (2)",
            Description = "Windows default.",
            Apply = () => RegistryHelper.SetDword(RegistryHive.LocalMachine,
                          @"SYSTEM\CurrentControlSet\Control\PriorityControl",
                          "Win32PrioritySeparation", 2),
        },
    },
},
```

### Registry hive reference
```csharp
RegistryHive.LocalMachine   // HKLM
RegistryHive.CurrentUser    // HKCU
```

---

## Build & Run
Requires **.NET 8 SDK** on Windows:
```
dotnet build NullFrameApp/NullFrame.csproj
dotnet run --project NullFrameApp/NullFrame.csproj
```
Or open `NullFrameApp/NullFrame.csproj` in **Visual Studio 2022**.

---

## Prompt To Paste Into New Chat

> I'm building **NullFrame** — a C# WPF Windows performance optimizer styled after the nullframe.gg website. Clone the repo: `https://github.com/vertweaks/NullFrame`
>
> **Design:** Dark military-esports aesthetic. Background `#080809`, sidebar `#0C0C10`, cards `#0F0F15`, primary red `#E8152B`, bright red `#FF1F35`. Sidebar on the left (248px) with logo, numbered module buttons (active = red left bar + surface bg), and a dim footer. Header bar at top of content with red accent line, eyebrow label (`◆ NULLFRAME // MODULE 01`), category title, and tweak count. Tweak cards have a 3px red left bar, dark card body with tweak name (white, bold, uppercase), description (dim grey), optional warning icon (⚠ orange), optional FREE badge, status label (APPLIED ✓ green / FAILED ✗ red, clears after 3s), and either a red/grey toggle switch, a solid red APPLY ▶ button, or a transparent CONFIGURE button.
>
> **Status:** Infrastructure is complete — `MainWindow.xaml`, `MainWindow.xaml.cs`, `Styles/Theme.xaml`, `Models/Tweak.cs`, `Services/RegistryHelper.cs`, `Services/SystemHelper.cs` are all done.
>
> **Task:** Create the 8 tweak definition files in `NullFrameApp/Tweaks/` by reading the Python source files in `WinTweaker/tweaks/` and porting each tweak using `RegistryHelper.SetDword/GetDword`, `SystemHelper.RunPowerShell`, `SystemHelper.RunCmd`, and `SystemHelper.RunBcdedit`. Port all tweaks faithfully — keep `IsFree = true` for any tweak that was marked FREE in the Python source. Create all 8 files: `CpuSystemTweaks.cs`, `GpuGamingTweaks.cs`, `NetworkTweaks.cs`, `UsbInputTweaks.cs`, `DeviceTweaks.cs`, `StorageTweaks.cs`, `MemoryTweaks.cs`, `PrivacyTweaks.cs`.
