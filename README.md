# Screen Blackout

Instantly blacks out **all** connected monitors with fullscreen, always-on-top black windows. Built for dual-monitor setups but works with any number of displays.

## Usage

Double-click `ScreenBlackout.exe` once. Every monitor goes black and the cursor is hidden. The app stays resident in the system tray.

| Action | Effect |
|--------|--------|
| `Esc` or double-click a black screen | Hide the blackout (app keeps running in the tray) |
| `Left Ctrl` + `Numpad 9` | Black out all monitors again, from anywhere |
| Tray icon double-click / "Black out now" | Same as the hotkey |
| Tray icon right-click → **Start with Windows** | Toggle auto-start at login (starts quietly in the tray, no blackout) |
| Tray icon right-click → **Exit** | Quit the app completely |

> **Note:** the hotkey uses the numpad **9**, so **NumLock must be on**. Right Ctrl deliberately does not trigger it.

## Start with Windows

Toggle **Start with Windows** in the tray menu. It writes a per-user registry Run entry (`HKCU\Software\Microsoft\Windows\CurrentVersion\Run`) that launches the app with `--tray`, so at login it arms the hotkey silently instead of blacking out your screens. Launching the exe manually (without `--tray`) still blacks out immediately.

## Building from source

No SDK needed — uses the C# compiler that ships with Windows (.NET Framework 4.x):

```
build.cmd
```

Produces a portable, dependency-free `ScreenBlackout.exe`.

## How it works

- Enumerates displays with `Screen.AllScreens` on every activation and creates one borderless black `TopMost` window per monitor, so monitor changes are picked up without a restart.
- Declares per-monitor DPI awareness before creating windows, so scaled displays (125%/150%) are fully covered with no gaps.
- Registers a global `RegisterHotKey` (Ctrl + Numpad 9) on a hidden message window; the handler additionally checks `GetAsyncKeyState(VK_LCONTROL)` so only **left** Ctrl triggers it.
- Closing any one blackout window (Esc, double-click, Alt+F4) hides them all; the tray icon owns the process lifetime.
- Trims its working set after startup and every time the blackout is dismissed, so it idles in the tray at ~1–3 MB.

See [PRD.md](PRD.md) for the full product requirements.
