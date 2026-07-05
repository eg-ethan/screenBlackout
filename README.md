# Screen Blackout

Instantly blacks out **all** connected monitors with fullscreen, always-on-top black windows. Built for dual-monitor setups but works with any number of displays.

## Usage

Double-click `ScreenBlackout.exe`. Every monitor goes black and the cursor is hidden.

**To exit:** press `Esc` or double-click anywhere.

## Building from source

No SDK needed — uses the C# compiler that ships with Windows (.NET Framework 4.x):

```
build.cmd
```

Produces a portable, dependency-free `ScreenBlackout.exe`.

## How it works

- Enumerates displays with `Screen.AllScreens` and creates one borderless black `TopMost` window per monitor.
- Declares per-monitor DPI awareness before creating windows, so scaled displays (125%/150%) are fully covered with no gaps.
- Closing any one window (Esc, double-click, Alt+F4) exits the whole app.

See [PRD.md](PRD.md) for the full product requirements.
