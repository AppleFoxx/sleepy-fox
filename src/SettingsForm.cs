using System;
using System.Drawing;
using System.Windows.Forms;

namespace SleepyFox
{
    /// <summary>
    /// Окно настроек. Без дизайнера: все контролы создаются в коде,
    /// координаты заданы вручную, окно не растягивается.
    /// </summary>
    internal sealed class SettingsForm : Form
    {
        private NumericUpDown nightStartBox;
        private NumericUpDown nightEndBox;
        private NumericUpDown idleBox;
        private NumericUpDown delayBox;
        private NumericUpDown intervalBox;
        private RadioButton hibernateRadio;
        private RadioButton suspendRadio;
        private CheckBox pauseMediaBox;
        private CheckBox dryRunBox;
        private CheckBox runAtLogonBox;
        private Button saveButton;
        private Button cancelButton;

        private AppSettings result;

        public SettingsForm(AppSettings current)
        {
            if (current == null)
            {
                throw new ArgumentNullException("current");
            }

            result = current.Clone();
            BuildUi();
            LoadValues(current);
        }

        /// <summary>Заполнено только при закрытии кнопкой «Сохранить».</summary>
        public AppSettings Result
        {
            get { return result; }
        }

        private void BuildUi()
        {
            SuspendLayout();

            Text = "The Sleepy Fox — настройки";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = true;
            StartPosition = FormStartPosition.CenterScreen;
            AutoScaleMode = AutoScaleMode.None;
            ClientSize = new Size(508, 508);
            Font = new Font("Segoe UI", 9f, FontStyle.Regular, GraphicsUnit.Point);

            try
            {
                Icon = FoxIcon.Create();
            }
            catch (Exception)
            {
                // без иконки окно всё равно работает
            }

            // --- ночное окно -------------------------------------------------
            GroupBox nightGroup = MakeGroup("Ночное окно", 12, 10, 484, 88);

            Label nightFromLabel = MakeLabel("ночь с:", 14, 30);
            nightGroup.Controls.Add(nightFromLabel);

            nightStartBox = MakeNumberBox(80, 26, 60, 0, 23);
            nightGroup.Controls.Add(nightStartBox);

            Label nightToLabel = MakeLabel("до:", 156, 30);
            nightGroup.Controls.Add(nightToLabel);

            nightEndBox = MakeNumberBox(190, 26, 60, 0, 23);
            nightGroup.Controls.Add(nightEndBox);

            Label nightHint = MakeLabel(
                "часы 0–23; окно может переходить через полночь, например с 23 до 7",
                14, 58);
            nightHint.ForeColor = Color.FromArgb(110, 110, 110);
            nightGroup.Controls.Add(nightHint);

            // --- признаки сна ------------------------------------------------
            GroupBox sleepGroup = MakeGroup("Когда считать, что человек уснул", 12, 104, 484, 116);

            Label idleLabel = MakeLabel("Считать заснувшей после, мин:", 14, 30);
            sleepGroup.Controls.Add(idleLabel);

            idleBox = MakeNumberBox(300, 26, 80, 1, 1440);
            sleepGroup.Controls.Add(idleBox);

            Label delayLabel = MakeLabel("Уходить в сон/гибернацию через, мин:", 14, 60);
            sleepGroup.Controls.Add(delayLabel);

            delayBox = MakeNumberBox(300, 56, 80, 1, 1440);
            sleepGroup.Controls.Add(delayBox);

            Label intervalLabel = MakeLabel("Проверять каждые, мин:", 14, 90);
            sleepGroup.Controls.Add(intervalLabel);

            intervalBox = MakeNumberBox(300, 86, 80, 1, 120);
            sleepGroup.Controls.Add(intervalBox);

            // --- что делать --------------------------------------------------
            GroupBox actionGroup = MakeGroup("Что делать", 12, 228, 484, 104);

            hibernateRadio = new RadioButton();
            hibernateRadio.Text = "Гибернация (по умолчанию)";
            hibernateRadio.AutoSize = true;
            hibernateRadio.Location = new Point(18, 26);
            actionGroup.Controls.Add(hibernateRadio);

            suspendRadio = new RadioButton();
            suspendRadio.Text = "Сон";
            suspendRadio.AutoSize = true;
            suspendRadio.Location = new Point(18, 52);
            actionGroup.Controls.Add(suspendRadio);

            pauseMediaBox = new CheckBox();
            pauseMediaBox.Text = "Ставить видео на паузу, если идёт звук";
            pauseMediaBox.AutoSize = true;
            pauseMediaBox.Location = new Point(18, 76);
            actionGroup.Controls.Add(pauseMediaBox);

            // --- режим проверки ----------------------------------------------
            GroupBox safeGroup = MakeGroup("Безопасность", 12, 340, 484, 72);

            dryRunBox = new CheckBox();
            dryRunBox.Text = "Только показывать, ничего не делать";
            dryRunBox.AutoSize = true;
            dryRunBox.Location = new Point(18, 24);
            safeGroup.Controls.Add(dryRunBox);

            Label dryHint = new Label();
            dryHint.Text = "Совет: на первую ночь включите — лиса только запишет в журнал, что сделала бы.";
            dryHint.ForeColor = Color.FromArgb(110, 110, 110);
            dryHint.AutoSize = true;
            dryHint.Location = new Point(34, 46);
            safeGroup.Controls.Add(dryHint);

            // --- прочее -------------------------------------------------------
            // Раньше эти четыре группы создавались, но на форму не добавлялись:
            // в окне настроек оставались видны только галочка автозапуска, путь и кнопки.
            Controls.Add(nightGroup);
            Controls.Add(sleepGroup);
            Controls.Add(actionGroup);
            Controls.Add(safeGroup);

            runAtLogonBox = new CheckBox();
            runAtLogonBox.Text = "Запускать при входе в систему";
            runAtLogonBox.AutoSize = true;
            runAtLogonBox.Location = new Point(18, 424);
            Controls.Add(runAtLogonBox);

            Label pathLabel = new Label();
            pathLabel.Text = "Настройки: %APPDATA%\\SleepyFox\\settings.ini";
            pathLabel.ForeColor = Color.FromArgb(110, 110, 110);
            pathLabel.AutoSize = true;
            pathLabel.Location = new Point(18, 470);
            Controls.Add(pathLabel);

            saveButton = new Button();
            saveButton.Text = "Сохранить";
            saveButton.Location = new Point(306, 466);
            saveButton.Size = new Size(90, 30);
            saveButton.Click += SaveButtonClick;
            Controls.Add(saveButton);

            cancelButton = new Button();
            cancelButton.Text = "Отмена";
            cancelButton.Location = new Point(404, 466);
            cancelButton.Size = new Size(90, 30);
            cancelButton.DialogResult = DialogResult.Cancel;
            Controls.Add(cancelButton);

            AcceptButton = saveButton;
            CancelButton = cancelButton;

            ResumeLayout(false);
        }

        private static GroupBox MakeGroup(string title, int x, int y, int width, int height)
        {
            GroupBox group = new GroupBox();
            group.Text = title;
            group.Location = new Point(x, y);
            group.Size = new Size(width, height);
            return group;
        }

        private static Label MakeLabel(string text, int x, int y)
        {
            Label label = new Label();
            label.Text = text;
            label.AutoSize = true;
            label.Location = new Point(x, y);
            return label;
        }

        private static NumericUpDown MakeNumberBox(int x, int y, int width, int min, int max)
        {
            NumericUpDown box = new NumericUpDown();
            box.Minimum = min;
            box.Maximum = max;
            box.Location = new Point(x, y);
            box.Size = new Size(width, 23);
            box.TextAlign = HorizontalAlignment.Right;
            return box;
        }

        private void LoadValues(AppSettings current)
        {
            nightStartBox.Value = ClampToBox(current.NightStartHour, nightStartBox);
            nightEndBox.Value = ClampToBox(current.NightEndHour, nightEndBox);
            idleBox.Value = ClampToBox(current.IdleMinutes, idleBox);
            delayBox.Value = ClampToBox(current.ActionDelayMinutes, delayBox);
            intervalBox.Value = ClampToBox(current.CheckIntervalMinutes, intervalBox);

            hibernateRadio.Checked = current.UseHibernate;
            suspendRadio.Checked = !current.UseHibernate;
            pauseMediaBox.Checked = current.PauseMedia;
            dryRunBox.Checked = current.DryRun;
            runAtLogonBox.Checked = current.RunAtLogon;
        }

        private static decimal ClampToBox(int value, NumericUpDown box)
        {
            if (value < box.Minimum)
            {
                return box.Minimum;
            }
            if (value > box.Maximum)
            {
                return box.Maximum;
            }
            return value;
        }

        private void SaveButtonClick(object sender, EventArgs e)
        {
            int startHour = (int)nightStartBox.Value;
            int endHour = (int)nightEndBox.Value;

            if (startHour == endHour)
            {
                DialogResult answer = MessageBox.Show(
                    this,
                    "«Ночь с» и «до» совпадают, значит ночи нет вообще и лиса ничего делать не будет."
                        + Environment.NewLine + Environment.NewLine + "Всё равно сохранить?",
                    "The Sleepy Fox",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (answer != DialogResult.Yes)
                {
                    return;
                }
            }

            AppSettings updated = new AppSettings();
            updated.NightStartHour = startHour;
            updated.NightEndHour = endHour;
            updated.IdleMinutes = (int)idleBox.Value;
            updated.ActionDelayMinutes = (int)delayBox.Value;
            updated.CheckIntervalMinutes = (int)intervalBox.Value;
            updated.UseHibernate = hibernateRadio.Checked;
            updated.PauseMedia = pauseMediaBox.Checked;
            updated.DryRun = dryRunBox.Checked;
            updated.RunAtLogon = runAtLogonBox.Checked;
            updated.Normalize();

            result = updated;
            DialogResult = DialogResult.OK;
        }
    }
}
