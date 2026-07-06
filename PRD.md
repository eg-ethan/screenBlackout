# Product Requirements Document — Screen Blackout

## 1. Overview

**Product name:** Screen Blackout
**Platform:** Windows 10/11
**Deliverable:** A single standalone executable (`ScreenBlackout.exe`) with no runtime dependencies beyond the .NET Framework 4.x that ships with Windows.

Screen Blackout instantly covers every connected monitor with a solid black, fullscreen, always-on-top window. It is useful for hiding screen contents, resting eyes, presentations, or protecting OLED panels without putting displays to sleep.

The app is launched once and stays resident in the system tray: dismissing the blackout (Esc) hides it, and a global hotkey (**Left Ctrl + Numpad 9**) re-blacks-out the screens instantly without relaunching.

## 2. Goals

- One double-click blacks out **all** connected monitors simultaneously.
- Works out of the box on a standard two-monitor setup (primary use case) but supports any number of monitors dynamically via display enumeration.
- Dismissing the blackout is instant; re-engaging it via the global hotkey is instant.

## 3. Non-Goals

- Turning monitors off at the hardware/power level (DPMS).
- Dimming/opacity controls, scheduling, or hotkey daemons.
- macOS/Linux support.
- Installer — a bare portable .exe is the deliverable.

## 4. Functional Requirements

| ID | Requirement |
|----|-------------|
| FR-1 | On launch, the app immediately blacks out all displays and stays resident in the system tray. |
| FR-2 | Displays are enumerated (`Screen.AllScreens`) on every blackout activation, so monitor hotplug/layout changes are picked up. |
| FR-3 | For each display, the app creates one borderless, fullscreen window filled solid black, positioned exactly over that display's bounds. |
| FR-4 | All blackout windows are **always on top** so nothing bleeds through. |
| FR-5 | The mouse cursor is invisible while over any blackout window. |
| FR-6 | Pressing **Esc** (or double-clicking) on any window **hides** the blackout on all monitors; the app keeps running in the tray. |
| FR-7 | Pressing the global hotkey **Left Ctrl + Numpad 9** re-blacks-out all monitors from anywhere, even when no app window has focus. Right Ctrl does not trigger it. |
| FR-8 | The tray icon offers **Black out now** and **Exit**; double-clicking the tray icon also blacks out. Exit fully terminates the app and unregisters the hotkey. |
| FR-9 | The app is per-monitor DPI aware so scaled displays (125%/150%) are still fully covered with no gaps. |
| FR-10 | Blackout windows show no taskbar buttons; the tray icon is the app's only persistent UI. |
| FR-11 | If the hotkey cannot be registered (owned by another app), the user is notified via a tray balloon and can still use the tray menu. |

## 5. Technical Requirements

| ID | Requirement |
|----|-------------|
| TR-1 | Language/stack: C# WinForms targeting .NET Framework 4.x (compiled with the in-box `csc.exe`), so no SDK or runtime install is required to build or run. |
| TR-2 | Output: single `ScreenBlackout.exe`, portable, no config files. |
| TR-3 | DPI awareness declared programmatically (`SetProcessDpiAwareness` / `SetProcessDPIAware` fallback) before any window is created. |
| TR-4 | Windows use `FormBorderStyle.None`, `TopMost = true`, manual `Bounds` assignment per screen. |
| TR-5 | Closing any one blackout window (Esc, double-click, Alt+F4) hides all blackout windows together; the process keeps running until tray Exit. |
| TR-6 | The global hotkey uses `RegisterHotKey` (Ctrl + Numpad 9, `MOD_NOREPEAT`) on a hidden message window; since `RegisterHotKey` cannot distinguish left/right Ctrl, the handler additionally checks `GetAsyncKeyState(VK_LCONTROL)`. |
| TR-7 | The hotkey matches the **Numpad 9** virtual key, which requires **NumLock on** (with NumLock off the key reports PageUp and intentionally does not trigger, to avoid hijacking Ctrl+PageUp globally). |

## 6. User Experience

1. User double-clicks `ScreenBlackout.exe` once.
2. Both (all) monitors instantly turn black; cursor is invisible over the black areas.
3. User presses **Esc** (or double-clicks) → blackout hides, desktop is back; the app waits in the tray.
4. User presses **Left Ctrl + Numpad 9** at any time → both monitors instantly black out again.
5. To quit for good: right-click the tray icon → **Exit**.

No UI chrome beyond the tray icon, no settings, no prompts.

## 7. Acceptance Criteria

- [ ] Launching on a two-monitor machine produces two black fullscreen windows, one per monitor, with no visible gaps, taskbar, or borders.
- [ ] Esc from either monitor hides both windows; the process stays alive in the tray.
- [ ] Double-click hides both windows the same way.
- [ ] Left Ctrl + Numpad 9 re-shows the blackout from any foreground app; repeated hide/show cycles work indefinitely.
- [ ] Tray Exit terminates the process and unregisters the hotkey (no orphan windows/processes).
- [ ] Works on monitors with different resolutions and DPI scaling factors.
- [ ] Source compiles with the stock Windows `csc.exe` via the included build script.

## 8. Risks / Mitigations

- **DPI virtualization gaps** on scaled monitors → declared DPI awareness before window creation (TR-3).
- **OS overlays** (UAC prompts, lock screen) render above TopMost windows by design → out of scope, acceptable.
- **Screen-saver/sleep** may still activate → acceptable; app does not block system idle by design (it is a visual blackout, not a keep-awake tool).
