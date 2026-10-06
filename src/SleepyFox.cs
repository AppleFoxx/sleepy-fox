using System;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace SleepyFox
{
    /// <summary>Где лиса хранит свои пожитки.</summary>
    internal static class AppPaths
    {
        public static string DataDirectory
        {
            get
            {
                string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                return Path.Combine(appData, "SleepyFox");
            }
        }

        public static string SettingsFile
        {
            get { return Path.Combine(DataDirectory, "settings.ini"); }
        }

        public static string StateFile
        {
            get { return Path.Combine(DataDirectory, "state.txt"); }
        }

        public static string LogFile
        {
            get { return Path.Combine(DataDirectory, "sleepyfox.log"); }
        }
    }

    /// <summary>
    /// Сама лиса: живёт в трее, по таймеру принюхивается к ночи и к бездействию,
    /// ставит видео на паузу и укладывает компьютер спать.
    /// </summary>
    internal sealed class SleepyFoxContext : ApplicationContext
    {
        public const string AppVersion = "1.0.4";

        private const string StateTimeFormat = "yyyy-MM-dd HH:mm:ss";

        private AppSettings settings;

        private NotifyIcon trayIcon;
        private ContextMenuStrip trayMenu;
        private ToolStripMenuItem pauseItem;
        private Icon trayImage;

        private Timer checkTimer;
        private Timer startupTimer;

        /// <summary>«Пауза до утра» — до следующего запуска не сохраняется, живёт только в памяти.</summary>
        private bool pausedUntilMorning;

        /// <summary>Отметка «человек уснул»: время, когда бездействие перешагнуло порог.</summary>
        private bool markSet;
        private DateTime markTime;

        /// <summary>Чтобы не писать в журнал одно и то же каждую проверку.</summary>
        private bool actionReported;

        /// <summary>Защита от повторного входа, пока система уходит в сон.</summary>
        private bool suspendInProgress;

        /// <summary>
        /// Своё нажатие медиаклавиши: Windows считает его вводом пользователя и обнуляет
        /// счётчик бездействия. Храним сырое время и момент нажатия, чтобы досчитать бездействие.
        /// </summary>
        private uint ownPressTick;
        private DateTime ownPressAt;
        private bool ownPressKnown;

        public SleepyFoxContext()
        {
            settings = AppSettings.Load();

            Logger.Log("бодрствую: The Sleepy Fox " + AppVersion + " села в трей, проверка каждые "
                + settings.CheckIntervalMinutes + " мин"
                + (settings.DryRun ? ", режим проверки" : ""));

            ApplyAutoStart(true);

            BuildTrayIcon();
            LoadState();

            // однократная проверка через 30 секунд после старта
            startupTimer = new Timer();
            startupTimer.Interval = 30000;
            startupTimer.Tick += StartupTimerTick;
            startupTimer.Start();

            checkTimer = new Timer();
            ApplyCheckInterval();
            checkTimer.Tick += CheckTimerTick;
            checkTimer.Start();
        }

        // ---------------------------------------------------------------- меню трея

        private void BuildTrayIcon()
        {
            trayMenu = new ContextMenuStrip();

            ToolStripMenuItem settingsItem = new ToolStripMenuItem("Настройки...");
            settingsItem.Click += SettingsItemClick;

            ToolStripMenuItem checkItem = new ToolStripMenuItem("Проверить сейчас (о чём лиса думает)");
            checkItem.Click += CheckNowItemClick;

            pauseItem = new ToolStripMenuItem("Не усыплять сегодня");
            pauseItem.Click += PauseItemClick;

            ToolStripMenuItem logItem = new ToolStripMenuItem("Журнал...");
            logItem.Click += LogItemClick;

            ToolStripMenuItem updateItem = new ToolStripMenuItem("Проверить обновления");
            updateItem.Click += UpdateItemClick;

            ToolStripMenuItem aboutItem = new ToolStripMenuItem("О программе");
            aboutItem.Click += AboutItemClick;

            ToolStripMenuItem exitItem = new ToolStripMenuItem("Выход");
            exitItem.Click += ExitItemClick;

            trayMenu.Items.Add(settingsItem);
            trayMenu.Items.Add(checkItem);
            trayMenu.Items.Add(pauseItem);
            trayMenu.Items.Add(logItem);
            trayMenu.Items.Add(new ToolStripSeparator());
            trayMenu.Items.Add(updateItem);
            trayMenu.Items.Add(aboutItem);
            trayMenu.Items.Add(exitItem);
            trayMenu.Opening += TrayMenuOpening;

            trayImage = FoxIcon.Create();

            trayIcon = new NotifyIcon();
            trayIcon.Icon = trayImage;
            trayIcon.Text = "The Sleepy Fox — сонная лиса";
            trayIcon.ContextMenuStrip = trayMenu;
            trayIcon.Visible = true;
            trayIcon.DoubleClick += TrayIconDoubleClick;
        }

        private void TrayMenuOpening(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (pausedUntilMorning)
            {
                pauseItem.Text = "Возобновить слежку";
                pauseItem.Checked = true;
            }
            else
            {
                pauseItem.Text = "Не усыплять сегодня";
                pauseItem.Checked = false;
            }
        }

        private void SettingsItemClick(object sender, EventArgs e)
        {
            ShowSettings();
        }

        private void TrayIconDoubleClick(object sender, EventArgs e)
        {
            ShowSettings();
        }

        private void CheckNowItemClick(object sender, EventArgs e)
        {
            RunCheck(true);
        }

        private void PauseItemClick(object sender, EventArgs e)
        {
            pausedUntilMorning = !pausedUntilMorning;

            if (pausedUntilMorning)
            {
                Logger.Log("пауза до утра: свернулась клубком и не трогаю компьютер");
                Balloon("Пауза до утра", "Лиса не трогает компьютер до утра.");
            }
            else
            {
                Logger.Log("возобновляю работу: пауза до утра снята");
                Balloon("The Sleepy Fox", "Лиса снова следит за компьютером.");
            }
        }

        private void LogItemClick(object sender, EventArgs e)
        {
            using (LogForm form = new LogForm())
            {
                form.ShowDialog();
            }
        }

        /// <summary>
        /// Единственное действие, которое ходит в сеть, — и только по нажатию человека.
        /// </summary>
        private void UpdateItemClick(object sender, EventArgs e)
        {
            UpdateCheck.CheckForUpdates();
        }

        private void AboutItemClick(object sender, EventArgs e)
        {
            string text =
                "The Sleepy Fox — сонная лиса, версия " + AppVersion + Environment.NewLine +
                Environment.NewLine +
                "Ночью, если вы давно не трогали мышь и клавиатуру, лиса поставит видео " +
                "на паузу и через заданное время уложит компьютер спать." + Environment.NewLine +
                Environment.NewLine +
                "Настройки: " + AppPaths.SettingsFile + Environment.NewLine +
                "Журнал: " + AppPaths.LogFile + Environment.NewLine +
                Environment.NewLine +
                "Ярлыки на рабочем столе лиса не создаёт никогда.";

            MessageBox.Show(text, "The Sleepy Fox " + AppVersion,
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void ExitItemClick(object sender, EventArgs e)
        {
            Logger.Log("выхожу: лиса уходит спать в нору");
            Shutdown();
            ExitThread();
        }

        private void ShowSettings()
        {
            using (SettingsForm form = new SettingsForm(settings))
            {
                if (form.ShowDialog() != DialogResult.OK)
                {
                    return;
                }

                settings = form.Result;
                settings.Save();
                ApplyCheckInterval();
                ApplyAutoStart(false);

                Logger.Log("настройки сохранены: " + NightWindowText()
                    + ", порог " + settings.IdleMinutes + " мин, действие через "
                    + settings.ActionDelayMinutes + " мин, "
                    + (settings.UseHibernate ? "гибернация" : "сон")
                    + (settings.PauseMedia ? ", пауза видео" : ", без паузы видео")
                    + (settings.DryRun ? ", режим проверки" : ""));

                Balloon("The Sleepy Fox", "Настройки сохранены.");
            }
        }

        private void ApplyCheckInterval()
        {
            int minutes = settings.CheckIntervalMinutes;
            if (minutes < 1)
            {
                minutes = 1;
            }

            checkTimer.Interval = minutes * 60000;
        }

        private void ApplyAutoStart(bool quiet)
        {
            string error;
            if (!AutoStart.Apply(settings.RunAtLogon, out error))
            {
                Logger.Log("автозапуск не получился: " + error);
                if (!quiet)
                {
                    Balloon("The Sleepy Fox", "Не получилось изменить автозапуск: " + error);
                }
            }
        }

        // ---------------------------------------------------------------- проверки

        private void CheckTimerTick(object sender, EventArgs e)
        {
            RunCheck(false);
        }

        private void StartupTimerTick(object sender, EventArgs e)
        {
            startupTimer.Stop();
            RunCheck(false);
        }

        private void RunCheck(bool manual)
        {
            if (suspendInProgress)
            {
                return;
            }

            DateTime now = DateTime.Now;
            int nowMinutes = now.Hour * 60 + now.Minute;
            int fromMinutes = settings.NightStartHour * 60 + settings.NightStartMinute;
            int toMinutes = settings.NightEndHour * 60 + settings.NightEndMinute;
            bool night = fromMinutes != toMinutes && (fromMinutes < toMinutes
                ? (nowMinutes >= fromMinutes && nowMinutes < toMinutes)
                : (nowMinutes >= fromMinutes || nowMinutes < toMinutes));
            double idleMinutes = CurrentIdleMinutes();

            // «Пауза до утра» сама снимается, когда ночь закончилась
            if (pausedUntilMorning && !night)
            {
                pausedUntilMorning = false;
                Logger.Log("утро наступило — снимаю паузу до утра");
            }

            if (pausedUntilMorning)
            {
                if (manual)
                {
                    Balloon("Пауза до утра", "Лиса спит вполглаза и до утра ничего не делает.");
                }
                return;
            }

            string context = "[" + NightWindowText() + ", ночь=" + (night ? "True" : "False")
                + ", без ввода " + FormatMinutes(idleMinutes) + " мин]";

            if (!night)
            {
                if (markSet)
                {
                    ResetMark(context + " рассвело — сбрасываю отметку сна");
                }

                if (manual)
                {
                    Balloon("Проверка", "Сейчас не ночь — лиса спит вполглаза.");
                }
                return;
            }

            if (idleMinutes < settings.IdleMinutes)
            {
                if (markSet)
                {
                    ResetMark(context + " просыпаюсь: пользователь вернулся — сбрасываю отметку");
                }

                Logger.Log(context + " принюхиваюсь: рано, человек ещё не спит");

                if (manual)
                {
                    Balloon("Проверка", "Ночь, но вы ещё двигаетесь: " + FormatMinutes(idleMinutes)
                        + " мин без ввода, порог " + settings.IdleMinutes + " мин.");
                }
                return;
            }

            // Ночь и полная тишина — человек, похоже, уснул.
            if (!markSet)
            {
                markSet = true;
                markTime = now;
                actionReported = false;
                SaveMark(markTime);

                Logger.Log(context + " похоже, все уснули — ставлю отметку");
                TryPauseVideo(context);

                if (manual)
                {
                    Balloon("Проверка", "Похоже, вы уснули. Лиса отметила время и следит дальше.");
                }
                return;
            }

            double sinceMark = (now - markTime).TotalMinutes;
            if (sinceMark < settings.ActionDelayMinutes)
            {
                double left = settings.ActionDelayMinutes - sinceMark;
                Logger.Log(context + " жду ещё " + FormatMinutes(left) + " мин до "
                    + (settings.UseHibernate ? "гибернации" : "сна"));

                if (manual)
                {
                    Balloon("Проверка", "Отметка стоит, до сна осталось " + FormatMinutes(left) + " мин.");
                }
                return;
            }

            DoSleepAction(context, manual);
        }

        /// <summary>
        /// Минуты бездействия с поправкой на собственное нажатие медиаклавиши.
        /// Пока последним вводом в системе остаётся наше нажатие, время бездействия
        /// продолжает расти так, как будто мы ничего не нажимали. Как только человек
        /// по-настоящему тронет мышь или клавиатуру, dwTime изменится и поправка отключится.
        /// </summary>
        private double CurrentIdleMinutes()
        {
            double idleMinutes = NativeMethods.GetIdleMinutes();

            if (ownPressKnown && NativeMethods.GetLastInputTick() == ownPressTick)
            {
                double extra = (DateTime.Now - ownPressAt).TotalMinutes;
                if (extra > 0.0 && extra < 720.0)
                {
                    idleMinutes += extra;
                }
            }

            return idleMinutes;
        }

        /// <summary>
        /// Пик звука решает всё: клавиша play/pause — переключатель, и слепое нажатие
        /// может не поставить видео на паузу, а наоборот запустить его.
        /// </summary>
        private void TryPauseVideo(string context)
        {
            if (!settings.PauseMedia)
            {
                Logger.Log(context + " пауза видео выключена в настройках");
                return;
            }

            if (settings.DryRun)
            {
                string dryError;
                float dryPeak = AudioPeakMeter.TryGetPeakValue(out dryError);

                if (dryPeak < 0f)
                {
                    Logger.Log(context + " режим проверки: звук не измерился (" + dryError
                        + "), паузу я бы не жала");
                }
                else if (dryPeak <= AudioPeakMeter.SilenceThreshold)
                {
                    Logger.Log(context + " режим проверки: звука нет (пик "
                        + FormatPeak(dryPeak) + ") — пауза не нужна");
                }
                else
                {
                    Logger.Log(context + " режим проверки: звук идёт (пик " + FormatPeak(dryPeak)
                        + "), я бы поставила видео на паузу");
                }

                Balloon("Режим проверки", "Лиса ничего не нажимает: включена галочка «только показывать».");
                return;
            }

            string error;
            float peak = AudioPeakMeter.TryGetPeakValue(out error);

            if (peak < 0f)
            {
                Logger.Log(context + " не смогла измерить звук (" + error
                    + ") — паузу не жму, чтобы не запустить видео случайно");
                return;
            }

            if (peak <= AudioPeakMeter.SilenceThreshold)
            {
                Logger.Log(context + " звука нет (пик " + FormatPeak(peak) + ") — пауза не нужна");
                return;
            }

            Logger.Log(context + " звук идёт (пик " + FormatPeak(peak) + "), ставлю паузу");

            uint beforeTick = NativeMethods.GetLastInputTick();
            NativeMethods.SendPlayPauseKey();
            System.Threading.Thread.Sleep(150);
            uint afterTick = NativeMethods.GetLastInputTick();

            if (afterTick != beforeTick)
            {
                ownPressTick = afterTick;
                ownPressAt = DateTime.Now;

                if (!ownPressKnown)
                {
                    ownPressKnown = true;
                    Logger.Log("запоминаю своё нажатие: оно не должно считаться возвращением человека");
                }
            }

            Balloon("The Sleepy Fox", "Похоже, вы уснули: поставила видео на паузу.");
        }

        private void DoSleepAction(string context, bool manual)
        {
            string actionName = settings.UseHibernate ? "гибернацию" : "сон";

            if (settings.DryRun)
            {
                if (!actionReported)
                {
                    actionReported = true;
                    Logger.Log(context + " режим проверки: я бы отправила компьютер в " + actionName);

                    if (!manual)
                    {
                        Balloon("Режим проверки", "Я бы отправила компьютер в " + actionName
                            + ", но включена галочка «только показывать».");
                    }
                }
                return;
            }

            Logger.Log(context + " укладываю компьютер спать (" + actionName + ")");

            suspendInProgress = true;
            bool ok;
            try
            {
                PowerState state = settings.UseHibernate ? PowerState.Hibernate : PowerState.Suspend;
                ok = Application.SetSuspendState(state, false, false);
            }
            catch (Exception ex)
            {
                ok = false;
                Logger.Log("ой: " + ex.Message);
            }
            finally
            {
                suspendInProgress = false;
            }

            // отсчёт начинаем заново: и после удачного сна, и чтобы не долбить систему каждую проверку
            markTime = DateTime.Now;
            SaveMark(markTime);

            if (ok)
            {
                Logger.Log("просыпаюсь: компьютер вернулся, продолжаю следить");
            }
            else
            {
                Logger.Log("не получилось уложить компьютер в " + actionName
                    + " — возможно, она отключена в системе; попробую на следующей проверке");
            }
        }

        // ---------------------------------------------------------------- отметка в файле

        private void LoadState()
        {
            try
            {
                if (!File.Exists(AppPaths.StateFile))
                {
                    return;
                }

                string raw = File.ReadAllText(AppPaths.StateFile).Trim();
                DateTime parsed;
                if (!DateTime.TryParseExact(raw, StateTimeFormat, CultureInfo.InvariantCulture,
                        DateTimeStyles.None, out parsed))
                {
                    Logger.Log("в state.txt что-то непонятное — начинаю с чистого листа");
                    return;
                }

                TimeSpan age = DateTime.Now - parsed;
                if (age.TotalHours > 12 || age.TotalMinutes < -5)
                {
                    Logger.Log("отметка сна от " + raw + " уже не в счёт — начинаю заново");
                    ClearMark();
                    return;
                }

                markSet = true;
                markTime = parsed;
                Logger.Log("нашла отметку сна от " + raw + " — продолжаю с неё");
            }
            catch (Exception ex)
            {
                Logger.Log("не смогла прочитать state.txt: " + ex.Message);
            }
        }

        private void SaveMark(DateTime moment)
        {
            try
            {
                Directory.CreateDirectory(AppPaths.DataDirectory);
                File.WriteAllText(AppPaths.StateFile,
                    moment.ToString(StateTimeFormat, CultureInfo.InvariantCulture),
                    new UTF8Encoding(false));
            }
            catch (Exception ex)
            {
                Logger.Log("не смогла записать state.txt: " + ex.Message);
            }
        }

        private void ClearMark()
        {
            try
            {
                if (File.Exists(AppPaths.StateFile))
                {
                    File.Delete(AppPaths.StateFile);
                }
            }
            catch (Exception ex)
            {
                Logger.Log("не смогла удалить state.txt: " + ex.Message);
            }
        }

        private void ResetMark(string message)
        {
            markSet = false;
            markTime = DateTime.MinValue;
            actionReported = false;
            ClearMark();
            Logger.Log(message);
        }

        // ---------------------------------------------------------------- мелочи

        private void Balloon(string title, string text)
        {
            try
            {
                if (trayIcon != null && trayIcon.Visible)
                {
                    trayIcon.BalloonTipTitle = title;
                    trayIcon.BalloonTipText = text;
                    trayIcon.BalloonTipIcon = ToolTipIcon.Info;
                    trayIcon.ShowBalloonTip(5000);
                }
            }
            catch (Exception)
            {
                // подсказка — не повод падать
            }
        }

        private static string FormatMinutes(double minutes)
        {
            if (minutes < 1.0)
            {
                return "меньше 1";
            }

            return ((int)Math.Round(minutes)).ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>Ночное окно словами: «ночь 23:00–07:00».</summary>
        private string NightWindowText()
        {
            return "ночь " + FormatClock(settings.NightStartHour, settings.NightStartMinute)
                + "–" + FormatClock(settings.NightEndHour, settings.NightEndMinute);
        }

        private static string FormatClock(int hour, int minute)
        {
            return hour.ToString("00", CultureInfo.InvariantCulture) + ":"
                + minute.ToString("00", CultureInfo.InvariantCulture);
        }

        private static string FormatPeak(float peak)
        {
            return peak.ToString("0.0000", CultureInfo.InvariantCulture);
        }

        private void Shutdown()
        {
            if (checkTimer != null)
            {
                checkTimer.Stop();
                checkTimer.Dispose();
                checkTimer = null;
            }

            if (startupTimer != null)
            {
                startupTimer.Stop();
                startupTimer.Dispose();
                startupTimer = null;
            }

            if (trayIcon != null)
            {
                trayIcon.Visible = false;
                trayIcon.Dispose();
                trayIcon = null;
            }

            if (trayMenu != null)
            {
                trayMenu.Dispose();
                trayMenu = null;
            }

            if (trayImage != null)
            {
                trayImage.Dispose();
                trayImage = null;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Shutdown();
            }

            base.Dispose(disposing);
        }
    }
}
