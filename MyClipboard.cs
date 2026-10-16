using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Win32;

namespace PersonalClipboard {
    public static class Theme {
        public static bool Dark;
        public static int? RandomSeed;
        public static readonly Image Mascot = LoadMascot();
        static Image LoadMascot() {
            using (var stream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("Copybara.Mascot")) {
                if (stream == null) return null;
                using (var image = Image.FromStream(stream)) return new Bitmap(image);
            }
        }
        public static Color Hue(double hue, double saturation, double lightness) {
            hue = (hue % 360 + 360) % 360 / 60;
            double c = (1 - Math.Abs(2 * lightness - 1)) * saturation, x = c * (1 - Math.Abs(hue % 2 - 1)), m = lightness - c / 2;
            double r = 0, g = 0, b = 0;
            if (hue < 1) { r = c; g = x; } else if (hue < 2) { r = x; g = c; } else if (hue < 3) { g = c; b = x; }
            else if (hue < 4) { g = x; b = c; } else if (hue < 5) { r = x; b = c; } else { r = c; b = x; }
            return Color.FromArgb((int)Math.Round((r + m) * 255), (int)Math.Round((g + m) * 255), (int)Math.Round((b + m) * 255));
        }
        static Color Palette(double saturation, double lightness) { return Hue(RandomSeed.GetValueOrDefault() % 360, saturation, lightness); }
        public static Color Background { get { return RandomSeed.HasValue ? Palette(.22, Dark ? .10 : .94) : Dark ? Color.FromArgb(21, 26, 36) : Color.FromArgb(243, 246, 250); } }
        public static Color Surface { get { return RandomSeed.HasValue ? Palette(.24, Dark ? .15 : .99) : Dark ? Color.FromArgb(32, 39, 53) : Color.White; } }
        public static Color Ink { get { return RandomSeed.HasValue ? Palette(.15, Dark ? .95 : .12) : Dark ? Color.FromArgb(232, 237, 247) : Color.FromArgb(29, 42, 63); } }
        public static Color Muted { get { return RandomSeed.HasValue ? Palette(.12, Dark ? .76 : .30) : Dark ? Color.FromArgb(163, 177, 201) : Color.FromArgb(99, 113, 134); } }
        public static Color Accent { get { return RandomSeed.HasValue ? Palette(.60, Dark ? .77 : .28) : Dark ? Color.FromArgb(112, 153, 255) : Color.FromArgb(52, 92, 226); } }
        public static Color Primary { get { return RandomSeed.HasValue ? Palette(.70, .28) : Color.FromArgb(52, 92, 226); } }
        public static Color Border { get { return RandomSeed.HasValue ? Palette(.18, Dark ? .31 : .80) : Dark ? Color.FromArgb(64, 76, 98) : Color.FromArgb(225, 231, 239); } }
        public static Color Selected { get { return RandomSeed.HasValue ? Palette(.32, Dark ? .25 : .87) : Dark ? Color.FromArgb(45, 65, 106) : Color.FromArgb(233, 239, 255); } }
        public static Color Hover { get { return RandomSeed.HasValue ? Palette(.25, Dark ? .22 : .89) : Dark ? Color.FromArgb(48, 58, 77) : Color.FromArgb(232, 237, 245); } }
        public static Color Pinned { get { return Dark ? Color.FromArgb(87, 73, 30) : Color.FromArgb(255, 245, 202); } }
        public static Color Selection = Color.FromArgb(255, 146, 38);
        public static string Choice { get { return RandomSeed.HasValue ? "random|" + RandomSeed.Value : Dark ? "dark" : "light"; } }
        public static void Choose(string choice) {
            RandomSeed = null; Dark = choice == "dark";
            if (choice.StartsWith("random|", StringComparison.Ordinal)) {
                int seed; if (int.TryParse(choice.Substring(7), out seed) && seed >= 0) { RandomSeed = seed; Dark = ((seed / 360) & 1) == 1; }
            }
        }
        public static void Randomize() {
            int seed;
            using (var rng = RandomNumberGenerator.Create()) { var bytes = new byte[4]; rng.GetBytes(bytes); seed = BitConverter.ToInt32(bytes, 0) & int.MaxValue; }
            if (RandomSeed.HasValue && seed % 720 == RandomSeed.Value % 720) seed = seed == int.MaxValue ? 0 : seed + 1;
            Choose("random|" + seed);
        }
        // Draw icons directly: no icon font or internet assets required.
        public static void DrawIcon(Graphics g, string kind, Rectangle r, Color ink) {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            if (kind == "image" && Mascot != null) { g.DrawImage(Mascot, r); return; }
            float x = r.X, y = r.Y, w = r.Width, h = r.Height;
            using (var pen = new Pen(ink, 1.6f)) {
                pen.LineJoin = LineJoin.Round;
                g.DrawRectangle(pen, x + 1, y + 1, w - 3, h - 3);
                if (kind == "image") {
                    g.DrawEllipse(pen, x + w * .62f, y + h * .2f, w * .12f, h * .12f);
                    g.DrawLines(pen, new PointF[] { new PointF(x + 3, y + h - 4), new PointF(x + w * .4f, y + h * .47f), new PointF(x + w * .62f, y + h * .72f), new PointF(x + w * .77f, y + h * .57f), new PointF(x + w - 3, y + h - 4) });
                } else {
                    for (int i = 0; i < 3; i++) g.DrawLine(pen, x + 5, y + 5 + i * (h - 10) / 2, x + w - 5, y + 5 + i * (h - 10) / 2);
                }
            }
        }
        public static GraphicsPath Round(Rectangle bounds, int radius) {
            var path = new GraphicsPath(); int d = Math.Min(radius * 2, Math.Min(bounds.Width, bounds.Height));
            if (d <= 0) { path.AddRectangle(bounds); return path; }
            path.AddArc(bounds.X, bounds.Y, d, d, 180, 90);
            path.AddArc(bounds.Right - d, bounds.Y, d, d, 270, 90);
            path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
            path.AddArc(bounds.X, bounds.Bottom - d, d, d, 90, 90); path.CloseFigure(); return path;
        }
    }
    public class SoftPanel : Panel {
        public SoftPanel() { DoubleBuffered = true; BackColor = Theme.Background; Padding = new Padding(14); }
        protected override void OnPaint(PaintEventArgs e) {
            base.OnPaint(e); e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using (var path = Theme.Round(new Rectangle(0, 0, Width - 1, Height - 1), 12))
            using (var brush = new SolidBrush(Theme.Surface))
            using (var pen = new Pen(Theme.Border)) { e.Graphics.FillPath(brush, path); e.Graphics.DrawPath(pen, path); }
        }
    }
    public class SoftButton : Button {
        public bool Primary, Active, Danger;
        public string IconKind;
        bool hover;
        public SoftButton() {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
            FlatStyle = FlatStyle.Flat; FlatAppearance.BorderSize = 0; BackColor = Theme.Background;
            Height = 36; Cursor = Cursors.Hand; Margin = new Padding(0, 0, 8, 0); Font = new Font("Segoe UI", 10);
        }
        protected override void OnMouseEnter(EventArgs e) { hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { hover = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnPaint(PaintEventArgs e) {
            e.Graphics.Clear(BackColor); e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Color fill = Primary ? Theme.Primary : Active ? Theme.Selected : hover ? Theme.Hover : Theme.Surface;
            Color ink = Primary ? Color.White : Danger ? (Theme.Dark ? Color.FromArgb(255, 150, 164) : Color.FromArgb(169, 47, 63)) : Active ? Theme.Accent : Theme.Ink;
            if (!Enabled) ink = Theme.Muted;
            if (SystemInformation.HighContrast) { fill = Primary || Active ? SystemColors.Highlight : SystemColors.ButtonFace; ink = Primary || Active ? SystemColors.HighlightText : SystemColors.ControlText; }
            using (var path = Theme.Round(new Rectangle(0, 0, Width - 1, Height - 1), 8))
            using (var brush = new SolidBrush(fill)) e.Graphics.FillPath(brush, path);
            Rectangle labelBounds = ClientRectangle;
            if (IconKind != null) { Theme.DrawIcon(e.Graphics, IconKind, new Rectangle(22, (Height - 20) / 2, 20, 20), ink); labelBounds = new Rectangle(54, 0, Math.Max(0, Width - 66), Height); }
            TextRenderer.DrawText(e.Graphics, Text, Font, labelBounds, ink, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix);
            if (Focused && ShowFocusCues) ControlPaint.DrawFocusRectangle(e.Graphics, new Rectangle(4, 4, Width - 8, Height - 8), ink, fill);
        }
    }
    public class IconLabel : Label {
        public string IconKind;
        protected override void OnPaint(PaintEventArgs e) {
            if (IconKind == null) { base.OnPaint(e); return; }
            e.Graphics.Clear(BackColor);
            Theme.DrawIcon(e.Graphics, IconKind, new Rectangle(1, 2, 20, 20), ForeColor);
            TextRenderer.DrawText(e.Graphics, Text, Font, new Rectangle(30, 0, Math.Max(0, Width - 30), Height), ForeColor, TextFormatFlags.NoPrefix | TextFormatFlags.WordBreak);
        }
    }
    public class Clip {
        public string Id, Kind, Label, Hash;
        public DateTime Created;
        public bool Pinned;
        public override string ToString() { return (Kind == "image" ? "[Image]  " : "[Text]  ") + Label; }
    }
    public partial class Window : Form {
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] static extern IntPtr SendMessage(IntPtr h, int message, IntPtr w, string text);
        [DllImport("user32.dll")] static extern bool AddClipboardFormatListener(IntPtr hwnd);
        [DllImport("user32.dll")] static extern bool RemoveClipboardFormatListener(IntPtr hwnd);
        [DllImport("user32.dll")] static extern uint GetClipboardSequenceNumber();
        [DllImport("user32.dll")] static extern bool RegisterHotKey(IntPtr h, int id, uint modifiers, uint key);
        [DllImport("user32.dll")] static extern bool UnregisterHotKey(IntPtr h, int id);
        readonly string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "JairoClipboard", "History");
        readonly List<Clip> clips = new List<Clip>();
        readonly ListBox list = new ListBox();
        readonly TextBox search = new TextBox();
        readonly TextBox preview = new TextBox();
        readonly PictureBox picture = new PictureBox();
        readonly Label status = new Label();
        readonly SoftButton pin = new SoftButton(), pause = new SoftButton();
        readonly Label totals = new Label();
        readonly IconLabel previewMeta = new IconLabel();
        readonly SoftButton lightMode = new SoftButton(), darkMode = new SoftButton(), randomMode = new SoftButton();
        readonly ToolTip tips = new ToolTip();
        readonly string themeFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "JairoClipboard", "theme.txt");
        readonly Dictionary<string, SoftButton> filters = new Dictionary<string, SoftButton>();
        string currentFilter = "all";
        readonly CheckBox startup = new CheckBox();
        readonly bool background;
        readonly string startupChoice = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "JairoClipboard", "startup-configured.txt");
        const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
        const string RunName = "JairoClipboard";
        readonly NotifyIcon tray = new NotifyIcon();
        readonly System.Windows.Forms.Timer capture = new System.Windows.Forms.Timer();
        bool paused, quitting, listener, shortcut, reflectingStartup;
        uint lastSequence;
        int attempts, captureSuspensions;
        const int MaxPayload = 25 * 1024 * 1024;

        public Window(bool startInBackground) {
            try { Theme.Choose(File.Exists(themeFile) ? File.ReadAllText(themeFile).Trim() : "light"); } catch { Theme.Choose("light"); }
            background = startInBackground;
            if (background) { WindowState = FormWindowState.Minimized; ShowInTaskbar = false; }
            using (var iconStream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("Copybara.Icon")) {
                if (iconStream != null) using (var loaded = new Icon(iconStream)) Icon = (Icon)loaded.Clone();
            }
            Text = "Copybara"; Width = 1160; Height = 780; MinimumSize = new Size(960, 640);
            Font = new Font("Segoe UI", 10); ForeColor = Theme.Ink; StartPosition = FormStartPosition.CenterScreen;
            AutoScaleMode = AutoScaleMode.Font; BackColor = Theme.Background; DoubleBuffered = true;
            var shell = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Padding = new Padding(22) };
            shell.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180)); shell.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); Controls.Add(shell);
            var rail = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 10, Padding = new Padding(0, 6, 18, 0) };
            rail.RowStyles.Add(new RowStyle(SizeType.Absolute, 58)); rail.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            for (int i = 0; i < 4; i++) rail.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
            rail.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); rail.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
            rail.RowStyles.Add(new RowStyle(SizeType.Absolute, 38)); rail.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
            shell.Controls.Add(rail, 0, 0);
            var brand = new Panel { Dock = DockStyle.Fill };
            brand.Controls.Add(new PictureBox { Tag = "mascot", Image = Theme.Mascot, SizeMode = PictureBoxSizeMode.Zoom, Size = new Size(34, 34), Location = new Point(0, 0) });
            brand.Controls.Add(new Label { Text = "Copybara", AutoSize = true, Location = new Point(38, 4), Font = new Font("Segoe UI", 14, FontStyle.Bold), ForeColor = Theme.Ink }); rail.Controls.Add(brand);
            rail.Controls.Add(new Label { Text = "YOUR COLLECTION", AutoSize = true, Font = new Font("Segoe UI", 8, FontStyle.Bold), ForeColor = Theme.Muted });
            AddFilter(rail, "all", "All items"); AddFilter(rail, "text", "Text"); AddFilter(rail, "image", "Images"); AddFilter(rail, "pinned", "Pinned");
            rail.Controls.Add(new Label { Text = "Open anytime\r\nCtrl + Alt + V", Dock = DockStyle.Bottom, Height = 54, ForeColor = Theme.Muted });
            pause.Text = "Pause saving"; pause.Dock = DockStyle.Fill; pause.Click += delegate { TogglePause(); }; rail.Controls.Add(pause);
            startup.Text = "Start with Windows"; startup.AutoSize = true; startup.Font = new Font("Segoe UI", 9); startup.Margin = new Padding(0, 8, 0, 0);
            try { using (var key = Registry.CurrentUser.OpenSubKey(RunKey)) startup.Checked = key != null && key.GetValue(RunName) != null; } catch { startup.Checked = false; }
            startup.CheckedChanged += delegate {
                if (reflectingStartup) return;
                try { SetStartup(startup.Checked); }
                catch (Exception ex) { MessageBox.Show(this, "Could not change startup setting: " + ex.Message, "Copybara"); RefreshStartupCheckbox(); }
            }; rail.Controls.Add(startup);
            var quit = new SoftButton { Text = "Quit app", Dock = DockStyle.Fill }; quit.Click += delegate { quitting = true; Close(); }; rail.Controls.Add(quit);
            var content = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 6, ColumnCount = 1 };
            content.RowStyles.Add(new RowStyle(SizeType.Absolute, 85)); content.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
            content.RowStyles.Add(new RowStyle(SizeType.Absolute, 52)); content.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
            content.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); content.RowStyles.Add(new RowStyle(SizeType.Absolute, 42)); shell.Controls.Add(content, 1, 0);
            var heading = new Panel { Dock = DockStyle.Fill };
            heading.Controls.Add(new Label { Text = "A little space for everything you copy.", Location = new Point(0, 46), AutoSize = true, ForeColor = Theme.Muted });
            heading.Controls.Add(new Label { Text = "Your clipboard", Location = new Point(-2, 0), AutoSize = true, Font = new Font("Segoe UI", 25, FontStyle.Bold), ForeColor = Theme.Ink });
            var themeButtons = new FlowLayoutPanel { Dock = DockStyle.Right, Width = 250, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, Padding = new Padding(0, 8, 0, 0) };
            lightMode.Text = "Light"; lightMode.Width = 68; lightMode.Click += delegate { ChangeMode("light"); };
            darkMode.Text = "Dark"; darkMode.Width = 68; darkMode.Click += delegate { ChangeMode("dark"); };
            randomMode.Text = "Random"; randomMode.Width = 84; randomMode.Click += delegate { ChangeMode("random"); };
            themeButtons.Controls.Add(lightMode); themeButtons.Controls.Add(darkMode); themeButtons.Controls.Add(randomMode);
            heading.Controls.Add(themeButtons); themeButtons.BringToFront();
            content.Controls.Add(heading);
            var searchBox = new SoftPanel { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 0, 10), Padding = new Padding(14, 10, 14, 8) };
            search.BorderStyle = BorderStyle.None; search.Dock = DockStyle.Fill; search.BackColor = Theme.Surface;
            search.AccessibleName = "Search your clipboard"; search.Font = new Font("Segoe UI", 11);
            search.HandleCreated += delegate { SendMessage(search.Handle, 0x1501, new IntPtr(1), "Search your clipboard..."); };
            search.TextChanged += delegate { RefreshList(null); }; searchBox.Controls.Add(search); content.Controls.Add(searchBox);
            var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, Margin = new Padding(0), Padding = new Padding(0, 2, 0, 0) };
            var copy = new SoftButton { Text = "Copy selected", Primary = true, Width = 144 }; copy.Click += delegate { CopySelected(); }; actions.Controls.Add(copy);
            pin.Text = "Pin"; pin.Width = 88; pin.Click += delegate { TogglePin(); }; actions.Controls.Add(pin);
            var delete = new SoftButton { Text = "Delete", Danger = true, Width = 88 }; delete.Click += delegate { DeleteSelected(); }; actions.Controls.Add(delete);
            var clear = new SoftButton { Text = "Clear", Width = 96 };
            tips.SetToolTip(clear, "Delete unpinned items only. Pinned items will be kept.");
            clear.Click += delegate { ClearUnpinned(); }; actions.Controls.Add(clear);
            var backup = new SoftButton { Text = "Backup", Width = 88 }; backup.Click += delegate { CreateBackup(); }; actions.Controls.Add(backup);
            var restore = new SoftButton { Text = "Restore", Width = 88 }; restore.Click += delegate { RestoreBackup(); }; actions.Controls.Add(restore);
            tips.SetToolTip(backup, "Save a password-protected backup of all clips and pins.");
            tips.SetToolTip(restore, "Restore clips from a Copybara backup without deleting current clips."); content.Controls.Add(actions);
            totals.Dock = DockStyle.Fill; totals.ForeColor = Theme.Muted; totals.Font = new Font("Segoe UI", 9); content.Controls.Add(totals);
            var split = new SplitContainer { Dock = DockStyle.Fill, Size = new Size(900, 470), SplitterDistance = 415, SplitterWidth = 12, BackColor = Theme.Background, Margin = new Padding(0) };
            split.Panel1MinSize = 220; split.Panel2MinSize = 220;
            var collection = new SoftPanel { Dock = DockStyle.Fill, Padding = new Padding(8) };
            list.Dock = DockStyle.Fill; list.BorderStyle = BorderStyle.None; list.BackColor = Theme.Background; list.IntegralHeight = false;
            list.DrawMode = DrawMode.OwnerDrawFixed; list.ItemHeight = 88; list.DrawItem += DrawClip;
            list.SelectedIndexChanged += delegate { ShowPreview(); }; list.DoubleClick += delegate { CopySelected(); };
            collection.Controls.Add(list); split.Panel1.Controls.Add(collection);
            var detail = new SoftPanel { Dock = DockStyle.Fill, Padding = new Padding(18) };
            var detailLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, BackColor = Theme.Surface };
            detailLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 52)); detailLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); detail.Controls.Add(detailLayout);
            previewMeta.Text = "PREVIEW"; previewMeta.Dock = DockStyle.Fill; previewMeta.BackColor = Theme.Surface; previewMeta.ForeColor = Theme.Muted; previewMeta.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            detailLayout.Controls.Add(previewMeta);
            var previewHost = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Surface };
            preview.Multiline = true; preview.ReadOnly = true; preview.BorderStyle = BorderStyle.None; preview.ScrollBars = ScrollBars.Vertical; preview.WordWrap = true; preview.Dock = DockStyle.Fill; preview.BackColor = Theme.Surface; preview.ForeColor = Theme.Ink; preview.Font = new Font("Segoe UI", 12);
            picture.Dock = DockStyle.Fill; picture.SizeMode = PictureBoxSizeMode.Zoom; picture.BackColor = Theme.Surface;
            previewHost.Controls.Add(preview); previewHost.Controls.Add(picture); detailLayout.Controls.Add(previewHost); split.Panel2.Controls.Add(detail); content.Controls.Add(split);
            status.Dock = DockStyle.Fill; status.ForeColor = Theme.Muted; status.Font = new Font("Segoe UI", 9); status.AutoEllipsis = true; status.Padding = new Padding(0, 12, 0, 0); content.Controls.Add(status);
            var menu = new ContextMenuStrip();
            menu.Items.Add("Open Copybara", null, delegate { Reveal(); });
            menu.Items.Add("Pause / resume saving", null, delegate { TogglePause(); });
            menu.Items.Add("Quit", null, delegate { quitting = true; Close(); });
            tray.Icon = Icon; tray.Text = "Copybara - saving locally"; tray.ContextMenuStrip = menu; tray.Visible = true;
            tray.DoubleClick += delegate { Reveal(); };
            capture.Interval = 200; capture.Tick += delegate { TryCapture(); };
            Directory.CreateDirectory(folder);
            int unreadable = 0;
            foreach (string path in Directory.GetFiles(folder, "*.clip")) {
                try { Clip item; Read(path, out item); if (item.Id != Path.GetFileNameWithoutExtension(path)) throw new InvalidDataException(); clips.Add(item); }
                catch { unreadable++; }
            }
            ApplyCurrentTheme();
            RefreshList(null);
            status.Text = unreadable == 0 ? "Ready. Copy text or an image to begin. Search above; double-click an item to copy it." : unreadable + " saved files could not be read. They have been left untouched.";
            FormClosing += delegate(object sender, FormClosingEventArgs e) {
                if (!quitting && e.CloseReason == CloseReason.UserClosing) { e.Cancel = true; Hide(); tray.ShowBalloonTip(2500, "Copybara is still saving", "Open with Ctrl + Alt + V or the tray icon. Use Quit to stop.", ToolTipIcon.Info); }
            };
            FormClosed += delegate { capture.Stop(); if (listener) RemoveClipboardFormatListener(Handle); if (shortcut) UnregisterHotKey(Handle, 1); tray.Visible = false; tray.Dispose(); tips.Dispose(); if (picture.Image != null) picture.Image.Dispose(); };
        }
        protected override void OnHandleCreated(EventArgs e) {
            base.OnHandleCreated(e);
            lastSequence = GetClipboardSequenceNumber();
            listener = AddClipboardFormatListener(Handle);
            shortcut = RegisterHotKey(Handle, 1, 0x4003, (uint)Keys.V);
        }
        protected override void OnShown(EventArgs e) {
            base.OnShown(e);
            // The first launch of this update enables the requested sign-in startup.
            if (!File.Exists(startupChoice)) {
                try { SetStartup(true); RefreshStartupCheckbox(); }
                catch (Exception ex) { MessageBox.Show(this, "Automatic startup could not be enabled: " + ex.Message, "Copybara"); }
            } else if (!background && startup.Checked) {
                // Keep the registered executable path current when updating the app.
                try { SetStartup(true); }
                catch (Exception ex) { status.Text = "Startup path could not be updated: " + ex.Message; }
            }
            if (background) BeginInvoke(new Action(delegate { Hide(); }));
            if (!listener) MessageBox.Show(this, "Windows could not start clipboard monitoring. Please quit and reopen the app.", "Copybara");
            if (!shortcut) MessageBox.Show(this, "Ctrl + Alt + V is already in use. You can still open Copybara from its tray icon.", "Copybara");
        }
        protected override void WndProc(ref Message m) {
            if (m.Msg == 0x0312) Reveal();
            if (m.Msg == 0x031D && !paused && captureSuspensions == 0) { attempts = 0; capture.Start(); }
            base.WndProc(ref m);
        }
        void Reveal() { ShowInTaskbar = true; Show(); WindowState = FormWindowState.Normal; Activate(); search.Focus(); }
        void RefreshStartupCheckbox() {
            // Avoid triggering writes while reflecting the actual Windows setting.
            try {
                using (var key = Registry.CurrentUser.OpenSubKey(RunKey)) {
                    bool enabled = key != null && key.GetValue(RunName) != null;
                    reflectingStartup = true;
                    try { startup.Checked = enabled; } finally { reflectingStartup = false; }
                }
            } catch { }
        }
        void SetStartup(bool enabled) {
            using (var key = Registry.CurrentUser.CreateSubKey(RunKey)) {
                if (key == null) throw new IOException("Windows startup settings are unavailable.");
                if (enabled) {
                    string command = "\"" + Application.ExecutablePath + "\" --background";
                    if (command.Length > 260) throw new IOException("Move the app to a folder with a shorter path, then enable startup again.");
                    key.SetValue(RunName, command, RegistryValueKind.String);
                }
                else key.DeleteValue(RunName, false);
            }
            File.WriteAllText(startupChoice, enabled ? "enabled" : "disabled");
            status.Text = enabled ? "Automatic startup enabled. Copybara will start in the tray when you sign in." : "Automatic startup disabled.";
        }
        void ChangeMode(string choice) {
            if (choice == "random") Theme.Randomize(); else Theme.Choose(choice);
            ApplyCurrentTheme();
        }
        void ApplyCurrentTheme() {
            lightMode.Active = !Theme.RandomSeed.HasValue && !Theme.Dark;
            darkMode.Active = !Theme.RandomSeed.HasValue && Theme.Dark;
            randomMode.Active = Theme.RandomSeed.HasValue;
            ApplyPalette(this, false);
            try { Directory.CreateDirectory(Path.GetDirectoryName(themeFile)); File.WriteAllText(themeFile, Theme.Choice); }
            catch (Exception ex) { status.Text = "Theme changed, but could not save the preference: " + ex.Message; }
        }
        void ApplyPalette(Control control, bool onSurface) {
            bool surface = onSurface || control is SoftPanel;
            bool input = control is TextBox || control is PictureBox;
            bool high = SystemInformation.HighContrast;
            control.BackColor = high ? SystemColors.Window : (control.Tag as string) == "mascot" ? Theme.Background : control is SoftPanel || control is ListBox || control is SoftButton ? Theme.Background : surface || input ? Theme.Surface : Theme.Background;
            control.ForeColor = high ? SystemColors.WindowText : control is Label && (control == totals || control == status || control == previewMeta || control.Font.Size < 10 || control.Font.Style == FontStyle.Regular) ? Theme.Muted : Theme.Ink;
            foreach (Control child in control.Controls) ApplyPalette(child, surface);
            control.Invalidate(true);
        }
        void AddFilter(TableLayoutPanel parent, string key, string label) {
            var button = new SoftButton { Text = key == "text" || key == "image" ? "" : label, IconKind = key == "text" || key == "image" ? key : null, AccessibleName = label, Dock = DockStyle.Fill, Active = key == currentFilter, Margin = new Padding(0, 0, 0, 8) };
            tips.SetToolTip(button, label);
            button.Tag = label; button.Click += delegate {
                currentFilter = key;
                foreach (var entry in filters) { entry.Value.Active = entry.Key == key; entry.Value.Invalidate(); }
                RefreshList(null);
            }; filters.Add(key, button); parent.Controls.Add(button);
        }
        void DrawClip(object sender, DrawItemEventArgs e) {
            if (e.Index < 0 || e.Index >= list.Items.Count) return;
            Clip c = (Clip)list.Items[e.Index]; bool selected = (e.State & DrawItemState.Selected) != 0;
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using (var backgroundBrush = new SolidBrush(Theme.Background)) e.Graphics.FillRectangle(backgroundBrush, e.Bounds);
            Color fill = selected ? Theme.Selection : c.Pinned ? Theme.Pinned : Theme.Surface;
            Color ink = selected ? Color.FromArgb(39, 24, 8) : Theme.Ink, muted = selected ? Color.FromArgb(64, 35, 9) : Theme.Muted;
            if (SystemInformation.HighContrast) { fill = selected ? SystemColors.Highlight : SystemColors.Window; ink = selected ? SystemColors.HighlightText : SystemColors.WindowText; muted = ink; }
            var card = new Rectangle(e.Bounds.X + 2, e.Bounds.Y + 3, Math.Max(1, e.Bounds.Width - 5), e.Bounds.Height - 8);
            using (var path = Theme.Round(card, 9)) using (var brush = new SolidBrush(fill))
            using (var border = new Pen(selected ? Color.FromArgb(184, 60, 0) : c.Pinned ? (Theme.Dark ? Color.FromArgb(150, 127, 62) : Color.FromArgb(230, 208, 125)) : Theme.Border, selected ? 2 : 1)) { e.Graphics.FillPath(brush, path); e.Graphics.DrawPath(border, path); }
            string label = c.Label.Replace("\r", " ").Replace("\n", " ").Replace("\t", " ");
            if (label.Length > 300) label = label.Substring(0, 300);
            var title = new Rectangle(card.X + 14, card.Y + 11, Math.Max(0, card.Width - 28), 27);
            TextRenderer.DrawText(e.Graphics, label, e.Font, title, ink, TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix | TextFormatFlags.VerticalCenter);
            Theme.DrawIcon(e.Graphics, c.Kind, new Rectangle(card.X + 14, card.Y + 46, 16, 16), muted);
            string meta = c.Created.ToLocalTime().ToString("dd MMM, HH:mm");
            using (var small = new Font("Segoe UI", 8.5f)) TextRenderer.DrawText(e.Graphics, meta, small, new Rectangle(card.X + 40, card.Y + 44, Math.Max(0, card.Width - 54), 21), muted, TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
            if ((e.State & DrawItemState.Focus) != 0) ControlPaint.DrawFocusRectangle(e.Graphics, new Rectangle(card.X + 5, card.Y + 5, card.Width - 10, card.Height - 10), ink, fill);
        }
        Clip Selected() { return list.SelectedItem as Clip; }
        string FileFor(Clip c) { return Path.Combine(folder, c.Id + ".clip"); }
        void RefreshList(string id) {
            if (id == null && Selected() != null) id = Selected().Id;
            string query = search.Text;
            list.BeginUpdate(); list.Items.Clear();
            foreach (Clip c in clips.Where(c => c.Label.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 &&
                (currentFilter == "all" || currentFilter == "pinned" && c.Pinned || currentFilter == c.Kind)).OrderBy(c => c.Pinned).ThenByDescending(c => c.Created)) list.Items.Add(c);
            if (id != null) for (int i = 0; i < list.Items.Count; i++) if (((Clip)list.Items[i]).Id == id) { list.SelectedIndex = i; break; }
            list.EndUpdate(); ShowPreview();
            totals.Text = list.Items.Count + " shown   /   " + clips.Count + " saved   /   " + clips.Count(c => c.Pinned) + " pinned below";
            foreach (var entry in filters) {
                int count = entry.Key == "all" ? clips.Count : entry.Key == "pinned" ? clips.Count(c => c.Pinned) : clips.Count(c => c.Kind == entry.Key);
                entry.Value.Text = (entry.Value.IconKind == null ? entry.Value.Tag.ToString() + "   " : "") + count;
                entry.Value.AccessibleName = entry.Value.Tag.ToString() + ": " + count;
                entry.Value.Invalidate();
            }
            Text = "Copybara - " + clips.Count + " saved";
        }
        static string Hash(byte[] bytes) { using (var sha = SHA256.Create()) return Convert.ToBase64String(sha.ComputeHash(bytes)); }
        void Save(Clip c, byte[] payload) { SaveAt(c, payload, FileFor(c)); }
        void SaveAt(Clip c, byte[] payload, string target) {
            byte[] plain;
            using (var ms = new MemoryStream()) {
                using (var w = new BinaryWriter(ms, Encoding.UTF8)) {
                    w.Write(1); w.Write(c.Id); w.Write(c.Kind); w.Write(c.Created.ToBinary()); w.Write(c.Pinned); w.Write(payload.Length); w.Write(payload); w.Flush(); plain = ms.ToArray();
                }
            }
            byte[] encrypted = ProtectedData.Protect(plain, null, DataProtectionScope.CurrentUser);
            string temp = target + ".tmp";
            try {
                using (var fs = new FileStream(temp, FileMode.Create, FileAccess.Write, FileShare.None)) { fs.Write(encrypted, 0, encrypted.Length); fs.Flush(true); }
                if (File.Exists(target)) File.Replace(temp, target, null); else File.Move(temp, target);
            } finally { if (File.Exists(temp)) File.Delete(temp); Array.Clear(plain, 0, plain.Length); }
        }
        static byte[] Read(string path, out Clip c) {
            if (new FileInfo(path).Length > MaxPayload + 1048576) throw new InvalidDataException("File too large");
            byte[] plain = ProtectedData.Unprotect(File.ReadAllBytes(path), null, DataProtectionScope.CurrentUser);
            try {
                using (var ms = new MemoryStream(plain)) using (var r = new BinaryReader(ms, Encoding.UTF8)) {
                    if (r.ReadInt32() != 1) throw new InvalidDataException("Unknown format");
                    c = new Clip { Id = r.ReadString(), Kind = r.ReadString(), Created = DateTime.FromBinary(r.ReadInt64()), Pinned = r.ReadBoolean() };
                    Guid parsed; if (!Guid.TryParseExact(c.Id, "N", out parsed)) throw new InvalidDataException();
                    if (c.Kind != "text" && c.Kind != "image") throw new InvalidDataException();
                    int size = r.ReadInt32(); if (size < 0 || size > MaxPayload || size != ms.Length - ms.Position) throw new InvalidDataException();
                    byte[] payload = r.ReadBytes(size); c.Hash = Hash(payload);
                    c.Label = c.Kind == "text" ? Encoding.UTF8.GetString(payload) : c.Created.ToLocalTime().ToString("dd MMM yyyy HH:mm:ss") + " - PNG image";
                    return payload;
                }
            } finally { Array.Clear(plain, 0, plain.Length); }
        }
        void TryCapture() {
            if (paused || captureSuspensions > 0) { capture.Stop(); return; }
            uint seq = GetClipboardSequenceNumber();
            if (seq == lastSequence) { capture.Stop(); return; }
            try {
                // Honour standard clipboard privacy flags used by password managers.
                var data = Clipboard.GetDataObject();
                if (data == null) throw new ExternalException("Clipboard busy");
                if (data.GetDataPresent("ExcludeClipboardContentFromMonitorProcessing") || data.GetDataPresent("Clipboard Viewer Ignore")) { lastSequence = seq; capture.Stop(); return; }
                string kind; byte[] payload; string label;
                if (Clipboard.ContainsImage()) {
                    using (Image img = Clipboard.GetImage()) {
                        if (img == null) throw new ExternalException("Image unavailable");
                        if ((long)img.Width * img.Height > 40000000) { status.Text = "Skipped image larger than 40 megapixels."; lastSequence = seq; capture.Stop(); return; }
                        using (var ms = new MemoryStream()) { img.Save(ms, ImageFormat.Png); payload = ms.ToArray(); }
                        label = img.Width + " x " + img.Height + " pixels - " + DateTime.Now.ToString("dd MMM HH:mm:ss");
                    }
                    kind = "image";
                } else if (Clipboard.ContainsText()) { kind = "text"; label = Clipboard.GetText(); payload = Encoding.UTF8.GetBytes(label); }
                else { lastSequence = seq; capture.Stop(); return; }
                if (GetClipboardSequenceNumber() != seq) return;
                if (payload.Length > MaxPayload) { status.Text = "Skipped item larger than 25 MB."; lastSequence = seq; capture.Stop(); return; }
                string hash = Hash(payload);
                if (!clips.Any(c => c.Kind == kind && c.Hash == hash)) {
                    var c = new Clip { Id = Guid.NewGuid().ToString("N"), Kind = kind, Label = label, Hash = hash, Created = DateTime.UtcNow };
                    Save(c, payload); clips.Add(c); RefreshList(null);
                    status.Text = "Saved " + kind + " locally. " + clips.Count + " items. No automatic deletion.";
                }
                lastSequence = seq; capture.Stop();
            } catch (Exception ex) {
                if (++attempts >= 10) { capture.Stop(); status.Text = "Could not save clipboard item: " + ex.Message + " Copy it again to retry."; }
            }
        }
        void ShowPreview() {
            if (picture.Image != null) { var old = picture.Image; picture.Image = null; old.Dispose(); }
            Clip c = Selected(); pin.Text = c != null && c.Pinned ? "Unpin" : "Pin";
            previewMeta.IconKind = c == null ? null : c.Kind;
            previewMeta.Text = c == null ? "PREVIEW" : c.Created.ToLocalTime().ToString("dd MMM yyyy, HH:mm") + (c.Pinned ? "\r\nPinned" : "");
            previewMeta.AccessibleName = c == null ? "Preview" : (c.Kind == "image" ? "Image preview" : "Text preview"); previewMeta.Invalidate();
            preview.Visible = c == null || c.Kind == "text"; picture.Visible = !preview.Visible;
            preview.Text = c == null ? "A place for your next idea.\r\n\r\nSelect a saved item to see it here.\r\n\r\nTo use it, click Copy selected, return to your document and press Ctrl + V.\r\n\r\nPinned items stay yellow below the rest. Your selected item is bright orange." : "";
            if (c == null) return;
            try {
                Clip saved; byte[] bytes = Read(FileFor(c), out saved);
                if (c.Kind == "text") preview.Text = Encoding.UTF8.GetString(bytes);
                else using (var ms = new MemoryStream(bytes)) using (var img = Image.FromStream(ms)) picture.Image = new Bitmap(img);
            } catch (Exception ex) { status.Text = "Cannot preview: " + ex.Message; }
        }
        void CopySelected() {
            Clip c = Selected(); if (c == null) return;
            try {
                Clip saved; byte[] bytes = Read(FileFor(c), out saved);
                if (c.Kind == "text") Clipboard.SetText(Encoding.UTF8.GetString(bytes));
                else using (var ms = new MemoryStream(bytes)) using (var img = Image.FromStream(ms)) Clipboard.SetImage(img);
                lastSequence = GetClipboardSequenceNumber();
                status.Text = "Copied. Return to your document and press Ctrl + V.";
            } catch (Exception ex) { MessageBox.Show(this, "Cannot copy: " + ex.Message); }
        }
        void TogglePin() {
            Clip c = Selected(); if (c == null) return;
            try { Clip saved; byte[] payload = Read(FileFor(c), out saved); saved.Pinned = !c.Pinned; Save(saved, payload); c.Pinned = saved.Pinned; RefreshList(c.Id); }
            catch (Exception ex) { MessageBox.Show(this, "Cannot change pin: " + ex.Message); }
        }
        void DeleteSelected() {
            Clip c = Selected(); if (c == null) return;
            if (MessageBox.Show(this, "Delete this saved item?", "Copybara", MessageBoxButtons.YesNo) != DialogResult.Yes) return;
            try { File.Delete(FileFor(c)); clips.Remove(c); RefreshList(null); status.Text = "Deleted from this app. The current Windows clipboard is unchanged."; }
            catch (Exception ex) { MessageBox.Show(this, "Cannot delete: " + ex.Message); }
        }
        void ClearUnpinned() {
            if (MessageBox.Show(this, "Delete all unpinned saved items? Pinned items will stay.", "Copybara", MessageBoxButtons.YesNo) != DialogResult.Yes) return;
            try { foreach (Clip c in clips.Where(c => !c.Pinned).ToArray()) { File.Delete(FileFor(c)); clips.Remove(c); } }
            catch (Exception ex) { MessageBox.Show(this, "Cleanup stopped: " + ex.Message); }
            RefreshList(null);
        }
        void TogglePause() {
            paused = !paused; capture.Stop(); lastSequence = GetClipboardSequenceNumber();
            pause.Text = paused ? "Resume saving" : "Pause saving";
            tray.Text = paused ? "Copybara - paused" : "Copybara - saving locally";
            status.Text = paused ? "Paused. New copies will not be saved." : "Saving resumed. Copy something to save it.";
        }
    }
    public static class Program {
        [STAThread] public static void Main(string[] args) {
            bool first;
            using (var mutex = new Mutex(true, "Local\\JairoPersonalClipboardV1", out first)) {
                if (!first) { MessageBox.Show("Copybara is already running. Press Ctrl + Alt + V or use its tray icon."); return; }
                Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false);
                try { Application.Run(new Window(args.Contains("--background"))); }
                catch (Exception ex) { MessageBox.Show("Copybara could not start: " + ex.Message); }
            }
        }
    }
}
