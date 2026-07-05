# Product Requirements Document — Screen Blackout

## 1. Overview

**Product name:** Screen Blackout
**Platform:** Windows 10/11
**Deliverable:** A single standalone executable (`ScreenBlackout.exe`) with no runtime dependencies beyond the .NET Framework 4.x that ships with Windows.

Screen Blackout instantly covers every connected monitor with a solid black, fullscreen, always-on-top window. It is useful for hiding screen contents, resting eyes, presentations, or protecting OLED panels without putting displays to sleep.

## 2. Goals

- One double-click blacks out **all** connected monitors simultaneously.
- Works out of the box on a standard two-monitor setup (primary use case) but supports any number of monitors dynamically via display enumeration.
- Exiting is instant and obvious.

## 3. Non-Goals

- Turning monitors off at the hardware/power level (DPMS).
- Dimming/opacity controls, scheduling, or hotkey daemons.
- macOS/Linux support.
- Installer — a bare portable .exe is the deliverable.

## 4. Functional Requirements

| ID | Requirement |
|----|-------------|
| FR-1 | On launch, the app enumerates all connected displays (`Screen.AllScreens`). |
| FR-2 | For each display, the app creates one borderless, fullscreen window filled solid black, positioned exactly over that display's bounds. |
| FR-3 | All blackout windows are **always on top** so nothing bleeds through. |
| FR-4 | The mouse cursor is hidden while over any blackout window. |
| FR-5 | Pressing **Esc** on any window closes the entire application (all windows). |
| FR-6 | **Double-clicking** any blackout window also exits the application. |
| FR-7 | The app is per-monitor DPI aware so scaled displays (125%/150%) are still fully covered with no gaps. |
| FR-8 | The app does not appear as one taskbar button per monitor — a single instance entry at most. |

## 5. Technical Requirements

| ID | Requirement |
|----|-------------|
| TR-1 | Language/stack: C# WinForms targeting .NET Framework 4.x (compiled with the in-box `csc.exe`), so no SDK or runtime install is required to build or run. |
| TR-2 | Output: single `ScreenBlackout.exe`, portable, no config files. |
| TR-3 | DPI awareness declared programmatically (`SetProcessDpiAwareness` / `SetProcessDPIAware` fallback) before any window is created. |
| TR-4 | Windows use `FormBorderStyle.None`, `TopMost = true`, manual `Bounds` assignment per screen. |
| TR-5 | Closing any one window tears down the whole process cleanly (`Application.Exit`). |

## 6. User Experience

1. User double-clicks `ScreenBlackout.exe`.
2. Both (all) monitors instantly turn black; cursor disappears over the black areas.
3. User presses **Esc** (or double-clicks) → all windows close, desktop is back.

No UI chrome, no settings, no prompts.

## 7. Acceptance Criteria

- [ ] Launching on a two-monitor machine produces two black fullscreen windows, one per monitor, with no visible gaps, taskbar, or borders.
- [ ] Esc from either monitor exits the app completely (no orphan windows/processes).
- [ ] Double-click exits the app completely.
- [ ] Works on monitors with different resolutions and DPI scaling factors.
- [ ] Source compiles with the stock Windows `csc.exe` via the included build script.

## 8. Risks / Mitigations

- **DPI virtualization gaps** on scaled monitors → declared DPI awareness before window creation (TR-3).
- **OS overlays** (UAC prompts, lock screen) render above TopMost windows by design → out of scope, acceptable.
- **Screen-saver/sleep** may still activate → acceptable; app does not block system idle by design (it is a visual blackout, not a keep-awake tool).
