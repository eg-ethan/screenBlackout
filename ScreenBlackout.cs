using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Win32;

namespace ScreenBlackout
{
    internal static class Program
    {
        [DllImport("shcore.dll")]
        private static extern int SetProcessDpiAwareness(int value);

        [DllImport("user32.dll")]
        private static extern bool SetProcessDPIAware();

        // 2 = PROCESS_PER_MONITOR_DPI_AWARE
        private static void EnableDpiAwareness()
        {
            try
            {
                SetProcessDpiAwareness(2);
            }
            catch
            {
                // shcore.dll unavailable (pre-Win 8.1) — fall back to system DPI awareness
                try { SetProcessDPIAware(); } catch { }
            }
        }

        [STAThread]
        private static void Main(string[] args)
        {
            // --tray: start resident in the tray without blacking out (used by
            // the "Start with Windows" registration so logins aren't blacked out).
            bool startHidden = Array.Exists(args,
                a => a.Equals("--tray", StringComparison.OrdinalIgnoreCase)
                  || a.Equals("/tray", StringComparison.OrdinalIgnoreCase));

            EnableDpiAwareness();
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new BlackoutAppContext(startHidden));
        }
    }

    /// <summary>
    /// Tray-resident controller. Blacks out all monitors on launch; Esc/double-click
    /// hides the blackout but keeps the app running so Left Ctrl + Numpad 9 can
    /// bring it back instantly. Exit via the tray icon menu.
    /// </summary>
    internal sealed class BlackoutAppContext : ApplicationContext
    {
        private readonly NotifyIcon _trayIcon;
        private readonly HotkeyWindow _hotkeyWindow;
        private readonly List<BlackoutForm> _forms = new List<BlackoutForm>();

        public BlackoutAppContext(bool startHidden)
        {
            _hotkeyWindow = new HotkeyWindow(ShowBlackout);

            var menu = new ContextMenuStrip();
            menu.Items.Add("Black out now  (Left Ctrl + Numpad 9)", null, (s, e) => ShowBlackout());
            menu.Items.Add(new ToolStripSeparator());
            var startupItem = new ToolStripMenuItem("Start with Windows")
            {
                CheckOnClick = true,
                Checked = StartupRegistration.IsEnabled(),
            };
            startupItem.CheckedChanged += (s, e) => StartupRegistration.SetEnabled(startupItem.Checked);
            menu.Items.Add(startupItem);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Exit", null, (s, e) => ExitApp());

            _trayIcon = new NotifyIcon
            {
                Icon = SystemIcons.Application,
                Text = "Screen Blackout — Left Ctrl + Numpad 9",
                ContextMenuStrip = menu,
                Visible = true,
            };
            _trayIcon.DoubleClick += (s, e) => ShowBlackout();

            if (!_hotkeyWindow.Registered)
            {
                _trayIcon.ShowBalloonTip(5000, "Screen Blackout",
                    "Could not register Ctrl + Numpad 9 (another app owns it). " +
                    "Use the tray icon to black out instead.", ToolTipIcon.Warning);
            }

            if (!startHidden)
            {
                ShowBlackout();
            }
            else
            {
                MemoryTrimmer.Trim();
            }
        }

        private void ShowBlackout()
        {
            if (_forms.Count > 0)
            {
                return; // already visible
            }

            // Forms are recreated on every show so monitor hotplug/layout changes
            // between activations are picked up.
            foreach (Screen screen in Screen.AllScreens)
            {
                _forms.Add(new BlackoutForm(screen, HideBlackout));
            }
            foreach (BlackoutForm form in _forms)
            {
                form.Show();
            }
            if (_forms.Count > 0)
            {
                _forms[0].Activate();
            }
        }

        private void HideBlackout()
        {
            if (_forms.Count == 0)
            {
                return;
            }
            BlackoutForm[] forms = _forms.ToArray();
            _forms.Clear(); // cleared first so FormClosed re-entry is a no-op
            foreach (BlackoutForm form in forms)
            {
                form.Close();
                form.Dispose();
            }
            MemoryTrimmer.Trim();
        }

        private void ExitApp()
        {
            HideBlackout();
            _trayIcon.Visible = false;
            _trayIcon.Dispose();
            _hotkeyWindow.Dispose();
            ExitThread();
        }
    }

    /// <summary>
    /// Keeps the idle footprint small. The CLR holds on to startup and
    /// blackout-session allocations even when the app is just waiting in the
    /// tray; a GC plus a working-set trim releases them back to the OS.
    /// </summary>
    internal static class MemoryTrimmer
    {
        [DllImport("kernel32.dll")]
        private static extern IntPtr GetCurrentProcess();

        [DllImport("kernel32.dll")]
        private static extern bool SetProcessWorkingSetSize(IntPtr process, IntPtr min, IntPtr max);

        public static void Trim()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            try
            {
                // min/max of -1 tells Windows to empty the working set; pages
                // still in use fault back in on demand.
                SetProcessWorkingSetSize(GetCurrentProcess(), (IntPtr)(-1), (IntPtr)(-1));
            }
            catch { }
        }
    }

    /// <summary>
    /// Manages the per-user "Start with Windows" registration (HKCU Run key).
    /// Registered with --tray so logins start quietly in the tray instead of
    /// blacking out the screens.
    /// </summary>
    internal static class StartupRegistration
    {
        private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string ValueName = "ScreenBlackout";

        private static string Command
        {
            get { return "\"" + Application.ExecutablePath + "\" --tray"; }
        }

        public static bool IsEnabled()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RunKeyPath))
                {
                    return key != null && key.GetValue(ValueName) as string == Command;
                }
            }
            catch
            {
                return false;
            }
        }

        public static void SetEnabled(bool enabled)
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(RunKeyPath))
                {
                    if (enabled)
                    {
                        key.SetValue(ValueName, Command);
                    }
                    else
                    {
                        key.DeleteValue(ValueName, false);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not update the startup setting:\n" + ex.Message,
                    "Screen Blackout", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }

    /// <summary>
    /// Hidden message-only window that owns the global hotkey registration.
    /// RegisterHotKey cannot distinguish left from right Ctrl, so the handler
    /// additionally requires the LEFT Ctrl key to be down.
    /// </summary>
    internal sealed class HotkeyWindow : NativeWindow, IDisposable
    {
        private const int WM_HOTKEY = 0x0312;
        private const int HOTKEY_ID = 1;
        private const uint MOD_CONTROL = 0x0002;
        private const uint MOD_NOREPEAT = 0x4000;
        private const uint VK_NUMPAD9 = 0x69;
        private const int VK_LCONTROL = 0xA2;

        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        private readonly Action _onHotkey;

        public bool Registered { get; private set; }

        public HotkeyWindow(Action onHotkey)
        {
            _onHotkey = onHotkey;
            CreateHandle(new CreateParams());
            Registered = RegisterHotKey(Handle, HOTKEY_ID, MOD_CONTROL | MOD_NOREPEAT, VK_NUMPAD9);
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_HOTKEY && (int)m.WParam == HOTKEY_ID)
            {
                if (GetAsyncKeyState(VK_LCONTROL) < 0)
                {
                    _onHotkey();
                }
                return;
            }
            base.WndProc(ref m);
        }

        public void Dispose()
        {
            if (Registered)
            {
                UnregisterHotKey(Handle, HOTKEY_ID);
                Registered = false;
            }
            DestroyHandle();
        }
    }

    internal sealed class BlackoutForm : Form
    {
        // A fully transparent cursor confines the "hidden cursor" effect to this
        // window without touching the global Cursor.Hide/Show counter.
        private static readonly Cursor BlankCursor = CreateBlankCursor();

        private readonly Action _requestHide;

        public BlackoutForm(Screen screen, Action requestHide)
        {
            _requestHide = requestHide;

            SuspendLayout();

            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            BackColor = Color.Black;
            TopMost = true;
            ShowInTaskbar = false;
            Text = "Screen Blackout";
            KeyPreview = true;
            Cursor = BlankCursor;
            Bounds = screen.Bounds;

            KeyDown += OnKeyDown;
            MouseDoubleClick += (s, e) => _requestHide();
            FormClosed += (s, e) => _requestHide(); // covers Alt+F4 on one window

            ResumeLayout(false);
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            // Re-assert bounds after the window handle exists; some DPI/window-manager
            // combinations adjust size during creation.
            Bounds = Screen.FromControl(this).Bounds;
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                _requestHide();
            }
        }

        private static Cursor CreateBlankCursor()
        {
            using (var bmp = new Bitmap(32, 32)) // 32bpp ARGB, fully transparent
            {
                return new Cursor(bmp.GetHicon());
            }
        }
    }
}
