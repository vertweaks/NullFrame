"""
NULLFRAME — Windows Performance Optimizer
nullframe.gg · system-level performance suite
"""
import sys
import threading
import tkinter as tk
import tkinter.font as tkfont
from tkinter import messagebox

import customtkinter as ctk

# ── NULLFRAME colour palette ──────────────────────────────────────────────────
NF_RED     = "#E8152B"   # primary red  (matches nullframe.gg)
NF_BRIGHT  = "#FF1F35"   # hover / accent
NF_DEEP    = "#AA0D1C"   # pressed / deep shadow
NF_ORANGE  = "#FF4D00"   # warning

BG         = "#080809"   # near-black page background
PANEL      = "#0C0C10"   # sidebar / header panel
CARD       = "#0F0F15"   # tweak card background
SURFACE    = "#141420"   # active/hover surface

BORDER     = "#1A0507"   # subtle card border
BORDER_MID = "#480E18"   # mid-emphasis border / rule

TEXT       = "#FFFFFF"
TEXT_MID   = "#AAAAAA"
TEXT_DIM   = "#666677"

SUCCESS    = "#4ADE80"
WARNING_C  = NF_ORANGE

# Keep legacy names so tweak cards / dialogs still compile
AMD_RED    = NF_RED
AMD_BRIGHT = NF_BRIGHT
AMD_DEEP   = NF_DEEP
AMD_ORANGE = NF_ORANGE

ctk.set_appearance_mode("dark")
ctk.set_default_color_theme("dark-blue")

CATEGORIES = [
    ("CPU & SYSTEM",     "cpu",     "01"),
    ("GPU & GAMING",     "gpu",     "02"),
    ("NETWORK",          "network", "03"),
    ("USB & INPUT",      "usb",     "04"),
    ("DEVICES",          "devices", "05"),
    ("STORAGE & SSD",    "storage", "06"),
    ("MEMORY & RAM",     "memory",  "07"),
    ("PRIVACY & DEBLOAT","privacy", "08"),
    ("BACKUP & RESTORE", "backup",  "09"),
]

# ── Font resolution ───────────────────────────────────────────────────────────
_FONTS_CACHED = None  # Optional[set]

def _avail():
    global _FONTS_CACHED
    if _FONTS_CACHED is None:
        try:
            _FONTS_CACHED = set(tkfont.families())
        except Exception:
            _FONTS_CACHED = set()
    return _FONTS_CACHED

def F(size: int, weight: str = "normal", condensed: bool = False) -> ctk.CTkFont:
    if condensed:
        candidates = ("Barlow Condensed", "Impact", "Arial Narrow", "Arial Black", "Arial")
    else:
        candidates = ("Barlow", "Rajdhani", "Segoe UI", "Arial")
    avail = _avail()
    for c in candidates:
        if c in avail:
            return ctk.CTkFont(family=c, size=size, weight=weight)
    return ctk.CTkFont(size=size, weight=weight)


# ── Shared primitives ─────────────────────────────────────────────────────────

class LogoMark(tk.Canvas):
    def __init__(self, parent, size: int = 34, label: str = "VT", **kw):
        super().__init__(parent, width=size, height=size,
                         bg=PANEL, highlightthickness=0, **kw)
        p, s = 3, size - 6
        cut = s * 0.28
        pts = [p+cut,p, p+s-cut,p, p+s,p+cut, p+s,p+s-cut,
               p+s-cut,p+s, p+cut,p+s, p,p+s-cut, p,p+cut]
        self.create_polygon(pts, fill=AMD_RED, outline="")
        self.create_text(size//2, size//2, text=label,
                         fill=TEXT, font=("Impact", int(size*0.38), "bold"))


def RedRule(parent) -> ctk.CTkFrame:
    return ctk.CTkFrame(parent, height=1, fg_color=BORDER_MID, corner_radius=0)

def AccentLine(parent) -> ctk.CTkFrame:
    return ctk.CTkFrame(parent, height=2, fg_color=AMD_RED, corner_radius=0)


class Eyebrow(ctk.CTkFrame):
    def __init__(self, parent, label: str, **kw):
        super().__init__(parent, fg_color="transparent", **kw)
        ctk.CTkFrame(self, width=24, height=2,
                     fg_color=AMD_RED, corner_radius=0).pack(side="left", padx=(0, 10))
        ctk.CTkLabel(self, text=label.upper(),
                     font=F(10, "bold"), text_color=AMD_BRIGHT).pack(side="left")


# ── Preset dialog ─────────────────────────────────────────────────────────────

class PresetDialog(ctk.CTkToplevel):
    def __init__(self, parent, tweak: dict):
        super().__init__(parent)
        self.tweak = tweak
        self.title(tweak["name"].upper())
        self.geometry("500x440")
        self.configure(fg_color="#08080A")
        self.resizable(False, False)
        self.grab_set()
        self._sel = tk.IntVar(value=0)
        self._build()

    def _build(self):
        AccentLine(self).pack(fill="x")

        hdr = ctk.CTkFrame(self, fg_color=PANEL, corner_radius=0)
        hdr.pack(fill="x")
        Eyebrow(hdr, "Configure Preset").pack(anchor="w", padx=24, pady=(18, 4))
        ctk.CTkLabel(hdr, text=self.tweak["name"].upper(),
                     font=F(18, "bold", condensed=True), text_color=TEXT
                     ).pack(anchor="w", padx=24, pady=(0, 6))
        ctk.CTkLabel(hdr, text=self.tweak["desc"],
                     font=F(11), text_color=TEXT_DIM, wraplength=450, justify="left"
                     ).pack(anchor="w", padx=24, pady=(0, 18))

        RedRule(self).pack(fill="x")
        ctk.CTkLabel(self, text="PRE-SET VALUES",
                     font=F(10, "bold"), text_color=TEXT_DIM
                     ).pack(anchor="w", padx=24, pady=(14, 8))

        active = 0
        for i, p in enumerate(self.tweak["presets"]):
            if "check" in p:
                try:
                    if p["check"]():
                        active = i
                        break
                except Exception:
                    pass
        if active == 0:
            for i, p in enumerate(self.tweak["presets"]):
                if p.get("recommended"):
                    active = i
                    break
        self._sel.set(active)

        scr = ctk.CTkScrollableFrame(self, fg_color="transparent",
                                     scrollbar_button_color=SURFACE,
                                     scrollbar_button_hover_color=BORDER_MID,
                                     height=170)
        scr.pack(fill="x", padx=20, pady=(0, 10))
        scr.grid_columnconfigure(0, weight=1)

        for i, preset in enumerate(self.tweak["presets"]):
            row = ctk.CTkFrame(scr, fg_color=CARD, corner_radius=0,
                               border_width=1, border_color=BORDER)
            row.grid(row=i, column=0, sticky="ew", pady=4)
            row.grid_columnconfigure(1, weight=1)

            ctk.CTkRadioButton(row, text="", variable=self._sel, value=i,
                               fg_color=AMD_RED, hover_color=AMD_BRIGHT,
                               radiobutton_width=16, radiobutton_height=16
                               ).grid(row=0, column=0, rowspan=2, padx=(14,8), pady=14)

            rec = preset.get("recommended", False)
            ctk.CTkLabel(row,
                         text=preset["name"].upper() + ("  ◆ RECOMMENDED" if rec else ""),
                         font=F(12, "bold", condensed=True),
                         text_color=AMD_BRIGHT if rec else TEXT, anchor="w"
                         ).grid(row=0, column=1, sticky="w", pady=(10, 2))
            ctk.CTkLabel(row, text=preset.get("desc", ""),
                         font=F(10), text_color=TEXT_DIM, anchor="w", wraplength=350
                         ).grid(row=1, column=1, sticky="w", pady=(0, 10))

        ctk.CTkButton(self, text="APPLY  ▶",
                      fg_color=AMD_RED, hover_color=AMD_BRIGHT,
                      font=F(13, "bold", condensed=True), text_color=TEXT,
                      height=44, corner_radius=0, command=self._apply
                      ).pack(fill="x", padx=20, pady=(0, 20))

    def _apply(self):
        fn = self.tweak["presets"][self._sel.get()].get("apply")
        if callable(fn):
            threading.Thread(target=fn, daemon=True).start()
        self.destroy()


# ── Tweak card ────────────────────────────────────────────────────────────────

class TweakCard(ctk.CTkFrame):
    def __init__(self, parent, tweak: dict, **kw):
        super().__init__(parent, fg_color=AMD_RED, corner_radius=0, **kw)
        self.tweak = tweak
        self._var  = tk.BooleanVar(value=False)
        self._status_job = None

        self._inner = ctk.CTkFrame(self, fg_color=CARD, corner_radius=0,
                                   border_width=1, border_color=BORDER)
        self._inner.pack(fill="both", expand=True, padx=0, pady=(2, 0))
        self._inner.grid_columnconfigure(1, weight=1)

        self._build()
        self._load_state()

    def _build(self):
        if self.tweak.get("warning"):
            ctk.CTkLabel(self._inner, text="⚠", font=F(14),
                         text_color=WARNING_C, width=30
                         ).grid(row=0, column=0, rowspan=2, padx=(14,4), pady=16, sticky="n")
            lpad = 4
        else:
            lpad = 16

        ctk.CTkLabel(self._inner, text=self.tweak["name"].upper(),
                     font=F(13, "bold", condensed=True), text_color=TEXT, anchor="w"
                     ).grid(row=0, column=1, sticky="w", padx=(lpad, 8), pady=(14, 2))

        ctk.CTkLabel(self._inner, text=self.tweak["desc"],
                     font=F(11), text_color=TEXT_DIM, anchor="w",
                     justify="left", wraplength=560
                     ).grid(row=1, column=1, sticky="w", padx=(lpad, 8), pady=(0, 14))

        self._status = ctk.CTkLabel(self._inner, text="", width=90,
                                    font=F(10, "bold"), text_color=SUCCESS)
        self._status.grid(row=0, column=2, rowspan=2, padx=6)

        kind = self.tweak.get("type", "toggle")
        if kind == "toggle":
            ctk.CTkSwitch(self._inner, text="", variable=self._var,
                          fg_color=SURFACE, progress_color=AMD_RED,
                          button_color=TEXT, button_hover_color=TEXT_MID,
                          width=52, height=26, command=self._on_toggle
                          ).grid(row=0, column=3, rowspan=2, padx=20, pady=18)
        elif kind == "apply":
            ctk.CTkButton(self._inner, text="APPLY  ▶",
                          fg_color=AMD_RED, hover_color=AMD_BRIGHT, text_color=TEXT,
                          font=F(11, "bold", condensed=True),
                          width=100, height=34, corner_radius=0, command=self._on_apply
                          ).grid(row=0, column=3, rowspan=2, padx=20, pady=18)
        elif kind == "preset":
            ctk.CTkButton(self._inner, text="CONFIGURE",
                          fg_color="transparent", hover_color=SURFACE, text_color=TEXT_MID,
                          font=F(11, "bold", condensed=True),
                          width=100, height=34, corner_radius=0,
                          border_width=1, border_color=BORDER_MID,
                          command=self._open_preset
                          ).grid(row=0, column=3, rowspan=2, padx=20, pady=18)

    def _load_state(self):
        fn = self.tweak.get("check")
        if fn and self.tweak.get("type") == "toggle":
            threading.Thread(target=lambda: self._check_bg(fn), daemon=True).start()

    def _check_bg(self, fn):
        try:
            state = bool(fn())
            self.after(0, lambda: self._var.set(state))
        except Exception:
            pass

    def _on_toggle(self):
        fn = self.tweak.get("enable") if self._var.get() else self.tweak.get("disable")
        if fn:
            threading.Thread(target=self._run, args=(fn,), daemon=True).start()

    def _on_apply(self):
        fn = self.tweak.get("apply")
        if fn:
            threading.Thread(target=self._run, args=(fn,), daemon=True).start()

    def _open_preset(self):
        PresetDialog(self, self.tweak)

    def _run(self, fn):
        try:
            ok = bool(fn())
        except Exception:
            ok = False
        self.after(0, lambda: self._flash(ok))

    def _flash(self, ok: bool):
        self._status.configure(text="APPLIED ✓" if ok else "FAILED ✗",
                               text_color=SUCCESS if ok else AMD_BRIGHT)
        if self._status_job:
            self.after_cancel(self._status_job)
        self._status_job = self.after(3200, lambda: self._status.configure(text=""))


# ── Restore point card ────────────────────────────────────────────────────────

class RestorePointCard(ctk.CTkFrame):
    def __init__(self, parent, point: dict, on_restore_done, **kw):
        super().__init__(parent, fg_color=AMD_RED, corner_radius=0, **kw)
        self._point = point
        self._on_restore_done = on_restore_done

        inner = ctk.CTkFrame(self, fg_color=CARD, corner_radius=0,
                             border_width=1, border_color=BORDER)
        inner.pack(fill="both", expand=True, padx=0, pady=(2, 0))
        inner.grid_columnconfigure(1, weight=1)

        # Sequence badge
        badge = ctk.CTkFrame(inner, fg_color=SURFACE, corner_radius=0, width=52)
        badge.grid(row=0, column=0, rowspan=2, sticky="ns", padx=0, pady=0)
        badge.grid_propagate(False)
        ctk.CTkLabel(badge, text=f"#{point.get('SequenceNumber', '?')}",
                     font=F(13, "bold", condensed=True), text_color=AMD_BRIGHT
                     ).place(relx=0.5, rely=0.5, anchor="center")

        # Name
        ctk.CTkLabel(inner, text=point.get("Description", "Unknown").upper(),
                     font=F(13, "bold", condensed=True), text_color=TEXT, anchor="w"
                     ).grid(row=0, column=1, sticky="w", padx=16, pady=(12, 2))

        # Date + type
        meta = f"{point.get('CreationTime', '')}   //   {point.get('RestorePointType', '')}"
        ctk.CTkLabel(inner, text=meta, font=F(10), text_color=TEXT_DIM, anchor="w"
                     ).grid(row=1, column=1, sticky="w", padx=16, pady=(0, 12))

        # Restore button
        ctk.CTkButton(inner, text="RESTORE  ▶",
                      fg_color="transparent", hover_color=SURFACE,
                      text_color=AMD_BRIGHT, font=F(11, "bold", condensed=True),
                      width=110, height=34, corner_radius=0,
                      border_width=1, border_color=BORDER_MID,
                      command=self._confirm_restore
                      ).grid(row=0, column=2, rowspan=2, padx=16, pady=14)

    def _confirm_restore(self):
        seq  = self._point.get("SequenceNumber")
        name = self._point.get("Description", "this point")
        ans  = messagebox.askyesno(
            "Confirm System Restore",
            f'Restore to "{name}"?\n\n'
            f'Sequence #{seq} — {self._point.get("CreationTime", "")}\n\n'
            "⚠  Your computer will restart automatically.",
            icon="warning",
        )
        if ans:
            threading.Thread(target=self._do_restore, args=(seq,), daemon=True).start()

    def _do_restore(self, seq):
        from tweaks.backup import restore_to_point
        ok = restore_to_point(seq)
        if not ok:
            self.after(0, lambda: messagebox.showerror(
                "Restore Failed",
                "System Restore could not be initiated.\n"
                "Try opening the System Restore wizard instead.",
            ))


# ── Backup & Restore page ─────────────────────────────────────────────────────

class BackupPage(ctk.CTkScrollableFrame):
    def __init__(self, parent, **kw):
        super().__init__(parent, fg_color=BG,
                         scrollbar_button_color=SURFACE,
                         scrollbar_button_hover_color=BORDER_MID, **kw)
        self.grid_columnconfigure(0, weight=1)
        self._cards_start_row = 0
        self._build_static()
        self._load_points()

    # ── static widgets ────────────────────────────────────────────────────────

    def _build_static(self):
        r = 0

        # ── Create restore point ──────────────────────────────────────────
        section = ctk.CTkFrame(self, fg_color=AMD_RED, corner_radius=0)
        section.grid(row=r, column=0, sticky="ew", padx=24, pady=(20, 4))
        inner = ctk.CTkFrame(section, fg_color=PANEL, corner_radius=0,
                             border_width=1, border_color=BORDER_MID)
        inner.pack(fill="both", expand=True, padx=0, pady=(2, 0))
        inner.grid_columnconfigure(0, weight=1)

        Eyebrow(inner, "Create New Restore Point").grid(
            row=0, column=0, columnspan=3, sticky="w", padx=20, pady=(16, 8))

        ctk.CTkLabel(inner,
                     text="Creates a Windows System Restore snapshot of your current registry, "
                          "drivers, and settings so you can roll back any tweak instantly.",
                     font=F(11), text_color=TEXT_DIM, wraplength=640, justify="left", anchor="w"
                     ).grid(row=1, column=0, columnspan=3, sticky="w", padx=20, pady=(0, 12))

        self._desc_var = tk.StringVar(value="VER TWEAKS Backup")
        ctk.CTkEntry(inner, textvariable=self._desc_var,
                     fg_color=SURFACE, border_color=BORDER_MID, border_width=1,
                     text_color=TEXT, font=F(12), height=38, corner_radius=0,
                     placeholder_text="Restore point name..."
                     ).grid(row=2, column=0, sticky="ew", padx=(20, 8), pady=(0, 16))

        self._create_btn = ctk.CTkButton(inner, text="CREATE  ▶",
                                         fg_color=AMD_RED, hover_color=AMD_BRIGHT,
                                         text_color=TEXT, font=F(12, "bold", condensed=True),
                                         width=120, height=38, corner_radius=0,
                                         command=self._create_point)
        self._create_btn.grid(row=2, column=1, padx=(0, 20), pady=(0, 16))

        r += 1

        # ── System Restore wizard button ──────────────────────────────────
        wizard_outer = ctk.CTkFrame(self, fg_color=BORDER_MID, corner_radius=0)
        wizard_outer.grid(row=r, column=0, sticky="ew", padx=24, pady=4)
        wizard_inner = ctk.CTkFrame(wizard_outer, fg_color=PANEL, corner_radius=0)
        wizard_inner.pack(fill="both", expand=True, padx=0, pady=(1, 0))
        wizard_inner.grid_columnconfigure(0, weight=1)

        ctk.CTkLabel(wizard_inner, text="SYSTEM RESTORE WIZARD",
                     font=F(11, "bold", condensed=True), text_color=TEXT_DIM, anchor="w"
                     ).grid(row=0, column=0, sticky="w", padx=20, pady=(14, 2))
        ctk.CTkLabel(wizard_inner,
                     text="Open Windows' built-in System Restore interface to browse, "
                          "undo, or confirm restore points using the OS wizard.",
                     font=F(11), text_color=TEXT_DIM, anchor="w", wraplength=560
                     ).grid(row=1, column=0, sticky="w", padx=20, pady=(0, 12))
        ctk.CTkButton(wizard_inner, text="OPEN SYSTEM RESTORE  ▶",
                      fg_color=AMD_RED, hover_color=AMD_BRIGHT,
                      text_color=TEXT, font=F(12, "bold", condensed=True),
                      height=38, corner_radius=0, command=self._open_wizard
                      ).grid(row=2, column=0, sticky="w", padx=20, pady=(0, 16))

        r += 1

        # ── Existing points header ────────────────────────────────────────
        hdr_row = ctk.CTkFrame(self, fg_color="transparent")
        hdr_row.grid(row=r, column=0, sticky="ew", padx=24, pady=(20, 6))
        hdr_row.grid_columnconfigure(0, weight=1)

        Eyebrow(hdr_row, "Existing Restore Points").grid(row=0, column=0, sticky="w")

        self._refresh_btn = ctk.CTkButton(hdr_row, text="↻  REFRESH",
                                          fg_color="transparent", hover_color=SURFACE,
                                          text_color=TEXT_DIM, font=F(10, "bold"),
                                          width=90, height=26, corner_radius=0,
                                          border_width=1, border_color=BORDER,
                                          command=self._load_points)
        self._refresh_btn.grid(row=0, column=1, padx=(10, 0))

        r += 1
        self._cards_start_row = r

    # ── dynamic restore-point list ────────────────────────────────────────────

    def _clear_cards(self):
        for w in self.grid_slaves():
            if int(w.grid_info().get("row", 0)) >= self._cards_start_row:
                w.destroy()

    def _load_points(self):
        self._clear_cards()
        self._refresh_btn.configure(state="disabled", text="LOADING...")
        lbl = ctk.CTkLabel(self, text="SCANNING RESTORE POINTS...",
                           font=F(12, "bold"), text_color=TEXT_DIM)
        lbl.grid(row=self._cards_start_row, column=0, pady=40)
        threading.Thread(target=self._fetch, daemon=True).start()

    def _fetch(self):
        from tweaks.backup import get_restore_points
        points = get_restore_points()
        self.after(0, lambda: self._display(points))

    def _display(self, points: list):
        self._clear_cards()
        self._refresh_btn.configure(state="normal", text="↻  REFRESH")

        if not points:
            no_lbl = ctk.CTkFrame(self, fg_color=CARD, corner_radius=0,
                                  border_width=1, border_color=BORDER)
            no_lbl.grid(row=self._cards_start_row, column=0,
                        sticky="ew", padx=24, pady=4)
            ctk.CTkLabel(no_lbl,
                         text="NO RESTORE POINTS FOUND\n\n"
                              "Create one above, or enable System Restore in Windows Settings.",
                         font=F(12, "bold"), text_color=TEXT_DIM, justify="center"
                         ).pack(pady=30)
            return

        for i, pt in enumerate(points):
            card = RestorePointCard(self, pt,
                                   on_restore_done=self._load_points)
            card.grid(row=self._cards_start_row + i, column=0,
                      sticky="ew", padx=24, pady=4)

        ctk.CTkFrame(self, height=24, fg_color="transparent").grid(
            row=self._cards_start_row + len(points), column=0)

    # ── actions ───────────────────────────────────────────────────────────────

    def _create_point(self):
        desc = self._desc_var.get().strip() or "VER TWEAKS Backup"
        self._create_btn.configure(text="CREATING...", state="disabled")

        def _do():
            from tweaks.backup import create_restore_point
            ok = create_restore_point(desc)
            self.after(0, lambda: self._on_created(ok))

        threading.Thread(target=_do, daemon=True).start()

    def _on_created(self, ok: bool):
        self._create_btn.configure(text="CREATE  ▶", state="normal")
        if ok:
            self._load_points()
        else:
            messagebox.showerror(
                "Failed to Create Restore Point",
                "Could not create the restore point.\n\n"
                "Make sure you are running as Administrator and that\n"
                "System Restore is enabled for your system drive.",
            )

    def _open_wizard(self):
        from tweaks.backup import open_system_restore
        threading.Thread(target=open_system_restore, daemon=True).start()


# ── Scrollable tweak list page ────────────────────────────────────────────────

class CategoryPage(ctk.CTkScrollableFrame):
    def __init__(self, parent, tweaks: list, **kw):
        super().__init__(parent, fg_color=BG,
                         scrollbar_button_color=SURFACE,
                         scrollbar_button_hover_color=BORDER_MID, **kw)
        self.grid_columnconfigure(0, weight=1)
        for i, tweak in enumerate(tweaks):
            TweakCard(self, tweak).grid(row=i, column=0, sticky="ew",
                                        padx=24, pady=4)
        ctk.CTkFrame(self, height=24, fg_color="transparent").grid(
            row=len(tweaks), column=0)


# ── Main application window ────────────────────────────────────────────────────

class App(ctk.CTk):
    def __init__(self, tweak_map: dict):
        super().__init__()
        self.tweak_map = tweak_map
        self._cur_page = None

        self.title("NULLFRAME — Windows Performance Optimizer")
        self.geometry("1160x760")
        self.minsize(960, 600)
        self.configure(fg_color=BG)

        self._build_ui()
        self._nav("cpu")

    # ── layout ────────────────────────────────────────────────────────────────

    def _build_ui(self):
        self.grid_columnconfigure(1, weight=1)
        self.grid_rowconfigure(0, weight=1)

        # ── Sidebar ──────────────────────────────────────────────────────────
        sb = ctk.CTkFrame(self, fg_color=PANEL, width=248, corner_radius=0)
        sb.grid(row=0, column=0, sticky="nsew")
        sb.grid_propagate(False)
        sb.grid_columnconfigure(0, weight=1)
        sb.grid_rowconfigure(99, weight=1)

        # ── Red accent stripe at top of sidebar ──────────────────────────────
        ctk.CTkFrame(sb, height=3, fg_color=NF_RED, corner_radius=0
                     ).grid(row=0, column=0, sticky="ew")

        # Logo
        logo = ctk.CTkFrame(sb, fg_color="transparent")
        logo.grid(row=1, column=0, sticky="ew", padx=18, pady=(18, 14))
        logo.grid_columnconfigure(1, weight=1)
        LogoMark(logo, size=40, label="NF").grid(row=0, column=0, rowspan=2)
        ctk.CTkLabel(logo, text="NULL", font=F(22, "bold", condensed=True),
                     text_color=TEXT).grid(row=0, column=1, sticky="sw", padx=(10, 0))
        ctk.CTkLabel(logo, text="FRAME", font=F(22, "bold", condensed=True),
                     text_color=NF_BRIGHT).grid(row=1, column=1, sticky="nw", padx=(10, 0))

        RedRule(sb).grid(row=2, column=0, sticky="ew", padx=14, pady=(0, 10))
        ctk.CTkLabel(sb, text="MODULES", font=F(9, "bold"),
                     text_color=TEXT_DIM).grid(row=3, column=0, sticky="w", padx=20, pady=(0, 6))

        self._cat_btns: dict[str, ctk.CTkButton] = {}
        for idx, (name, key, num) in enumerate(CATEGORIES):
            is_backup = key == "backup"
            btn = ctk.CTkButton(
                sb,
                text=f"  {num}  {name}",
                fg_color="transparent",
                hover_color=SURFACE,
                text_color=NF_BRIGHT if is_backup else TEXT_DIM,
                anchor="w",
                font=F(12, "bold", condensed=True),
                height=40 if not is_backup else 42,
                corner_radius=0,
                border_width=1 if is_backup else 0,
                border_color=BORDER_MID if is_backup else "transparent",
                command=lambda k=key: self._nav(k),
            )
            btn.grid(row=idx + 4, column=0, sticky="ew",
                     padx=10, pady=(1 if not is_backup else 5))
            self._cat_btns[key] = btn

        RedRule(sb).grid(row=98, column=0, sticky="ew", padx=14)
        ctk.CTkLabel(sb, text="NULLFRAME  //  V1.0\nRUN AS ADMINISTRATOR",
                     font=F(9), text_color=TEXT_DIM, justify="left"
                     ).grid(row=100, column=0, padx=20, pady=12, sticky="sw")

        # ── Content area ──────────────────────────────────────────────────────
        content = ctk.CTkFrame(self, fg_color=BG, corner_radius=0)
        content.grid(row=0, column=1, sticky="nsew")
        content.grid_columnconfigure(0, weight=1)
        content.grid_rowconfigure(1, weight=1)

        # Header bar
        hdr = ctk.CTkFrame(content, fg_color=PANEL, height=72, corner_radius=0)
        hdr.grid(row=0, column=0, sticky="ew")
        hdr.grid_propagate(False)
        hdr.grid_columnconfigure(0, weight=1)

        AccentLine(hdr).grid(row=0, column=0, columnspan=2, sticky="ew")

        self._hdr_eye = ctk.CTkLabel(hdr, text="",
                                     font=F(9, "bold"), text_color=AMD_BRIGHT)
        self._hdr_eye.grid(row=1, column=0, sticky="w", padx=26, pady=(10, 2))

        self._hdr_title = ctk.CTkLabel(hdr, text="",
                                       font=F(24, "bold", condensed=True), text_color=TEXT)
        self._hdr_title.grid(row=2, column=0, sticky="w", padx=26, pady=(0, 12))

        self._hdr_count = ctk.CTkLabel(hdr, text="",
                                       font=F(10, "bold"), text_color=TEXT_DIM)
        self._hdr_count.grid(row=1, column=1, rowspan=2, sticky="e", padx=26)

        # Page container
        self._pf = ctk.CTkFrame(content, fg_color=BG, corner_radius=0)
        self._pf.grid(row=1, column=0, sticky="nsew")
        self._pf.grid_columnconfigure(0, weight=1)
        self._pf.grid_rowconfigure(0, weight=1)

    # ── navigation ────────────────────────────────────────────────────────────

    def _nav(self, key: str):
        for k, btn in self._cat_btns.items():
            if k == key:
                btn.configure(fg_color=SURFACE, text_color=AMD_BRIGHT,
                               border_width=1, border_color=BORDER_MID)
            else:
                btn.configure(fg_color="transparent", text_color=TEXT_DIM,
                               border_width=0)

        for name, k, num in CATEGORIES:
            if k == key:
                is_backup = k == "backup"
                tweaks = self.tweak_map.get(k, [])
                self._hdr_eye.configure(
                    text=f"◆  NULLFRAME  //  MODULE {num}"
                )
                self._hdr_title.configure(text=name)
                self._hdr_count.configure(
                    text="BACKUP & RESTORE" if is_backup
                    else f"{len(tweaks)} TWEAKS AVAILABLE"
                )
                break

        if self._cur_page:
            self._cur_page.destroy()

        if key == "backup":
            self._cur_page = BackupPage(self._pf)
        else:
            self._cur_page = CategoryPage(self._pf, self.tweak_map.get(key, []))

        self._cur_page.grid(row=0, column=0, sticky="nsew")


# ── Entry point ───────────────────────────────────────────────────────────────

def main():
    try:
        from utils.system import check_admin, run_as_admin
        if not check_admin():
            root = tk.Tk()
            root.withdraw()
            ans = messagebox.askyesno(
                "Administrator Required",
                "NULLFRAME requires administrator privileges.\n\nRestart as Administrator?",
            )
            root.destroy()
            if ans:
                run_as_admin()
            sys.exit(0)
    except ImportError:
        pass

    try:
        from tweaks.cpu_system import CPU_TWEAKS
        from tweaks.gpu_gaming import GPU_TWEAKS
        from tweaks.network    import NETWORK_TWEAKS
        from tweaks.usb_input  import USB_INPUT_TWEAKS
        from tweaks.devices    import DEVICE_TWEAKS
        from tweaks.storage    import STORAGE_TWEAKS
        from tweaks.memory     import MEMORY_TWEAKS
        from tweaks.privacy    import PRIVACY_TWEAKS
    except ImportError as e:
        messagebox.showerror("Import Error", str(e))
        sys.exit(1)

    tweak_map = {
        "cpu":     CPU_TWEAKS,
        "gpu":     GPU_TWEAKS,
        "network": NETWORK_TWEAKS,
        "usb":     USB_INPUT_TWEAKS,
        "devices": DEVICE_TWEAKS,
        "storage": STORAGE_TWEAKS,
        "memory":  MEMORY_TWEAKS,
        "privacy": PRIVACY_TWEAKS,
        "backup":  [],   # BackupPage is self-contained, no tweak list needed
    }

    App(tweak_map).mainloop()


if __name__ == "__main__":
    main()
