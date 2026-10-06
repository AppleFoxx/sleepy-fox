using System;
using System.Drawing;
using System.Windows.Forms;

namespace SleepyFox
{
    /// <summary>
    /// Окно журнала: последние 200 строк, с автообновлением раз в три секунды.
    /// </summary>
    internal sealed class LogForm : Form
    {
        private TextBox logBox;
        private Timer refreshTimer;
        private Button refreshButton;
        private Button openButton;
        private Button closeButton;

        public LogForm()
        {
            BuildUi();
            Reload();

            refreshTimer = new Timer();
            refreshTimer.Interval = 3000;
            refreshTimer.Tick += RefreshTimerTick;
            refreshTimer.Start();
        }

        private void BuildUi()
        {
            SuspendLayout();

            Text = "The Sleepy Fox — журнал";
            ClientSize = new Size(720, 460);
            StartPosition = FormStartPosition.CenterScreen;
            MinimizeBox = false;
            AutoScaleMode = AutoScaleMode.None;

            try
            {
                Icon = FoxIcon.Create();
            }
            catch (Exception)
            {
                // без иконки окно всё равно работает
            }

            logBox = new TextBox();
            logBox.Multiline = true;
            logBox.ReadOnly = true;
            logBox.ScrollBars = ScrollBars.Both;
            logBox.WordWrap = false;
            logBox.Font = new Font("Consolas", 9f, FontStyle.Regular, GraphicsUnit.Point);
            logBox.Location = new Point(12, 12);
            logBox.Size = new Size(696, 400);
            logBox.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            Controls.Add(logBox);

            refreshButton = new Button();
            refreshButton.Text = "Обновить";
            refreshButton.Location = new Point(432, 422);
            refreshButton.Size = new Size(88, 28);
            refreshButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            refreshButton.Click += RefreshButtonClick;
            Controls.Add(refreshButton);

            openButton = new Button();
            openButton.Text = "Открыть файл";
            openButton.Location = new Point(526, 422);
            openButton.Size = new Size(88, 28);
            openButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            openButton.Click += OpenButtonClick;
            Controls.Add(openButton);

            closeButton = new Button();
            closeButton.Text = "Закрыть";
            closeButton.Location = new Point(620, 422);
            closeButton.Size = new Size(88, 28);
            closeButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            closeButton.DialogResult = DialogResult.Cancel;
            Controls.Add(closeButton);

            CancelButton = closeButton;

            ResumeLayout(false);
        }

        private void Reload()
        {
            if (logBox == null)
            {
                return;
            }

            string text = Logger.ReadTail(200);
            if (logBox.Text == text)
            {
                return;
            }

            logBox.Text = text;
            logBox.SelectionStart = logBox.TextLength;
            logBox.ScrollToCaret();
        }

        private void RefreshTimerTick(object sender, EventArgs e)
        {
            Reload();
        }

        private void RefreshButtonClick(object sender, EventArgs e)
        {
            Reload();
        }

        private void OpenButtonClick(object sender, EventArgs e)
        {
            try
            {
                if (!System.IO.File.Exists(AppPaths.LogFile))
                {
                    MessageBox.Show(this, "Журнала пока нет.", "The Sleepy Fox",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                System.Diagnostics.Process.Start("notepad.exe", "\"" + AppPaths.LogFile + "\"");
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Не удалось открыть журнал: " + ex.Message, "The Sleepy Fox",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            if (refreshTimer != null)
            {
                refreshTimer.Stop();
                refreshTimer.Dispose();
                refreshTimer = null;
            }

            base.OnFormClosed(e);
        }
    }
}
