using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace SleepyFox
{
    /// <summary>
    /// Настройки приложения. Лежат простым текстом в
    /// %APPDATA%\SleepyFox\settings.ini, формат «ключ=значение».
    /// </summary>
    internal sealed class AppSettings
    {
        private int nightStartHour;
        private int nightStartMinute;
        private int nightEndHour;
        private int nightEndMinute;
        private int idleMinutes;
        private int actionDelayMinutes;
        private int checkIntervalMinutes;
        private bool useHibernate;
        private bool pauseMedia;
        private bool dryRun;
        private bool runAtLogon;

        public AppSettings()
        {
            nightStartHour = 23;
            nightStartMinute = 0;
            nightEndHour = 7;
            nightEndMinute = 0;
            idleMinutes = 60;
            actionDelayMinutes = 15;
            checkIntervalMinutes = 5;
            useHibernate = true;
            pauseMedia = true;
            dryRun = false;
            runAtLogon = false;
        }

        /// <summary>Час, с которого начинается ночь (0–23).</summary>
        public int NightStartHour
        {
            get { return nightStartHour; }
            set { nightStartHour = value; }
        }

        /// <summary>Час, в котором ночь заканчивается (0–23).</summary>
        public int NightEndHour
        {
            get { return nightEndHour; }
            set { nightEndHour = value; }
        }

        /// <summary>Минута, с которой начинается ночь (0–59).</summary>
        public int NightStartMinute
        {
            get { return nightStartMinute; }
            set { nightStartMinute = value; }
        }

        /// <summary>Минута, в которой ночь заканчивается (0–59).</summary>
        public int NightEndMinute
        {
            get { return nightEndMinute; }
            set { nightEndMinute = value; }
        }

        /// <summary>Минут без ввода мыши и клавиатуры, после которых считаем, что человек уснул.</summary>
        public int IdleMinutes
        {
            get { return idleMinutes; }
            set { idleMinutes = value; }
        }

        /// <summary>Минут после постановки паузы, через которые уходим в сон или гибернацию.</summary>
        public int ActionDelayMinutes
        {
            get { return actionDelayMinutes; }
            set { actionDelayMinutes = value; }
        }

        /// <summary>Как часто просыпаться и проверять обстановку.</summary>
        public int CheckIntervalMinutes
        {
            get { return checkIntervalMinutes; }
            set { checkIntervalMinutes = value; }
        }

        /// <summary>true — гибернация, false — обычный сон.</summary>
        public bool UseHibernate
        {
            get { return useHibernate; }
            set { useHibernate = value; }
        }

        /// <summary>Ставить ли видео на паузу, если слышно звук.</summary>
        public bool PauseMedia
        {
            get { return pauseMedia; }
            set { pauseMedia = value; }
        }

        /// <summary>Режим проверки: только писать в журнал, ничего не нажимать и не усыплять.</summary>
        public bool DryRun
        {
            get { return dryRun; }
            set { dryRun = value; }
        }

        /// <summary>Прописывать ли себя в автозапуск текущего пользователя.</summary>
        public bool RunAtLogon
        {
            get { return runAtLogon; }
            set { runAtLogon = value; }
        }

        public AppSettings Clone()
        {
            AppSettings copy = new AppSettings();
            copy.nightStartHour = nightStartHour;
            copy.nightStartMinute = nightStartMinute;
            copy.nightEndHour = nightEndHour;
            copy.nightEndMinute = nightEndMinute;
            copy.idleMinutes = idleMinutes;
            copy.actionDelayMinutes = actionDelayMinutes;
            copy.checkIntervalMinutes = checkIntervalMinutes;
            copy.useHibernate = useHibernate;
            copy.pauseMedia = pauseMedia;
            copy.dryRun = dryRun;
            copy.runAtLogon = runAtLogon;
            return copy;
        }

        /// <summary>Приводит значения в разумные рамки. Вызывается и при чтении, и при записи.</summary>
        public void Normalize()
        {
            nightStartHour = Clamp(nightStartHour, 0, 23);
            nightStartMinute = Clamp(nightStartMinute, 0, 59);
            nightEndHour = Clamp(nightEndHour, 0, 23);
            nightEndMinute = Clamp(nightEndMinute, 0, 59);
            idleMinutes = Clamp(idleMinutes, 1, 1440);
            actionDelayMinutes = Clamp(actionDelayMinutes, 1, 1440);
            checkIntervalMinutes = Clamp(checkIntervalMinutes, 1, 120);
        }

        /// <summary>
        /// Ночь ли сейчас. Окно задаётся часами и минутами и может переходить
        /// через полночь: с 23:00 до 07:00. Если «с» и «до» совпадают, ночи нет
        /// вообще — так безопаснее.
        /// </summary>
        public bool IsNight(DateTime moment)
        {
            int nowMinutes = moment.Hour * 60 + moment.Minute;
            int fromMinutes = nightStartHour * 60 + nightStartMinute;
            int toMinutes = nightEndHour * 60 + nightEndMinute;

            if (fromMinutes == toMinutes)
            {
                return false;
            }

            if (fromMinutes < toMinutes)
            {
                return nowMinutes >= fromMinutes && nowMinutes < toMinutes;
            }

            return nowMinutes >= fromMinutes || nowMinutes < toMinutes;
        }

        public static AppSettings Load()
        {
            AppSettings settings = new AppSettings();

            try
            {
                if (File.Exists(AppPaths.SettingsFile))
                {
                    Dictionary<string, string> values =
                        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                    string[] lines = File.ReadAllLines(AppPaths.SettingsFile, Encoding.UTF8);
                    for (int i = 0; i < lines.Length; i++)
                    {
                        string line = lines[i].Trim();
                        if (line.Length == 0)
                        {
                            continue;
                        }
                        if (line.StartsWith("#", StringComparison.Ordinal) ||
                            line.StartsWith(";", StringComparison.Ordinal))
                        {
                            continue;
                        }

                        int separator = line.IndexOf('=');
                        if (separator <= 0)
                        {
                            continue;
                        }

                        string key = line.Substring(0, separator).Trim();
                        string value = line.Substring(separator + 1).Trim();
                        values[key] = value;
                    }

                    settings.nightStartHour = ReadInt(values, "NightStartHour", settings.nightStartHour);
                    settings.nightStartMinute = ReadInt(values, "NightStartMinute", settings.nightStartMinute);
                    settings.nightEndHour = ReadInt(values, "NightEndHour", settings.nightEndHour);
                    settings.nightEndMinute = ReadInt(values, "NightEndMinute", settings.nightEndMinute);
                    settings.idleMinutes = ReadInt(values, "IdleMinutes", settings.idleMinutes);
                    settings.actionDelayMinutes = ReadInt(values, "ActionDelayMinutes", settings.actionDelayMinutes);
                    settings.checkIntervalMinutes = ReadInt(values, "CheckIntervalMinutes", settings.checkIntervalMinutes);
                    settings.useHibernate = ReadBool(values, "UseHibernate", settings.useHibernate);
                    settings.pauseMedia = ReadBool(values, "PauseMedia", settings.pauseMedia);
                    settings.dryRun = ReadBool(values, "DryRun", settings.dryRun);
                    settings.runAtLogon = ReadBool(values, "RunAtLogon", settings.runAtLogon);
                }
            }
            catch (Exception ex)
            {
                Logger.Log("не смогла прочитать настройки (" + ex.Message + ") — беру значения по умолчанию");
            }

            settings.Normalize();
            return settings;
        }

        public void Save()
        {
            Normalize();

            try
            {
                Directory.CreateDirectory(AppPaths.DataDirectory);

                List<string> lines = new List<string>();
                lines.Add("# The Sleepy Fox: настройки. Приложение перезапишет файл при сохранении.");
                lines.Add("NightStartHour=" + nightStartHour.ToString(CultureInfo.InvariantCulture));
                lines.Add("NightStartMinute=" + nightStartMinute.ToString(CultureInfo.InvariantCulture));
                lines.Add("NightEndHour=" + nightEndHour.ToString(CultureInfo.InvariantCulture));
                lines.Add("NightEndMinute=" + nightEndMinute.ToString(CultureInfo.InvariantCulture));
                lines.Add("IdleMinutes=" + idleMinutes.ToString(CultureInfo.InvariantCulture));
                lines.Add("ActionDelayMinutes=" + actionDelayMinutes.ToString(CultureInfo.InvariantCulture));
                lines.Add("CheckIntervalMinutes=" + checkIntervalMinutes.ToString(CultureInfo.InvariantCulture));
                lines.Add("UseHibernate=" + BoolToText(useHibernate));
                lines.Add("PauseMedia=" + BoolToText(pauseMedia));
                lines.Add("DryRun=" + BoolToText(dryRun));
                lines.Add("RunAtLogon=" + BoolToText(runAtLogon));

                File.WriteAllLines(AppPaths.SettingsFile, lines.ToArray(), new UTF8Encoding(true));
            }
            catch (Exception ex)
            {
                Logger.Log("не смогла сохранить настройки: " + ex.Message);
            }
        }

        private static int Clamp(int value, int min, int max)
        {
            if (value < min)
            {
                return min;
            }
            if (value > max)
            {
                return max;
            }
            return value;
        }

        private static string BoolToText(bool value)
        {
            return value ? "true" : "false";
        }

        private static int ReadInt(Dictionary<string, string> values, string key, int fallback)
        {
            string raw;
            if (!values.TryGetValue(key, out raw))
            {
                return fallback;
            }

            int parsed;
            if (int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed))
            {
                return parsed;
            }

            return fallback;
        }

        private static bool ReadBool(Dictionary<string, string> values, string key, bool fallback)
        {
            string raw;
            if (!values.TryGetValue(key, out raw))
            {
                return fallback;
            }

            if (string.Equals(raw, "true", StringComparison.OrdinalIgnoreCase) || raw == "1")
            {
                return true;
            }
            if (string.Equals(raw, "false", StringComparison.OrdinalIgnoreCase) || raw == "0")
            {
                return false;
            }

            return fallback;
        }
    }
}
