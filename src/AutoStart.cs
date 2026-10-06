using System;
using System.Windows.Forms;
using Microsoft.Win32;

namespace SleepyFox
{
    /// <summary>
    /// Автозапуск при входе в систему: значение SleepyFox в ветке Run текущего пользователя.
    /// Прав администратора не требует и никаких ярлыков не создаёт — ни на рабочем столе, ни где-либо ещё.
    /// </summary>
    internal static class AutoStart
    {
        private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string ValueName = "SleepyFox";

        public static bool Apply(bool enabled, out string error)
        {
            error = null;

            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RunKeyPath, true))
                {
                    if (key == null)
                    {
                        error = "не нашла ветку автозапуска в реестре";
                        return false;
                    }

                    if (enabled)
                    {
                        key.SetValue(ValueName, "\"" + Application.ExecutablePath + "\"", RegistryValueKind.String);
                    }
                    else
                    {
                        key.DeleteValue(ValueName, false);
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }
    }
}
