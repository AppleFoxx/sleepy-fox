using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SleepyFox
{
    /// <summary>
    /// Журнал лисы: %APPDATA%\SleepyFox\sleepyfox.log.
    /// Каждая строка начинается с отметки времени, поэтому в самом тексте
    /// сообщения время писать не нужно.
    /// </summary>
    internal static class Logger
    {
        private const long MaxLogBytes = 1024 * 1024; // 1 МБ, как договорились
        private const int LinesToKeepAfterTrim = 500;

        private static readonly object SyncRoot = new object();
        private static readonly Encoding Utf8NoBom = new UTF8Encoding(false);

        public static void Log(string message)
        {
            lock (SyncRoot)
            {
                try
                {
                    Directory.CreateDirectory(AppPaths.DataDirectory);
                    string line = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "  " + message;
                    File.AppendAllText(AppPaths.LogFile, line + Environment.NewLine, Utf8NoBom);
                    TrimIfTooBig();
                }
                catch (Exception)
                {
                    // журнал ни при каких обстоятельствах не должен ронять приложение
                }
            }
        }

        /// <summary>Последние строки журнала для окна «Журнал…».</summary>
        public static string ReadTail(int maxLines)
        {
            lock (SyncRoot)
            {
                try
                {
                    if (!File.Exists(AppPaths.LogFile))
                    {
                        return "Журнал пока пуст.";
                    }

                    string[] lines = File.ReadAllLines(AppPaths.LogFile, Utf8NoBom);
                    int start = lines.Length > maxLines ? lines.Length - maxLines : 0;

                    StringBuilder builder = new StringBuilder();
                    for (int i = start; i < lines.Length; i++)
                    {
                        builder.AppendLine(lines[i]);
                    }

                    if (builder.Length == 0)
                    {
                        return "Журнал пока пуст.";
                    }

                    return builder.ToString();
                }
                catch (Exception ex)
                {
                    return "Не удалось прочитать журнал: " + ex.Message;
                }
            }
        }

        private static void TrimIfTooBig()
        {
            FileInfo info = new FileInfo(AppPaths.LogFile);
            if (!info.Exists || info.Length <= MaxLogBytes)
            {
                return;
            }

            string[] lines = File.ReadAllLines(AppPaths.LogFile, Utf8NoBom);
            int start = lines.Length > LinesToKeepAfterTrim ? lines.Length - LinesToKeepAfterTrim : 0;

            List<string> tail = new List<string>();
            tail.Add("... журнал вырос больше мегабайта, старые записи убраны ...");
            for (int i = start; i < lines.Length; i++)
            {
                tail.Add(lines[i]);
            }

            File.WriteAllLines(AppPaths.LogFile, tail.ToArray(), Utf8NoBom);
        }
    }
}
