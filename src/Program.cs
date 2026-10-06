using System;
using System.Threading;
using System.Windows.Forms;

namespace SleepyFox
{
    internal static class Program
    {
        private const string MutexName = "SleepyFox.SingleInstance.v1";

        [STAThread]
        private static void Main()
        {
            bool createdNew;

            // один экземпляр на сеанс: второй только вежливо сообщает, что лиса уже сидит в трее
            using (Mutex mutex = new Mutex(true, MutexName, out createdNew))
            {
                if (!createdNew)
                {
                    MessageBox.Show(
                        "The Sleepy Fox уже запущена. Ищите лису в трее — рядом с часами.",
                        "The Sleepy Fox",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    return;
                }

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                Application.ThreadException += OnThreadException;
                AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;

                SleepyFoxContext context = new SleepyFoxContext();
                Application.Run(context);

                GC.KeepAlive(mutex);
            }
        }

        private static void OnThreadException(object sender, ThreadExceptionEventArgs e)
        {
            Logger.Log("ой, что-то пошло не так: " + e.Exception);
        }

        private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Logger.Log("ой, приложение падает: " + e.ExceptionObject);
        }
    }
}
