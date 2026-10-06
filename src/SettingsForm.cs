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
        private NumericUpDown nightStartMinuteBox;
        private NumericUpDown nightEndBox;
        private NumericUpDown nightEndMinuteBox;
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
            ClientSize = new Size(600, 656);
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
            GroupBox nightGroup = MakeGroup("Ночное окно", 12, 8, 576, 90);

            Label nightFromLabel = MakeLabel("ночь с:", 14, 26);
            nightGroup.Controls.Add(nightFromLabel);

            nightStartBox = MakeNumberBox(78, 22, 56, 0, 23);
            nightGroup.Controls.Add(nightStartBox);

            Label nightFromColon = MakeLabel(":", 138, 26);
            nightGroup.Controls.Add(nightFromColon);

            nightStartMinuteBox = MakeNumberBox(148, 22, 56, 0, 59);
            nightGroup.Controls.Add(nightStartMinuteBox);

            Label nightToLabel = MakeLabel("до:", 222, 26);
            nightGroup.Controls.Add(nightToLabel);

            nightEndBox = MakeNumberBox(258, 22, 56, 0, 23);
            nightGroup.Controls.Add(nightEndBox);

            Label nightToColon = MakeLabel(":", 318, 26);
            nightGroup.Controls.Add(nightToColon);

            nightEndMinuteBox = MakeNumberBox(328, 22, 56, 0, 59);
            nightGroup.Controls.Add(nightEndMinuteBox);

            Label nightHint = MakeHint(
                "Часы, когда лиса следит за компьютером. Окно может переходить через полночь: например, с 23:00 до 07:00.",
                14, 48, 548, 34);
            nightGroup.Controls.Add(nightHint);

            // --- признаки сна ------------------------------------------------
            GroupBox sleepGroup = MakeGroup("Когда считать, что человек уснул", 12, 102, 576, 218);

            Label idleLabel = MakeLabel("Считать заснувшим после, мин", 14, 26);
            sleepGroup.Controls.Add(idleLabel);

            idleBox = MakeNumberBox(330, 22, 80, 1, 1440);
            sleepGroup.Controls.Add(idleBox);

            Label idleHint = MakeHint(
                "Столько минут подряд не трогали мышь и клавиатуру. Пока трогаешь — лиса только принюхивается.",
                14, 48, 548, 34);
            sleepGroup.Controls.Add(idleHint);

            Label delayLabel = MakeLabel("Уходить в сон или гибернацию через, мин", 14, 86);
            sleepGroup.Controls.Add(delayLabel);

            delayBox = MakeNumberBox(330, 82, 80, 1, 1440);
            sleepGroup.Controls.Add(delayBox);

            Label delayHint = MakeHint(
                "Сколько ждать после того, как лиса поставила видео на паузу. Если за это время тронешь мышь или клавиатуру, лиса передумает и компьютер не тронет.",
                14, 108, 548, 50);
            sleepGroup.Controls.Add(delayHint);

            Label intervalLabel = MakeLabel("Проверять каждые, мин", 14, 164);
            sleepGroup.Controls.Add(intervalLabel);

            intervalBox = MakeNumberBox(330, 160, 80, 1, 120);
            sleepGroup.Controls.Add(intervalBox);

            Label intervalHint = MakeHint(
                "Как часто лиса просыпается и смотрит: ночь ли сейчас, давно ли тебя не было и идёт ли звук.",
                14, 182, 548, 34);
            sleepGroup.Controls.Add(intervalHint);

            // --- что делать --------------------------------------------------
            GroupBox actionGroup = MakeGroup("Что делать", 12, 326, 576, 150);

            hibernateRadio = new RadioButton();
            hibernateRadio.Text = "Гибернация — надёжнее: всё сохраняется на диск, но просыпается медленнее";
            hibernateRadio.AutoSize = true;
            hibernateRadio.Location = new Point(18, 24);
            actionGroup.Controls.Add(hibernateRadio);

            suspendRadio = new RadioButton();
            suspendRadio.Text = "Сон — просыпается быстрее, но батарея расходуется";
            suspendRadio.AutoSize = true;
            suspendRadio.Location = new Point(18, 48);
            actionGroup.Controls.Add(suspendRadio);

            pauseMediaBox = new CheckBox();
            pauseMediaBox.Text = "Ставить видео на паузу, если идёт звук";
            pauseMediaBox.AutoSize = true;
            pauseMediaBox.Location = new Point(18, 72);
            actionGroup.Controls.Add(pauseMediaBox);

            Label actionHint = MakeHint(
                "Лиса сначала слушает: если звук идёт, значит видео играет, и она ставит его на паузу. Если звука нет — она ничего не нажимает, потому что та же клавиша включает видео обратно.",
                14, 96, 548, 50);
            actionGroup.Controls.Add(actionHint);

            // --- режим проверки ----------------------------------------------
            GroupBox safeGroup = MakeGroup("Безопасность", 12, 482, 576, 84);

            dryRunBox = new CheckBox();
            dryRunBox.Text = "Только показывать, ничего не делать (режим проверки)";
            dryRunBox.AutoSize = true;
            dryRunBox.Location = new Point(18, 22);
            safeGroup.Controls.Add(dryRunBox);

            Label dryHint = MakeHint(
                "Совет: на первую ночь включи. Лиса ничего не нажмёт и не усыпит компьютер, а только запишет в журнал, что сделала бы.",
                14, 46, 548, 34);
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
            runAtLogonBox.Location = new Point(18, 572);
            Controls.Add(runAtLogonBox);

            Label pathLabel = new Label();
            pathLabel.Text = "Настройки лежат здесь: %APPDATA%\\SleepyFox\\settings.ini";
            pathLabel.ForeColor = Color.FromArgb(110, 110, 110);
            pathLabel.AutoSize = true;
            pathLabel.Location = new Point(18, 594);
            Controls.Add(pathLabel);

            saveButton = new Button();
            saveButton.Text = "Сохранить";
            saveButton.Location = new Point(402, 616);
            saveButton.Size = new Size(90, 30);
            saveButton.Click += SaveButtonClick;
            Controls.Add(saveButton);

            cancelButton = new Button();
            cancelButton.Text = "Отмена";
            cancelButton.Location = new Point(498, 616);
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

        /// <summary>Серая подсказка: ширина задана, поэтому длинный текст переносится по словам.</summary>
        private static Label MakeHint(string text, int x, int y, int width, int height)
        {
            Label label = new Label();
            label.Text = text;
            label.AutoSize = false;
            label.Size = new Size(width, height);
            label.Location = new Point(x, y);
            label.ForeColor = Color.FromArgb(110, 110, 110);
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
            nightStartMinuteBox.Value = ClampToBox(current.NightStartMinute, nightStartMinuteBox);
            nightEndBox.Value = ClampToBox(current.NightEndHour, nightEndBox);
            nightEndMinuteBox.Value = ClampToBox(current.NightEndMinute, nightEndMinuteBox);
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
            int startMinute = (int)nightStartMinuteBox.Value;
            int endHour = (int)nightEndBox.Value;
            int endMinute = (int)nightEndMinuteBox.Value;

            if (startHour == endHour && startMinute == endMinute)
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
            updated.NightStartMinute = startMinute;
            updated.NightEndHour = endHour;
            updated.NightEndMinute = endMinute;
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
