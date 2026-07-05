using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

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
        private static void Main()
        {
            EnableDpiAwareness();
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            var forms = new List<BlackoutForm>();
            foreach (Screen screen in Screen.AllScreens)
            {
                forms.Add(new BlackoutForm(screen, forms.Count == 0));
            }

            if (forms.Count == 0)
            {
                return;
            }

            // Show secondary windows first, then run the message loop on the primary one.
            for (int i = 1; i < forms.Count; i++)
            {
                forms[i].Show();
            }
            Application.Run(forms[0]);
        }
    }

    internal sealed class BlackoutForm : Form
    {
        public BlackoutForm(Screen screen, bool isPrimaryWindow)
        {
            SuspendLayout();

            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            BackColor = Color.Black;
            TopMost = true;
            ShowInTaskbar = isPrimaryWindow;
            Text = "Screen Blackout";
            KeyPreview = true;
            Bounds = screen.Bounds;

            KeyDown += OnKeyDown;
            MouseDoubleClick += OnMouseDoubleClick;
            MouseEnter += OnMouseEnter;
            MouseLeave += OnMouseLeave;
            FormClosed += OnFormClosed;

            ResumeLayout(false);
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            // Re-assert bounds after the window handle exists; some DPI/window-manager
            // combinations adjust size during creation.
            Bounds = Screen.FromControl(this).Bounds;
            Activate();
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                Application.Exit();
            }
        }

        private void OnMouseDoubleClick(object sender, MouseEventArgs e)
        {
            Application.Exit();
        }

        private void OnMouseEnter(object sender, EventArgs e)
        {
            Cursor.Hide();
        }

        private void OnMouseLeave(object sender, EventArgs e)
        {
            Cursor.Show();
        }

        private void OnFormClosed(object sender, FormClosedEventArgs e)
        {
            // Closing any window (Esc, double-click, Alt+F4) ends the whole app.
            Cursor.Show();
            Application.Exit();
        }
    }
}
