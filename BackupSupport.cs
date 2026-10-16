using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace PersonalClipboard {
    public sealed class PasswordPrompt : Form {
        readonly TextBox password = new TextBox(), confirmation = new TextBox();
        public string Password { get { return password.Text; } }
        public PasswordPrompt(bool creating) {
            Text = creating ? "Back up Copybara" : "Restore Copybara backup";
            ClientSize = new Size(440, creating ? 270 : 225); FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false; MinimizeBox = false; StartPosition = FormStartPosition.CenterParent;
            BackColor = Theme.Background; ForeColor = Theme.Ink; Font = new Font("Segoe UI", 10);
            var heading = new Label { Text = creating ? "Choose a backup password" : "Enter your backup password", AutoSize = true, Location = new Point(22, 20), Font = new Font("Segoe UI", 13, FontStyle.Bold) }; Controls.Add(heading);
            Controls.Add(new Label { Text = creating ? "Keep this password safe: it is needed to restore the file." : "Your current clips will be kept. Missing clips will be added.", Location = new Point(22, 55), Width = 398, Height = 42, ForeColor = Theme.Muted });
            password.UseSystemPasswordChar = true; password.MaxLength = 1024; password.AccessibleName = "Backup password";
            password.SetBounds(22, 103, 392, 28); password.BackColor = Theme.Surface; password.ForeColor = Theme.Ink; Controls.Add(password);
            if (creating) {
                Controls.Add(new Label { Text = "Repeat password", Location = new Point(22, 140), AutoSize = true });
                confirmation.UseSystemPasswordChar = true; confirmation.MaxLength = 1024; confirmation.AccessibleName = "Repeat backup password";
                confirmation.SetBounds(22, 162, 392, 28); confirmation.BackColor = Theme.Surface; confirmation.ForeColor = Theme.Ink; Controls.Add(confirmation);
            }
            int y = creating ? 214 : 164;
            var ok = new SoftButton { Text = creating ? "Save backup" : "Restore", Primary = true, Location = new Point(268, y), Width = 146 };
            var cancel = new SoftButton { Text = "Cancel", Location = new Point(164, y), Width = 96, DialogResult = DialogResult.Cancel };
            ok.Click += delegate {
                if (creating && password.Text.Length < 10) { MessageBox.Show(this, "Use at least 10 characters for the backup password."); return; }
                if (creating && password.Text != confirmation.Text) { MessageBox.Show(this, "The two passwords do not match."); return; }
                if (password.Text.Length == 0) return;
                DialogResult = DialogResult.OK;
            };
            Controls.Add(ok); Controls.Add(cancel); AcceptButton = ok; CancelButton = cancel;
            Shown += delegate { password.Focus(); };
        }
    }
    // Restrict decryption to ciphertext, excluding the authentication tag at EOF.
    public sealed class LimitedReadStream : Stream {
        readonly Stream inner; readonly long length; long remaining;
        public LimitedReadStream(Stream source, long count) { inner = source; length = remaining = count; }
        public override bool CanRead { get { return true; } }
        public override bool CanSeek { get { return false; } }
        public override bool CanWrite { get { return false; } }
        public override long Length { get { return length; } }
        public override long Position { get { return length - remaining; } set { throw new NotSupportedException(); } }
        public override int Read(byte[] buffer, int offset, int count) {
            if (remaining == 0) return 0;
            int n = inner.Read(buffer, offset, (int)Math.Min(count, remaining));
            if (n == 0) throw new EndOfStreamException("Backup is incomplete."); remaining -= n; return n;
        }
        public override void Flush() { }
        public override long Seek(long o, SeekOrigin origin) { throw new NotSupportedException(); }
        public override void SetLength(long value) { throw new NotSupportedException(); }
        public override void Write(byte[] buffer, int offset, int count) { throw new NotSupportedException(); }
    }
    public partial class Window {
        const int BackupIterations = 200000;
        static readonly byte[] BackupMagic = Encoding.ASCII.GetBytes("CPYBAK01");
        static byte[] RandomBytes(int count) { var b = new byte[count]; using (var rng = RandomNumberGenerator.Create()) rng.GetBytes(b); return b; }
        static byte[] MacPrefix(Stream file, byte[] key, long count) {
            file.Position = 0;
            using (var hmac = new HMACSHA256(key)) {
                byte[] buffer = new byte[65536];
                while (count > 0) {
                    int n = file.Read(buffer, 0, (int)Math.Min(buffer.Length, count));
                    if (n <= 0) throw new EndOfStreamException("Backup is incomplete.");
                    hmac.TransformBlock(buffer, 0, n, buffer, 0); count -= n;
                }
                hmac.TransformFinalBlock(new byte[0], 0, 0); return hmac.Hash;
            }
        }
        static bool SameBytes(byte[] a, byte[] b) {
            if (a.Length != b.Length) return false;
            int difference = 0; for (int i = 0; i < a.Length; i++) difference |= a[i] ^ b[i]; return difference == 0;
        }
        static void DeriveKeys(string password, byte[] salt, out byte[] encryption, out byte[] authentication) {
            using (var derive = new Rfc2898DeriveBytes(password, salt, BackupIterations)) { encryption = derive.GetBytes(32); authentication = derive.GetBytes(32); }
        }
        sealed class CapturePause : IDisposable {
            readonly Window owner;
            public CapturePause(Window window) {
                owner = window;
                if (!owner.paused && owner.captureSuspensions == 0) owner.TryCapture();
                owner.captureSuspensions++; owner.capture.Stop();
            }
            public void Dispose() { owner.captureSuspensions--; owner.lastSequence = GetClipboardSequenceNumber(); }
        }
        void CreateBackup() {
            using (var collectionPause = new CapturePause(this))
            using (var dialog = new SaveFileDialog { Title = "Save Copybara backup", Filter = "Copybara encrypted backup|*.copybara", DefaultExt = "copybara", AddExtension = true, FileName = "Copybara-" + DateTime.Now.ToString("yyyy-MM-dd-HHmm") + ".copybara", OverwritePrompt = true }) {
                if (dialog.ShowDialog(this) != DialogResult.OK) return;
                using (var prompt = new PasswordPrompt(true)) {
                    if (prompt.ShowDialog(this) != DialogResult.OK) return;
                    string temporary = dialog.FileName + "." + Guid.NewGuid().ToString("N") + ".tmp";
                    byte[] encryption = null, authentication = null; bool wasPaused = paused;
                    try {
                        paused = true; capture.Stop(); UseWaitCursor = true; status.Text = "Creating encrypted backup..."; status.Refresh();
                        byte[] salt = RandomBytes(32), iv = RandomBytes(16);
                        DeriveKeys(prompt.Password, salt, out encryption, out authentication);
                        if (clips.Count > 100000) throw new InvalidOperationException("This backup format supports up to 100,000 saved clips.");
                        using (var file = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None)) {
                            file.Write(BackupMagic, 0, 8); file.Write(salt, 0, salt.Length); file.Write(iv, 0, iv.Length);
                            byte[] iterationBytes = BitConverter.GetBytes(BackupIterations); file.Write(iterationBytes, 0, 4);
                            using (var aes = Aes.Create()) {
                                aes.Key = encryption; aes.IV = iv; aes.Mode = CipherMode.CBC; aes.Padding = PaddingMode.PKCS7;
                                using (var crypto = new CryptoStream(file, aes.CreateEncryptor(), CryptoStreamMode.Write))
                                using (var writer = new BinaryWriter(crypto, Encoding.UTF8)) {
                                    writer.Write(clips.Count); writer.Write(Theme.Choice);
                                    foreach (Clip c in clips) {
                                        Clip item; byte[] payload = Read(FileFor(c), out item);
                                        try { writer.Write(item.Kind); writer.Write(item.Created.ToBinary()); writer.Write(item.Pinned); writer.Write(payload.Length); writer.Write(payload); }
                                        finally { Array.Clear(payload, 0, payload.Length); }
                                    }
                                    writer.Write(0x4342414B); writer.Flush(); crypto.FlushFinalBlock();
                                }
                            }
                        }
                        using (var file = new FileStream(temporary, FileMode.Open, FileAccess.ReadWrite, FileShare.None)) {
                            long length = file.Length; byte[] mac = MacPrefix(file, authentication, length); file.Position = length; file.Write(mac, 0, mac.Length); file.Flush(true);
                        }
                        if (File.Exists(dialog.FileName)) File.Replace(temporary, dialog.FileName, null); else File.Move(temporary, dialog.FileName);
                        status.Text = "Backup saved: " + clips.Count + " clips, including pins and theme.";
                    } catch (Exception ex) { MessageBox.Show(this, "Could not create backup: " + ex.Message, "Copybara"); status.Text = "Backup was not saved. Your clips are unchanged."; }
                    finally {
                        if (encryption != null) Array.Clear(encryption, 0, encryption.Length);
                        if (authentication != null) Array.Clear(authentication, 0, authentication.Length);
                        try { if (File.Exists(temporary)) File.Delete(temporary); } catch { }
                        paused = wasPaused; lastSequence = GetClipboardSequenceNumber(); UseWaitCursor = false;
                    }
                }
            }
        }
        void RestoreBackup() {
            using (var collectionPause = new CapturePause(this))
            using (var dialog = new OpenFileDialog { Title = "Restore Copybara backup", Filter = "Copybara encrypted backup|*.copybara", CheckFileExists = true }) {
                if (dialog.ShowDialog(this) != DialogResult.OK) return;
                using (var prompt = new PasswordPrompt(false)) {
                    if (prompt.ShowDialog(this) != DialogResult.OK) return;
                    string stage = Path.Combine(Path.GetDirectoryName(folder), "Restore-" + Guid.NewGuid().ToString("N"));
                    byte[] encryption = null, authentication = null; bool wasPaused = paused; int imported = 0;
                    try {
                        paused = true; capture.Stop(); UseWaitCursor = true; status.Text = "Checking and restoring backup..."; status.Refresh();
                        Directory.CreateDirectory(stage);
                        var pending = new Dictionary<string, Clip>(); string themeChoice;
                        using (var file = new FileStream(dialog.FileName, FileMode.Open, FileAccess.Read, FileShare.Read)) {
                            if (file.Length < 108 || (file.Length - 92) % 16 != 0) throw new InvalidDataException("Not a valid Copybara backup.");
                            var header = new BinaryReader(file, Encoding.UTF8, true);
                            if (!SameBytes(header.ReadBytes(8), BackupMagic)) throw new InvalidDataException("Not a Copybara backup.");
                            byte[] salt = header.ReadBytes(32), iv = header.ReadBytes(16);
                            if (header.ReadInt32() != BackupIterations) throw new InvalidDataException("Unsupported backup version.");
                            DeriveKeys(prompt.Password, salt, out encryption, out authentication);
                            byte[] expected = MacPrefix(file, authentication, file.Length - 32); file.Position = file.Length - 32;
                            if (!SameBytes(expected, header.ReadBytes(32))) throw new InvalidDataException("The password is incorrect, or the backup is damaged.");
                            file.Position = 60;
                            using (var aes = Aes.Create()) {
                                aes.Key = encryption; aes.IV = iv; aes.Mode = CipherMode.CBC; aes.Padding = PaddingMode.PKCS7;
                                using (var limited = new LimitedReadStream(file, file.Length - 92))
                                using (var crypto = new CryptoStream(limited, aes.CreateDecryptor(), CryptoStreamMode.Read))
                                using (var reader = new BinaryReader(crypto, Encoding.UTF8)) {
                                    int count = reader.ReadInt32(); if (count < 0 || count > 100000) throw new InvalidDataException("Invalid clip count.");
                                    themeChoice = reader.ReadString();
                                    for (int i = 0; i < count; i++) {
                                        string kind = reader.ReadString(); DateTime created = DateTime.FromBinary(reader.ReadInt64()); bool pinned = reader.ReadBoolean(); int size = reader.ReadInt32();
                                        if ((kind != "text" && kind != "image") || size < 0 || size > MaxPayload) throw new InvalidDataException("Invalid clip in backup.");
                                        byte[] payload = reader.ReadBytes(size); if (payload.Length != size) throw new EndOfStreamException();
                                        try {
                                            string hash = Hash(payload), identity = kind + ":" + hash; Clip staged;
                                            if (pending.TryGetValue(identity, out staged)) {
                                                if (pinned && !staged.Pinned) { staged.Pinned = true; SaveAt(staged, payload, Path.Combine(stage, staged.Id + ".clip")); } continue;
                                            }
                                            Clip existing = clips.FirstOrDefault(c => c.Kind == kind && c.Hash == hash);
                                            if (existing != null && (!pinned || existing.Pinned)) continue;
                                            var cnew = new Clip { Id = existing == null ? Guid.NewGuid().ToString("N") : existing.Id, Kind = kind, Created = existing == null ? created : existing.Created, Pinned = pinned, Hash = hash,
                                                Label = kind == "text" ? Encoding.UTF8.GetString(payload) : created.ToLocalTime().ToString("dd MMM yyyy HH:mm:ss") + " - PNG image" };
                                            SaveAt(cnew, payload, Path.Combine(stage, cnew.Id + ".clip")); pending.Add(identity, cnew);
                                        } finally { Array.Clear(payload, 0, payload.Length); }
                                    }
                                    if (reader.ReadInt32() != 0x4342414B || crypto.ReadByte() != -1) throw new InvalidDataException("Invalid backup ending.");
                                }
                            }
                        }
                        // All authentication and parsing checks passed. Staging contains only DPAPI-encrypted files.
                        foreach (Clip c in pending.Values) {
                            string from = Path.Combine(stage, c.Id + ".clip"), target = FileFor(c);
                            if (File.Exists(target)) File.Replace(from, target, null); else File.Move(from, target);
                            clips.RemoveAll(existing => existing.Id == c.Id); clips.Add(c); imported++;
                        }
                        Theme.Choose(themeChoice); ApplyCurrentTheme(); RefreshList(null);
                        status.Text = "Restore complete: " + imported + " clips added or pins restored. Your other clips were kept.";
                    } catch (Exception ex) {
                        MessageBox.Show(this, "Could not finish restore: " + ex.Message + (imported > 0 ? "\r\nAlready imported: " + imported + " clips." : "\r\nYour existing history is unchanged."), "Copybara");
                        RefreshList(null); status.Text = "Restore stopped. " + imported + " clips imported.";
                    } finally {
                        if (encryption != null) Array.Clear(encryption, 0, encryption.Length);
                        if (authentication != null) Array.Clear(authentication, 0, authentication.Length);
                        try { if (Directory.Exists(stage)) Directory.Delete(stage, true); } catch { }
                        paused = wasPaused; lastSequence = GetClipboardSequenceNumber(); UseWaitCursor = false;
                    }
                }
            }
        }
    }
}
